/**
 * @file    GoGdpSpots.h
 * @brief   Declares the GoPxLSdk.GoGdpSpots class.
 * 
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#ifndef GO_PXL_SDK_GOGDPSPOTS_H
#define GO_PXL_SDK_GOGDPSPOTS_H

#include <GoPxLSdk/GoGdpMsg/GoGdpMsg.h>

class GoGdpMsgTests;

typedef struct GdpSpot
{
    k16u slice;
    k32u centre;
}GdpSpot;

namespace GoPxLSdk
{
    class GoPxLSdkClass GoGdpSpots : public GoGdpMsg
    {
    public:
        /**
        * Constructs GoGdpSpots.
        *
        * @public                @memberof GoGdpSpots
        * @version               Introduced in 0.2.1.53.
        */
        GoGdpSpots();
        ~GoGdpSpots() = default;

        /**
        * Deserialize spots message.
        *.-
        * @public                @memberof GoGdpSpots
        * @version               Introduced in 0.2.1.53.
        * @param serializer      The serializer to read.
        * @throws Go::Exception  If failed to deserialize spots message.
        */
        void Deserialize(kSerializer serializer) override;

        /**
        * Get the count of points.
        * 
        * @public                @memberof GoGdpSpots
        * @version               Introduced in 0.2.1.53.
        * @return                The number of points.
        */
        const k32u PointCount() const;

        /**
        * Get exposure (ns).
        * 
        * @public                @memberof GoGdpSpots
        * @version               Introduced in 0.2.1.53.
        * @return                The exposure value.
        */
        const k32f Exposure() const;

        /**
        * Get the flag indicating spots are column-based or row based.
        * 
        * @public                @memberof GoGdpSpots
        * @version               Introduced in 0.2.1.53.
        * @return                The flag indicating column-based (true) or row-based (false).
        */
        const bool ColumnBased() const;

        /**
        * Get the scale value to apply to the slice to obtain slice position.
        * 
        * @public                @memberof GoGdpSpots
        * @version               Introduced in 0.2.1.53.
        * @return                The slice scale.
        */
        const k32f SliceScale() const;

        /**
        * Get the offset value to apply to the slice to obtain slice position.
        * 
        * @public                @memberof GoGdpSpots
        * @version               Introduced in 0.2.1.53.
        * @return                The slice offset.
        */
        const k32f SliceOffset() const;

        /**
        * Get the scale value to apply to the center to obtain center position.
        * 
        * @public                @memberof GoGdpSpots
        * @version               Introduced in 0.2.1.53.
        * @return                The center scale.
        */
        const k32f CenterScale() const;

        /**
        * Get the offset value to apply to the center to obtain center position.
        * 
        * @public                @memberof GoGdpSpots
        * @version               Introduced in 0.2.1.53.
        * @return                The center offset.
        */
        const k32f CenterOffset() const;

        /**
        * Get the Max slice count of the spots.
        * 
        * @public                @memberof GoGdpSpots
        * @version               Introduced in 0.3.5.24.
        * @return                The max slice count.
        */
        const k32u MaxSliceCount() const;

        /**
        * Get the minimum spot center value.
        * 
        * @public                @memberof GoGdpSpots
        * @version               Introduced in 0.3.5.24.
        * @return                The center minimum value.
        */
        const k32u SpotCenterMin() const;

        /**
        * Get the maximum spot center value.
        * 
        * @public                @memberof GoGdpSpots
        * @version               Introduced in 0.3.5.24.
        * @return                The center maximum value.
        */
        const k32u SpotCenterMax() const;

        /**
        * Get points representing slice and centre.
        * 
        * @public                @memberof GoGdpSpots
        * @version               Introduced in 0.2.1.53.
        * @return                The spot array reference.
        */
        const std::vector<GdpSpot>& Spots() const;

    private:
        k32u pointCount = 0;
        k32f exposure = 0.0;
        bool columnBased = false;
        k32f sliceScale = 1.0;
        k32f sliceOffset = 0;
        k32f centerScale = 1.0;
        k32f centerOffset = 0;
        k32u maxSliceCount = 0;
        k32u spotCenterMin = 0;
        k32u spotCenterMax = 0;
        std::vector<GdpSpot> spots;

        friend class ::GoGdpMsgTests;
    };
}

#endif
