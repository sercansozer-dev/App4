/**
 * @file    GoGdpSurfaceUniform.h
 * @brief   Declares the GoPxLSdk.GoGdpSurfaceUniform class.
 * 
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#ifndef GO_PXL_SDK_GOGDPSURFACEUNIFORM_H
#define GO_PXL_SDK_GOGDPSURFACEUNIFORM_H

#include <GoPxLSdk/GoGdpMsg/GoGdpMsg.h>
#include <GoPxLSdk/GoGdpMsg/GoGdpSurfaceBase.h>

class GoGdpMsgTests;

namespace GoPxLSdk
{
    class GoPxLSdkClass GoGdpSurfaceUniform : public GoGdpSurfaceBase
    {
    public:
        /**
        * Constructs GoGdpSurfaceUniform.
        *
        * @public                @memberof GoGdpSurfaceUniform
        * @version               Introduced in 0.2.1.53.
        */
        GoGdpSurfaceUniform();
        ~GoGdpSurfaceUniform() = default;

        /**
        * Deserialize surface uniform message.
        *
        * @public                @memberof GoGdpSurfaceUniform
        * @version               Introduced in 0.2.1.53.
        * @param serializer      The serializer to read.
        * @throws Go::Exception  If failed to deserialize surface uniform message.
        */
        void Deserialize(kSerializer serializer) override;

        /**
        * Get surface range array.
        *
        * @public                @memberof GoGdpSurfaceUniform
        * @version               Introduced in 0.2.1.53.
        * @return                The surface range array.
        */
        const kArray2 Ranges() const;

        /**
        * Get surface intensity array.
        *
        * @public                @memberof GoGdpSurfaceUniform
        * @version               Introduced in 0.2.1.53.
        * @return                The surface intensity array.
        */
        const kArray2 Intensities() const;

    private:
        Go::Object<kArray2> ranges;
        Go::Object<kArray2> intensities;

        friend class ::GoGdpMsgTests;
    };
}

#endif
