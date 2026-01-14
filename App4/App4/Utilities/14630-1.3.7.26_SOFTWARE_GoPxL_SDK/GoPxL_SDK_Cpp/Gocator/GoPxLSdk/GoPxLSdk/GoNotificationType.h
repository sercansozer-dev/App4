/**
 * @file    GoNotificationType.h
 * @brief   Declares the GoPxLSdk.GoNotificationType class.
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#ifndef GO_PXL_SDK_NOTIFICATIONTYPE_H
#define GO_PXL_SDK_NOTIFICATIONTYPE_H

#include <GoPxLSdk/Def.h>

namespace GoPxLSdk
{

class GoPxLSdkClass GoNotificationType
{
public:
    enum Type : k32s
    {
        Created,        ///< A resource was created.
        Deleted,        ///< A resource was deleted.
        Changed         ///< A resource was updated.
    };

    GoNotificationType() = default;

    constexpr GoNotificationType(Type type) : type(type) { }

    operator Type() const { return type; }
    constexpr bool operator==(GoNotificationType t) const { return type == t.type; }
    constexpr bool operator!=(GoNotificationType t) const { return type != t.type; }

    const std::string ToString() const;

    static GoNotificationType FromString(const std::string& type);

private:
    Type type;
};

}

#endif