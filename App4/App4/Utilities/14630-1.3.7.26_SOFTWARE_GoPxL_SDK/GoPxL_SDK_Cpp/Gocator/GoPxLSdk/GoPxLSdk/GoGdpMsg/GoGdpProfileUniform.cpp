/**
 * @file    GoGdpProfileUniform.cpp
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#include <GoPxLSdk/GoGdpMsg/GoGdpProfileUniform.h>
#include <kApi/Data/kArray1.h>

namespace GoPxLSdk
{
    GoGdpProfileUniform::GoGdpProfileUniform()
        : GoGdpProfileBase(MessageType::UNIFORM_PROFILE)
    {
        ranges.Reset();
        intensities.Reset();
    }

    void GoGdpProfileUniform::Deserialize(kSerializer serializer)
    {
        GoGdpProfileBase::Deserialize(serializer);

        try
        {
            // GOS-12890 : Check whether the current section has at least the number of bytes left to be deserialized for range data.
            k64u bytesRemaining = kSerializer_ReadBytesLeft(serializer);
            k64u bytesNeeded = Width() * kType_Size(kTypeOf(k16s));

            GoThrowMsgIf(bytesRemaining < bytesNeeded, 
                kERROR_INCOMPLETE,
                "Insufficient bytes left in the current section to deserialize uniform profile range data. %llu bytes are needed, but only %llu bytes are left.",
                bytesNeeded, bytesRemaining);

            GoTest(kArray1_Construct(ranges.Ref(), kTypeOf(k16s), Width(), kAlloc_App()));
            GoTest(kSerializer_Read16sArray(serializer, kArray1_DataT(ranges, k16s), Width()));

            // GOS-12890 : Optionally check whether the current section has at least the number of bytes left to be deserialized for intensity data.
            if (IntensityWidth() > 0)
            {
                bytesRemaining = kSerializer_ReadBytesLeft(serializer);
                bytesNeeded = IntensityWidth() * kType_Size(kTypeOf(k8u));

                GoThrowMsgIf(bytesRemaining < bytesNeeded, 
                    kERROR_INCOMPLETE,
                    "Insufficient bytes left in the current section to deserialize profile uniform intensity data. %llu bytes are needed, but only %llu bytes are left.",
                    bytesNeeded, bytesRemaining);

                GoTest(kArray1_Construct(intensities.Ref(), kTypeOf(k8u), IntensityWidth(), kAlloc_App()));
                GoTest(kSerializer_Read8uArray(serializer, kArray1_DataT(intensities, k8u), IntensityWidth()));
            }

        }
        catch (const Go::Exception&)
        {
            GoRethrow("Failed to deserialize Profile Uniform GDP message.");
        }
    }

    const kArray1 GoGdpProfileUniform::Ranges() const
    {
        return ranges.Get();
    }

    const kArray1 GoGdpProfileUniform::Intensities() const
    {
        return intensities.Get();
    }
}