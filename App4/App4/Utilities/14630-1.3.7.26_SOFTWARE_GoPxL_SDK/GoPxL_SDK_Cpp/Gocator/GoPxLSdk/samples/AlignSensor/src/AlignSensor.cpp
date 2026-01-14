/*
 * @file            AlignSensor.cpp
 * @brief           Demonstrates sensor alignment operations using the GoPxL SDK
 *
 * @details         This sample connects to a Gocator sensor and performs alignment operations.
 *                  It demonstrates how to:
 *                  - Save a job before alignment to preserve current configuration
 *                  - Clear any existing alignment state
 *                  - Execute alignment and monitor its progress
 *                  - Read alignment state and retrieve transform data
 *                  - Handle extended timeouts for alignment operations
 *
 *                  Prerequisites: Scan data must be available on the sensor for alignment
 *                  to succeed. Alignment operations may take several seconds, especially
 *                  on G3 sensors.
 *
 * GoPxLSdk Sample
 * Copyright (C) 2023-2025 by LMI Technologies Inc.
 *
 * Licensed under the MIT License.
 * Redistributions of files must retain the above copyright notice.
 */

#include <fstream>

#include <kApi/kApiDef.h>
#include <kApi/Threads/kThread.h>
#include <GoApi/GoApiLib.h>
#include <GoPxLSdk/GoSystem.h>
#include <GoPxLSdk/GoChannelError.h>
#include <GoPxLSdk/GoRequestError.h>
#include "../Common/src/SampleUtils.h"

using std::string;
using namespace GoPxLSdk;
using namespace GoPxLSdkSamplesCommon;

// Command Paths
const string ALIGN_COMMAND_PATH             = SCANNER_PATH + "/commands/align";
const string UNALIGN_COMMAND_PATH           = SCANNER_PATH + "/commands/clearAlign";
const string ALIGNMENT_STATE_PATH           = SCANNER_PATH + "/alignment";
const string TRANSFORM_PATH                 = SCANNER_PATH + "/transform";
const string JOB_SAVE_PATH                  = "/jobs/commands/save";

// Alignment Status Codes
constexpr int UNALIGNED_STATUS              = 0;
constexpr int ALIGNED_STATUS                = 1;
constexpr int ALIGNING_STATUS               = 2;

// Command Timeouts
// Alignment delays REST responses, and G3 alignment takes a few seconds, so an increased timeout is needed.
constexpr int REST_COMMAND_TIMEOUT_MSEC_EX  = 30000;

// Network Configuration Addresses
// If sensor is remote-controlled (previously referred to as "accelerated"), 
// use the IP address of the remote controller, typically 127.0.0.1.
constexpr const kChar* SENSOR_IP            = "192.168.1.10";
// The control port is 3600 by default, but could vary for each GoPxL instance.
// To check ports in GoPxL Manager, select an instance, click Modify, and hover over "Base port";
// or check in GoPxL Discovery, where all ports are listed in columns.
constexpr k16u CONTROL_PORT                 = GO_PXL_SDK_DEFAULT_CONTROL_PORT;

int AlignSensor()
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

    if (VerifyConnection(system) == ERROR_STATUS) {
        return ERROR_STATUS;
    }

    // Save job before alignment.
    payload = GoJson(R"({
        "name" : "SDK_alignment_sample"
    })");

    std::cout << "\nSaving job as " << payload.At("name") << "..." << std::endl;
    system.Client().Call(JOB_SAVE_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);

    // Clear existing alignment.
    std::cout << "\nClearing alignment..." << std::endl;
    system.Client().Call(UNALIGN_COMMAND_PATH).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);

    // Alignment state mapping.
    const std::map<int, string> alignmentStates{
        {UNALIGNED_STATUS, "Not Aligned"},
        {ALIGNED_STATUS, "Aligned"},
        {ALIGNING_STATUS, "Aligning"}
    };

    std::cout << "\nExecuting alignment..." << std::endl;
    try
    {
        system.Client().Call(ALIGN_COMMAND_PATH).CheckResponse(REST_COMMAND_TIMEOUT_MSEC_EX);

        // Poll alignment state until complete.
        bool aligning = true;
        int alignmentState = UNALIGNED_STATUS;
        while (aligning)
        {
            kThread_Sleep(1'000'000);
            response = system.Client().Read(ALIGNMENT_STATE_PATH)
                .GetResponse(REST_COMMAND_TIMEOUT_MSEC).Payload();
            alignmentState = response.At("/alignState").Get<int>();
            if (alignmentState != ALIGNING_STATUS)
            {
                aligning = false;
            }
        }
        std::cout << "Alignment state: " << alignmentStates.at(alignmentState) << std::endl;
        response = system.Client().Read(ALIGNMENT_STATE_PATH).GetResponse(REST_COMMAND_TIMEOUT_MSEC).Payload();

        if (alignmentState == UNALIGNED_STATUS)
        {
            std::cout << "\nAlignment failed. Check alignment configuration on sensor." << std::endl;
        }
        response = system.Client().Read(TRANSFORM_PATH).GetResponse(REST_COMMAND_TIMEOUT_MSEC).Payload();
        std::cout << "Alignment transform: " << response.At("/transform") << std::endl;
        std::cout << "Encoder resolution: " << response.At("/encoderResolution") << std::endl;
        std::cout << "Speed: " << response.At("/speed") << std::endl;
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what()
            << " - Alignment failed. Check API path, sensor state and availability of scan data, or try increasing timeout value." << std::endl;
        return ERROR_STATUS;
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
        // Construct GoPxL API core framework before using GoSystem.
        // Required because GoSystem implicitly constructs GoRestClient.
        if ((gpApiLibConstructionStatus = GoApiLib_Construct(&goApiLib)) != kOK)
        {
            std::cout << "Error: " << gpApiLibConstructionStatus
                << " - Failed to construct GoApiLib." << std::endl;
            return ERROR_STATUS;
        }
        status = AlignSensor();
    }
    catch (const GoRequestError& e)
    {
        GoJson errors = e.GetResponse().Payload().At("errors");

        string errorMessage = "";

        if (!errors.Empty())
        {
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