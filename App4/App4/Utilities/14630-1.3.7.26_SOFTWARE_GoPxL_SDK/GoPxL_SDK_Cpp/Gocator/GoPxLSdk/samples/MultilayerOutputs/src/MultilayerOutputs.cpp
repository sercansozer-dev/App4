/*
 * @file            MultilayerOutputs.cpp
 * @brief           Demonstrates receiving single and multi-layer profile outputs via GDP
 *
 * @details         This sample shows how to configure and receive profile data from
 *                  a Confocal Line Profiler sensor with multiple layers.
 *                  It demonstrates how to:
 *                  - Configure sensor for single layer profile output
 *                  - Receive and process arrayed profile data via GoPxL Data Protocol (GDP)
 *                  - Configure sensor for multiple layer array output (up to 8 layers)
 *                  - Separate individual layers and receive them independently
 *                  - Transform profile data to real-world coordinates
 *                  - Handle both uniform and point cloud profile formats
 *
 *                  Prerequisites: Requires an LMI Confocal Line Profiler sensor with
 *                  uniform spacing enabled.
 *
 * GoPxL Sample
 * Copyright (C) 2023-2025 by LMI Technologies Inc.
 *
 * Licensed under the MIT License.
 * Redistributions of files must retain the above copyright notice.
 */
#include <GoApi/GoApiLib.h>
#include <kApi/kApiDef.h>
#include <kApi/Data/kArray1.h>
#include <GoPxLSdk/GoSystem.h>
#include <GoPxLSdk/GoChannelError.h>
#include <GoPxLSdk/GoRequestError.h>
#include "../Common/src/SampleUtils.h"

#include <GoPxLSdk/GoGdpClient.h>
#include <GoPxLSdk/GoGdpMsg/GoGdpStamp.h>
#include <GoPxLSdk/GoGdpMsg/GoGdpProfileUniform.h>
#include <GoPxLSdk/GoGdpMsg/GoGdpProfilePointCloud.h>

using std::string;
using namespace GoPxLSdk;
using namespace GoPxLSdkSamplesCommon;

const string SENSOR_POSITION                 = "top";
const string ARRAY_PROFILE_OUTPUT_ITEM       = "\"scan:" + ENGINE_ID + ":scanner-0:" + SENSOR_POSITION + "UniformProfileArray\"";
constexpr int PROFILE_MODE                   = 2;

// Control Paths
const string GOCATOR_CONTROL_PATH            = "/controls/gocator";
const string GOCATOR_ADD_OUTPUT_PATH         = "/controls/gocator/outputs/commands/add";
const string GOCATOR_REMOVE_ALL_OUTPUTS_PATH = "/controls/gocator/outputs/commands/removeAll";

// Command Timeouts
const int RECEIVE_DATA_TIMEOUT_MSEC          = 20000;

// Network Configuration Addresses
// If sensor is remote-controlled (previously referred to as ""accelerated""), 
// use the IP address of the remote controller, typically 127.0.0.1.
constexpr const kChar* SENSOR_IP             = "192.168.1.10";
// The control port is 3600 by default, but could vary for each GoPxL instance.
// To check ports in GoPxL Manager, select an instance, click Modify, and hover over "Base port";
// or check in GoPxL Discovery, where all ports are listed in columns.
constexpr k16u CONTROL_PORT                  = GO_PXL_SDK_DEFAULT_CONTROL_PORT;

using namespace GoPxLSdk;

// Represents a point in a profile.
typedef struct ProfilePoint
{
    double x;   // X coordinate - position along laser line (mm).
    double z;   // Z coordinate - height at a given x position (mm).
    unsigned char intensity;
} ProfilePoint;

// Receive data from GDP (must be connected).
const GoDataSet& ReceiveData(const std::unique_ptr<GoGdpClient>& gdpClient)
{
    try
    {
        gdpClient->ReceiveDataSync(RECEIVE_DATA_TIMEOUT_MSEC);
        std::cout << "\nSynchronous data transfer established." << std::endl;
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what()
            << " - Failed to receive data via synchronous connection." << std::endl;
    }

    std::cout << "\nTotal number of messages received: "
        << gdpClient->DataSet().Count() << std::endl;

    return gdpClient->DataSet();
}

