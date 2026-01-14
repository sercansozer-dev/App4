/**
 * @file    GoGdpProfileBase.cpp
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#include <GoPxLSdk/GoGdpMsg/GoGdpProfileBase.h>
#include <kApi/Data/kArray1.h>

namespace GoPxLSdk
{
    GoGdpProfileBase::GoGdpProfileBase(MessageType type)
        : GoGdpMsg(type) 
    {
    }

    void GoGdpProfileBase::Deserialize(kSerializer serializer)
    {
        bool needEndRead = false;

        GoGdpMsg::Deserialize(serializer);

        try
        {
            GoTest(kSerializer_BeginRead(serializer, kTypeOf(k16u), kTRUE));
            needEndRead = true;

            GoTest(kSerializer_Read32u(serializer, &width));
            GoTest(kSerializer_Read32u(serializer, &intensityWidth));

            GoTest(kSerializer_Read64f(serializer, &resolution.x));
            GoTest(kSerializer_Read64f(serializer, &resolution.z));

            GoTest(kSerializer_Read64f(serializer, &offset.x));
            GoTest(kSerializer_Read64f(serializer, &offset.z));

            GoTest(kSerializer_Read32f(serializer, &exposure));

            GoTest(kSerializer_EndRead(serializer));
            needEndRead = false;
        }
        catch (const Go::Exception&)
        {
            if (needEndRead)
            {
                kSerializer_EndRead(serializer);
            }
            GoRethrow("Failed to deserialize Profile Base GDP message.");
        }
    }

    const k32u GoGdpProfileBase::Width() const
    {
        return width;
    }

    const k32u GoGdpProfileBase::IntensityWidth() const
    {
        return intensityWidth;
    }

    const kPoint3d64f GoGdpProfileBase::Resolution() const
    {
        return resolution;
    }

    const kPoint3d64f GoGdpProfileBase::Offset() const
    {
        return offset;
    }

    const k32f GoGdpProfileBase::Exposure() const
    {
        return exposure;
    }
}