/**
 * @file    GoGdpFeaturePoint.cpp
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#include <GoPxLSdk/GoGdpMsg/GoGdpFeaturePoint.h>

namespace GoPxLSdk
{
    GoGdpFeaturePoint::GoGdpFeaturePoint()
        : GoGdpMsg(MessageType::POINT_FEATURE) {}
    

    void GoGdpFeaturePoint::Deserialize(kSerializer serializer)
    {
        GoGdpMsg::Deserialize(serializer);

        try
        {
            GoTest(kSerializer_Read64f(serializer, &point.x));
            GoTest(kSerializer_Read64f(serializer, &point.y));
            GoTest(kSerializer_Read64f(serializer, &point.z));
        }
        catch (const Go::Exception&)
        {
            GoRethrow("Failed to deserialize Feature Point GDP message.");
        }
    }

    const k64f GoGdpFeaturePoint::PositionX() const
    {
        return point.x;
    }

    const k64f GoGdpFeaturePoint::PositionY() const
    {
        return point.y;
    }

    const k64f GoGdpFeaturePoint::PositionZ() const
    {
        return point.z;
    }

}
