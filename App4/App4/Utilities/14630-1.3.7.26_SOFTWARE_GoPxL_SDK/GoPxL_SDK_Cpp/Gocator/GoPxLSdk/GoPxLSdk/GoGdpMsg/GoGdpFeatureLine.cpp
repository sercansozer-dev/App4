/**
 * @file    GoGdpFeatureLine.cpp
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#include <GoPxLSdk/GoGdpMsg/GoGdpFeatureLine.h>

namespace GoPxLSdk
{
    GoGdpFeatureLine::GoGdpFeatureLine()
        : GoGdpMsg(MessageType::LINE_FEATURE) {}

    void GoGdpFeatureLine::Deserialize(kSerializer serializer)
    {
        GoGdpMsg::Deserialize(serializer);

        try
        {
            GoTest(kSerializer_Read64f(serializer, &point.x));
            GoTest(kSerializer_Read64f(serializer, &point.y));
            GoTest(kSerializer_Read64f(serializer, &point.z));

            GoTest(kSerializer_Read64f(serializer, &direction.x));
            GoTest(kSerializer_Read64f(serializer, &direction.y));
            GoTest(kSerializer_Read64f(serializer, &direction.z));
        }
        catch (const Go::Exception&)
        {
            GoRethrow("Failed to deserialize Feature Line GDP message.");
        }
    }

    const k64f GoGdpFeatureLine::PositionX() const
    {
        return point.x;
    }

    const k64f GoGdpFeatureLine::PositionY() const
    {
        return point.y;
    }

    const k64f GoGdpFeatureLine::PositionZ() const
    {
        return point.z;
    }

    const k64f GoGdpFeatureLine::DirectionX() const
    {
        return direction.x;
    }

    const k64f GoGdpFeatureLine::DirectionY() const
    {
        return direction.y;
    }

    const k64f GoGdpFeatureLine::DirectionZ() const
    {
        return direction.z;
    }
}