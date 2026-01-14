/**
 * @file    GoGdpSurfaceBase.cpp
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#include <GoPxLSdk/GoGdpMsg/GoGdpSurfaceBase.h>
#include <kApi/Data/kArray2.h>

namespace GoPxLSdk
{
    GoGdpSurfaceBase::GoGdpSurfaceBase(MessageType type)
        : GoGdpMsg(type) 
    {
    }

    void GoGdpSurfaceBase::Deserialize(kSerializer serializer)
    {
        bool needEndRead = false;

        GoGdpMsg::Deserialize(serializer);

        try
        {
            GoTest(kSerializer_BeginRead(serializer, kTypeOf(k16u), kTRUE));
            needEndRead = true;

            GoTest(kSerializer_Read32u(serializer, &length));
            GoTest(kSerializer_Read32u(serializer, &width));
            GoTest(kSerializer_Read32u(serializer, &intensityLength));
            GoTest(kSerializer_Read32u(serializer, &intensityWidth));

            GoTest(kSerializer_Read64f(serializer, &resolution.x));
            GoTest(kSerializer_Read64f(serializer, &resolution.y));
            GoTest(kSerializer_Read64f(serializer, &resolution.z));

            GoTest(kSerializer_Read64f(serializer, &offset.x));
            GoTest(kSerializer_Read64f(serializer, &offset.y));
            GoTest(kSerializer_Read64f(serializer, &offset.z));

            GoTest(kSerializer_Read32u(serializer, &surfaceId));
            GoTest(kSerializer_Read32f(serializer, &exposure));

            // IMPORTANT!!!
            // Surface attributes serializer read section is still open here.
            // This is because the derived classes (ie. surface point cloud) has additional attributes
            // to read, so keep the section open for the derived class to finish reading
            // their specific attributes.
            //
            // The read section must be closed by the derived classes.
        }
        catch (const Go::Exception&)
        {
            // Close read section on error.
            if (needEndRead)
            {
                kSerializer_EndRead(serializer);
            }
            GoRethrow("Failed to deserialize Surface Base GDP message.");
        }
    }

    const k32u GoGdpSurfaceBase::Length() const
    {
        return length;
    }

    const k32u GoGdpSurfaceBase::Width() const
    {
        return width;
    }

    const k32u GoGdpSurfaceBase::IntensityLength() const
    {
        return intensityLength;
    }

    const k32u GoGdpSurfaceBase::IntensityWidth() const
    {
        return intensityWidth;
    }

    const kPoint3d64f GoGdpSurfaceBase::Resolution() const
    {
        return resolution;
    }

    const kPoint3d64f GoGdpSurfaceBase::Offset() const
    {
        return offset;
    }

    const k32u GoGdpSurfaceBase::SurfaceId() const
    {
        return surfaceId;
    }

    const k32f GoGdpSurfaceBase::Exposure() const
    {
        return exposure;
    }
}
