/**
 * @file    GoCtrlChannel.h
 * @brief   Declares the GoPxLSdk.GoCtrlChannel class.
 * 
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#ifndef GO_PXL_SDK_GOCTRLCHANNEL_H
#define GO_PXL_SDK_GOCTRLCHANNEL_H


#include <kApi/Io/kNetwork.h>
#include <kApi/kApiDef.h>

#include <GoPxLSdk/Def.h>
#include <GoPxLSdk/GoResponse.h>
#include <GoPxLSdk/GoRequest.h>

namespace GoPxLSdk
{

enum class ReadErrorType
{
    Unknown,                // Indicates there was an unhandled error.
    DeserializationError,   // Indicates there was an error while deserializing data.
    InvalidMessageType,     // Indicates the received message type is invalid.
    InvalidMessageContent,  // Indicates the received message content is invalid.
};

/**
 * Function to receive responses from the server asynchronously.
 */
using GoCtrlChannelResponseHandler = std::function<void(const std::shared_ptr<GoResponse> response)>;

/**
 * Function to receive write thread errors.
 */
using GoCtrlChannelWriteErrorHandler = std::function<void(k64u id, const std::exception& e)>;

/**
 * Function to receive read thread errors.
 */
using GoCtrlChannelReadErrorHandler = std::function<void(ReadErrorType readErrorType, const std::exception& e)>;

/**
 * Interface for all control channel implementations.
 * Currently only implemented by GoCtrlChannelV1.
 */
class GoPxLSdkClass GoCtrlChannel
{
public:
    /**
    * Constructs GoCtrlChannel.
    *
    * @public                @memberof GoCtrlChannel
    * @version               Introduced in 0.2.1.53
    */
    virtual ~GoCtrlChannel() = default;

    /**
     * Connects to the server.
     *
     * @public                  @memberof GoCtrlChannel
     * @version                 Introduced in 1.1.50.15
     * @param address           The remote IP address.
     * @param connectionTimeout (optional) timeout in milliseconds. Default value is defined in
                                GO_PXL_SDK_DEFAULT_TCP_TIMEOUT_MILLISECONDS.
     * @param port              The remote port number.
     */
    virtual void Connect(const kIpAddress& address, k16u port, 
                         k64u connectionTimeout = GO_PXL_SDK_DEFAULT_TCP_TIMEOUT_MILLISECONDS) = 0;

    /**
     * Disconnects from the server.
     *
     * @public                @memberof GoCtrlChannel
     * @version               Introduced in 0.2.1.53
     */
    virtual void Disconnect() = 0;

    /**
     * Checks if connected to the server.
     *
     * @public                @memberof GoCtrlChannel
     * @version               Introduced in 0.2.1.53
     * @return                true if connected
     */
    virtual bool IsConnected() const = 0;

    /**
     * Sends a request according to the implemented carrier protocol.
     * Should store the raw, complete request json on the GoRequest object in case this object
     * is cached and possible reviewed for debugging purposes.
     *
     * @public                @memberof GoCtrlChannel
     * @version               Introduced in 0.2.1.53
     * @throw GoChannelError  If not connected.
     * @param request         The GoRequest sent to the implemeted carrier protocol.
     */
    virtual void SendRequest(const GoRequest& request) = 0;

    /**
     * Sets a callback to receive responses from the server, using the implemented carrier protocol.
     *
     * @public                @memberof GoCtrlChannel
     * @version               Introduced in 0.2.1.53
     * @param callback        The response handler to receive responses from the server.
     */
    virtual void SetResponseHandler(GoCtrlChannelResponseHandler callback) = 0;


    /**
     * Sets a callback to receive exceptions from the write thread.
     * Error handler should not throw or block.
     *
     * @public                @memberof GoCtrlChannel
     * @version               Introduced in 1.1.9.56
     * @param callback
     */
    virtual void SetWriteErrorHandler(GoCtrlChannelWriteErrorHandler callback) = 0;

    /**
     * Sets a callback to receive exceptions from the read thread.
     * Error handler should not throw or block.
     *
     * @public                @memberof GoCtrlChannel
     * @version               Introduced in 1.2.1.114
     * @param callback
     */
    virtual void SetReadErrorHandler(GoCtrlChannelReadErrorHandler callback) = 0;
};

}

#endif
