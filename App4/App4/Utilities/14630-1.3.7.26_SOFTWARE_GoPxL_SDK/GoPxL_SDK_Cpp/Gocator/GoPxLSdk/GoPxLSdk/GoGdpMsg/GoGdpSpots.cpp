/**
 * @file    GoGdpSpots.cpp
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#include <GoPxLSdk/GoGdpMsg/GoGdpSpots.h>
#include <kApi/Data/kArray1.h>

namespace GoPxLSdk
{
    GoGdpSpots::GoGdpSpots()
        : GoGdpMsg(MessageType::SPOTS)
    {
        spots.clear();
    }

    void GoGdpSpots::Deserialize(kSerializer serializer)
    {
        GdpSpot spot;
        k8u byteValue;
        bool needEndRead = false;

        GoGdpMsg::Deserialize(serializer);

        try
        {
            GoTest(kSerializer_BeginRead(serializer, kTypeOf(k16u), kTRUE));
            needEndRead = true;

            GoTest(kSerializer_Read32u(serializer, &pointCount));
            GoTest(kSerializer_Read32f(serializer, &exposure));
            GoTest(kSerializer_Read8u(serializer, &byteValue));
            columnBased = (byteValue > 0);
            GoTest(kSerializer_Read32f(serializer, &sliceScale));
            GoTest(kSerializer_Read32f(serializer, &sliceOffset));
            GoTest(kSerializer_Read32f(serializer, &centerScale));
            GoTest(kSerializer_Read32f(serializer, &centerOffset));
            GoTest(kSerializer_Read32u(serializer, &maxSliceCount));
            GoTest(kSerializer_Read32u(serializer, &spotCenterMin));
            GoTest(kSerializer_Read32u(serializer, &spotCenterMax));

            GoTest(kSerializer_EndRead(serializer));
            needEndRead = false;

            // GOS-12890 : Check whether the current section has at least the number of bytes left to be deserialized for spot data.
            k64u bytesRemaining = kSerializer_ReadBytesLeft(serializer);
            k64u bytesNeeded = pointCount * (kType_Size(kTypeOf(k16u)) + kType_Size(kTypeOf(k32u)));

            GoThrowMsgIf(bytesRemaining < bytesNeeded,
                kERROR_INCOMPLETE,
                "Insufficient bytes left in the current section to deserialize spot data. %llu bytes are needed, but only %llu bytes are left.",
                bytesNeeded, bytesRemaining);

            for (kSize i = 0; i < pointCount; i++)
            {
                GoTest(kSerializer_Read16u(serializer, &spot.slice));
                GoTest(kSerializer_Read32u(serializer, &spot.centre));
                spots.push_back(spot);
            }

            
        }
        catch (const Go::Exception&)
        {
            if (needEndRead)
            {
                kSerializer_EndRead(serializer);
            }
            GoRethrow("Failed to deserialize Spots GDP message.");
        }
    }

    const k32u GoGdpSpots::PointCount() const
    {
        return pointCount;
    }

    const k32f GoGdpSpots::Exposure() const
    {
        return exposure;
    }

    const bool GoGdpSpots::ColumnBased() const
    {
        return columnBased;
    }

    const k32f GoGdpSpots::SliceScale() const
    {
        return sliceScale;
    }

    const k32f GoGdpSpots::SliceOffset() const
    {
        return sliceOffset;
    }

    const k32f GoGdpSpots::CenterScale() const
    {
        return centerScale;
    }

    const k32f GoGdpSpots::CenterOffset() const
    {
        return centerOffset;
    }

    const k32u GoGdpSpots::MaxSliceCount() const
    {
        return maxSliceCount;
    }

    const k32u GoGdpSpots::SpotCenterMin() const
    {
        return spotCenterMin;
    }

    const k32u GoGdpSpots::SpotCenterMax() const
    {
        return spotCenterMax;
    }

    const std::vector<GdpSpot>& GoGdpSpots::Spots() const
    {
        return spots;
    }

}
