/*
 * @file            Discover.cpp
 * @brief           Demonstrates network discovery of sensors and GoPxL instances
 *
 * @details         This sample shows how to discover Gocator sensors and GoPxL PC instances
 *                  on the network using the Discovery Protocol.
 *                  It demonstrates how to:
 *                  - Perform blocking discovery with configurable timeout
 *                  - Retrieve instance information (IP, ports, version, application ID)
 *                  - Identify remote-controlled vs. local instances
 *                  - Connect to discovered instances
 *                  - Enumerate sensors in multi-sensor systems
 *                  - Read sensor properties (serial number, model, engine ID)
 *                  - Check network configuration (IP, gateway, subnet, DHCP)
 *
 *                  Note that remote-controlled (previously called accelerated or buddied) sensors cannot be directly
 *                  connected to.
 *
 * GoPxLSdk Sample
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 *
 * Licensed under the MIT License.
 * Redistributions of files must retain the above copyright notice.
 */

#include <kApi/kApiDef.h>
#include <GoApi/GoApiLib.h>
#include <GoPxLSdk/GoDiscoveryClient.h>
#include <GoPxLSdk/GoSystem.h>
#include <GoPxLSdk/GoChannelError.h>
#include <GoPxLSdk/GoRequestError.h>
#include <GoPxLSdk/Version.h>
#include "../Common/src/SampleUtils.h"

using std::string;
using namespace GoPxLSdk;
using namespace GoPxLSdkSamplesCommon;

// API Paths
const string IP_CONFIG_PATH         = "/environ/ipConfig";

enum class ProtocolStatus : int
{
    RUNNING = 0,
    STOPPED = 1,
    STARTING = 2,
    STOPPING = 3,
    FAILED_TO_START = 4,
    FAILED_TO_STOP = 5
};

// Command Timeouts
constexpr int DISCOVER_TIMEOUT_MSEC = 3000;

const char* GetProtocolStatusString(ProtocolStatus status)
{
    switch (status)
    {
    case ProtocolStatus::RUNNING:          return "RUNNING";
    case ProtocolStatus::STOPPED:          return "STOPPED";
    case ProtocolStatus::STARTING:         return "STARTING";
    case ProtocolStatus::STOPPING:         return "STOPPING";
    case ProtocolStatus::FAILED_TO_START:  return "FAILED_TO_START";
    case ProtocolStatus::FAILED_TO_STOP:   return "FAILED_TO_STOP";
    default:                               return "UNKNOWN";
    }
}

