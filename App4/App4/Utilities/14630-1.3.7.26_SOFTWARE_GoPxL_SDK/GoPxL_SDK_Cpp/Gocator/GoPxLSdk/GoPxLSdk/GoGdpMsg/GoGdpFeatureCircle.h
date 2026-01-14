/**
 * @file    GoGdpFeatureCircle.h
 * @brief   Declares the GoPxLSdk.GoGdpFeatureCircle class.
 * 
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#ifndef GO_PXL_SDK_GOGDPFEATURECIRCLE_H
#define GO_PXL_SDK_GOGDPFEATURECIRCLE_H

#include <GoPxLSdk/GoGdpMsg/GoGdpMsg.h>

class GoGdpMsgTests;

namespace GoPxLSdk
{
    class GoPxLSdkClass GoGdpFeatureCircle : public GoGdpMsg
    {
    public:
        /**
        * Constructs class GoGdpFeatureCircle.
        * 
        * @public                @memberof GoGdpFeatureCircle
        * @version               Introduced in 0.2.1.53.
        */
        GoGdpFeatureCircle();

        /**
        * Deserialize gdp circle feature message.
        * 
        * @public                @memberof GoGdpFeatureCircle
        * @version               Introduced in 0.2.1.53.
        * @param serializer      The serializer to read.
        * @throws Go::Exception  If failed to deserialize feature circle message.
        */
        void Deserialize(kSerializer serializer) override;
        
        /**
        * Get x coordinate of the circle center point.
        * 
        * @public                @memberof GoGdpFeatureCircle
        * @version               Introduced in 0.2.1.53.
        * @return                The x-coordinate value of the circle center point.
        */
        const k64f CenterX() const;

        /**
        * Get y coordinate of the circle center point.
        * 
        * @public                @memberof GoGdpFeatureCircle
        * @version               Introduced in 0.2.1.53.
        * @return                The y-coordinate value of the circle center point.
        */
        const k64f CenterY() const;

        /**
        * Get z coordinate of the circle center point.
        * 
        * @public                @memberof GoGdpFeatureCircle
        * @version               Introduced in 0.2.1.53.
        * @return                The z-coordinate value of the circle center point.
        */
        const k64f CenterZ() const;
        
        /**
         * Get x component of the normal vector.
        * 
        * @public                @memberof GoGdpFeatureCircle
        * @version               Introduced in 0.2.1.53.
        * @return                The x-component of normal vector.
        */
        const k64f NormalX() const;

        /**
        * Get y component of the normal vector.
        * 
        * @public                @memberof GoGdpFeatureCircle
        * @version               Introduced in 0.2.1.53.
        * @return                The y-component of normal vector.
        */
        const k64f NormalY() const;

        /**
        * Get z component of the normal vector.
        * 
        * @public                @memberof GoGdpFeatureCircle
        * @version               Introduced in 0.2.1.53.
        * @return                The z-component of normal vector.
        */
        const k64f NormalZ() const;

        /**
        * Get radius of the circle.
        * 
        * @public                @memberof GoGdpFeatureCircle
        * @version               Introduced in 0.2.1.53.
        * @return                The radius value of the circle.
        */
        const k64f Radius() const;

    private:
        kPoint3d64f center;
        kPoint3d64f normal;
        k64f radius;
        friend class ::GoGdpMsgTests;
    };

}


#endif
