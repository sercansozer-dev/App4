/*
 * @file            ReceiveString.cpp
 * @brief           Demonstrates receiving tool string outputs via GoPxL Data Protocol (GDP)
 *
 * @details         This sample shows how to receive string data from measurement tools.
 *                  It demonstrates how to:
 *                  - Enable GoPxL Data Protocol (GDP)
 *                  - Connect to GDP server
 *                  - Receive string data synchronously via GDP
 *                  - Process string outputs from tools
 *                  - Handle NULL messages when strings are invalid
 *
 *                  Prerequisites: Tool string outputs must be enabled and added to GDP
 *                  output. See ConfigureTool.cpp sample for setting up measurement tools
 *                  and enabling their outputs. This sample can work with both live sensor
 *                  data and replay data.
 *
 * GoPxLSdk Sample
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 *
 * Licensed under the MIT License.
 * Redistributions of files must retain the above copyright notice.
 */

#include <kApi/kApiDef.h>
#include <GoApi/GoApiLib.h>
#include <GoPxLSdk/GoSystem.h>
#include <GoPxLSdk/GoChannelError.h>
#include <GoPxLSdk/GoRequestError.h>
#include "../Common/src/SampleUtils.h"

#include <GoPxLSdk/GoGdpClient.h>
#include <GoPxLSdk/GoGdpMsg/GoGdpString.h>
#include <GoPxLSdk/GoGdpMsg/GoGdpNull.h>

using std::string;
using namespace GoPxLSdk;
using namespace GoPxLSdkSamplesCommon;

// Replay and Control Paths
const string REPLAY_PATH                = "/replay/playback";
const std::string GOCATOR_CONTROL_PATH  = "/controls/gocator";

// Command Timeouts
constexpr int RECEIVE_DATA_TIMEOUT_MSEC = 20000;

// Network Configuration Addresses
// If sensor is remote-controlled (previously referred to as ""accelerated""), 
// use the IP address of the remote controller, typically 127.0.0.1.
constexpr const kChar* SENSOR_IP        = "192.168.1.10";
// The control port is 3600 by default, but could vary for each GoPxL instance.
// To check ports in GoPxL Manager, select an instance, click Modify, and hover over "Base port";
// or check in GoPxL Discovery, where all ports are listed in columns.
constexpr k16u CONTROL_PORT             = GO_PXL_SDK_DEFAULT_CONTROL_PORT;