// Transform profile data to real-world coordinates.
void TransformDataSet(const GoDataSet& receivedDataSet)
{
    for (size_t msgIndex = 0; msgIndex < receivedDataSet.Count(); msgIndex++)
    {
        std::cout << std::string(64, '-') << " GDP Output Source " << msgIndex + 1 << std::endl;
        const auto& msg = receivedDataSet.GdpMsgAt(msgIndex);

        switch (msg.Type())
        {

        case GoPxLSdk::MessageType::STAMP:
        {
            const auto& stampMsg = static_cast<const GoGdpStamp&>(msg);
            std::cout << "Message type: Stamp" << std::endl;
            std::cout << "GDP ID: " << stampMsg.GdpId() << std::endl;
            std::cout << "Data source iD: " << stampMsg.DataSourceId() << std::endl;
            std::cout << "Frame index: " << stampMsg.FrameIndex() << std::endl;
            std::cout << "\tTimestamp: " << stampMsg.Timestamp() << std::endl;
            std::cout << "\tEncoder: " << stampMsg.Encoder() << std::endl;
            break;
        }

        case GoPxLSdk::MessageType::UNIFORM_PROFILE:
        {
            const auto& uniformProfileMsg = static_cast<const GoGdpProfileUniform&>(msg);
            std::cout << "Message type: Uniform Profile" << std::endl;
            std::cout << "GDP ID: " << uniformProfileMsg.GdpId() << std::endl;
            std::cout << "Date Source ID: " << uniformProfileMsg.DataSourceId() << std::endl;

            // Check if data is arrayed.
            if (uniformProfileMsg.ArrayedCount() == 0) {
                std::cout << "Not arrayed" << std::endl;
            }
            else {
                std::cout << "Arrayed" << std::endl;
                std::cout << "\tCount: " << uniformProfileMsg.ArrayedCount() << std::endl;
                std::cout << "\tIndex: " << uniformProfileMsg.ArrayedIndex() << std::endl;

            }

            // Transform uniform profile data to real-world coordinates.
            int validPointCount = 0;
            int profilePointCount = uniformProfileMsg.Width();
            auto profileBuffer = new ProfilePoint[profilePointCount];

            /*  Uniform profile data contains z values only.
            *  X coordinates are calculated from array index using x offset and resolution.
            *  Z values are transformed using z offset and resolution.
            */

            for (int k = 0; k < profilePointCount; k++)
            {
                k16s range;
                kArray1_Item(uniformProfileMsg.Ranges(), k, &range);

                profileBuffer[k].x = uniformProfileMsg.Offset().x + uniformProfileMsg.Resolution().x * k;
                // -32768 indicates invalid data.
                if (range != -32768)
                {
                    profileBuffer[k].z = uniformProfileMsg.Offset().z + uniformProfileMsg.Resolution().z * range;
                    validPointCount++;
                }
                else
                {
                    profileBuffer[k].z = -32768;
                }
            }

            delete[] profileBuffer;
            std::cout << "Profile points count: " << profilePointCount << std::endl;
            std::cout << "Valid points count: " << validPointCount << std::endl;

            break;
        }

        case GoPxLSdk::MessageType::PROFILE_POINT_CLOUD:
        {
            const auto& pointCloudProfileMsg = static_cast<const GoGdpProfilePointCloud&>(msg);
            std::cout << "Message type: Uniform Profile" << std::endl;
            std::cout << "GDP ID: " << pointCloudProfileMsg.GdpId() << std::endl;
            std::cout << "Date Source ID: " << pointCloudProfileMsg.DataSourceId() << std::endl;

            // Check if data is arrayed.
            if (pointCloudProfileMsg.ArrayedCount() == 0) {
                std::cout << "Not arrayed" << std::endl;
            }
            else {
                std::cout << "Arrayed" << std::endl;
                std::cout << "\tCount: " << pointCloudProfileMsg.ArrayedCount() << std::endl;
                std::cout << "\tIndex: " << pointCloudProfileMsg.ArrayedIndex() << std::endl;
            }

            // Transform point cloud profile data to real-world coordinates.
            int validPointCount = 0;
            int profilePointCount = pointCloudProfileMsg.Width();
            auto profileBuffer = new ProfilePoint[profilePointCount];

            /*  Point cloud profile data contains x and z values.
            *  Both coordinates are transformed using respective offset and resolution values.
            */

            for (int k = 0; k < profilePointCount; k++)
            {
                kPoint16s point;
                kArray1_Item(pointCloudProfileMsg.Ranges(), k, &point);

                // -32768 indicates invalid data.
                if (point.x != -32768)
                {
                    profileBuffer[k].x = pointCloudProfileMsg.Offset().x + pointCloudProfileMsg.Resolution().x * point.x;
                    profileBuffer[k].z = pointCloudProfileMsg.Offset().z + pointCloudProfileMsg.Resolution().z * point.y;
                    validPointCount++;
                }
                else
                {
                    profileBuffer[k].x = -32768;
                    profileBuffer[k].z = -32768;
                }
            }

            delete[] profileBuffer;
            std::cout << "Received range points count: " << profilePointCount << std::endl;
            std::cout << "Valid points count: " << validPointCount << std::endl;

            break;
        }
        default:
            std::cout << "\nNo profile or stamp found in the message.\n" << std::endl;
            break;
        }
    }
}

