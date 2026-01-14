/**
 * @file    GoGdpStamp.cpp
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#include <GoPxLSdk/GoGdpMsg/GoGdpStamp.h>

namespace GoPxLSdk
{
    GoGdpStamp::GoGdpStamp()
        : GoGdpMsg(MessageType::STAMP) {}

    void GoGdpStamp::Deserialize(kSerializer serializer)
    {
        bool needEndRead = false;

        GoGdpMsg::Deserialize(serializer);

        try
        {
            GoTest(kSerializer_BeginRead(serializer, kTypeOf(k16u), kTRUE));
            needEndRead = true;

            GoTest(kSerializer_Read64u(serializer, &frameIndex));
            GoTest(kSerializer_Read64u(serializer, &timestamp));
            GoTest(kSerializer_Read64s(serializer, &encoder));
            GoTest(kSerializer_Read64s(serializer, &encoderAtZ));
            GoTest(kSerializer_Read64u(serializer, &status));
            GoTest(kSerializer_Read64u(serializer, &systemTimeSeconds));
            GoTest(kSerializer_Read64u(serializer, &systemTimeNanoseconds));

            GoTest(kSerializer_EndRead(serializer));
            needEndRead = false;
        }
        catch (const Go::Exception&)
        {
            if (needEndRead)
            {
                kSerializer_EndRead(serializer);
            }
            GoRethrow("Failed to deserialize Stamp GDP message.");
        }
    }

    const k64u GoGdpStamp::FrameIndex() const
    {
        return frameIndex;
    }

    const k64u GoGdpStamp::Timestamp() const
    {
        return timestamp;
    }

    const k64s GoGdpStamp::Encoder() const
    {
        return encoder;
    }

    const k64s GoGdpStamp::EncoderAtZ() const
    {
        return encoderAtZ;
    }

    const k64u GoGdpStamp::Status() const
    {
        return status;
    }

    const k64u GoGdpStamp::SystemTimeSeconds() const
    {
        return systemTimeSeconds;
    }

    const k64u GoGdpStamp::SystemTimeNanoseconds() const
    {
        return systemTimeNanoseconds;
    }
}