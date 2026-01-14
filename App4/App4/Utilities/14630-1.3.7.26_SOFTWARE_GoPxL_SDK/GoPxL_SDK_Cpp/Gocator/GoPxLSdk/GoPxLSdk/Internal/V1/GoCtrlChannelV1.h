/**
 * @file    GoCtrlChannelV1.h
 * @brief   Declares the GoPxLSdk.GoCtrlChannelV1 class.
 * 
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#ifndef GO_PXL_SDK_GOCTRLCHANNELV1_H
#define GO_PXL_SDK_GOCTRLCHANNELV1_H

#include "GoPxLSdk/Def.h"
#include "GoPxLSdk/Internal/GoCtrlChannel.h"

#include "GoApi/Object.h"

#include "kApi/Threads/kSemaphore.h"

class GoCtrlChannelV1Tests;

namespace GoPxLSdk
{
constexpr k16u MSGPACK_MESSAGE_TYPE = 0xB000;
constexpr k16u JSON_MESSAGE_TYPE = 0xB001;

class GoPxLSdkClass GoCtrlChannelV1 : public GoCtrlChannel
{
public:
    /**
    * Constructs GoCtrlChannelV1.
    *
    * @public                @memberof GoCtrlChannelV1
    * @version               Introduced in 0.2.1.53
    */
    GoCtrlChannelV1();

    ~GoCtrlChannelV1();

    /**
     * Connects to the server using carrier protocol v1.
     *
     * @public                  @memberof GoCtrlChannelV1
     * @version                 Introduced in 1.1.50.15
     * @param address           The remote IP address.
     * @param port              The remote port number.
     * @param connectionTimeout (optional) timeout in milliseconds. Default value is defined in
                                GO_PXL_SDK_DEFAULT_TCP_TIMEOUT_MILLISECONDS.
     */
    void Connect(const kIpAddress& address, k16u port,
                 k64u connectionTimeout = GO_PXL_SDK_DEFAULT_TCP_TIMEOUT_MILLISECONDS) override;

    /**
     * Disconnects from the server.
     *
     * @public                @memberof GoCtrlChannelV1
     * @version               Introduced in 0.2.1.53
     */
    void Disconnect() override;

    /**
     * Checks if connected to the server.
     *
     * @public                @memberof GoCtrlChannelV1
     * @version               Introduced in 0.2.1.53
     * @return true if connected
     */
    bool IsConnected() const override;

    /**
     * Sends a request using carrier protocol v1.
     *
     * @public                @memberof GoCtrlChannelV1
     * @version               Introduced in 0.2.1.53
     * @throw GoChannelError  if not connected.
     * @param request         The GoRequest sent using carrier protocol v1.
     */
    void SendRequest(const GoRequest& request) override;

    /**
     * Sets a callback to receive responses from the server, using the implemented carrier protocol.
     *
     * @public                @memberof GoCtrlChannelV1
     * @version               Introduced in 0.2.1.53
     * @param callback        The response handler to receive responses from the server.
     */
    void SetResponseHandler(GoCtrlChannelResponseHandler callback) override;

    /**
     * Sets a callback to receive exceptions from the write thread.
     *
     * @public                @memberof GoCtrlChannel
     * @version               Introduced in 1.1.9.56
     * @param callback
     */
    void SetWriteErrorHandler(GoCtrlChannelWriteErrorHandler callback) override;

    /**
     * Sets a callback to receive exceptions from the read thread.
     * Error handler should not throw or block.
     *
     * @public                @memberof GoCtrlChannel
     * @version               Introduced in 1.2.1.114
     * @param callback
     */
    void SetReadErrorHandler(GoCtrlChannelReadErrorHandler callback) override;

    /**
     *
     * @public                @memberof GoCtrlChannelV1
     * @version               Introduced in 0.2.1.53
     * @return                server address
     */
    const kIpAddress& Address() const;

    /**
     *
     * @public                @memberof GoCtrlChannelV1
     * @version               Introduced in 1.1.50.15
     * @return                server port
     */
    const k16u Port() const;

// Private constants
private:
    const k64u WRITE_THREAD_LOOP_SLEEP_USEC = 100000;   // 100 msec.
    const k64u READ_THREAD_LOOP_SLEEP_USEC = 100000;     // 100 msec.

private:
    void ProcessRequest(const ByteArray& msgpack) const;
    void ProcessResponse() const;

    void OnResponse(const ByteArray& bytes) const;
    void OnWriteError(k64u id, const std::exception& e) const;
    void OnReadError(ReadErrorType readErrorType, const std::exception& e) const;

    static kStatus kCall WriteThreadFunc(void* context);
    static kStatus kCall ReadThreadFunc(void* context);

private:
    kIpAddress address = { };
    k16u port = GO_PXL_SDK_DEFAULT_CONTROL_PORT;

    Go::Object<kTcpClient> client;
    bool isConnected = false;

    Go::Object<kThread> readThread;
    Go::Object<kThread> writeThread;

    std::mutex mutex;

    Go::Object<kSerializer> serializer;

    std::queue<GoRequest> sendQueue;
    Go::Object<kSemaphore> sendQueueSema;

    GoCtrlChannelResponseHandler responseHandler;

    GoCtrlChannelWriteErrorHandler writeErrorHandler;
    GoCtrlChannelReadErrorHandler readErrorHandler;

    friend class ::GoCtrlChannelV1Tests;
};

}

#endif