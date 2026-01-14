/**
 * @file    GoGdpSignal.h
 * @brief   Declares the GoPxLSdk.GoGdpSignal class.
 * 
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#ifndef GO_PXL_SDK_GOGDPSIGNAL_H
#define GO_PXL_SDK_GOGDPSIGNAL_H

#include <GoPxLSdk/GoGdpMsg/GoGdpMsg.h>

class GoGdpMsgTests;

namespace GoPxLSdk
{
    class GoPxLSdkClass GoGdpSignal : public GoGdpMsg
    {
    public:
        /**
        * Constructs GoGdpSignal.
        * 
        * @public                @memberof GoGdpSignal
        * @version               Introduced in 0.2.1.53.
        */
        GoGdpSignal();
        ~GoGdpSignal() = default;

        /**
        * Deserialize void message.
        * @param serializer The serializer to read.
        * 
        * @public                @memberof GoGdpSignal
        * @version               Introduced in 0.2.1.53.
        * @throws Go::Exception  If failed to deserialize signal message.
        */
        void Deserialize(kSerializer serializer) override;

    private:
        friend class ::GoGdpMsgTests;
    };
}

#endif
