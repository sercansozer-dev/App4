/**
 * @file    GoNotificationType.cpp
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#include <GoPxLSdk/GoNotificationType.h>

namespace GoPxLSdk
{

const std::string GoNotificationType::ToString() const
{
    switch (type)
    {
        case Type::Created: return "created";
        case Type::Deleted: return "deleted";
        case Type::Changed: return "changed";
        default: return "undefined";
    }
}

GoNotificationType GoNotificationType::FromString(const std::string& type)
{
    if (type == "created") {
        return Type::Created;
    }
    else if (type == "deleted") {
        return Type::Deleted;
    }

    return Type::Changed;
}

}