int ReceiveString()
{
    GoSystem system;
    GoJson response;
    GoJson payload;

    // Connect to sensor.
    kIpAddress systemIpAddress;
    kIpAddress_Parse(&systemIpAddress, SENSOR_IP);
    system.SetAddress(systemIpAddress);
    system.SetControlPort(CONTROL_PORT);

    kChar ipAddress[16];
    kIpAddress_Format(system.Address(), ipAddress, sizeof(ipAddress));

    std::cout << "\nConnecting to " << ipAddress << ":" << system.ControlPort() << "..." << std::endl;
    try
    {
        system.Connect();
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what() << " - Connection failed. Check if sensor is powered on, connected, and using correct IP/port."
            << std::endl;
        return ERROR_STATUS;
    }

    // Verify that the device is not remote controlled.
    if (VerifyConnection(system) == ERROR_STATUS)
    {
        return ERROR_STATUS;
    }

    // Check if sensor is using replay or live data
    bool replayDataEnabled;
    try
    {
        response = system.Client().Read(REPLAY_PATH).GetResponse().Payload();
        replayDataEnabled = response.Get<bool>("enabled");
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what() << " - Check API path." << std::endl;
        return ERROR_STATUS;
    }

    if (replayDataEnabled)
    {
        std::cout << "\nUsing replay data" << std::endl;
    }
    else
    {
        std::cout << "\nUsing live data" << std::endl;
    }

    // Enable Gocator Protocol.
    std::cout << "\nEnabling Gocator Protocol..." << std::endl;
    try
    {
        system.Client().Update(GOCATOR_CONTROL_PATH, GoJson("{\"enabled\":true}")).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what() << " - Check timeout value or API path." << std::endl;
        return ERROR_STATUS;
    }

    // Connect to GDP server.
    std::cout << "\nConnecting to Gocator Protocol..." << std::endl;
    auto gdpClient = std::make_unique<GoGdpClient>();
    try
    {
        gdpClient->Connect(system.Address(), system.GdpPort());
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what() << " - Failed to connect to GDP server." << std::endl;
        return ERROR_STATUS;
    }

    // Run the sensor.
    if (system.RunningState() == GoSystem::State::Ready)
    {
        std::cout << "\nStarting system..." << std::endl;
        try
        {
            system.Start();
        }
        catch (const std::exception& e)
        {
            std::cerr << "Error: " << e.what()
                << " - Failed to run sensor. Check if sensor is powered on and connected."
                << std::endl;
            return ERROR_STATUS;
        }
    }

    // Receive data synchronously.
    try
    {
        gdpClient->ReceiveDataSync(RECEIVE_DATA_TIMEOUT_MSEC);
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what()
            << " - Failed to receive data via synchronous connection. "
            << "Ensure string data is available as output or try increasing timeout value."
            << std::endl;
        return ERROR_STATUS;
    }

    const GoDataSet& receivedDataSet = gdpClient->DataSet();
    std::cout << "\nTotal number of messages: " << receivedDataSet.Count() << std::endl;

    // Loop through messages in the data set.
    for (size_t msgIndex = 0; msgIndex < receivedDataSet.Count(); msgIndex++)
    {
        std::cout << std::string(64, '-') << " GDP Output Source " << msgIndex + 1 << std::endl;
        const auto& msg = receivedDataSet.GdpMsgAt(msgIndex);

        switch (msg.Type())
        {
            case GoPxLSdk::MessageType::STRING:
            {
                const auto& stringMsg = static_cast<const GoGdpString&>(msg);
                std::cout << "Message type: String" << std::endl;
                std::cout << "GDP ID: " << stringMsg.GdpId() << std::endl;
                std::cout << "Data source ID: " << stringMsg.DataSourceId() << std::endl;

                if (stringMsg.ArrayedCount() == 0) {
                    std::cout << "Not arrayed" << std::endl;
                }
                else {
                    std::cout << "Arrayed" << std::endl;
                    std::cout << "\tCount: " << stringMsg.ArrayedCount() << std::endl;
                    std::cout << "\tIndex: " << stringMsg.ArrayedIndex() << std::endl;
                }

                std::cout << "\tValue: " << stringMsg.String() << std::endl;
                break;
            }
            case GoPxLSdk::MessageType::NULL_TYPE:
            {
                const auto& nullMsg = static_cast<const GoGdpNull&>(msg);
                std::cout << "Message type: Null" << std::endl;
                std::cout << "GDP ID: " << nullMsg.GdpId() << std::endl;
                std::cout << "Data source ID: " << nullMsg.DataSourceId() << std::endl;
                break;
            }
            default:
                std::cout << "No string found in the message." << std::endl;
                break;
        }
    }

    std::cout << "\nPress Enter to exit the program..." << std::endl;
    std::ignore = getchar();
    gdpClient->Close();
    system.Stop();
    system.Disconnect();

    return OK_STATUS;

}

int main(int argc, char** argv)
{
    int status;
    kStatus gpApiLibConstructionStatus;
    kAssembly goApiLib = kNULL;

    try
    {
        // Constructs GoPxL API core framework.
        // It is important to construct goApiLib before declaration of GoSystem, 
        // this is because GoSystem implicitly calls constructor for GoRestClient. 
        if ((gpApiLibConstructionStatus = GoApiLib_Construct(&goApiLib)) != kOK)
        {
            std::cout << "Error: " << gpApiLibConstructionStatus 
                      << " - Failed to construct GoApiLib." << std::endl;
            return ERROR_STATUS;
        }
        status = ReceiveString();
    }
    catch (const GoRequestError& e)
    {
        std::string errorMessage = "";

        const GoJson& payload = e.GetResponse().Payload();
        if (!payload.Empty() && payload.HasKey("errors"))
        {
            GoJson errors = payload.At("errors");
            errorMessage.append(errors.At("/0/status").ToString());
            errorMessage.append(" ");
            errorMessage.append(errors.At("/0/description").ToString());
        }

        std::cerr << "GoRequestError: " << errorMessage << std::endl;
        std::cerr << "Error sending a REST command to " << e.GetResponse().Path() << std::endl;
        status = ERROR_STATUS;
    }
    catch (const GoChannelError& e)
    {
        std::cerr << "Error: " << e.what()
                  << " - Check sensor status, ensure it is connected, or try increasing timeout value."
                  << std::endl;
        status = ERROR_STATUS;
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what() << std::endl;
        status = ERROR_STATUS;
    }

    kDestroyRef(&goApiLib);
    return status;
}