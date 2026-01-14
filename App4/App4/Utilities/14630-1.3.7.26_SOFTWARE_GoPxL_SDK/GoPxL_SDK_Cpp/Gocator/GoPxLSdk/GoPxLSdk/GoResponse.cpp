/**
 * @file    GoResponse.cpp
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#include <GoPxLSdk/Def.h>
#include <GoPxLSdk/GoResponse.h>
#include <GoApi/Exception.h>

namespace GoPxLSdk
{

GoResponse::GoResponse(GoResponseType type, const GoJson& fullmsg)
{
    this->type = type;
    this->raw = fullmsg;

    if (this->raw.HasKey("status"))
    {
        this->status = this->raw.Get<kStatus>("status");
    }

    if (this->raw.HasKey("payload"))
    {
        this->payload = this->raw.At("payload");
    }

    if (this->raw.HasKey("path"))
    {
        this->path = this->raw.Get<std::string>("path");
    }
}

kStatus GoResponse::Status() const
{
    return this->status;
}

GoResponseType GoResponse::Type() const
{
    return this->type;
}

const std::string& GoResponse::Path() const
{
    return this->path;
}

const GoJson& GoResponse::Payload() const
{
    return this->payload;
}

const GoJson& GoResponse::Raw() const
{
    return this->raw;
}

GoRequestResponse::GoRequestResponse(const GoJson& fullmsg)
    : GoResponse(GoResponseType::Request, fullmsg)
{
}

GoNotificationResponse::GoNotificationResponse(const GoJson& fullmsg) :
    GoResponse(GoResponseType::Notification, fullmsg)
{
}

GoNotificationType GoNotificationResponse::NotificationType() const
{
    GoThrowIf(!(Raw().HasKey("eventType")), kERROR_NOT_FOUND);

    return GoNotificationType::FromString(Raw().Get<std::string>("eventType"));
}

GoStreamResponse::GoStreamResponse(const GoJson& fullmsg) :
    GoResponse(GoResponseType::Stream, fullmsg)
{
}

GoStreamResponse::StreamId GoStreamResponse::StreamIdentifier() const
{
    GoThrowIf(!(Raw().HasKey("streamId")), kERROR_NOT_FOUND);

    return Raw().Get<StreamId>("streamId");
}

const std::string GoStreamResponse::StreamStatus() const
{
    GoThrowIf(!(Raw().HasKey("streamStatus")), kERROR_NOT_FOUND);

    return Raw().Get<std::string>("streamStatus");
}

}
