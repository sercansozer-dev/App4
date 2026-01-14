/**
 * @file    GoCtrlChannelV1.cpp
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#include <GoPxLSdk/Internal/V1/GoCtrlChannelV1.h>

#include <GoPxLSdk/GoChannelError.h>

#include <GoApi/GoApi.h>

#include <kApi/Io/kTcpClient.h>
#include <kApi/Io/kSerializer.h>
#include <kApi/Threads/kThread.h>

namespace GoPxLSdk
{
GoCtrlChannelV1::GoCtrlChannelV1()
{
    GoTest(kThread_Construct(writeThread.Ref(), kAlloc_App()));
    GoTest(kThread_Construct(readThread.Ref(), kAlloc_App()));

    GoTest(kSemaphore_Construct(sendQueueSema.Ref(), 0, kAlloc_App()));
}

GoCtrlChannelV1::~GoCtrlChannelV1()
{
    if (IsConnected())
    {
        Disconnect();
    }
}

void GoCtrlChannelV1::Connect(const kIpAddress& address, k16u port, k64u connectionTimeout)
{
    if (IsConnected())
    {
        return;
    }

    this->address = address;
    this->port = port;

    GoTest(kTcpClient_Construct(client.Ref(), kIP_VERSION_4, kAlloc_App()));
    GoTest(kSerializer_Construct(serializer.Ref(), client, kNULL, kAlloc_App()));

    auto status = kTcpClient_Connect(client, this->address, port, connectionTimeout * 1000);
    if (status != kOK)
    {
        serializer.Reset();
        client.Reset();
    }
    GoTest(status);

    isConnected = true;

    /**
     * TODO: kSerializer may not be thread-safe.This needs to be revisited as a part of GOS-925.
     */
    GoTest(kThread_Start(writeThread, GoCtrlChannelV1::WriteThreadFunc, this, "GoCtrlChV1.Wrt"));
    GoTest(kThread_Start(readThread, GoCtrlChannelV1::ReadThreadFunc, this, "GoCtrlChV1.Rd"));
}

void GoCtrlChannelV1::Disconnect()
{
    if (!IsConnected())
    {
        return;
    }

    isConnected = false;

    GoTest(kThread_Join(readThread, kINFINITE, nullptr));
    GoTest(kThread_Join(writeThread, kINFINITE, nullptr));

    GoTest(kTcpClient_Shutdown(client));

    serializer.Reset();
    client.Reset();
}

bool GoCtrlChannelV1::IsConnected() const
{
    return isConnected;
}

void GoCtrlChannelV1::SendRequest(const GoRequest& request)
{
    if (IsConnected())
    {
        std::lock_guard<std::mutex> lock(mutex);

        sendQueue.push(request);
        GoTest(kSemaphore_Post(sendQueueSema.Get()));
    }
    else
    {
        throw GoChannelError("Not connected.");
    }
}

void GoCtrlChannelV1::SetResponseHandler(GoCtrlChannelResponseHandler callback)
{
    responseHandler = callback;
}

void GoCtrlChannelV1::SetWriteErrorHandler(GoCtrlChannelWriteErrorHandler callback)
{
    writeErrorHandler = callback;
}

void GoCtrlChannelV1::SetReadErrorHandler(GoCtrlChannelReadErrorHandler callback)
{
    readErrorHandler = callback;
}

const kIpAddress& GoCtrlChannelV1::Address() const
{
    return address;
}

const k16u GoCtrlChannelV1::Port() const
{
    return port;
}

void GoCtrlChannelV1::ProcessRequest(const ByteArray& msgpack) const
{
    if (serializer == nullptr)
    {
        return;
    }

    GoTest(kSerializer_BeginWrite(serializer, kTypeOf(k32u), kTRUE));

    GoTest(kSerializer_Write16u(serializer, MSGPACK_MESSAGE_TYPE));
    GoTest(kSerializer_Write32u(serializer, (k32u)msgpack.size()));
    GoTest(kSerializer_WriteCharArray(serializer, (const kChar*)msgpack.data(), msgpack.size()));

    GoTest(kSerializer_EndWrite(serializer));

    GoTest(kSerializer_Flush(serializer));
}

