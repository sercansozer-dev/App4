/**
 * @file    GoInstance.cpp
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#include <GoPxLSdk/Def.h>
#include <GoPxLSdk/GoInstance.h>

namespace GoPxLSdk 
{
GoInstance::GoInstance() : 
    ipAddress{0},
    appId{""},
    appName{""},
    appVersion{""},
    controlPort{0},
    serialNumber{0},
    deviceModel{""},
    isDhcp{false},
    gateway{0},
    gdpPort{0},
    isAddressConflict{false},
    isRemote{false},
    mask{0},
    webPort{0},
    hmiStatus{1}
{
}

const kIpAddress& GoInstance::GetIpAddress() const
{
    return ipAddress;
}

void GoInstance::SetIpAddress(kIpAddress ipAddress)
{
    this->ipAddress = ipAddress;
}

const std::string& GoInstance::GetAppId() const
{
    return appId;
}
void GoInstance::SetAppId(std::string appId)
{
    this->appId = appId;
}

const std::string& GoInstance::GetAppName() const
{
    return appName;
}

void GoInstance::SetAppName(std::string appName)
{
    this->appName = appName;
}

const std::string& GoInstance::GetAppVersion() const
{
    return appVersion;
}

void GoInstance::SetAppVersion(std::string appVersion)
{
    this->appVersion = appVersion;
}

const k16u GoInstance::GetControlPort() const
{
    return controlPort;
}

void GoInstance::SetControlPort(k16u controlPort)
{
    this->controlPort = controlPort;
}

const k32u& GoInstance::GetSerialNumber() const
{
    return serialNumber;
}

void GoInstance::SetSerialNumber(k32u serialNumber)
{
    this->serialNumber = serialNumber;
}

const std::string& GoInstance::GetDeviceModel() const
{
    return deviceModel;
}

void GoInstance::SetDeviceModel(std::string deviceModel)
{
    this->deviceModel = deviceModel;
}

const bool GoInstance::GetIsDhcp() const
{
    return isDhcp;
}

void GoInstance::SetIsDhcp(bool isDhcp)
{
    this->isDhcp = isDhcp;
}

const kIpAddress& GoInstance::GetGateway() const
{
    return gateway;
}

void GoInstance::SetGateway(kIpAddress gateway)
{
    this->gateway = gateway;
}

const k16u GoInstance::GetGdpPort() const
{
    return gdpPort;
}

void GoInstance::SetGdpPort(k16u gdpPort)
{
    this->gdpPort = gdpPort;
}

const bool GoInstance::GetIsAddressConflict() const
{
    return isAddressConflict;
}

void GoInstance::SetIsAddressConflict(bool isAddressConflict)
{
    this->isAddressConflict = isAddressConflict;
}

const bool GoInstance::GetIsRemote() const
{
    return isRemote;
}

void GoInstance::SetIsRemote(bool isRemote)
{
    this->isRemote = isRemote;
}

const kIpAddress& GoInstance::GetMask() const
{
    return mask;
}

void GoInstance::SetMask(kIpAddress mask)
{
    this->mask = mask;
}

const k16u GoInstance::GetWebPort() const
{
    return webPort;
}

void GoInstance::SetWebPort(k16u webPort)
{
    this->webPort = webPort;
}

int GoInstance::GetHMIStatus() const
{
    return hmiStatus;
}

void GoInstance::SetHMIStatus(int status)
{
    this->hmiStatus = status;
}

}