/*
 * @file            ReceiveProfile.cpp
 * @brief           Demonstrates receiving profile data via GoPxL Data Protocol (GDP)
 *
 * @details         This sample shows how to receive profile data from a Gocator sensor.
 *                  It demonstrates how to:
 *                  - Configure sensor for Profile scan mode
 *                  - Enable intensity data acquisition
 *                  - Enable GoPxL Data Protocol (GDP)
 *                  - Add profile output to GDP output map
 *                  - Connect to GDP server and receive profile data synchronously
 *                  - Process both uniform and point cloud profile formats
 *                  - Transform profile data to real-world coordinates
 *                  - Extract intensity values from profile data
 *
 *                  Profiles are received via GDP and transformed using offset and resolution
 *                  values to obtain X and Z coordinates in millimeters. The sample handles
 *                  both uniform spacing profiles and point cloud profiles.
 *
 * GoPxLSdk Sample
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
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

// Profile Mode Parameters and Paths
const string PROFILE_SOURCE_KEY         = "topUniformProfile";
const string PROFILE_SOURCE_ID          = "\"scan:" + ENGINE_ID + ":scanner-0:" + PROFILE_SOURCE_KEY + (ENGINE_ID == "LMIConfocalLineProfiler" ? "Layer0\"" : "\"");
constexpr int PROFILE_MODE              = 2;

// Replay Paths
const string REPLAY_PATH                = "/replay/playback";

// Control Paths
const string GOCATOR_CONTROL_PATH       = "/controls/gocator";
const string GOCATOR_OUTPUT_PATH        = GOCATOR_CONTROL_PATH + "/outputs";
const string GOCATOR_ADD_OUTPUT_PATH    = GOCATOR_OUTPUT_PATH + "/commands/add";

// Command Timeouts
constexpr int RECEIVE_DATA_TIMEOUT_MSEC = 3'000;

// Network Configuration Addresses
// If sensor is remote-controlled (previously referred to as ""accelerated""), 
// use the IP address of the remote controller, typically 127.0.0.1.
constexpr const kChar* SENSOR_IP        = "192.168.1.10";
// The control port is 3600 by default, but could vary for each GoPxL instance.
// To check ports in GoPxL Manager, select an instance, click Modify, and hover over "Base port";
// or check in GoPxL Discovery, where all ports are listed in columns.
constexpr k16u CONTROL_PORT             = GO_PXL_SDK_DEFAULT_CONTROL_PORT;

// Represents a point in a profile.
typedef struct ProfilePoint
{
    double x; // X coordinate - position along laser line (mm).
    double z; // Z coordinate - height at a given x position (mm).
    unsigned char intensity;
} ProfilePoint;

int ReceiveProfile()
{
    GoSystem system;
    GoJson response;
    GoJson payload;

    std::vector<ProfilePoint> profileBuffer;

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

        // Read current scan mode.
        response = system.Client().Read(SCANNER_PATH).GetResponse().Payload();
        auto scanModeValue = response.At("/parameters/scanModeSettings/scanMode").Get<int>();

        // Set scan mode to Profile (2) if not set already.
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
    }

    // Enable intensity.
    payload = GoJson(R"({
        "parameters" : {
            "scanModeSettings" : {
                "intensityEnabled" : true
            }
        }
    })");

    std::cout << "\nEnabling intensity..." << std::endl;
    try
    {
        system.Client().Update(SCANNER_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what() << " - Check timeout value or API path." << std::endl;
        return ERROR_STATUS;
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

    // Check if profile already added to Gocator Protocol output
    bool profileOutputAdded = false;

    try
    {
        response = system.Client().Read(GOCATOR_OUTPUT_PATH).GetResponse().Payload();
        GoJson map = response.Get<GoJson>("map");

        for (GoJsonIterator i = map.Begin(); i != map.End(); i++)
        {
            GoJson value = i.Value();
            if (value.Get<std::string>("source").find(PROFILE_SOURCE_KEY))
            {
                profileOutputAdded = true;
                break;
            }
        }
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what() << " - Check API path." << std::endl;
        return ERROR_STATUS;
    }

    if (!profileOutputAdded)
    {
        // Add profile to Gocator Protocol output.
        try
        {
            payload = GoJson(R"({
                "source" : )" + PROFILE_SOURCE_ID + R"(,
                "outputId" : 0,
                "autoShift" : true
            })");

            system.Client().Call(GOCATOR_ADD_OUTPUT_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
        }
        catch (const std::exception& e)
        {
            std::cerr << "Error: " << e.what() << " - Failed to add profile to Gocator Protocol output. "
                << "Check API path and ensure uniform spacing is enabled, "
                << "or try increasing timeout value."
                << std::endl;
            return ERROR_STATUS;
        }
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
            << " - Failed to receive data via synchronous connection. Try increasing timeout value."
            << std::endl;
        return ERROR_STATUS;
    }

    const GoDataSet& receivedDataSet = gdpClient->DataSet();

    std::cout << "\nTotal number of messages: " << receivedDataSet.Count() << std::endl;

    // Loop through messages in the data set.
    for (size_t msgIndex = 0; msgIndex < receivedDataSet.Count(); msgIndex++)
    {
        std::cout << "\n" << std::string(64, '-') << std::endl;
        std::cout << "GDP Output Source " << msgIndex + 1 << std::endl;
        const auto& msg = receivedDataSet.GdpMsgAt(msgIndex);

        switch (msg.Type())
        {
            case GoPxLSdk::MessageType::UNIFORM_PROFILE:
            {
                const auto& uniformProfileMsg = static_cast<const GoGdpProfileUniform&>(msg);
                std::cout << "Message type: Uniform Profile" << std::endl;
                std::cout << "GDP ID: " << uniformProfileMsg.GdpId() << std::endl;
                std::cout << "Data Source ID: " << uniformProfileMsg.DataSourceId() << std::endl;

                if (uniformProfileMsg.ArrayedCount() == 0) {
                    std::cout << "Not arrayed" << std::endl;
                }
                else {
                    std::cout << "Arrayed" << std::endl;
                    std::cout << "\tCount: " << uniformProfileMsg.ArrayedCount() << std::endl;
                    std::cout << "\tIndex: " << uniformProfileMsg.ArrayedIndex() << std::endl;

                }

                // Transform to real world coordinates.
                unsigned int validPointCount = 0;
                unsigned int profilePointCount = uniformProfileMsg.Width();
                auto intensityArray = uniformProfileMsg.Intensities();
                unsigned int intensityWidth = uniformProfileMsg.IntensityWidth();
                profileBuffer.resize(profilePointCount);

                /*  The uniform profile data is sent in an array of z values.
                 *  The x real-world coordinates can be calculated from the index of the array,
                 *  using the x offset and resolution.
                 *  The z value must be transformed into real world coordinates before being used.
                 */
                for (unsigned int pointIndex = 0; pointIndex < profilePointCount; pointIndex++)
                {
                    k16s range;
                    kArray1_Item(uniformProfileMsg.Ranges(), pointIndex, &range);

                    profileBuffer[pointIndex].x = uniformProfileMsg.Offset().x + uniformProfileMsg.Resolution().x * pointIndex;

                    if (range != k16S_NULL)
                    {
                        // Calculates the Z position in mm from the given data.
                        // Z = Z offset + Z resolution * z
                        profileBuffer[pointIndex].z = uniformProfileMsg.Offset().z + uniformProfileMsg.Resolution().z * range;
                        validPointCount++;
                    }
                    else
                    {
                        profileBuffer[pointIndex].z = k16S_NULL;
                    }
                }

                // Read intensities.
                if (intensityArray)
                {
                    for (unsigned int intensityIndex = 0; intensityIndex < intensityWidth; intensityIndex++)
                    {
                        kArray1_Item(uniformProfileMsg.Intensities(), intensityIndex, &(profileBuffer[intensityIndex].intensity));
                    }
                }

                std::cout << "Profile points count: " << profilePointCount << std::endl;
                std::cout << "Intensity count: " << intensityWidth << std::endl;
                std::cout << "Valid points count: " << validPointCount << std::endl;

                break;
            }
            case GoPxLSdk::MessageType::PROFILE_POINT_CLOUD:
            {
                const auto& pointCloudProfileMsg = static_cast<const GoGdpProfilePointCloud&>(msg);
                std::cout << "Message type: Point Cloud Profile" << std::endl;
                std::cout << "GDP ID: " << pointCloudProfileMsg.GdpId() << std::endl;
                std::cout << "Data Source ID: " << pointCloudProfileMsg.DataSourceId() << std::endl;

                if (pointCloudProfileMsg.ArrayedCount() == 0) {
                    std::cout << "Not arrayed" << std::endl;
                }
                else {
                    std::cout << "Arrayed" << std::endl;
                    std::cout << "\tCount: " << pointCloudProfileMsg.ArrayedCount() << std::endl;
                    std::cout << "\tIndex: " << pointCloudProfileMsg.ArrayedIndex() << std::endl;
                }

                // Transform to real world coordinates.
                unsigned int validPointCount = 0;
                unsigned int profilePointCount = pointCloudProfileMsg.Width();
                auto intensityArray = pointCloudProfileMsg.Intensities();
                unsigned int intensityWidth = pointCloudProfileMsg.IntensityWidth();
                profileBuffer.resize(profilePointCount);

                /*  The non-uniform profile data is sent in an array of x and z values.
                 *  The x and z values must be transformed into real world coordinates,
                 *  using the x and z offset and resolution values.
                 */

                for (unsigned int pointIndex = 0; pointIndex < profilePointCount; pointIndex++)
                {
                    kPoint16s point;
                    kArray1_Item(pointCloudProfileMsg.Ranges(), pointIndex, &point);

                    if (point.x != k16S_NULL)
                    {
                        // Calculates the X and Z position in mm from the given data.
                        // X = X offset + X resolution * x
                        // Z = Z offset + Z resolution * z
                        profileBuffer[pointIndex].x = pointCloudProfileMsg.Offset().x + pointCloudProfileMsg.Resolution().x * point.x;
                        profileBuffer[pointIndex].z = pointCloudProfileMsg.Offset().z + pointCloudProfileMsg.Resolution().z * point.y;
                        validPointCount++;
                    }
                    else
                    {
                        profileBuffer[pointIndex].x = k16S_NULL;
                        profileBuffer[pointIndex].z = k16S_NULL;
                    }
                }

                // Read intensities.
                if (intensityArray)
                {
                    for (unsigned int intensityIndex = 0; intensityIndex < intensityWidth; intensityIndex++)
                    {
                        kArray1_Item(pointCloudProfileMsg.Intensities(), intensityIndex, &(profileBuffer[intensityIndex].intensity));
                    }
                }

                std::cout << "Profile points count: " << profilePointCount << std::endl;
                std::cout << "Intensity width: " << intensityWidth << std::endl;
                std::cout << "Valid points count: " << validPointCount << std::endl;

                break;
            }

            default:
                std::cout << "\nNo profile found in the message.\n" << std::endl;
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
            std::cout << "Error: " << gpApiLibConstructionStatus << " - Failed to construct GoApiLib." << std::endl;
            return ERROR_STATUS;
        }
        status = ReceiveProfile();
    }
    catch (const GoRequestError& e)
    {
        string errorMessage = "";

        GoJson payload = e.GetResponse().Payload();
        if (!payload.Empty())
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