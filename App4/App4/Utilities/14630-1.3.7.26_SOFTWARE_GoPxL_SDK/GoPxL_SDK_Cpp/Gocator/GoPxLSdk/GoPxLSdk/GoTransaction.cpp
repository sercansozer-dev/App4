/**
 * @file    GoTransaction.cpp
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#include <GoPxLSdk/Def.h>
#include <GoPxLSdk/GoTransaction.h>

#include <GoPxLSdk/GoRequestError.h>
#include <GoPxLSdk/GoChannelError.h>

#include <GoApi/GoApi.h>

namespace GoPxLSdk
{

GoTransaction::GoTransaction(const GoRequest& request) : request(request)
{
    promise = std::make_shared<std::promise<GoRequestResponse>>();
    futureResponse = promise->get_future().share();
}

void GoTransaction::CheckResponse(k64u timeoutInMilliseconds)
{
    std::future_status const status = futureResponse.wait_for(std::chrono::microseconds(timeoutInMilliseconds * GO_PXL_SDK_MILLISECONDS_TO_MICROSECONDS_CONVERSION));
    if (status == std::future_status::timeout)
    {
        throw GoChannelError("The request timed out.");
    }

    GoRequestResponse response;

    try
    {
        // It will throw if an exception was set by ::set_exception.
        response = futureResponse.get();
    }
    catch (const std::exception& e)
    {
        // Wrap std::exception with GoChannelError.
        throw GoChannelError(e.what());
    }

    if (response.Status() != kOK)
    {
        throw GoRequestError(response);
    }
}

const GoRequestResponse& GoTransaction::GetResponse(k64u timeoutInMilliseconds)
{
    CheckResponse(timeoutInMilliseconds);
    return futureResponse.get();
}

const GoRequest& GoTransaction::GetRequest()
{
    return request;
}

void GoTransaction::OnResponse(const std::shared_ptr<GoRequestResponse>& response)
{
    promise->set_value(*response.get());
}

void GoTransaction::OnError(const std::exception& e)
{
    promise->set_exception(std::make_exception_ptr(e));
}

std::shared_future<GoRequestResponse>& GoTransaction::GetResponseFuture()
{
    return futureResponse;
}

}
