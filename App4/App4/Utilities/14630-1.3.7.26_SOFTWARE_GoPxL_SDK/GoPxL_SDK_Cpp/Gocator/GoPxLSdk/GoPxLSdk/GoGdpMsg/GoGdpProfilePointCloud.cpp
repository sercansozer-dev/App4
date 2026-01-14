/**
 * @file    GoGdpProfilePointCloud.cpp
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#include <GoPxLSdk/GoGdpMsg/GoGdpProfilePointCloud.h>
#include <kApi/Data/kArray1.h>

namespace GoPxLSdk
{
    GoGdpProfilePointCloud::GoGdpProfilePointCloud()
        : GoGdpProfileBase(MessageType::PROFILE_POINT_CLOUD)
    {
        ranges.Reset();
        intensities.Reset();
    }

    void GoGdpProfilePointCloud::Deserialize(kSerializer serializer)
    {
        const kPoint16s* points;

        GoGdpProfileBase::Deserialize(serializer);

        try
        {
            // GOS-12890 : Check whether the current section has at least the number of bytes left to be deserialized for range data.
            k64u bytesRemaining = kSerializer_ReadBytesLeft(serializer);
            k64u bytesNeeded = Width() * kType_Size(kTypeOf(kPoint16s));

            GoThrowMsgIf(bytesRemaining < bytesNeeded, 
                kERROR_INCOMPLETE,
                "Insufficient bytes left in the current section to deserialize profile point cloud range data. %llu bytes are needed, but only %llu bytes are left.",
                bytesNeeded, bytesRemaining);

            GoTest(kArray1_Construct(ranges.Ref(), kTypeOf(kPoint16s), Width(), kAlloc_App()));
            points = kArray1_DataT(ranges, kPoint16s);

            // Treat the kPoint16s array as an array of 16s.
            GoTest(kSerializer_Read16sArray(serializer, (k16s*) points, Width() * 2));

            // GOS-12890 : Optionally check whether the current section has at least the number of bytes left to be deserialized for intensity data.
            if (IntensityWidth() > 0)
            {
                bytesRemaining = kSerializer_ReadBytesLeft(serializer);
                bytesNeeded = IntensityWidth() * kType_Size(kTypeOf(k8u));

                GoThrowMsgIf(bytesRemaining < bytesNeeded,
                    kERROR_INCOMPLETE,
                    "Insufficient bytes left in the current section to deserialize profile point cloud intensity data. %llu bytes are needed, but only %llu bytes are left.",
                    bytesNeeded, bytesRemaining);

                GoTest(kArray1_Construct(intensities.Ref(), kTypeOf(k8u), IntensityWidth(), kAlloc_App()));
                GoTest(kSerializer_Read8uArray(serializer, kArray1_DataT(intensities, k8u), IntensityWidth()));
            }
        }
        catch (const Go::Exception&)
        {
            GoRethrow("Failed to deserialize Profile Point Cloud GDP message.");
        }
    }

    const kArray1 GoGdpProfilePointCloud::Ranges() const
    {
        return ranges.Get();
    }

    const kArray1 GoGdpProfilePointCloud::Intensities() const
    {
        return intensities.Get();
    }
}