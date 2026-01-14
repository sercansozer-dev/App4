/**
 * @file    GoGdpProfileUniform.h
 * @brief   Declares the GoPxLSdk.GoGdpProfileUniform class.
 * 
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#ifndef GO_PXL_SDK_GOGDPPROFILEUNIFORM_H
#define GO_PXL_SDK_GOGDPPROFILEUNIFORM_H

#include <GoPxLSdk/GoGdpMsg/GoGdpMsg.h>
#include <GoPxLSdk/GoGdpMsg/GoGdpProfileBase.h>

class GoGdpMsgTests;

namespace GoPxLSdk
{
    class GoPxLSdkClass GoGdpProfileUniform : public GoGdpProfileBase
    {
    public:
        /**
        * Constructs GoGdpProfileUniform.
        *
        * @public                @memberof GoGdpProfileUniform
        * @version               Introduced in 0.2.1.53.
        */
        GoGdpProfileUniform();
        ~GoGdpProfileUniform() = default;

        /**
        * Deserialize profile uniform message.
        *
        * @public                @memberof GoGdpProfileUniform
        * @version               Introduced in 0.2.1.53.
        * @param serializer      The serializer to read.
        * @throws Go::Exception  If failed to deserialize profile uniform message.
        */
        void Deserialize(kSerializer serializer) override;

        /**
        * Get profile range array.
        *
        * @public                @memberof GoGdpProfileUniform
        * @version               Introduced in 0.2.1.53.
        * @return                The profile range array.
        */
        const kArray1 Ranges() const;

        /**
        * Get profile intensity array.
        *
        * @public                @memberof GoGdpProfileUniform
        * @version               Introduced in 0.2.1.53.
        * @return                The profile intensity array.
        */
        const kArray1 Intensities() const;

    private:
        Go::Object<kArray1> ranges;
        Go::Object<kArray1> intensities;

        friend class ::GoGdpMsgTests;
    };


}

#endif


