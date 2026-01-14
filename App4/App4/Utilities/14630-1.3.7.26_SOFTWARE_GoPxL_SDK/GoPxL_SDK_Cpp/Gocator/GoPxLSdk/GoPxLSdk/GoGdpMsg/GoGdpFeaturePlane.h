/**
 * @file    GoGdpFeaturePlane.h
 * @brief   Declares the GoPxLSdk.GoGdpFeaturePlane class.
 * 
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#ifndef GO_PXL_SDK_GOGDPFEATUREPLANE_H
#define GO_PXL_SDK_GOGDPFEATUREPLANE_H

#include <GoPxLSdk/GoGdpMsg/GoGdpMsg.h>

class GoGdpMsgTests;

namespace GoPxLSdk
{
    
    class GoPxLSdkClass GoGdpFeaturePlane : public GoGdpMsg
    {
    public:
        /**
        * Constructs class GoGdpFeaturePlane.
        * 
        * @public                @memberof GoGdpFeaturePlane
        * @version               Introduced in 0.2.1.53.
        */
        GoGdpFeaturePlane();
        
        /**
        * Deserialize gdp plane feature message.
        * 
        * @public                @memberof GoGdpFeaturePlane
        * @version               Introduced in 0.2.1.53.
        * @param serializer      The serializer to read.
        * @throws Go::Exception  If failed to deserialize feature plane message.
        */
        void Deserialize(kSerializer serializer) override;

        /**
        * Get x component of the normal vector.
        * 
        * @public                @memberof GoGdpFeaturePlane
        * @version               Introduced in 0.2.1.53.
        * @return                The x-component of normal vector.
        */
        const k64f NormalX() const;

        /**
        * Get y component of the normal vector.
        * 
        * @public                @memberof GoGdpFeaturePlane
        * @version               Introduced in 0.2.1.53.
        * @return                The y-component of normal vector.
        */
        const k64f NormalY() const;

        /**
        * Get z component of the normal vector.
        * 
        * @public                @memberof GoGdpFeaturePlane
        * @version               Introduced in 0.2.1.53.
        * @return                The z-component of normal vector.
        */
        const k64f NormalZ() const;

       /**
        * Get distance to origin.
        * 
        * @public                @memberof GoGdpFeaturePlane
        * @version               Introduced in 0.2.1.53.
        * @return                The distance value to origin.
        */
        const k64f DistanceToOrigin() const;

    private:
        kPoint3d64f normal;
        k64f distanceToOrigin;
        friend class ::GoGdpMsgTests;
    };

}


#endif
