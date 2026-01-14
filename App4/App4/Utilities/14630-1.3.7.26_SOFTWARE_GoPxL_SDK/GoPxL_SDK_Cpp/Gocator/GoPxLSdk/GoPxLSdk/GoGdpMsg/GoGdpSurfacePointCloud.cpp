/**
 * @file    GoGdpSurfacePointCloud.cpp
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#include <GoPxLSdk/GoGdpMsg/GoGdpSurfacePointCloud.h>
#include <kApi/Data/kArray2.h>

namespace GoPxLSdk
{
    GoGdpSurfacePointCloud::GoGdpSurfacePointCloud()
        : GoGdpSurfaceBase(MessageType::SURFACE_POINT_CLOUD)
    {
        ranges.Reset();
        intensities.Reset();
    }

    void GoGdpSurfacePointCloud::Deserialize(kSerializer serializer)
    {
        bool needEndRead;
        k8u byteValue;

        GoGdpSurfaceBase::Deserialize(serializer);

        // Surface attributes deserialization succeeded, which means the read
        // section is still open.
        needEndRead = true;

        try
        {
            // Finish reading the surface attributes here.
            // This surface attribute is only sent for surface point clouds.
            // It is not applicable to uniform surfaces.
            GoTest(kSerializer_Read8u(serializer, (k8u*) &byteValue));
            isAdjacent = (byteValue > 0);
            // This closes the surface attributes read section.
            GoTest(kSerializer_EndRead(serializer));
            needEndRead = false;

            // GOS-12890 : Check whether the current section has at least the number of bytes left to be deserialized for range data.
            k64u bytesRemaining = kSerializer_ReadBytesLeft(serializer);
            k64u bytesNeeded = Length() * Width() * kType_Size(kTypeOf(kPoint3d16s));

            GoThrowMsgIf(bytesRemaining < bytesNeeded, 
                kERROR_INCOMPLETE,
                "Insufficient bytes left in the current section to deserialize surface point cloud range data. %llu bytes are needed, but only %llu bytes are left.",
                bytesNeeded, bytesRemaining);

            GoTest(kArray2_Construct(ranges.Ref(), kTypeOf(kPoint3d16s), Length(), Width(), kAlloc_App()));
            GoTest(kSerializer_Read16sArray(serializer, (k16s*)kArray2_DataT(ranges, kPoint3d16s), Length() * Width() * 3));

            kSize intensitySize = IntensityLength() * IntensityWidth();

            // GOS-12890 : Optionally check whether the current section has at least the number of bytes left to be deserialized for intensity data.
            if (intensitySize > 0)
            {
                bytesRemaining = kSerializer_ReadBytesLeft(serializer);
                bytesNeeded = intensitySize * kType_Size(kTypeOf(k8u));

                GoThrowMsgIf(bytesRemaining < bytesNeeded, 
                    kERROR_INCOMPLETE,
                    "Insufficient bytes left in the current section to deserialize surface point cloud intensity data. %llu bytes are needed, but only %llu bytes are left.",
                    bytesNeeded, bytesRemaining);

                GoTest(kArray2_Construct(intensities.Ref(), kTypeOf(k8u), IntensityLength(), IntensityWidth(), kAlloc_App()));
                GoTest(kSerializer_Read8uArray(serializer, kArray2_DataT(intensities, k8u), kArray2_Count(intensities)));
            }
        }
        catch (const Go::Exception&)
        {
            // This closes the surface attributes read section if there was an error
            // reading an attribute from the section.
            if (needEndRead)
            {
                kSerializer_EndRead(serializer);
            }

            GoRethrow("Failed to deserialize Surface Point Cloud GDP message.");
        }
    }

    const bool GoGdpSurfacePointCloud::IsAdjacent() const
    {
        return isAdjacent;
    }

    const kArray2 GoGdpSurfacePointCloud::Ranges() const
    {
        return ranges.Get();
    }

    const kArray2 GoGdpSurfacePointCloud::Intensities() const
    {
        return intensities.Get();
    }
}
