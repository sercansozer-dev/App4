/**
 * @file    GoDiscoveryClient.h
 * @brief   Declares the GoPxLSdk.GoDiscoveryClient class.
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#ifndef GO_PXL_SDK_GODISCOVERYCLIENT_H
#define GO_PXL_SDK_GODISCOVERYCLIENT_H

#include <kApi/kApi.h>
#include <GoPxLSdk/GoJson.h>
#include <GoPxLSdk/GoInstance.h>
#include <GoApi/Exception.h>

class GoDiscoveryClientTests;

namespace GoPxLSdk 
{
const k64u GOPXL_BROADCAST_SIGNATURE                = 0x4C58504F47494D4C; // "LMIGOPXL"
const k16u GOPXL_RESERVED_PORT_DISCOVERY_PROTOCOL   = 3320;
const k64u GOPXL_DISCOVERY_MESSAGE_DISCOVER         = 0x0001;
const k64u GOPXL_DISCOVERY_MESSAGE_ANNOUNCE         = 0x1001;
const kSize MAX_MESSAGE_SIZE                        = 1536;
const kSSize CLIENT_BUFFER_SIZE                     = 1536;
const kSSize CLIENT_SOCKET_SIZE                     = -1;

const std::string GOPXL_DISCOVERY_SERIAL_NUMBER     = "SerialNumber";
const std::string GOPXL_DISCOVERY_DEVICE_MODEL      = "DeviceModel";
const std::string GOPXL_DISCOVERY_APP_NAME          = "AppName";
const std::string GOPXL_DISCOVERY_APP_ID            = "AppId";
const std::string GOPXL_DISCOVERY_APP_VERSION       = "AppVersion";
const std::string GOPXL_DISCOVERY_CONTROL_PORT      = "ControlPort";
const std::string GOPXL_DISCOVERY_WEB_PORT          = "WebPort";
const std::string GOPXL_DISCOVERY_GDP_PORT          = "GdpPort";
const std::string GOPXL_DISCOVERY_ADDRESS           = "Address";
const std::string GOPXL_DISCOVERY_MASK              = "Mask";
const std::string GOPXL_DISCOVERY_GATEWAY           = "Gateway";
const std::string GOPXL_DISCOVERY_DHCP              = "Dhcp";
const std::string GOPXL_DISCOVERY_IS_REMOTE         = "IsRemote";
const std::string GOPXL_DISCOVERY_ADDRESS_CONFLICT  = "AddressConflict";
const std::string GOPXL_DISCOVERY_HMI_STATUS        = "HMIStatus";

// Message request that is sent to the discovery server.
struct DiscoveryBroadcastHeader
{
    k64u            length;
    k64u            messageId;
    k64u            signature;
};

// Message header of the response that the discovery server sends back 
// to discovery client.
struct DiscoveryServerHeader
{
    k64u            length;
    k64u            messageId;
    k64u            signature;
    k64u            messageStatus;
};

class GoPxLSdkClass GoDiscoveryClient
{
    friend class ::GoDiscoveryClientTests;

public:
    /**
     * Constructs GoDiscoveryClient.
     * 
     * @public                          @memberof GoDiscoveryClient
     * @version                         Introduced in 0.2.1.53.
     */
    GoDiscoveryClient() = default;
    ~GoDiscoveryClient() = default;

    /**
     * Discovers all GoPxL instances on the network.
     * 
     * @public                          @memberof GoDiscoveryClient
     * @version                         Introduced in 0.2.1.53.
     * @param timeoutInMilliseconds     Time limit to listen for GoPxL instance in milliseconds.
     * @throws Go::Exception            If failed to discover GoPxL instances or if buffer size is larger than maximum message size.
     */
    void BlockingDiscover(k64u timeoutInMilliseconds);

    /**
     * Returns a list of all GoPxL instances found on the network.
     * 
     * @public                          @memberof GoDiscoveryClient
     * @version                         Introduced in 0.2.1.53.
     * @return                          List of GoPxL instances.
     */
    const std::vector<GoInstance>& InstanceList();

    /**
     * Returns a single GoPxL instance that is found on the network and has the specified IP address and web port.
     * Both IP address and web port is required because multiple instances of GoPxl Service can share one IP Address.
     * 
     * @public                          @memberof GoDiscoveryClient
     * @version                         Introduced in 1.1.50.15
     * @param ipAddress                 IP address of the instance to search for.
     * @param webPort                   Web port of the instance to search for. 
     * @return                          GoPxL instance that matches IP address and web port.
     */
    const GoInstance* Instance(std::string ipAddress, k32u webPort);

private:

    /**
     * Constructs kUdpClient object for receiving data.
     * 
     * @public                          @memberof GoDiscoveryClient
     * @version                         Introduced in 0.2.1.53.
     * @param receiver                  The kUdpClient to be constructed.
     * @throws Go::Exception            If failed to construct receiver.
     */
    void ConstructReceiver(kUdpClient* receiver);

    /**
     * Constructs kUdpClient object for sending data.
     * 
     * @public                          @memberof GoDiscoveryClient
     * @version                         Introduced in 0.2.1.53.
     * @param sender                    The kUdpClient to be constructed.
     * @param address                   IP address of local interface.
     * @throws Go::Exception            If failed to construct sender.
     */
    void ConstructSender(kUdpClient* sender, kIpAddress address);

    /**
     * Broadcast the client message header to the broadcast port.
     * 
     * @public                          @memberof GoDiscoveryClient
     * @version                         Introduced in 0.2.1.53.
     * @param data                      Message header to be sent to port.
     * @param length                    Size of message header.
     * @throws Go::Exception            If failed to broadcast message.
     */
    void Broadcast(const void* data, k32u length);

    /**
     * Convert the message received from the server into a GoInstance object and 
     * save this object into the list of GoPxL instances found on the network.
     * 
     * @public                          @memberof GoDiscoveryClient
     * @version                         Introduced in 0.2.1.53.
     * @param bytes                     Message received from sender.
     * @throws Go::Exception            If messageId or signature is incorrect.
     */
    void ParseReply(Byte bytes[]);

    std::vector<GoInstance> instances;
};

}

#endif