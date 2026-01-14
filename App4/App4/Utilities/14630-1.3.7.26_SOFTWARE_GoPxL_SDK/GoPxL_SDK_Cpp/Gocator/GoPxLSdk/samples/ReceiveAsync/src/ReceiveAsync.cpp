/*
 * @file            ReceiveAsync.cpp
 * @brief           Demonstrates asynchronous data reception via GoPxL Data Protocol (GDP)
 *
 * @details         This sample shows how to receive sensor data asynchronously using callbacks.
 *                  It demonstrates how to:
 *                  - Enable GoPxL Data Protocol (GDP)
 *                  - Add stamp data as GDP output
 *                  - Connect to GDP server
 *                  - Set up asynchronous data reception with callback function
 *                  - Process received data in the callback
 *                  - Handle timeout conditions for data reception
 *
 *                  Asynchronous reception allows continuous data processing without blocking.
 *                  The callback function is invoked automatically when new data arrives via
 *                  GDP. This sample uses stamp data but the approach works for any GDP data
 *                  type (profiles, surfaces, measurements, etc.).
 *
 * GoPxLSdk Sample
 * Copyright (C) 2023-2025 by LMI Technologies Inc.
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
#include <thread>

#include <GoPxLSdk/GoGdpClient.h>
#include <GoPxLSdk/GoGdpMsg/GoGdpStamp.h>
#include <GoPxLSdk/GoGdpMsg/GoGdpMeasurement.h>

using std::string;
using namespace GoPxLSdk;
using namespace GoPxLSdkSamplesCommon;

// Scanner and Sensor API Paths
const string STAMP_DATA_SOURCE_ID       = "\"scan:" + ENGINE_ID + ":scanner-0:stamp\"";

// Replay and Control Paths
const string REPLAY_PATH                = "/replay/playback";
const string GOCATOR_CONTROL_PATH       = "/controls/gocator";
const string GOCATOR_ADD_OUTPUT_PATH    = "/controls/gocator/outputs/commands/add";

// Command Timeouts
constexpr int RECEIVE_DATA_TIMEOUT_SEC  = 3;

// Network Configuration Addresses
// If sensor is remote-controlled (previously referred to as "accelerated"), 
// use the IP address of the remote controller, typically 127.0.0.1.
constexpr const kChar* SENSOR_IP        = "192.168.1.10";
// The control port is 3600 by default, but could vary for each GoPxL instance.
// To check ports in GoPxL Manager, select an instance, click Modify, and hover over "Base port";
// or check in GoPxL Discovery, where all ports are listed in columns.
constexpr k16u CONTROL_PORT             = GO_PXL_SDK_DEFAULT_CONTROL_PORT;

// Callback function for asynchronous data reception.
void onData(const GoDataSet& receivedDataSet)
{
    // Process received data messages.
    for (size_t msgIndex = 0; msgIndex < receivedDataSet.Count(); ++msgIndex)
    {
        std::cout << std::string(64, '-') << " GDP Output Source " << msgIndex + 1 << std::endl;
        const auto& msg = receivedDataSet.GdpMsgAt(msgIndex);

        switch (msg.Type())
        {
        case  GoPxLSdk::MessageType::STAMP:
        {
            const auto& stampMsg = static_cast<const GoGdpStamp&>(msg);
            std::cout << "Message type: Stamp" << std::endl;
            std::cout << "GDP ID: " << stampMsg.GdpId() << std::endl;
            std::cout << "Data source ID: " << stampMsg.DataSourceId() << std::endl;
            std::cout << "Frame index: " << stampMsg.FrameIndex() << std::endl;
            std::cout << "\tTimestamp: " << stampMsg.Timestamp() << std::endl;
            std::cout << "\tEncoder: " << stampMsg.Encoder() << std::endl;
            std::cout << "System Time Sec: " << stampMsg.SystemTimeSeconds() << std::endl;
            std::cout << "System Time Nanosec: " << stampMsg.SystemTimeNanoseconds() << std::endl;
            break;
        }
        default:
            std::cout << "No stamp found in the message." << std::endl;
            break;

        }
    }
}

int ReceiveAsync()
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

    if (VerifyConnection(system) == ERROR_STATUS)
    {
        return ERROR_STATUS;
    }

    // Determine if replay or live data is enabled.
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
    system.Client().Update(GOCATOR_CONTROL_PATH, GoJson("{\"enabled\":true}")).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);

    // Add stamp data to GDP output.
    payload = GoJson(R"({
        "source" : )" + STAMP_DATA_SOURCE_ID + R"(,
        "outputId" : 0,
        "autoShift" : true
    })");

    try
    {
        system.Client().Call(GOCATOR_ADD_OUTPUT_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what() << " - Failed to add stamp as output." << std::endl;
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

    // Set up asynchronous data reception.
    std::cout << "\nRunning callback function to receive data..." << std::endl;
    std::cout << "Press Enter to close the connection and exit." << std::endl;

    bool dataReceived = false;

    std::function<void(const GoDataSet&)> func = [&](const GoDataSet& dataSet)
        {
            onData(dataSet);
            dataReceived = true;
        };

    // Start sensor.
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
    
    // Receive data asynchronously with timeout.
    try
    {
        gdpClient->ReceiveDataAsync(func);

        std::chrono::seconds timeoutDuration(RECEIVE_DATA_TIMEOUT_SEC);
        std::chrono::steady_clock::time_point start = std::chrono::steady_clock::now();

        while (!dataReceived) {
            std::this_thread::sleep_for(std::chrono::milliseconds(100));
            if (std::chrono::steady_clock::now() - start > timeoutDuration) {
                throw std::runtime_error("Timeout occurred while waiting for data");
            }
        }
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what() << " - Failed to receive data. Ensure data is available as output." << std::endl;
        return ERROR_STATUS;
    }

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
        // Construct GoPxL API core framework before using GoSystem.
        // Required because GoSystem implicitly constructs GoRestClient.
        if ((gpApiLibConstructionStatus = GoApiLib_Construct(&goApiLib)) != kOK)
        {
            std::cout << "Error: " << gpApiLibConstructionStatus
                << " - Failed to construct GoApiLib." << std::endl;
            return ERROR_STATUS;
        }
        status = ReceiveAsync();
    }
    catch (const GoRequestError& e)
    {
        string errorMessage = "";

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

    if (status == ERROR_STATUS)
    {
        std::cout << "\nPress Enter to exit the program..." << std::endl;
        std::ignore = getchar();
    }

    kDestroyRef(&goApiLib);
    return status;
}
