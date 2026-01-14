/**
 * @file    GoGdpBoundingBox.h
 * @brief   Declares the GoPxLSdk.GoGdpBoundingBox class.
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#ifndef GO_PXL_SDK_GOBOUNDINGBOX_H
#define GO_PXL_SDK_GOBOUNDINGBOX_H

#include <GoApi/GoApi.h>
#include <kApi/Io/kSerializer.h>
#include <GoPxLSdk/GoGdpMsg/GoGdpMsgDef.h>

class GoGdpMsgTests;

namespace GoPxLSdk
{
    class GoPxLSdkClass GoGdpBoundingBox
    {
    public:
        /**
         * Constructs GoGdpBoundingBox.
         *
         * @public                @memberof GoGdpBoundingBox
         * @version               Introduced in 0.2.1.53.
         */
        GoGdpBoundingBox() = default;

        /**
        * Deserialize the bounding box data.
        *
        * @public                @memberof GoGdpBoundingBox
        * @version               Introduced in 0.2.1.53.
        * @param serializer      The serializer to read.
        * @throws Go::Exception  If failed to deserialize bounding box.
        */
        void Deserialize(kSerializer serializer);

        /**
        * Get X-coordinate of the origin.
        *
        * @public                @memberof GoGdpBoundingBox
        * @version               Introduced in 0.2.1.53.
        * @return                The x-coordinate value of the bounding box origin.
        */
        const k64f X() const;

        /**
        * Get Y-coordinate of the origin.
        *
        * @public                @memberof GoGdpBoundingBox
        * @version               Introduced in 0.2.1.53.
        * @return                The y-coordinate value of the bounding box origin.
        */
        const k64f Y() const;

        /**
        * Get Z-coordinate of the origin.
        *
        * @public                @memberof GoGdpBoundingBox
        * @version               Introduced in 0.2.1.53.
        * @return                The z-coordinate value of the bounding box origin.
        */
        const k64f Z() const;

        /**
        * Get the bounding box size along the x-axis.
        *
        * @public                @memberof GoGdpBoundingBox
        * @version               Introduced in 0.2.1.53.
        * @return                The width of bounding box.
        */
        const k64f Width() const;

        /**
        * Get the bounding box size along the y-axis.
        *
        * @public                @memberof GoGdpBoundingBox
        * @version               Introduced in 0.2.1.53.
        * @return                The length of bounding box.
        */
        const k64f Length() const;

        /**
        * Get the bounding box size along the z-axis.
        *
        * @public                @memberof GoGdpBoundingBox
        * @version               Introduced in 0.2.1.53.
        * @return                The height of bounding box.
        */
        const k64f Height() const;

    private:
        k64f x = 0.0;
        k64f y = 0.0;
        k64f z = 0.0;
        k64f width = 0.0;
        k64f length = 0.0;
        k64f height = 0.0;

        GoGdpBoundingBox(k64f x, k64f y, k64f z, k64f width, k64f length, k64f height) :
            x(x), y(y), z(z), width(width), length(length), height(height) {}
        
        GoGdpBoundingBox& operator= (const GoGdpBoundingBox& box);

        friend class ::GoGdpMsgTests;
    };
}

#endif

