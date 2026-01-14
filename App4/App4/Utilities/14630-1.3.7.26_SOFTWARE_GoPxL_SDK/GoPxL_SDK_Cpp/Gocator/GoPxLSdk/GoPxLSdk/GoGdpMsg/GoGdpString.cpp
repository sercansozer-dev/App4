/**
 * @file    GoGdpString.cpp
 *
 * @internal
 * Copyright (C) 2024-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#include <GoPxLSdk/GoGdpMsg/GoGdpString.h>

namespace GoPxLSdk
{
    GoGdpString::GoGdpString()
        : GoGdpMsg(MessageType::STRING) {}

    void GoGdpString::Deserialize(kSerializer serializer)
    {
        GoGdpMsg::Deserialize(serializer);
        k32u strLen;

        try
        {
            GoTest(kSerializer_Read32u(serializer, &strLen));
            ReadText(serializer, strLen, str);
        }
        catch (const Go::Exception&)
        {
            GoRethrow("Failed to deserialize string GDP message.");
        }
    }

    const std::string& GoGdpString::String() const
    {
        return str;
    }
}