void GoCtrlChannelV1::ProcessResponse() const
{
    if (serializer == nullptr)
    {
        OnReadError(ReadErrorType::DeserializationError, GoChannelError("Uninitialized serializer."));

        return;
    }

    k16u messageType;
    k32s status;
    k32u bufferSize;

    try
    {
        GoTest(kSerializer_BeginRead(serializer, kTypeOf(k32u), kTRUE));
        GoTest(kSerializer_Read16u(serializer, &messageType));

        if (messageType != MSGPACK_MESSAGE_TYPE)
        {
            OnReadError(ReadErrorType::InvalidMessageType, GoChannelError(("Only msgpack is supported. Received " + std::to_string(messageType)).c_str()));

            GoLogWarn("Invalid message type.");
            GoTest(kSerializer_EndRead(serializer));

            return;
        }

        GoTest(kSerializer_Read32s(serializer, &status));
        GoTest(kSerializer_Read32u(serializer, &bufferSize));

        ByteArray buffer(bufferSize);
        GoTest(kSerializer_ReadByteArray(serializer, buffer.data(), bufferSize));

        OnResponse(buffer);

        GoTest(kSerializer_EndRead(serializer));
    }
    catch (const std::exception& e)
    {
        GoLogException(e);

        OnReadError(ReadErrorType::DeserializationError, e);
    }
}

void GoCtrlChannelV1::OnResponse(const ByteArray& bytes) const
{
    try
    {
        const GoJson json = GoJson::ParseMsgPack(bytes);

        std::shared_ptr<GoResponse> response;
        switch (GoResponseType::FromString(json.Get<std::string>("type")))
        {
            case GoResponseType::Request:
            default:
            {
                response = std::make_shared<GoRequestResponse>(json);
                break;
            }
            case GoResponseType::Notification:
            {
                response = std::make_shared<GoNotificationResponse>(json);
                break;
            }
            case GoResponseType::Stream:
            {
                response = std::make_shared<GoStreamResponse>(json);
                break;
            }
        }

        if (responseHandler != nullptr)
        {
            responseHandler(response);
        }
    }
    catch (const std::exception& e)
    {
        OnReadError(ReadErrorType::InvalidMessageContent, e);
    }
}

void GoCtrlChannelV1::OnWriteError(k64u id, const std::exception& e) const
{
    if (writeErrorHandler)
    {
        try
        {
            writeErrorHandler(id, e);
        }
        catch (const std::exception& e)
        {
            GoLogException(e);
        }
    }
}

void GoCtrlChannelV1::OnReadError(ReadErrorType readErrorType, const std::exception& e) const
{
    if (readErrorHandler)
    {
        try
        {
            readErrorHandler(readErrorType, e);
        }
        catch (const std::exception& e)
        {
            GoLogException(e);
        }
    }
}

kStatus kCall GoCtrlChannelV1::WriteThreadFunc(void* context)
{
    kStatus status;
    GoCtrlChannelV1* channel = (GoCtrlChannelV1*)(context);

    while (channel->IsConnected())
    {
        // Don't exit thread if waiting fails.
        status = kSemaphore_Wait(channel->sendQueueSema.Get(), channel->WRITE_THREAD_LOOP_SLEEP_USEC);
        if (kIsError(status) && (status != kERROR_TIMEOUT))
        {
            GoLogError("Error %d waiting for data in send queue", status);
        }
        else
        {
            while (!channel->sendQueue.empty())
            {
                std::lock_guard<std::mutex> lock(channel->mutex);

                const GoRequest& request = channel->sendQueue.front();

                // Catch exception during processing of request.
                try
                {
                    channel->ProcessRequest(request.ToByteArray());
                }
                catch (const std::exception& e)
                {
                    // Notify there was an exception writing the request.
                    channel->OnWriteError(request.Id(), e);

                    // Fall through and continue running the thread loop.
                }

                // Always try to pop the front entry in the queue, regardless of
                // whether processing of the request succeeded or not.
                // Catch any queue pop exception and continue running the thread loop.
                try
                {
                    channel->sendQueue.pop();
                }
                catch (const std::exception& e)
                {
                    GoLogException(e);

                    // Fall through and continue running the thread loop.
                }
            }
        }
    }

    return kOK;
}

kStatus kCall GoCtrlChannelV1::ReadThreadFunc(void* context)
{
    GoCtrlChannelV1* channel = (GoCtrlChannelV1*)(context);

    while (channel->IsConnected())
    {
        if (kTcpClient_Wait(channel->client, channel->READ_THREAD_LOOP_SLEEP_USEC) != kOK)
        {
            continue;
        }

        try
        {
            channel->ProcessResponse();
        }
        catch (const std::exception& e)
        {
            channel->OnReadError(ReadErrorType::Unknown, e);
        }
    }

    return kOK;
}

}
