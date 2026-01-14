/**
 * @file    GoResponse.h
 * @brief   Declares the GoPxLSdk.GoResponse class.
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#ifndef GO_PXL_SDK_GORESPONSE_H
#define GO_PXL_SDK_GORESPONSE_H

#include <kApi/kApiDef.h>

#include <GoPxLSdk/Def.h>
#include <GoPxLSdk/GoJson.h>
#include <GoPxLSdk/GoResponseType.h>
#include <GoPxLSdk/GoNotificationType.h>

class GoStreamResponseTests;

namespace GoPxLSdk
{

class GoPxLSdkClass GoResponse
{
public:
    /**
     * Constructs GoResponse.
     */
    GoResponse() = default;

    virtual ~GoResponse() = default;

    /**
     * Constructs GoResponse.
     *
     * @public                 @memberof GoResponse
     * @version                Introduced in 0.2.1.53
     * @param type             The type of this JSON message.
     * @param fullmsg          The JSON message.
     */
    GoResponse(GoResponseType type, const GoJson& fullmsg);

    /**
     * Gets the status of the response message.
     *
     * @public                 @memberof GoResponse
     * @version                Introduced in 1.0.109.191
     * @return                 Response message status value.
     */
    virtual kStatus Status() const;

    /**
     * Gets the type of the response message.
     *
     * @public                 @memberof GoResponse
     * @version                Introduced in 0.2.1.53
     * @return                 Type of response message.
     */
    virtual GoResponseType Type() const;

    /**
     * Gets the path of the resource that is associated with the message.
     *
     * @public                 @memberof GoResponse
     * @version                Introduced in 1.0.109.191
     * @return                 Resource path that corresponding to the message.
     */
    virtual const std::string& Path() const;

    /**
     * Gets the response payload field content of the message.
     *
     * @public                 @memberof GoResponse
     * @version                Introduced in 1.0.109.191
     * @return                 Payload field of the message.
     */
    virtual const GoJson& Payload() const;

    /**
     * Gets the full JSON message, including fields such as type, payload and status.
     *
     * @public                 @memberof GoResponse
     * @version                Introduced in 0.2.1.53
     * @return                 The JSON message.
     */
    virtual const GoJson& Raw() const;

private:
    kStatus status = kOK;
    GoResponseType type = GoResponseType::Request;
    std::string path;
    GoJson payload;
    GoJson raw;
};


/**
 * Represents a response to requests such as Read/Update.
 */
class GoPxLSdkClass GoRequestResponse final : public GoResponse
{
public:
    /**
     * Constructs GoRequestResponse.
     *
     * @public                 @memberof GoRequestResponse
     * @version                Introduced in 0.2.1.53
     */
    GoRequestResponse() = default;
    ~GoRequestResponse() = default;

    /**
     * Constructs GoRequestResponse.
     *
     * @public                 @memberof GoRequestResponse
     * @version                Introduced in 0.2.1.53
     * @param fullmsg          Full JSON message.
     */
    GoRequestResponse(const GoJson& fullmsg);
};


/**
 * Represents a response to change notifications.
 */
class GoPxLSdkClass GoNotificationResponse final : public GoResponse
{
public:
    /**
     * Constructs GoNotificationResponse.
     *
     * @public                 @memberof GoNotificationResponse
     * @version                Introduced in 0.2.1.53
     */
    GoNotificationResponse() = default;
    ~GoNotificationResponse() = default;

    /**
     * Constructs GoNotificationResponse.
     *
     * @public                 @memberof GoNotificationResponse
     * @version                Introduced in 1.0.109.191
     * @param fullmsg          The JSON message.
     */
    GoNotificationResponse(const GoJson& fullmsg);

    /**
     * Gets the type of notification change from the resource that generated this notification.
     *
     * @public                 @memberof GoNotificationResponse
     * @version                Introduced in 0.2.1.53
     * @return                 Type of notification change that generated this notification.
     * @throws Go::exception   If the key "eventType" is not found. 
     */
    GoNotificationType NotificationType() const;
};


/**
 * Represents streamed data.
 */
class GoPxLSdkClass GoStreamResponse final : public GoResponse
{
    friend class ::GoStreamResponseTests;

// Public declaration.
public:
    using StreamId = k32u;

public:
    /**
     * Constructs GoStreamResponse.
     *
     * @public                 @memberof GoStreamResponse
     * @version                Introduced in 0.2.1.53.
     */
    GoStreamResponse() = default;
    ~GoStreamResponse() = default;

    /**
     * Constructs GoStreamResponse.
     *
     * @public                 @memberof GoStreamResponse
     * @version                Introduced in 0.2.1.53.
     * @param fullmsg          The JSON message.
     */
    GoStreamResponse(const GoJson& fullmsg);

    /**
     * Gets the identifier of the data stream.
     *
     * @public                 @memberof GoNotificationResponse
     * @version                Introduced in 1.0.109.191.
     * @return                 Stream identifier that carried this stream message.
     * @throws Go::Exception   If the key "streamId" is not found.
     */
    GoStreamResponse::StreamId StreamIdentifier() const;

    /**
     * Gets the stream's current status.
     *
     * @public                 @memberof GoNotificationResponse
     * @version                Introduced in 1.0.109.191.
     * @return                 Stream status string of the stream message.
     * @throws Go::Exception   If the key "streamStatus" is not found.
     */
    const std::string StreamStatus() const;
};

}

#endif