int DiscoverSensors()
{

    // Discover GoPxL instances (sensors or multi-sensor systems).
    auto discovery = std::make_unique<GoDiscoveryClient>();
    discovery->BlockingDiscover(DISCOVER_TIMEOUT_MSEC);
    auto& instances = discovery->InstanceList();
    std::cout << "Number of sensors on the network: " << instances.size() << std::endl;

    if (instances.empty())
    {
        std::cout << "No sensors found, "
                  << "make sure sensors or GoPxl on PC/GoMax are available and connected." 
                  << std::endl;
        return ERROR_STATUS;
    }

    // Display information for each discovered instance.
    int instanceCount = 0;
    for (size_t instanceIndex = 0; instanceIndex < instances.size(); instanceIndex++)
    {
        kChar ipAddress[16], gateway[16], mask[16];
        const GoInstance& instance = discovery->InstanceList().at(instanceIndex);

        kIpAddress_Format(instance.GetIpAddress(), ipAddress, sizeof(ipAddress));
        kIpAddress_Format(instance.GetGateway(), gateway, sizeof(gateway));
        kIpAddress_Format(instance.GetMask(), mask, sizeof(mask));

        std::cout << std::endl;

        // Display instance header.
        std::cout << string(52, '*') << " GoPxL instance " << instanceCount + 1
                  << ": '" << (instance.GetAppName() == "" ? instance.GetAppId() : instance.GetAppName())
                  << "'" << std::endl;

        std::cout << "Is remote: " << (instance.GetIsRemote() ? "Yes" : "No") << std::endl;
        std::cout << "IP Address: " << ipAddress << std::endl;
        std::cout << "Gateway: " << (instance.GetGateway().version == 0 ? "0.0.0.0" : string(gateway))<< std::endl;
        std::cout << "Mask: " << (instance.GetMask().version == 0 ? "0.0.0.0" : mask) << std::endl;
        std::cout << "DHCP: " << (instance.GetIsDhcp() ? "True" : "False") << std::endl;
        std::cout << (instance.GetIsRemote() ? "Remote Controller Application ID: " : "Application ID: ") << instance.GetAppId() << std::endl;
        std::cout << "Application Name: " << instance.GetAppName() << std::endl;
        std::cout << "Application Version: " << instance.GetAppVersion() << std::endl;
        std::cout << "Control Port: " << instance.GetControlPort() << std::endl;
        std::cout << "GDP Port: " << instance.GetGdpPort() << std::endl;
        std::cout << "Web Port: " << instance.GetWebPort() << std::endl;
        std::cout << "IP Conflict: " << (instance.GetIsAddressConflict() ? "Yes" : "No") << std::endl;

        ProtocolStatus status = (ProtocolStatus)instance.GetHMIStatus();
        std::cout << "HMI Status: " << GetProtocolStatusString(status);

        ++instanceCount;

        // Remote sensors report ports as 0 and cannot be directly connected to.
        if (!instance.GetIsRemote())
        {
            // Connect to instance.
            auto system = GoSystem(instance.GetIpAddress(), instance.GetControlPort());

            try
            {
                system.Connect();
            }
            catch (const GoRequestError& e)
            {
                std::cerr << "Error: " << e.what() << " - Connection failed. Check if sensor is powered on, connected, and using correct IP/port."
                    << std::endl;
                return ERROR_STATUS;
            }

            try
            {
                // Enumerate sensors in the system.
                GoJson sensors = system.Client().Read(VISIBLE_SENSORS_PATH).GetResponse().Payload().At("/sensors");
                if (sensors == NULL || sensors.Size() < 1)
                {
                    std::cerr << "\nUnable to read any visible sensors at path: " << VISIBLE_SENSORS_PATH << std::endl;
                    return ERROR_STATUS;
                }

                int sensorCount = 0;
                for (auto sensor = sensors.Begin(); sensor != sensors.End(); sensor.operator++(0))
                {
                    auto sensorPath = system.SensorPath(sensor.Value().Get<string>("serialNumber"));
                    if (!sensorPath.empty() )
                    {
                        auto sensorSerialNumber = sensor.Value().At("/serialNumber").Get<string>();
                        GoJson ipConfig = system.Client().Read(IP_CONFIG_PATH).GetResponse().Payload().At("/interfaces");

                        std::cout << "\n" << string(22, '*') << " Sensor " << sensorCount + 1 << std::endl;
                        std::cout << "Local sensor: " << (sensor.Value().At("/isLocal").Get<bool>() ? "Yes" : "No") << std::endl;
                        std::cout << "IP Address: " << sensor.Value().At("/parameters/ipAddress").Get<string>() << std::endl;
                        std::cout << "Gateway Address: " << ipConfig.At("/0/gatewayAddress").Get<string>() << std::endl;
                        std::cout << "Subnet Mask: " << ipConfig.At("/0/subnetMask").Get<string>() << std::endl;
                        std::cout << "DHCP: " << ipConfig.At("/0/dhcp") << std::endl;
                        std::cout << "Serial Number: " << sensorSerialNumber << std::endl;
                        std::cout << "Model: " << sensor.Value().At("/model").Get<string>() << std::endl;
                        std::cout << "Engine ID: " << sensor.Value().At("/engineId").Get<string>() << std::endl;
                        std::cout << "Sensor Path: " << sensorPath << std::endl;
                    }
                    else
                    {
                        continue;
                    }
                    ++sensorCount;
                }
            }
            catch (const GoRequestError& e)
            {
                std::cout << "Error: " << e.what()
                    << " - Failed to connect. Sensor might be controlled by another instance." << std::endl;
            }
        }
    }

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
        status = DiscoverSensors();
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

    std::cout << "\nPress Enter to exit the program..." << std::endl;
    std::ignore = getchar(); 

    return status;
}
