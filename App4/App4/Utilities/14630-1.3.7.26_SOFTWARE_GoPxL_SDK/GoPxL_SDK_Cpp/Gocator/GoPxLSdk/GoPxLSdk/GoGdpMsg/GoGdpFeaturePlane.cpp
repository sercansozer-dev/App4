/**
 * @file    GoGdpFeaturePlane.cpp
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#include <GoPxLSdk/GoGdpMsg/GoGdpFeaturePlane.h>

namespace GoPxLSdk
{
    GoGdpFeaturePlane::GoGdpFeaturePlane()
        : GoGdpMsg(MessageType::PLANE_FEATURE) {}

    void GoGdpFeaturePlane::Deserialize(kSerializer serializer)
    {
        GoGdpMsg::Deserialize(serializer);

        try
        {
            GoTest(kSerializer_Read64f(serializer, &normal.x));
            GoTest(kSerializer_Read64f(serializer, &normal.y));
            GoTest(kSerializer_Read64f(serializer, &normal.z));

            GoTest(kSerializer_Read64f(serializer, &distanceToOrigin));
        }
        catch (const Go::Exception&)
        {
            GoRethrow("Failed to deserialize Feature Plane GDP message.");
        }
    }

    const k64f GoGdpFeaturePlane::NormalX() const
    {
        return normal.x;
    }

    const k64f GoGdpFeaturePlane::NormalY() const
    {
        return normal.y;
    }

    const k64f GoGdpFeaturePlane::NormalZ() const
    {
        return normal.z;
    }

    const k64f GoGdpFeaturePlane::DistanceToOrigin() const
    {
        return distanceToOrigin;
    }
}