/**
 * @file    GoSystem.cpp
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#include <GoPxLSdk/Def.h>
#include <GoPxLSdk/GoSystem.h>
#include <GoPxLSdk/GoRequestError.h>

namespace GoPxLSdk
{

const std::string CONTENT_PATH_EMBEDDED_ITEM = "/_embedded/item";
const std::string CONTENT_PATH_EMBEDDED_LINKS = "/_links/self/href";

const std::string SYSTEM_RESOURCE_PATH_ROOT = "/system";
const std::string SYSTEM_RESOURCE_PATH_START_CMD = "/system/commands/start";
const std::string SYSTEM_RESOURCE_PATH_STOP_CMD = "/system/commands/stop";
const std::string SYSTEM_RESOURCE_RUNSTATE_KEY = "runState";
const std::string SYSTEM_RESOURCE_QUICKEDIT_KEY = "quickEditEnabled";

const std::string SCAN_RESOURCE_PATH_ENGINES = "/scan/engines";
const std::string SCAN_RESOURCE_SUBPATH_SCANNERS = "/scanners";
const std::string SCAN_RESOURCE_SUBPATH_SENSORS = "/sensors";
const std::string SCAN_RESOURCE_SERIALNUM_KEY = "serialNumber";

const std::string GDP_RESOURCE_PATH_ROOT = "/controls/gocator";
const std::string GDP_RESOURCE_PORT_KEY = "serverPort";

// This constructor first validates that the arguments are valid before
// updating the member variables.
GoSystem::GoSystem(const kIpAddress& address, k16u port)
{
    // Argument validation is done by the setter functions.
    SetAddress(address);
    SetControlPort(port);
}

void GoSystem::SetAddress(const kIpAddress& ipAddress)
{
    // For now, only IPv4 is supported.
    GoThrowIf((ipAddress.version != kIP_VERSION_4), kERROR_PARAMETER);

    address = ipAddress;
}

kIpAddress GoSystem::Address() const
{
    return address;
}

void GoSystem::SetControlPort(k16u port)
{
    // Port value of 0 is meaningless so reject this value.
    GoThrowIf((port == 0), kERROR_PARAMETER);

    this->port = port;
}

k16u GoSystem::ControlPort() const
{
    return port;
}

k16u GoSystem::GdpPort()
{
    GoJson response = restClient.Read(GDP_RESOURCE_PATH_ROOT).GetResponse(GDP_TIMEOUT_MSEC).Payload();
    k16u gdpPort = (k16u) response.Get<int>(GDP_RESOURCE_PORT_KEY);
    return gdpPort;
}

void GoSystem::Connect()
{
    // Do a quick sanity check that address and port are valid.
    GoThrowIf((port == 0), kERROR_STATE);
    GoThrowIf((address.version != kIP_VERSION_4), kERROR_STATE);

    restClient.Connect(address, port);
}

void GoSystem::Disconnect()
{
    restClient.Disconnect();
}

bool GoSystem::IsConnected()
{
    return restClient.IsConnected();
}

void GoSystem::Start()
{
    // GOS-4972: Add CheckResponse() to properly wait for the command to complete & throw if unsuccessful.
    restClient.Call(SYSTEM_RESOURCE_PATH_START_CMD).CheckResponse(START_TIMEOUT_MSEC);
}

void GoSystem::Stop()
{
    // GOS-4972: Add CheckResponse() to properly wait for the command to complete & throw if unsuccessful.
    restClient.Call(SYSTEM_RESOURCE_PATH_STOP_CMD).CheckResponse(STOP_TIMEOUT_MSEC);
}

GoSystem::State GoSystem::RunningState()
{
    return (State) restClient.Read(SYSTEM_RESOURCE_PATH_ROOT).GetResponse(RUNNING_STATE_TIMEOUT_MSEC).Payload().Get<int>(SYSTEM_RESOURCE_RUNSTATE_KEY);
}

void GoSystem::EnableQuickEdit()
{
    GoJson payload;
    payload.Set<bool>(SYSTEM_RESOURCE_QUICKEDIT_KEY, true);

    restClient.Update(SYSTEM_RESOURCE_PATH_ROOT, payload).CheckResponse(QUICKEDIT_TIMEOUT_MSEC);
}

void GoSystem::DisableQuickEdit()
{
    GoJson payload;
    payload.Set<bool>(SYSTEM_RESOURCE_QUICKEDIT_KEY, false);

    restClient.Update(SYSTEM_RESOURCE_PATH_ROOT, payload).CheckResponse(QUICKEDIT_TIMEOUT_MSEC);
}

bool GoSystem::QuickEditEnabled()
{
    return restClient.Read(SYSTEM_RESOURCE_PATH_ROOT).GetResponse(QUICKEDIT_TIMEOUT_MSEC).Payload().Get<bool>(SYSTEM_RESOURCE_QUICKEDIT_KEY);
}

GoRestClient& GoSystem::Client()
{
    return restClient;
}

ResourcePath GoSystem::SensorPath(SerialNum serialNum)
{
    ResourcePath enginesPath = SCAN_RESOURCE_PATH_ENGINES;
    std::vector<ResourcePath> engines = GetChildPaths(enginesPath);
    for (ResourcePath enginePath : engines)
    {
        ResourcePath scannersPath = enginePath + SCAN_RESOURCE_SUBPATH_SCANNERS;
        std::vector<ResourcePath> scanners = GetChildPaths(scannersPath);
        for (ResourcePath scannerPath : scanners)
        {
            ResourcePath sensorsPath = scannerPath + SCAN_RESOURCE_SUBPATH_SENSORS;
            std::vector<ResourcePath> sensors = GetChildPaths(sensorsPath);
            for (ResourcePath sensorPath : sensors)
            {
                SerialNum candidateSerialNum = restClient.Read(sensorPath).GetResponse(SENSOR_PATH_TIMEOUT_MSEC).Payload().Get<SerialNum>(SCAN_RESOURCE_SERIALNUM_KEY);
                if (candidateSerialNum == serialNum)
                {
                    return sensorPath;
                }
            }
        }
    }

    // No matching serial number found.
    return "";
}

std::vector<ResourcePath> GoSystem::GetChildPaths(const ResourcePath& path)
{
    std::vector<ResourcePath> subPaths;

    try
    {
        // If there are no child paths (/_embedded/item doesn't exist), subItems will be an empty object.
        // GoJson::At will throw if key does not exist, so catch an GoJsonError and return an empty vector.
        const GoJson subItems = restClient.Read(path).GetResponse(CHILD_PATH_TIMEOUT_MSEC).Payload().At(CONTENT_PATH_EMBEDDED_ITEM);

        for (GoJsonIterator i = subItems.Begin(); i != subItems.End(); i++)
        {
            ResourcePath subPath = i.Value().Get<std::string>(CONTENT_PATH_EMBEDDED_LINKS);
            subPaths.push_back(subPath);
        }

        return subPaths;
    }
    catch (const GoJsonError&)
    {
        return subPaths;
    }
}

}