string GetSingleProfileOutputItem(int layer, string ENGINE_ID)
{
    if (layer < 0 || layer > 7)
    {
        return "";
    }

    string layerNumber = std::to_string(layer);
    return "\"scan:" + ENGINE_ID + ":scanner-0:" + SENSOR_POSITION + "UniformProfileLayer" + layerNumber + "\"";
}

int RecordDataProgram()
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

    // Read scanner (sensor group) configuration.
    try
    {
        response = system.Client().Read(SCANNER_PATH).GetResponse().Payload();
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what() << " - Read failed. Check if API path or scan engine is correct and ensure sensor is a LMIConfocalLineProfiler model."
            << std::endl;
        return ERROR_STATUS;
    }
    
    // Verify scan mode is set to Profile.
    auto scanModeValue = response.At(SCAN_MODE_PATH).Get<int>();

    if (scanModeValue != PROFILE_MODE)
    {
        payload = GoJson(R"({
            "parameters" : {
                "scanModeSettings" : {
                    "scanMode" : )" + std::to_string(PROFILE_MODE) + R"(
                }
            }
        })");

        std::cout << "\nSetting scan mode to Profile..." << std::endl;
        try
        {
            system.Client().Update(SCANNER_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
        }
        catch (const std::exception& e)
        {
            std::cerr << "Error: " << e.what()
                << " - Check if API path is valid or try increasing timeout value."
                << std::endl;
            return ERROR_STATUS;
        }
    }

    // Enable uniform spacing.
    payload = GoJson(R"({
        "parameters" : {
            "scanModeSettings" : {
                "uniformSpacingEnabled" : true,
                "individualLayersEnabled" : false
            }
        }
    })");

    std::cout << "\nEnabling uniform spacing..." << std::endl;
    try
    {
        system.Client().Update(SCANNER_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
    }
    catch (const std::exception& e)
    {
        std::cerr << e.what() << " Check the timeout value or the API path." << std::endl;
        return ERROR_STATUS;
    }

    // Enable Gocator Protocol (GDP).
    std::cout << "\nEnabling Gocator Protocol..." << std::endl;
    try
    {
        system.Client().Update(GOCATOR_CONTROL_PATH, GoJson("{\"enabled\":true}")).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
    }
    catch (const std::exception& e)
    {
        std::cerr << e.what() << " Check the timeout value or the API path." << std::endl;
        return ERROR_STATUS;
    }

    // Connect to GDP server.
    auto gdpClient = std::make_unique<GoGdpClient>();
    std::cout << "\nConnecting to Gocator Protocol..." << std::endl;
    try
    {
        gdpClient->Connect(system.Address(), system.GdpPort());
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what() << " - Failed to connect to GDP server." << std::endl;
        return ERROR_STATUS;
    }

    // Configure for single layer output.
    payload = GoJson(R"({
        "parameters" : {
            "layerSettings" : {
                "layerCount" : 1
            }
        }
    })");

    std::cout << "\nConfiguring sensor for single profile layer output..." << std::endl;
    try
    {
        system.Client().Update(SENSOR_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
    }
    catch (const std::exception& e)
    {
        std::cerr << e.what() << " Check the timeout value or the API path." << std::endl;
        return ERROR_STATUS;
    }

    // Add single profile to GDP output.
    std::cout << "Adding single profile to GDP output..." << std::endl;
    try
    {
        payload = GoJson(R"({
            "source" : )" + (std::string)ARRAY_PROFILE_OUTPUT_ITEM + R"(,
            "outputId" : 0,
            "autoShift" : true
        })");

        system.Client().Call(GOCATOR_ADD_OUTPUT_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what() << " - Failed to add profile to GDP output. Check API path, ensure uniform spacing and separate layer outputs are enabled, or try increasing timeout value."
            << std::endl;
        return ERROR_STATUS;
    }

    // Start sensor.
    if (system.RunningState() == GoSystem::State::Ready)
    {
        try
        {
            system.Start();
            std::cout << "\nSystem started." << std::endl;
        }
        catch (const std::exception& e)
        {
            std::cerr << e.what()
                << " - Failed to run sensor. Check if a sensor is powered on and connected."
                << std::endl;
            return ERROR_STATUS;
        }
    }

    // Receive and process data.
    const GoDataSet& receivedDataSet = ReceiveData(gdpClient);
    if (receivedDataSet.Count() < 1)
    {
        std::cout << "No data available." << std::endl;
    }
    else
    {
        TransformDataSet(receivedDataSet);
    }

    system.Stop();
    std::cout << "System stopped." << std::endl;

    // Configure for multi-layer array output.
    payload = GoJson(R"({
        "parameters" : {
            "layerSettings" : {
                "layerCount" : 8
            }
        }
    })");

    std::cout << "\nConfiguring sensor for multiple layer profile array output..." << std::endl;
    try
    {
        system.Client().Update(SENSOR_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what()
            << " - Failed to configure sensor for multiple layer profile array output." << std::endl;
        return ERROR_STATUS;
    }

    // Add profile array to GDP output.
    payload = GoJson(R"({
        "source" : )" + (std::string)ARRAY_PROFILE_OUTPUT_ITEM + R"(,
        "outputId" : 0,
        "autoShift" : true
    })");

    std::cout << "Adding array of profiles to GDP output..." << std::endl;
    try
    {
        system.Client().Call(GOCATOR_ADD_OUTPUT_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what() << " - Failed to add profile array as a data output." << std::endl;
        return ERROR_STATUS;
    }

    // Start sensor.
    if (system.RunningState() == GoSystem::State::Ready)
    {
        try
        {
            system.Start();
            std::cout << "\nSystem started." << std::endl;
        }
        catch (const std::exception& e)
        {
            std::cerr << e.what()
                << " - Failed to run sensor. Check if a sensor is powered on and connected."
                << std::endl;
            return ERROR_STATUS;
        }
    }

    // Receive and process data.
    const GoDataSet& receivedDataSet2 = ReceiveData(gdpClient);
    if (receivedDataSet2.Count() < 1)
    {
        std::cout << "No data available." << std::endl;
    }
    else
    {
        TransformDataSet(receivedDataSet2);
    }

    system.Stop();
    std::cout << "System stopped." << std::endl;

    // Configure for separated layer outputs.
    payload = GoJson(R"({
        "parameters" : {
            "scanModeSettings" : {
                "individualLayersEnabled" : true
            }
        }
    })");

    std::cout << "\nConfiguring sensor for multiple layer separated profile output..." << std::endl;
    try
    {
        system.Client().Update(SCANNER_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what()
            << " - Failed to configure sensor for a multiple layer separated profile output." << std::endl;
        return ERROR_STATUS;
    }

    // Remove all outputs and add individual layers.
    system.Client().Call(GOCATOR_REMOVE_ALL_OUTPUTS_PATH).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
    for (int i = 0; i < 8; i++)
    {
        std::cout << "Adding profile layer " << i << " to GDP output..." << std::endl;
        try
        {
            payload = GoJson(R"({
                "source" : )" + GetSingleProfileOutputItem(i, ENGINE_ID) + R"(,
                "outputId" : 0,
                "autoShift" : true
            })");

            system.Client().Call(GOCATOR_ADD_OUTPUT_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
        }
        catch (const std::exception& e)
        {
            std::cerr << "Error: " << e.what() << " - Failed to add profile layer " << i << " to GDP output." << std::endl;
            return ERROR_STATUS;
        }
    }

    // Start sensor.
    if (system.RunningState() == GoSystem::State::Ready)
    {
        try
        {
            system.Start();
            std::cout << "\nSystem started." << std::endl;
        }
        catch (const std::exception& e)
        {
            std::cerr << e.what()
                << " - Failed to run sensor. Check if a sensor is powered on and connected."
                << std::endl;
            return ERROR_STATUS;
        }
    }

    // Receive and process data.
    const GoDataSet& receivedDataSet3 = ReceiveData(gdpClient);
    if (receivedDataSet3.Count() < 1)
    {
        std::cout << "No data available." << std::endl;
    }
    else
    {
        TransformDataSet(receivedDataSet3);
    }

    // Close GDP connection and disconnect.
    try
    {
        gdpClient->Close();
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what() << " - Failed to close GDP connection." << std::endl;
    }

    system.Stop();
    std::cout << "System stopped." << std::endl;

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
            std::cout << "Error: " << gpApiLibConstructionStatus << " - Failed to construct GoApiLib." << std::endl;
            return ERROR_STATUS;
        }
        status = RecordDataProgram();
    }
    catch (const GoRequestError& e)
    {
        GoJson errors = e.GetResponse().Payload().At("errors");

        string errorMessage = "";

        if (!errors.Empty()) {
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

    std::cout << "\nPress Enter to exit the program..." << std::endl;
    std::ignore = getchar(); 

    return status;
}