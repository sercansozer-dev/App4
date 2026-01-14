/**
 * @file    GoResponseType.cpp
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#include <GoPxLSdk/Def.h>
#include <GoPxLSdk/GoResponseType.h>

namespace GoPxLSdk
{

std::string GoResponseType::ToString(Type type)
{
    switch (type)
    {
        case Type::Request: return "response";
        case Type::Notification: return "notification";
        case Type::Stream: return "stream";
        default: return "undefined";
    }
}

GoResponseType GoResponseType::FromString(const std::string& type)
{
    if (type == "response")
    {
        return Type::Request;
    }
    else if (type == "notification")
    {
        return Type::Notification;
    }
    else if (type == "stream")
    {
        return Type::Stream;
    }

    return Type::Request;
}

}