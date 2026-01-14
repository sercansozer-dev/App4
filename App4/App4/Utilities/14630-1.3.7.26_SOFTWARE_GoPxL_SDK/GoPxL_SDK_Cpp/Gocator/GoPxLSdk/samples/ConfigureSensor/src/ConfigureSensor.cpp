/*
 * @file            ConfigureSensor.cpp
 * @brief           Demonstrates comprehensive sensor configuration using the GoPxL SDK
 *
 * @details         This sample shows how to configure various sensor parameters and settings.
 *                  It demonstrates how to:
 *                  - Configure trigger settings (source, mode, software triggering)
 *                  - Modify exposure settings (single and multiple exposure modes)
 *                  - Update sensor properties (display name, active area)
 *                  - Configure digital output settings and trigger events
 *                  - Send software triggers and scheduled pulses
 *                  - Modify network configuration (IP address settings)
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

const string IP_CONFIG_PATH             = "/environ/ipConfig";

// Trigger Paths
const string TRIGGER_PATH               = SCANNER_PATH + "/actions/trigger";
const string TRIGGER_SOURCE_PATH        = "/parameters/triggerSettings/source";
constexpr int SOFTWARE_TRIGGER_MODE     = 3;

// Sensor Parameter Paths
const string ACTIVE_AREA_PATH           = "/parameters/activeAreaSettings/activeArea";
const string SINGLE_EXPOSURE_PATH       = "/parameters/exposureSettings/singleExposure";
const string MULTI_EXPOSURE_PATH        = "/parameters/exposureSettings/multipleExposures";
constexpr int SINGLE_EXPOSURE_MODE      = 0;
constexpr int MULTI_EXPOSURE_MODE       = 1;

// Control Paths
const string DIGITAL_OUTPUT_PATH        = "/controls/digitalOutput/";
const string DIO_TRIGGER_EVENT_PATH     = "/parameters/triggerEvent";

// Network Configuration Addresses
// If sensor is remote-controlled (previously referred to as ""accelerated""), 
// use the IP address of the remote controller, typically 127.0.0.1.
constexpr const kChar* SENSOR_IP        = "192.168.1.10";
// The control port is 3600 by default, but could vary for each GoPxL instance.
// To check ports in GoPxL Manager, select an instance, click Modify, and hover over "Base port";
// or check in GoPxL Discovery, where all ports are listed in columns.
constexpr k16u CONTROL_PORT             = GO_PXL_SDK_DEFAULT_CONTROL_PORT;

void PrintSectionHeader(const kChar* headerText)
{
    std::cout
        << std::endl
        << "=================================================================================" << std::endl
        << headerText << std::endl
        << "=================================================================================" << std::endl
        << std::endl;
}

int ConfigureSensor()
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

    PrintSectionHeader("Configuration 1: Update trigger source.");

    const std::map<int, string> scannerTriggerSources{
        {0, "Time"},
        {1, "Encoder"},
        {2, "External Input"},
        {3, "Software"}
    };

    // Update trigger source to 3 (Software).
    // Note that this software trigger source is completely unrelated to the software trigger event in digital output.
    payload = GoJson(R"({
        "parameters" : {
            "triggerSettings" : {
                "source" : )" + std::to_string(SOFTWARE_TRIGGER_MODE) + R"(
            }
        }
    })");

    std::cout << "Setting scanner trigger source to \"Software\"..." << std::endl;
    try
    {
        system.Client().Update(SCANNER_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
    }
    catch (const std::exception& e)
    {
        std::cerr << e.what()
            << " - Check if the API path is valid, or try increasing the timeout value."
            << std::endl;
        return ERROR_STATUS;
    }

    // Take a snapshot using the software trigger.
    system.Start();

    std::cout << "Triggering scan using software..." << std::endl;
    try
    {
        system.Client().Call(TRIGGER_PATH).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
    }
    catch(const std::exception& e)
    {
        std::cerr << "Error: " << e.what() << " - Failed to trigger scan. Check if API path is valid or try increasing timeout value."
                  << std::endl;
        return ERROR_STATUS;
    }

    system.Stop();

    PrintSectionHeader("Configuration 2: Update single exposure.");

    // Set exposure mode to single (0) and configure exposure value.
    payload = GoJson(R"({
        "parameters" : {
            "exposureSettings" : {
                "exposureMode" : )" + std::to_string(SINGLE_EXPOSURE_MODE) + R"(,
                "singleExposure" : 1200
            }
        }
    })");

    std::cout << "Setting single exposure to 1200..." << std::endl;
    try
    {
        system.Client().Update(SENSOR_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
        response = system.Client().Read(SENSOR_PATH).GetResponse().Payload();
        auto new_value = response.At(SINGLE_EXPOSURE_PATH).Get<int>();
        std::cout << "Exposure value: " << new_value << std::endl;
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what()
            << " - Check if API path is valid or try increasing timeout value."
            << std::endl;
        return ERROR_STATUS;
    }

    PrintSectionHeader("Configuration 3: Update multiple exposures.");

    // Set exposure mode to multiple (1) and configure exposure values.
    payload = GoJson(R"({
        "parameters" : {
            "exposureSettings" : {
                "exposureMode" : )" + std::to_string(MULTI_EXPOSURE_MODE) + R"(,
                "multipleExposures" : [
                    1080,
                    2010,
                    5040
                ]
            }
        }
    })");

    std::cout << "Setting multiple exposures..." << std::endl;
    system.Client().Update(SENSOR_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);

    response = system.Client().Read(SENSOR_PATH).GetResponse().Payload();
    std::cout << "Multi-exposure values: "
        << response.At(MULTI_EXPOSURE_PATH)
        << std::endl;

    PrintSectionHeader("Configuration 4: Update sensor name.");

    payload = GoJson(R"({
        "displayName": "Main-Sensor-Change"
    })");

    std::cout << "Setting display name..." << std::endl;
    system.Client().Update(SENSOR_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
    response = system.Client().Read(SENSOR_PATH).GetResponse().Payload();
    std::cout << "Display name: " << response.At("/displayName") << std::endl;

    PrintSectionHeader("Configuration 5: Change the active area.");

    // Active area parameters: center position (x,y,z) and dimensions (width, length, height) in mm.
    payload = GoJson(R"({
        "parameters" : {
            "activeAreaSettings" : {
                "activeArea" : {
                    "width" : 3.5
                }
            }
        }
    })");

    std::cout << "Updating active area..." << std::endl;
    system.Client().Update(SENSOR_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);

    response = system.Client().Read(SENSOR_PATH).GetResponse().Payload();
    std::cout << "Active area: "
        << response.At(ACTIVE_AREA_PATH)
        << std::endl;

    PrintSectionHeader("Configuration 6: Enable, trigger, and configure Digital Output.");

    payload = GoJson(R"({
        "enabled" : true
    })");

    std::cout << "Enabling Digital Output..." << std::endl;
    system.Client().Update(DIGITAL_OUTPUT_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);

    // If there is no current device, then add an available device.
    try
    {
        response = system.Client().Read(DIGITAL_OUTPUT_PATH + "devices").GetResponse().Payload();
        if (!response.HasKey("_embedded"))
        {
            std::cout << "\nAdding device to Digital Output..." << std::endl;
            GoJson deviceResponse = system.Client().Read(DIGITAL_OUTPUT_PATH + "availableDevices").GetResponse().Payload();
            GoJson device = deviceResponse.At("devices").Begin().Value();
            system.Client().Create(DIGITAL_OUTPUT_PATH + "devices", device).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
        }
    }
    catch (const std::exception& e)
    {
        std::cout << "Error: " << e.what()
            << " - Check if API path and device are valid."
            << std::endl;
        return ERROR_STATUS;
    }

    // Determine path to update.
    std::string outputDevicePath;
    try
    {
        response = system.Client().Read(DIGITAL_OUTPUT_PATH + "devices").GetResponse().Payload();
        outputDevicePath = response.At("_embedded/item").Begin().Value().At("_links/self/href").ToString();
        outputDevicePath.erase(0, 2);
        outputDevicePath.erase(outputDevicePath.size() - 1);
        outputDevicePath += "/ports/port-0";
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what()
            << " - Check if API path is valid or ensure a device is selected in Digital Output settings."
            << std::endl;
        return ERROR_STATUS;
    }

    const std::map<int, string> digitalOutputTriggerEvents{
        {1, "Measurement"},
        {2, "Software"},
        {3, "Alignment"},
        {4, "Exposure Begin"},
        {5, "Exposure End"},
        {6, "Part Detection"},
        {7, "System State"}
    };

    // Configure digital output to accept software triggers.
    // Trigger events: Measurement (1), Software (2), Alignment (3), Exposure Begin (4),
    // Exposure End (5), Part Detection (6), System State (7).
    // Note: Digital output's software trigger event is independent of scanner's software trigger source.
    payload = GoJson(R"({
        "parameters" : {
            "triggerEvent" : 2
        }
    })");

    std::cout << "Setting digital output trigger event to Software..." << std::endl;
    try
    {
        system.Client().Update(outputDevicePath, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
        response = system.Client().Read(outputDevicePath).GetResponse().Payload();
        auto new_value = digitalOutputTriggerEvents.at(response.At(DIO_TRIGGER_EVENT_PATH).Get<int>());
        std::cout << "Digital output trigger event: " << new_value << std::endl;
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what()
            << " - Check if API path is valid or try increasing timeout value."
            << std::endl;
        return ERROR_STATUS;
    }

    system.Client().Update(outputDevicePath, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);

    // Trigger single non-scheduled pulse.
    // target: time or encoder position (uS or mm), ignored unless scheduled and pulsed.
    // value: true = signal high, false = signal low.
    payload = GoJson(R"({
        "parameter" : {
            "target" : 0,
            "value" : true
        }
    })");

    std::cout << "\nTriggering single non-scheduled pulse..." << std::endl;
    try
    {
        system.Client().Call(outputDevicePath + "/commands/trigger", payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what()
            << " - Failed to trigger digital output pulse. Check if API path is valid or try increasing timeout value."
            << std::endl;
    }

    // Change configurations for a continuous scheduled pulse.
    // signalType: 0 = pulsed waveform, 1 = continuous waveform.
    // scheduled: true = signal set immediately, false = delayed until scheduled time/position.
    payload = GoJson(R"({
        "parameters" : {
            "signalType" : 1,
            "scheduled" : true
        }
    })");

    std::cout << "\nChanging configurations for a continuous scheduled pulse..." << std::endl;
    try
    {
        system.Client().Update(outputDevicePath, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what()
            << " - Failed to update digital output configuration. Check if API path is valid or try increasing timeout value."
            << std::endl;
    }
    
    // Trigger continuous scheduled pulse (value field ignored for pulsed signal type).
    payload = GoJson(R"({
        "parameter" : {
            "target" : 0
        }
    })");

    std::cout << "Triggering continuous scheduled pulse..." << std::endl;
    try
    {
        system.Client().Call(outputDevicePath + "/commands/trigger", payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what()
            << " - Failed to trigger continuous scheduled pulse. Check if API path is valid or try increasing timeout value."
            << std::endl;
    }

    PrintSectionHeader("Configuration 7: Update network configuration.");

    response = system.Client().Read(IP_CONFIG_PATH).GetResponse().Payload();
    std::cout << "Network configurations: " << response.At("/interfaces") << "." << std::endl;

    // Uncomment to update the current IP address to 192.168.1.11.
    // To connect later, update SENSOR_IP to 192.168.1.11.
    /*
    response.Replace<string>("/interfaces/0/ipAddress", "192.168.1.11");
    system.Client().Update(IP_CONFIG_PATH, response).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
    std::cout << "Updated IP address of the 1st interface to " << response.At("/interfaces/0/ipAddress") << "." << std::endl;
    */

    // Disconnect the sensor system.
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

        status = ConfigureSensor();
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
