/**
 * @file    GoGdpFeatureLine.h
 * @brief   Declares the GoPxLSdk.GoGdpFeatureLine class.
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#ifndef GO_PXL_SDK_GOGDPFEATURELINE_H
#define GO_PXL_SDK_GOGDPFEATURELINE_H

#include <GoPxLSdk/GoGdpMsg/GoGdpMsg.h>

class GoGdpMsgTests;

namespace GoPxLSdk
{
   
    class GoPxLSdkClass GoGdpFeatureLine : public GoGdpMsg
    {
    public:
        /**
        * Constructs class GoGdpFeatureLine.
        * 
        * @public                @memberof GoGdpFeatureLine
        * @version               Introduced in 0.2.1.53.
        */
        GoGdpFeatureLine();
       
        /**
        * Deserialize gdp line feature message.
        * 
        * @public                @memberof GoGdpFeatureLine
        * @version               Introduced in 0.2.1.53.
        * @param serializer      The serializer to read.
        * @throws Go::Exception  If failed to deserialize feature line message.
        */
        void Deserialize(kSerializer serializer) override;

        /**
        * Get x coordinate of the position.
        * 
        * @public                @memberof GoGdpFeatureLine
        * @version               Introduced in 0.2.1.53.
        * @return                The x-coordinate value of the position.
        */
        const k64f PositionX() const;

        /**
        * Get y coordinate of the position.
        * 
        * @public                @memberof GoGdpFeatureLine
        * @version               Introduced in 0.2.1.53.
        * @return                The y-coordinate value of the position.
        */
        const k64f PositionY() const;

        /**
        * Get z coordinate of the position.
        * 
        * @public                @memberof GoGdpFeatureLine
        * @version               Introduced in 0.2.1.53.
        * @return                The z-coordinate value of the position.
        */
        const k64f PositionZ() const;

        /**
        * Get x component of direction vector.
        * 
        * @public                @memberof GoGdpFeatureLine
        * @version               Introduced in 0.2.1.53.
        * @return                The x-component of direction vector.
        */
        const k64f DirectionX() const;

        /**
        * Get y component of direction vector.
        * 
        * @public                @memberof GoGdpFeatureLine
        * @version               Introduced in 0.2.1.53.
        * @return                The y-component of direction vector.
        */
        const k64f DirectionY() const;

        /**
        * Get z component of direction vector.
        * 
        * @public                @memberof GoGdpFeatureLine
        * @version               Introduced in 0.2.1.53.
        * @return                The z-component of direction vector.
        */
        const k64f DirectionZ() const;

    private:
        kPoint3d64f point;
        kPoint3d64f direction;
        friend class ::GoGdpMsgTests;
    };

}


#endif
