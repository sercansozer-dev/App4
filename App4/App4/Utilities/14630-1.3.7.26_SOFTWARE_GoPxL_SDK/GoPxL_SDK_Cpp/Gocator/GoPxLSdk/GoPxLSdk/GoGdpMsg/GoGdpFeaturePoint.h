/**
 * @file    GoGdpFeaturePoint.h
 * @brief   Declares the GoPxLSdk.GoGdpFeaturePoint class.
 * 
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#ifndef GO_PXL_SDK_GOGDPFEATUREPOINT_H
#define GO_PXL_SDK_GOGDPFEATUREPOINT_H

#include <GoPxLSdk/GoGdpMsg/GoGdpMsg.h>

class GoGdpMsgTests;

namespace GoPxLSdk
{
    class GoPxLSdkClass GoGdpFeaturePoint : public GoGdpMsg
    {
    public:
        /**
        * Constructs class GoGdpFeaturePoint.
        * 
        * @public                @memberof GoGdpFeaturePoint
        * @version               Introduced in 0.2.1.53.
        */
        GoGdpFeaturePoint();
        
        /**
        * Deserialize gdp point feature message.
        * 
        * @public                @memberof GoGdpFeaturePoint
        * @version               Introduced in 0.2.1.53.
        * @param serializer      The serializer to read.
        * @throws Go::Exception  If failed to deserialize feature point message.
        */
        void Deserialize(kSerializer serializer) override;
        
        /**
        * Get x coordinate of the position.
        *
        * @public                @memberof GoGdpFeaturePoint
        * @version               Introduced in 0.2.1.53.
        * @return                The x-coordinate value of the position.
        */
        const k64f PositionX() const;

        /**
        * Get y coordinate of the position.
        * 
        * @public                @memberof GoGdpFeaturePoint
        * @version               Introduced in 0.2.1.53.
        * @return                The y-coordinate value of the position.
        */
        const k64f PositionY() const;

        /**
        * Get z coordinate of the position.
        * 
        * @public                @memberof GoGdpFeaturePoint
        * @version               Introduced in 0.2.1.53.
        * @return                The z-coordinate value of the position.
        */
        const k64f PositionZ() const;

    private:
        kPoint3d64f point;
        friend class ::GoGdpMsgTests;
    };

}


#endif
