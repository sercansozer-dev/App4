/*
 * @file            ReplayData.cpp
 * @brief           Demonstrates recording, downloading, uploading, and replaying scan data
 *
 * @details         This sample shows how to work with recording files (.gprec) and replay mode.
 *                  It demonstrates how to:
 *                  - Enable recording mode on the sensor
 *                  - Configure frame rate for data acquisition
 *                  - Start and stop sensor to capture scan data
 *                  - Download recording file (.gprec) from sensor
 *                  - Upload recording file back to sensor
 *                  - Enable replay mode for playback
 *                  - Control playback (start, stop, seek to specific frame)
 *                  - Read frame count from recording
 *
 *                  Recording files (.gprec) contain scan data that can be played back for
 *                  testing and development. This sample demonstrates the workflow
 *                  of recording, downloading, uploading, and replaying scan data.
 *
 * GoPxLSdk Sample
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 *
 * Licensed under the MIT License.
 * Redistributions of files must retain the above copyright notice.
 */

#include <fstream>
#include <filesystem>

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

// Command Paths
const string COMMAND_PATH               = "/system/commands";
const string START_PATH                 = COMMAND_PATH + "/start";
const string STOP_PATH                  = COMMAND_PATH + "/stop";
const string BACKUP_PATH                = COMMAND_PATH + "/archive";
const string RESTORE_PATH               = COMMAND_PATH + "/restore";

// Replay Paths
const string REPLAY_SEEK_PATH           = "/replay/commands/seek";
const string REPLAY_PLAYBACK_PATH       = "/replay/playback";
const string RECORDING_PATH             = "/replay/recording";
const string RECORDING_FILE_PATH        = "./sample_recording.gprec";

// Command Timeouts
constexpr int RECEIVE_DATA_TIMEOUT_MSEC = 20000;     // Receiving data might require a higher timeout value.

// Network Configuration Addresses
// If sensor is remote-controlled (previously referred to as ""accelerated""), 
// use the IP address of the remote controller, typically 127.0.0.1.
constexpr const kChar* SENSOR_IP        = "192.168.1.10";
// The control port is 3600 by default, but could vary for each GoPxL instance.
// To check ports in GoPxL Manager, select an instance, click Modify, and hover over "Base port";
// or check in GoPxL Discovery, where all ports are listed in columns.
constexpr k16u CONTROL_PORT             = GO_PXL_SDK_DEFAULT_CONTROL_PORT;

int ReplayData()
{
    GoSystem system;
    GoJson response;
    GoJson payload;

    // Connect to a sensor system.
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

    // Enable recording.
    payload = GoJson(R"({
        "enabled" : true
    })");

    std::cout << "\nEnabling recording..." << std::endl;
    system.Client().Update(RECORDING_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);

    // Set frame rate for time trigger source (0=Time, 2=Digital input, 3=Software).
    payload = GoJson(R"({
        "parameters" : {
            "triggerSettings" : {
                "source" : 0,
                "frameRate" : 50
            }
        }
    })");

    std::cout << "\nSetting frame rate to "
        << payload.At("parameters/triggerSettings/frameRate")
        << "..." << std::endl;
    try
    {
        system.Client().Update(SENSOR_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what()
            << " - Check if API path is valid or try increasing timeout value."
            << std::endl;
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

    // Wait to obtain data.
    kThread_Sleep(500'000);

    // Stop the sensor.
    system.Stop();

    // Request live job and replay data
    payload = GoJson(R"({
        "contents" : [
            "liveJob",
            "replay"
        ]
    })");

    // Get data.
    response = system.Client().Call(BACKUP_PATH, payload).GetResponse(RECEIVE_DATA_TIMEOUT_MSEC).Payload();
    ByteArray data = response.At("data").GetBinary();

    // Download.
    std::cout << "\nDownloading .gprec file..." << std::endl;
    try
    {
        char* buffer = (char*)data.data();
        std::ofstream recFile(RECORDING_FILE_PATH, std::ios::out | std::ios::binary);
        if (recFile.is_open())
        {
            recFile.write(buffer, data.size());
            recFile.close();
        }
        std::cout << "Recording file location: " << std::filesystem::absolute(RECORDING_FILE_PATH) << std::endl;
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what() << " - Failed to download the recording to "
            << std::filesystem::absolute(RECORDING_FILE_PATH) << std::endl;
        return ERROR_STATUS;
    }

    // Upload.
    payload = GoJson(R"({
        "contents" : [
            "liveJob",
            "replay"
        ]
    })");

    std::ifstream recFile(RECORDING_FILE_PATH, std::ios::binary);
    std::vector<k8u> recByteArray;
    recByteArray = std::vector<k8u>(std::istreambuf_iterator<char>(recFile), {});
    payload.Set("data", recByteArray);

    std::cout << "\nUploading .gprec file..." << std::endl;
    try
    {
        system.Client().Call(RESTORE_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what()
            << " - Failed to upload the recording. Ensure file path is correct "
            << "or try increasing timeout value. Current file path: "
            << std::filesystem::absolute(RECORDING_FILE_PATH) << std::endl;
        return ERROR_STATUS;
    }

    // Enable replay mode.
    payload = GoJson(R"({
        "enabled" : true,
        "frameRate" : 5
    })");

    std::cout << "\nEnabling replay..." << std::endl;
    try
    {
        system.Client().Update(REPLAY_PLAYBACK_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what()
            << " - Failed to enable recording. Check API path or try increasing timeout value."
            << std::endl;
        return ERROR_STATUS;
    }

    // Read the number of frames in the recording.
    response = system.Client().Read("/replay").GetResponse(REST_COMMAND_TIMEOUT_MSEC).Payload();
    int frameCount = response.Get<int>("frameCount");
    std::cout << "Number of frames in recording: " << frameCount << std::endl;

    // Start playback.
    system.Client().Call(START_PATH).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);

    // Stop playback.
    system.Client().Call(STOP_PATH).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);

    // Check that frame count is not zero
    if (frameCount <= 0)
    {
        std::cout << "Number of frames is zero - Cannot jump to specific frame" << std::endl;
    }
    else
    {
        // Jump to a specific frame.
        payload = GoJson();
        payload.Set("positionDomain", 0);
        payload.Set("position", frameCount - 1); // Position is 0-indexed.
        system.Client().Call(REPLAY_SEEK_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
        std::cout << "Frame changed to " << frameCount << std::endl;
    }

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
        status = ReplayData();
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

    kDestroyRef(&goApiLib);
    std::cout << "\nPress Enter to exit the program..." << std::endl;
    std::ignore = getchar(); 

    return status;
}