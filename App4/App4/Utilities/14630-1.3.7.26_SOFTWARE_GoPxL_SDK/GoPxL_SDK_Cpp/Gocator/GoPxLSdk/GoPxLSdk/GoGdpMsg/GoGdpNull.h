/**
 * @file    GoGdpNull.h
 * @brief   Declares the GoPxLSdk.GoGdpNull class.
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#ifndef GO_PXL_SDK_GOGDPNULL_H
#define GO_PXL_SDK_GOGDPNULL_H

#include <GoPxLSdk/GoGdpMsg/GoGdpMsg.h>

class GoGdpMsgTests;

namespace GoPxLSdk
{
    class GoPxLSdkClass GoGdpNull : public GoGdpMsg
    {
    public:
        /**
        * Constructs GoGdpNull.
        * 
        * @public                @memberof GoGdpNull
        * @version               Introduced in 0.2.1.53.
        */
        GoGdpNull();
        ~GoGdpNull() = default;

        /**
        * Deserialize null message.
        * 
        * @public                @memberof GoGdpNull
        * @version               Introduced in 0.2.1.53.
        * @param serializer      The serializer to read.
        * @throws Go::Exception  If failed to deserialize null message.
        */
        void Deserialize(kSerializer serializer) override;

        /**
        * Get error status.
        * 
        * @public                @memberof GoGdpNull
        * @version               Introduced in 0.2.1.53.
        * @return                The error status code.
        */
        const k32s ErrorStatus() const;

    private:
        k32s errorStatus = 0;
        friend class ::GoGdpMsgTests;
    };
}

#endif
