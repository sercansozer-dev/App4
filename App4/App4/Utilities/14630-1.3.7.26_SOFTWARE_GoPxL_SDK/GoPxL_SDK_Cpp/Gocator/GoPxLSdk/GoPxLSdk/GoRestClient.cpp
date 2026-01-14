/**
 * @file    GoRestClient.cpp
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#include <GoPxLSdk/Def.h>
#include <GoPxLSdk/GoRestClient.h>
#include <functional>
#include <thread>

namespace GoPxLSdk
{

GoRestClient::GoRestClient()
{
    channel = std::make_shared<GoCtrlChannelV1>();

    channel->SetResponseHandler(std::bind(&GoRestClient::ResponseHandler, this, std::placeholders::_1));
    channel->SetWriteErrorHandler(std::bind(&GoRestClient::WriteErrorHandler, this, std::placeholders::_1, std::placeholders::_2));
}

void GoRestClient::Connect(kIpAddress address, k16u port)
{
    std::lock_guard<std::mutex> lock(commandMutex);

    channel->Connect(address, port);
}

void GoRestClient::Disconnect()
{
    std::lock_guard<std::mutex> lock(commandMutex);

    channel->Disconnect();
}

bool GoRestClient::IsConnected() const
{
    return channel->IsConnected();
}

GoTransaction GoRestClient::Read(const std::string& uri, const GoJson& content, const GoJson& args)
{
    std::lock_guard<std::mutex> lock(commandMutex);

    auto const request = GoRequest(GoRequestMethod::Read, uri, content, args);
    auto transaction = GoTransaction(request);

    transactionQueue.push_back(transaction);

    channel->SendRequest(request);

    return transaction;
}

GoTransaction GoRestClient::Update(const std::string& uri, const GoJson& content, const GoJson& args)
{
    std::lock_guard<std::mutex> lock(commandMutex);

    auto const request = GoRequest(GoRequestMethod::Update, uri, content, args);
    auto transaction = GoTransaction(request);

    transactionQueue.push_back(transaction);

    channel->SendRequest(request);

    return transaction;
}

GoTransaction GoRestClient::Create(const std::string& uri, const GoJson& content, const GoJson& args)
{
    std::lock_guard<std::mutex> lock(commandMutex);

    auto const request = GoRequest(GoRequestMethod::Create, uri, content, args);
    auto transaction = GoTransaction(request);

    transactionQueue.push_back(transaction);

    channel->SendRequest(request);

    return transaction;
}

GoTransaction GoRestClient::Delete(const std::string& uri, const GoJson& content, const GoJson& args)
{
    std::lock_guard<std::mutex> lock(commandMutex);

    auto const request = GoRequest(GoRequestMethod::Delete, uri, content,  args);
    auto transaction = GoTransaction(request);

    transactionQueue.push_back(transaction);

    channel->SendRequest(request);

    return transaction;
}

GoTransaction GoRestClient::Call(const std::string& uri, const GoJson& content, const GoJson& args)
{
    std::lock_guard<std::mutex> lock(commandMutex);

    auto const request = GoRequest(GoRequestMethod::Call, uri, content, args);
    auto transaction = GoTransaction(request);

    channel->SendRequest(request);

    // SendRequest() will throw if not connected. Only push to queue if it succeeds.
    transactionQueue.push_back(transaction);

    return transaction;
}

GoTransaction GoRestClient::Sub(const std::string& uri, const GoJson& content, const GoJson& args)
{
    std::lock_guard<std::mutex> lock(commandMutex);

    auto const request = GoRequest(GoRequestMethod::Sub, uri, content, args);
    auto transaction = GoTransaction(request);

    transactionQueue.push_back(transaction);

    channel->SendRequest(request);

    return transaction;
}

GoTransaction GoRestClient::UnSub(const std::string& uri, const GoJson& content, const GoJson& args)
{
    std::lock_guard<std::mutex> lock(commandMutex);

    auto const request = GoRequest(GoRequestMethod::UnSub, uri, content, args);
    auto transaction = GoTransaction(request);

    transactionQueue.push_back(transaction);

    channel->SendRequest(request);

    return transaction;
}

void GoRestClient::SetSubHandler(const GoNotificationHandler& callback)
{
    std::lock_guard<std::mutex> lock(commandMutex);

    notificationHandler = callback;
}

GoTransaction GoRestClient::StartStream(const std::string& uri, const GoJson& content, const GoJson& args)
{
    std::lock_guard<std::mutex> lock(commandMutex);

    auto const request = GoRequest(GoRequestMethod::StartStream, uri, content, args);
    auto transaction = GoTransaction(request);

    transactionQueue.push_back(transaction);

    channel->SendRequest(request);

    return transaction;
}

GoTransaction GoRestClient::StopStream(const std::string& uri, const GoJson& content, const GoJson& args)
{
    std::lock_guard<std::mutex> lock(commandMutex);

    auto const request = GoRequest(GoRequestMethod::StopStream, uri, content, args);
    auto transaction = GoTransaction(request);

    transactionQueue.push_back(transaction);

    channel->SendRequest(request);

    return transaction;
}

void GoRestClient::SetStreamHandler(const GoStreamHandler& callback)
{
    std::lock_guard<std::mutex> lock(commandMutex);

    streamHandler = callback;
}

void GoRestClient::SetReadErrorHandler(const GoCtrlChannelReadErrorHandler& callback)
{
    std::lock_guard<std::mutex> lock(commandMutex);

    channel->SetReadErrorHandler(callback);
}

void GoRestClient::ResponseHandler(const std::shared_ptr<GoResponse> response)
{
    if (response == nullptr) 
    {
        return;
    }


    try
    {
        if (response->Type() == GoResponseType::Request)
        {
            std::lock_guard<std::mutex> lock(commandMutex);

            if (!transactionQueue.empty())
            {
                transactionQueue.front().OnResponse(std::dynamic_pointer_cast<GoRequestResponse>(response));
                transactionQueue.pop_front();
            }
        }
        else
        {
            // Run in separate detached thread to allow additional requests to be made in the stream / notification handler.
            std::thread callbackThread([=] { 
                try
                {
                    Callback(response);
                }
                catch (const std::exception& e)
                {
                    GoLogExceptionMsg(e, "Error running %s callback thread.", GoResponseType::ToString(response->Type()).c_str());
                }
            });

            callbackThread.detach();
        }
    }
    catch (const std::exception& e)
    {
        GoLogExceptionMsg(e, "Error running %s handler.", GoResponseType::ToString(response->Type()).c_str());
    }
}

void GoRestClient::WriteErrorHandler(k64u requestId, const std::exception& e)
{
    std::lock_guard<std::mutex> lock(commandMutex);

    GoLogException(e);

    std::deque<GoTransaction>::iterator it = transactionQueue.begin();

    while (it != transactionQueue.end())
    {
        GoTransaction& transaction = *it;

        if (transaction.GetRequest().Id() == requestId)
        {
            // Set transaction error.
            transaction.OnError(e);

            // Erase transaction from deque.
            transactionQueue.erase(it);

            break;
        }

        it++;
    }
}

void GoRestClient::Callback(std::shared_ptr<GoResponse> response)
{
    // Catch exceptions thrown by application callback handler.
    try
    {
        switch (response->Type())
        {
        case GoResponseType::Notification:
            if (notificationHandler)
            {
                notificationHandler(std::dynamic_pointer_cast<GoNotificationResponse>(response));
            }
            break;
        case GoResponseType::Stream:
            if (streamHandler)
            {
                streamHandler(std::dynamic_pointer_cast<GoStreamResponse>(response));
            }
            break;
        default:
            break;
        }
    }
    catch (const std::exception& e)
    {
        GoLogExceptionMsg(e, "Error running %s handler", GoResponseType::ToString(response->Type()).c_str());

        // Propagate exception up.
        GoRethrow("Rethrow %s exception", GoResponseType::ToString(response->Type()).c_str());
    }
}

}
