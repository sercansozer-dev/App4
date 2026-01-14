/**
 * @file    GoResponseType.h
 * @brief   Declares the GoPxLSdk.GoResponseType class.
 * 
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#ifndef GO_PXL_SDK_RESPONSETYPE_H
#define GO_PXL_SDK_RESPONSETYPE_H

namespace GoPxLSdk
{

class GoResponseType
{
public:
    enum Type : k32s
    {
        Request,            ///< A reply to a request (e.g. Read or Update).
        Notification,       ///< A change notification.
        Stream              ///< A streamed message.
    };

    GoResponseType() = default;

    constexpr GoResponseType(Type type) : type(type) { }

    operator Type() const { return type; }
    constexpr bool operator==(GoResponseType t) const { return type == t.type; }
    constexpr bool operator==(Type t) const { return type == t; }
    constexpr bool operator!=(GoResponseType t) const { return type != t.type; }
    constexpr bool operator!=(Type t) const { return type != t; }

    static std::string ToString(Type type);

    static GoResponseType FromString(const std::string& type);

private:
    Type type;
};

}

#endif