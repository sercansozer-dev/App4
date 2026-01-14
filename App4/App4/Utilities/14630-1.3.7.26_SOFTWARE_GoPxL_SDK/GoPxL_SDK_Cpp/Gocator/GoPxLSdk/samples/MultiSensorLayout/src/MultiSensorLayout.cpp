/*
 * @file            MultiSensorLayout.cpp
 * @brief           Demonstrates multi-sensor system configuration and layout management
 *
 * @details         This sample shows how to work with multi-sensor systems (sensor groups).
 *                  It demonstrates how to:
 *                  - Connect to the main sensor in a multi-sensor system
 *                  - Create a scanner (sensor group) if one doesn't exist
 *                  - Add sensors to the sensor group using serial numbers
 *                  - Read and display current sensor layout configuration
 *                  - Modify sensor layout (grid position, orientation, multiplexing)
 *                  - Remove sensors from the sensor group
 *
 *                  Prerequisites: Requires multiple LMI Laser Line Profiler sensors on
 *                  the network. Sensors must have compatible firmware versions and not
 *                  be already part of another sensor group.
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

// Scanner and Sensor API Paths
const string SENSOR_LAYOUT_PATH         = SCANNER_PATH + "/layout";

// Serial Numbers
const string SENSOR_1_SERIAL_NO         = "62048";
const string SENSOR_2_SERIAL_NO         = "62984";

// Network Configuration Addresses
// If sensor is remote-controlled (previously referred to as ""accelerated""),
// use the IP address of the remote controller, typically 127.0.0.1.
constexpr const kChar* SENSOR_1_IP      = "192.168.1.10";
constexpr const kChar* SENSOR_2_IP      = "192.168.1.11";
// The control port is 3600 by default, but could vary for each GoPxL instance.
// To check ports in GoPxL Manager, select an instance, click Modify, and hover over "Base port";
// or check in GoPxL Discovery, where all ports are listed in columns.
constexpr k16u CONTROL_PORT             = GO_PXL_SDK_DEFAULT_CONTROL_PORT;

// Parse and format GoRequestError message.
string parseGoRequestError(const GoRequestError& e)
{
    GoJson errors = e.GetResponse().Payload().At("errors");

    string errorMessage = "";

    if (!errors.Empty())
    {
        errorMessage.append("Error: ");
        errorMessage.append(errors.At("/0/status").ToString());
        errorMessage.append(" ");
        errorMessage.append(errors.At("/0/description").ToString());
    }

    return errorMessage;
}

// Display sensor layout information.
void printSensorLayoutInfo(GoJson response)
{
    GoJson sensors = response.At("/grid/sensors");
    std::cout << "\n"
              << string(32, '*') << std::endl;
    for (auto sensor = sensors.Begin(); sensor < sensors.End(); sensor++)
    {
        const GoJson& sensorLayoutInfo = sensor.Value();
        std::cout << "Sensor ID: " << sensorLayoutInfo.At("sensorId") << std::endl;
        std::cout << "Row: " << sensorLayoutInfo.At("row") << ", Column: " << sensorLayoutInfo.At("column") << std::endl;
        std::cout << "Orientation: " << sensorLayoutInfo.At("orientation") << std::endl;
        std::cout << "Multiplexing Bank: " << sensorLayoutInfo.At("multiplexingBank") << "\n"
                  << std::endl;
    }
}

int MultiSensorLayout()
{
    GoSystem system;
    GoJson response;
    GoJson payload;

    // Connect to main sensor.
    kIpAddress systemIpAddress;
    kIpAddress_Parse(&systemIpAddress, SENSOR_1_IP);
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

    // Verify sensor compatibility.
    if (ENGINE_ID != "LMILaserLineProfiler") {
        std::cerr << " - One or more sensors are not compatible with multi-sensor setups. "
                  << "Ensure sensor is an LMILaserLineProfiler model."
                  << std::endl;
        return ERROR_STATUS;
    }

    // Create scanner if not present.
    response = system.Client().Read(ENGINE_PATH).GetResponse(REST_COMMAND_TIMEOUT_MSEC).Payload();
    if (response.At("/_embedded/go:scanner").Empty())
    {
        std::cout << "\nScanner not present, creating scanner" << std::endl;
        system.Client().Create(SCANNERS_PATH);
    }
    else
    {
        std::cout << "\nA Sensor Group exists." << std::endl;
    }

    // Add sensor to the group.
    std::vector<string> sensorSerials = { SENSOR_2_SERIAL_NO };
    
    for (const string& serial : sensorSerials)
    {
        payload = GoJson("{\"serialNumber\":\"" + serial + "\"}");
        std::cout << "\nAdding sensor " << serial << " to the sensor group..." << std::endl;
        try
        {
            system.Client().Create(SENSORS_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
        }
        catch (const std::exception& e)
        {
            std::cerr << "Error: " << e.what() << " - Failed to add sensor " << serial
                      << ". Check API path and serial number, "
                      << "ensure firmware is compatible and sensor is not already added, "
                      << "or try increasing timeout value." << std::endl;
            return ERROR_STATUS;
        }
    }

    // Display current layout.
    response = system.Client().Read(SENSOR_LAYOUT_PATH).GetResponse(REST_COMMAND_TIMEOUT_MSEC).Payload();
    printSensorLayoutInfo(response);

    // Modify sensor layout.
    // Grid
    //      Orientation
    //          0 - Forward
    //          1 - Reverse
    //      Row
    //          0 - Top / Row A
    //          1 - Bottom / Row B
    //      Column  -   Column index
    payload = response;
    payload.Replace("/grid/sensors/1/row", 0);
    payload.Replace("/grid/sensors/1/column", 1);
    payload.Replace("/grid/sensors/1/orientation", 0);
    std::cout << "Updating sensor layout..." << std::endl;
    try
    {
        system.Client().Update(SENSOR_LAYOUT_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what() << " - Failed to update the layout." << std::endl;
        return ERROR_STATUS;
    }

    // Display updated layout.
    response = system.Client().Read(SENSOR_LAYOUT_PATH).GetResponse(REST_COMMAND_TIMEOUT_MSEC).Payload();
    printSensorLayoutInfo(response);

    // Remove sensor from group.
    const string SENSOR_PATH = SENSORS_PATH + "/sensor-1";
    std::cout << "Removing " << SENSOR_PATH << " from sensor group..." << std::endl;
    try
    {
        system.Client().Delete(SENSOR_PATH).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
    }
    catch (const GoRequestError& e)
    {
        std::cerr << "Error: " << e.what()
                  << " - Failed to remove the sensor. Check API path or sensor ID, "
                  << "or try increasing timeout value." << std::endl;
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
            std::cout << "Error: " << gpApiLibConstructionStatus << " - Failed to construct GoApiLib." << std::endl;
            return ERROR_STATUS;
        }
        status = MultiSensorLayout();
    }
    catch (const GoRequestError& e)
    {
        std::cerr << parseGoRequestError(e) << std::endl;
        std::cerr << "GoRequestError - Error sending a REST command to " << e.GetResponse().Path() << std::endl;
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