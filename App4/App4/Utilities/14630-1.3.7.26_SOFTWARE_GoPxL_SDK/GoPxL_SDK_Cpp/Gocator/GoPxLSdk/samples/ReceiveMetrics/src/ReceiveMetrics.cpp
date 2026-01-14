/*
 * @file            ReceiveMetrics.cpp
 * @brief           Demonstrates receiving system metrics using streaming callbacks
 *
 * @details         This sample shows how to receive real-time system metrics from sensors.
 *                  It demonstrates how to:
 *                  - Set up streaming callbacks to receive metrics continuously
 *                  - Start metric streams for different system levels
 *                  - Process system-level metrics (CPU, memory, uptime)
 *                  - Process scanner-level metrics (frame count, latency)
 *                  - Process sensor-level metrics (temperature readings)
 *                  - Stop metric streams when done
 *
 *                  The sample uses the streaming API to receive metrics asynchronously via
 *                  callback functions for continuous monitoring of system performance.
 *                  Metrics are organized hierarchically (system, scanner, sensor, tools,
 *                  controls) for granular performance monitoring.
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

using std::string;
using namespace GoPxLSdk;
using namespace GoPxLSdkSamplesCommon;

// Tool and Metrics Paths
const string SYSTEM_METRICS_PATH            = "/system/metrics";
const string SCANNER_METRICS_PATH           = "/scan/engines/" + ENGINE_ID + "/scanners/scanner-0/metrics";
const string SENSOR_METRICS_PATH            = "/scan/engines/" + ENGINE_ID + "/scanners/scanner-0/sensors/sensor-0/metrics";
// const string TOOL_ID                 = "ProfileBoundingBox-0";
// const string TOOLS_METRICS_PATH     = "/tools/" + TOOL_ID + "/metrics";
// const string CONTROLS_METRICS_PATH  = "/controls/gocator/metrics";

// Network Configuration Addresses
// If sensor is remote-controlled (previously referred to as ""accelerated""), 
// use the IP address of the remote controller, typically 127.0.0.1.
constexpr const kChar* SENSOR_IP            = "192.168.1.10";
// The control port is 3600 by default, but could vary for each GoPxL instance.
// To check ports in GoPxL Manager, select an instance, click Modify, and hover over "Base port";
// or check in GoPxL Discovery, where all ports are listed in columns.
constexpr k16u CONTROL_PORT                 = GO_PXL_SDK_DEFAULT_CONTROL_PORT;

// Callback function for SetStreamHandler().
static void onMetrics(const std::shared_ptr<GoStreamResponse> &notification)
{
    using std::cout, std::endl;
    GoJson content = notification->Payload();
    std::string path = notification->Path();

    // System level metrics.
    if (path == SYSTEM_METRICS_PATH)
    {
        cout << "\nApplication Uptime: " << content.Get<int>("/appUpTime") << endl;
        cout << "CPU Cores Usage Average: " << content.Get<int>("/cpuCoresUsedAvg") << endl;
        cout << "Memory Capacity: " << content.Get<double>("/memCapacity") << endl;
        cout << "Memory Used: " << content.Get<double>("/memUsed") << endl;
        cout << "Type: " << notification->Type() << endl;
        cout << "Status: " << notification->Status() << endl;
        cout << "StreamId: " << notification->StreamIdentifier() << endl;
        cout << "StreamStatus: " << notification->StreamStatus() << endl;
        cout << "Path: " << notification->Path() << endl;
    }

    // Scan metrics.
    // This may be sub-divided further into metrics per instance of scan engines, scanners, and scan sensors.
    else if (path == SCANNER_METRICS_PATH)
    {
        cout << "\nCurrent Sync Time: " << content.Get<int>("/currentSyncTime") << endl;
        cout << "Frame Count: " << content.Get<int>("/frameCount") << endl;
        cout << "Processing Latency Average: " << content.Get<int>("/processingLatencyAvg") << endl;
        cout << "Processing Latency Maximum: " << content.Get<int>("/processingLatencyMax") << endl;
    }

    // Metrics for a given sensor.
    // Each sensor type will have its own set of metrics.
    else if (path == SENSOR_METRICS_PATH)
    {
        cout << "\nCamera Temperature 0: " << content.Get<int>("/cameraTemp0") << endl;
        cout << "CPU Temperature: " << content.Get<int>("/cpuTemp") << endl;
        cout << "Laser Driver Temperature: " << content.Get<int>("/laserDriverTemp") << endl;
    }
    
    // Tool execution, measurement and feature metrics.
    // These may be further sub-divided into metrics that are per tool.
    /*else if (path == TOOLS_METRICS_PATH)
    {
        cout << "\nTool Stats Run Time: " << content.Get<int>("/toolStats/runTime") << endl;
    }*/

    // Industrial protocol metrics.
    // These may be further sub-divided into metrics per industrial protocol type.
    // i.e. Gocator, Ascii, EtherIp, Modbus, Profinet, Hardware, etc.
    /*else if (path == CONTROLS_METRICS_PATH)
    {
        cout << "\nEthernet Output Count: " << content.Get<int>("/ethernetOutputCount") << endl;
        cout << "Input Buffer Message Count: " << content.Get<int>("/inputBufferMsgCount") << endl;
    }*/
}

int ReceiveMetrics()
{
    // Connect to sensor.
    GoSystem system;
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

    // Start receiving metrics data.
    std::cout << "\nRunning callback function to receive metrics..." << std::endl;
    std::cout << "Press Enter to close the connection and exit." << std::endl;
    try
    {
        system.Client().SetStreamHandler(onMetrics);
        system.Client().StartStream(SYSTEM_METRICS_PATH);
        system.Client().StartStream(SCANNER_METRICS_PATH);
        system.Client().StartStream(SENSOR_METRICS_PATH);
        // system.Client().StartStream(TOOLS_METRICS_PATH);
        // system.Client().StartStream(CONTROLS_METRICS_PATH);
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what() << " - Failed to receive data." << std::endl;
        return ERROR_STATUS;
    }

    std::ignore = getchar();
    system.Client().StopStream(SYSTEM_METRICS_PATH);
    system.Client().StopStream(SCANNER_METRICS_PATH);
    system.Client().StopStream(SENSOR_METRICS_PATH);
    // system.Client().StopStream(TOOLS_METRICS_PATH);
    // system.Client().StopStream(CONTROLS_METRICS_PATH);
    system.Stop();
    system.Disconnect();
    return OK_STATUS;
}

int main(int argc, char **argv)
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
        status = ReceiveMetrics();
    }
    catch (const GoRequestError &e)
    {
        std::string errorMessage = "";

        const GoJson &payload = e.GetResponse().Payload();
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
    catch (const GoChannelError &e)
    {
        std::cerr << "Error: " << e.what()
                  << " - Check sensor status, ensure it is connected, or try increasing timeout value." << std::endl;
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