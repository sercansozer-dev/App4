/**
 * @file    GoGdpFeatureCircle.cpp
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#include <GoPxLSdk/GoGdpMsg/GoGdpFeatureCircle.h>

namespace GoPxLSdk
{
    GoGdpFeatureCircle::GoGdpFeatureCircle()
        : GoGdpMsg(MessageType::CIRCLE_FEATURE) {}

    void GoGdpFeatureCircle::Deserialize(kSerializer serializer)
    {
        GoGdpMsg::Deserialize(serializer);

        try
        {
            GoTest(kSerializer_Read64f(serializer, &center.x));
            GoTest(kSerializer_Read64f(serializer, &center.y));
            GoTest(kSerializer_Read64f(serializer, &center.z));

            GoTest(kSerializer_Read64f(serializer, &normal.x));
            GoTest(kSerializer_Read64f(serializer, &normal.y));
            GoTest(kSerializer_Read64f(serializer, &normal.z));

            GoTest(kSerializer_Read64f(serializer, &radius));
        }
        catch (const Go::Exception&)
        {
            GoRethrow("Failed to deserialize Feature Circle GDP message.");
        }
    }

    const k64f GoGdpFeatureCircle::CenterX() const
    {
        return center.x;
    }

    const k64f GoGdpFeatureCircle::CenterY() const
    {
        return center.y;
    }

    const k64f GoGdpFeatureCircle::CenterZ() const
    {
        return center.z;
    }

    const k64f GoGdpFeatureCircle::NormalX() const
    {
        return normal.x;
    }

    const k64f GoGdpFeatureCircle::NormalY() const
    {
        return normal.y;
    }

    const k64f GoGdpFeatureCircle::NormalZ() const
    {
        return normal.z;
    }

    const k64f GoGdpFeatureCircle::Radius() const
    {
        return radius;
    }
}