/**
 * @file    GoDiscoveryClient.cpp
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#include <GoPxLSdk/Def.h>
#include <GoPxLSdk/GoDiscoveryClient.h>

namespace GoPxLSdk 
{

void GoDiscoveryClient::BlockingDiscover(k64u timeoutInMilliseconds)
{
    DiscoveryBroadcastHeader header;
    kUdpClient receiver = kNULL;
    Byte buffer[MAX_MESSAGE_SIZE];
    kSize readSize = 0;
    kTimer timer = kNULL;

    instances.clear();
    try
    {
        GoTest(kTimer_Construct(&timer, kAlloc_App()));

        // Fill the message header that will be broadcasted to the server.
        header.length = sizeof(header);
        header.messageId = GOPXL_DISCOVERY_MESSAGE_DISCOVER;
        header.signature = GOPXL_BROADCAST_SIGNATURE;

        ConstructReceiver(&receiver);
        Broadcast(&header, sizeof(header));
        GoTest(kTimer_Start(timer, timeoutInMilliseconds * GO_PXL_SDK_MILLISECONDS_TO_MICROSECONDS_CONVERSION));
        do
        {
            if (kSuccess(kUdpClient_Receive(receiver, kNULL, &readSize, kTimer_Remaining(timer))))
            {
                GoThrowIf(readSize > MAX_MESSAGE_SIZE, kERROR_MEMORY);
                if (kSuccess(kStream_Read(receiver, buffer, readSize)))
                { 
                    try
                    {
                        ParseReply(buffer);
                    }
                    catch (const std::exception&)
                    {
                        // We want to continue to receive UDP messages even if ParseReply fails
                        // to potentially parse more GoPxL instances.
                        continue;
                    }
                }
            }
        }
        while (!kTimer_IsExpired(timer));  
        GoTest(kTimer_Stop(timer));
    }
    catch (const Go::Exception&)
    {
        kDestroyRef(&receiver);
        kDestroyRef(&timer);
        GoRethrow("Failed in discovering GoPxL instances.");
    }
    kDestroyRef(&receiver);
    kDestroyRef(&timer);
}

const std::vector<GoInstance>& GoDiscoveryClient::InstanceList()
{
    return instances;
}

// GOS-9237 Modified Instance to use IP address and web port instead of appId.
// This change is necessary because an accelerated sensor now shares the appId
// with the GoPxl service (GOS-8266), rendering the function ineffective when searching by appId.
const GoInstance* GoDiscoveryClient::Instance(std::string ipAddress, k32u webPort)
{
    kText32 tempIpAddress;

    for (std::vector<GoInstance>::size_type i = 0; i < instances.size(); i++)
    {
        kIpAddress_Format(instances.at(i).GetIpAddress(), tempIpAddress, kCountOf(tempIpAddress));

        if (tempIpAddress == ipAddress  && instances.at(i).GetWebPort() == webPort)
        {
            return &instances.at(i);
        }
    }
    return nullptr;
}

void GoDiscoveryClient::ConstructReceiver(kUdpClient* receiver)
{
    kUdpClient localReceiver = kNULL;

    try
    {
        GoThrowIf(receiver == nullptr, kERROR_PARAMETER);
        GoTest(kUdpClient_Construct(&localReceiver, kIP_VERSION_4, kAlloc_App()));
        GoTest(kUdpClient_EnableBroadcast(localReceiver, kTRUE));
        GoTest(kUdpClient_EnableReuseAddress(localReceiver, kTRUE));
        GoTest(kUdpClient_SetReadBuffers(localReceiver, CLIENT_SOCKET_SIZE, CLIENT_BUFFER_SIZE));
        GoTest(kUdpClient_Bind(localReceiver, kIpAddress_AnyV4(), GOPXL_RESERVED_PORT_DISCOVERY_PROTOCOL));
        
        *receiver = localReceiver;
    }
    catch (const Go::Exception&)
    {
        kObject_Destroy(localReceiver);
        GoRethrow("Failed to construct receiver.");
    }

}

void GoDiscoveryClient::ConstructSender(kUdpClient* sender, kIpAddress address)
{
    kUdpClient localSender = kNULL;

    try
    {
        GoThrowIf(sender == nullptr, kERROR_PARAMETER);
        GoTest(kUdpClient_Construct(&localSender, kIP_VERSION_4, kAlloc_App()));
        GoTest(kUdpClient_EnableBroadcast(localSender, kTRUE));
        GoTest(kUdpClient_EnableReuseAddress(localSender, kTRUE));
        GoTest(kUdpClient_SetWriteBuffers(localSender, CLIENT_SOCKET_SIZE, CLIENT_BUFFER_SIZE));
        GoTest(kUdpClient_Bind(localSender, address, kIP_PORT_ANY));

        *sender = localSender;
    }
    catch (const Go::Exception&)
    {
        kObject_Destroy(localSender);
        GoRethrow("Failed to construct sender.");
    }

}

void GoDiscoveryClient::Broadcast(const void * data, k32u length)
{
    kNetworkInfo info = kNULL;
    kUdpClient sender = kNULL;

    try
    {
        GoTest(kNetworkInfo_Construct(&info, kNULL));

        for (kSize i = 0; i < kNetworkInfo_InterfaceCount(info); ++i)
        {
            kNetworkInterface interfaceAt = kNetworkInfo_InterfaceAt(info, i);
            kIpAddress address = kNetworkInterface_Address(interfaceAt);

            // We want to continue loop even if ConstructSender fails to create a sender for that 
            // interface instead of skipping all remaining interfaces.
            try
            {
                ConstructSender(&sender, address);
            }
            catch (const Go::Exception&)
            {
                continue;
            }

            GoTest(kStream_Write(sender, data, length));
            GoTest(kUdpClient_Send(sender, kIpAddress_BroadcastV4(), GOPXL_RESERVED_PORT_DISCOVERY_PROTOCOL, kINFINITE, kTRUE));
            GoTest(kDestroyRef(&sender));
        }
    }
    catch (const Go::Exception&)
    {
        kObject_Destroy(info);
        kDestroyRef(&sender);
        GoRethrow("Failed to broadcast message.");
    }
    kObject_Destroy(info);
    kDestroyRef(&sender);
}

void GoDiscoveryClient::ParseReply(Byte bytes[])
{
    GoInstance instanceInfo;
    int pos = sizeof(DiscoveryServerHeader);
    kChar* charArr = (kChar*)bytes+pos;

    // Extract the header of the message that was received from server.
    const auto serverHeader = reinterpret_cast<DiscoveryServerHeader*>(bytes);

    // Ignore any discover messages.
    if (serverHeader->messageId == GOPXL_DISCOVERY_MESSAGE_DISCOVER) 
    {
        return;
    }
    GoThrowIf(serverHeader->messageId != GOPXL_DISCOVERY_MESSAGE_ANNOUNCE || serverHeader->signature != GOPXL_BROADCAST_SIGNATURE, kERROR);

    // Convert the message payload to a string.
    std::string instanceInfoStr(charArr);

    // Convert the string to json object.
    auto instanceInfoJson = GoJson::ParseString(instanceInfoStr);

    // Store the information of the json object into a GoInstance object.
    if (instanceInfoJson.HasKey(GOPXL_DISCOVERY_APP_ID))
    {
        // If this instance is a repeat, do not store information.
        if (Instance(instanceInfoJson.Get<std::string>(GOPXL_DISCOVERY_ADDRESS), instanceInfoJson.Get<k32u>(GOPXL_DISCOVERY_WEB_PORT)) != NULL) 
        {
            return;
        }
        instanceInfo.SetAppId(instanceInfoJson.Get<std::string>(GOPXL_DISCOVERY_APP_ID));
    }
    if (instanceInfoJson.HasKey(GOPXL_DISCOVERY_SERIAL_NUMBER) && 
        !instanceInfoJson.Get<std::string>(GOPXL_DISCOVERY_SERIAL_NUMBER).empty())
    {
        k32u serialNumber;
        GoTest(k32u_Parse(&serialNumber, instanceInfoJson.Get<std::string>(GOPXL_DISCOVERY_SERIAL_NUMBER).c_str()));
        instanceInfo.SetSerialNumber(serialNumber);
    }
    if (instanceInfoJson.HasKey(GOPXL_DISCOVERY_ADDRESS) && 
        !instanceInfoJson.Get<std::string>(GOPXL_DISCOVERY_ADDRESS).empty())
    {
        kIpAddress ipAddress;
        GoTest(kIpAddress_Parse(&ipAddress, instanceInfoJson.Get<std::string>(GOPXL_DISCOVERY_ADDRESS).c_str()));
        instanceInfo.SetIpAddress(ipAddress);
    }
    if (instanceInfoJson.HasKey(GOPXL_DISCOVERY_GATEWAY) && 
        !instanceInfoJson.Get<std::string>(GOPXL_DISCOVERY_GATEWAY).empty()) 
    {
        kIpAddress gateway;
        GoTest(kIpAddress_Parse(&gateway, instanceInfoJson.Get<std::string>(GOPXL_DISCOVERY_GATEWAY).c_str()));
        instanceInfo.SetGateway(gateway);
    }
    if (instanceInfoJson.HasKey(GOPXL_DISCOVERY_MASK) && 
        !instanceInfoJson.Get<std::string>(GOPXL_DISCOVERY_MASK).empty())
    {
        kIpAddress mask;
        GoTest(kIpAddress_Parse(&mask, instanceInfoJson.Get<std::string>(GOPXL_DISCOVERY_MASK).c_str()));
        instanceInfo.SetMask(mask);
    }

    if (instanceInfoJson.HasKey(GOPXL_DISCOVERY_CONTROL_PORT)) 
    {
        k32u controlPort = instanceInfoJson.Get<k32u>(GOPXL_DISCOVERY_CONTROL_PORT);
        instanceInfo.SetControlPort((k16u)controlPort);
    }
    if (instanceInfoJson.HasKey(GOPXL_DISCOVERY_GDP_PORT)) 
    {
        k32u GdpPort = instanceInfoJson.Get<k32u>(GOPXL_DISCOVERY_GDP_PORT);
        instanceInfo.SetGdpPort((k16u)GdpPort);
    }
    if (instanceInfoJson.HasKey(GOPXL_DISCOVERY_WEB_PORT)) 
    {
        k32u webPort = instanceInfoJson.Get<k32u>(GOPXL_DISCOVERY_WEB_PORT);
        instanceInfo.SetWebPort((k16u)webPort);
    }
    if (instanceInfoJson.HasKey(GOPXL_DISCOVERY_APP_NAME))
    {
        instanceInfo.SetAppName(instanceInfoJson.Get<std::string>(GOPXL_DISCOVERY_APP_NAME));
    }
    if (instanceInfoJson.HasKey(GOPXL_DISCOVERY_APP_VERSION))
    {
        instanceInfo.SetAppVersion(instanceInfoJson.Get<std::string>(GOPXL_DISCOVERY_APP_VERSION));
    }
    if (instanceInfoJson.HasKey(GOPXL_DISCOVERY_DEVICE_MODEL))
    {
        instanceInfo.SetDeviceModel(instanceInfoJson.Get<std::string>(GOPXL_DISCOVERY_DEVICE_MODEL));
    }
    if (instanceInfoJson.HasKey(GOPXL_DISCOVERY_DHCP))
    {
        instanceInfo.SetIsDhcp(instanceInfoJson.Get<bool>(GOPXL_DISCOVERY_DHCP));
    }
    if (instanceInfoJson.HasKey(GOPXL_DISCOVERY_IS_REMOTE))
    {
        instanceInfo.SetIsRemote(instanceInfoJson.Get<bool>(GOPXL_DISCOVERY_IS_REMOTE));
    }
    if (instanceInfoJson.HasKey(GOPXL_DISCOVERY_ADDRESS_CONFLICT))
    {
        instanceInfo.SetIsAddressConflict(instanceInfoJson.Get<bool>(GOPXL_DISCOVERY_ADDRESS_CONFLICT));
    }
    if (instanceInfoJson.HasKey(GOPXL_DISCOVERY_HMI_STATUS))
    {
        instanceInfo.SetHMIStatus(instanceInfoJson.Get<int>(GOPXL_DISCOVERY_HMI_STATUS));
    }

    instances.push_back(instanceInfo);
}

}