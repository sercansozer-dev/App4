/**
 * @file    GoGdpSurfaceUniform.cpp
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#include <GoPxLSdk/GoGdpMsg/GoGdpSurfaceUniform.h>
#include <kApi/Data/kArray2.h>

namespace GoPxLSdk
{
    GoGdpSurfaceUniform::GoGdpSurfaceUniform()
        : GoGdpSurfaceBase(MessageType::UNIFORM_SURFACE)
    {
        ranges.Reset();
        intensities.Reset();
    }

    void GoGdpSurfaceUniform::Deserialize(kSerializer serializer)
    {
        GoGdpSurfaceBase::Deserialize(serializer);

        try
        {
            // This closes the surface attributes read section.
            GoTest(kSerializer_EndRead(serializer));

            // GOS-12890 : Check whether the current section has at least the number of bytes left to be deserialized for range data.
            k64u bytesRemaining = kSerializer_ReadBytesLeft(serializer);
            k64u bytesNeeded = Length() * Width() * kType_Size(kTypeOf(k16s));

            GoThrowMsgIf(bytesRemaining < bytesNeeded, 
                kERROR_INCOMPLETE,
                "Insufficient bytes left in the current section to deserialize uniform surface range data. %llu bytes are needed, but only %llu bytes are left.",
                bytesNeeded, bytesRemaining);

            GoTest(kArray2_Construct(ranges.Ref(), kTypeOf(k16s), Length(), Width(), kAlloc_App()));
            GoTest(kSerializer_Read16sArray(serializer, kArray2_DataT(ranges, k16s), Length() * Width()));

            kSize intensitySize = IntensityLength() * IntensityWidth();

            // GOS-12890 : Optionally check whether the current section has at least the number of bytes left to be deserialized for intensity data.
            if (intensitySize > 0)
            {
                bytesRemaining = kSerializer_ReadBytesLeft(serializer);
                bytesNeeded = intensitySize * kType_Size(kTypeOf(k8u));

                GoThrowMsgIf(bytesRemaining < bytesNeeded, 
                    kERROR_INCOMPLETE,
                    "Insufficient bytes left in the current section to deserialize surface uniform intensity data. %llu bytes are needed, but only %llu bytes are left.",
                    bytesNeeded, bytesRemaining);

                GoTest(kArray2_Construct(intensities.Ref(), kTypeOf(k8u), IntensityLength(), IntensityWidth(), kAlloc_App()));
                GoTest(kSerializer_Read8uArray(serializer, kArray2_DataT(intensities, k8u), kArray2_Count(intensities)));
            }
        }
        catch (const Go::Exception&)
        {
            GoRethrow("Failed to deserialize Surface Uniform GDP message.");
        }
    }

    const kArray2 GoGdpSurfaceUniform::Ranges() const
    {
        return ranges.Get();
    }

    const kArray2 GoGdpSurfaceUniform::Intensities() const
    {
        return intensities.Get();
    }
}
