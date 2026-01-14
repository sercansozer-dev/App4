/**
 * @file    GoGdpMeasurement.h
 * @brief   Declares the GoPxLSdk.GoGdpMeasurement class.
 * 
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#ifndef GO_PXL_SDK_GOGDPMEASUREMENT_H
#define GO_PXL_SDK_GOGDPMEASUREMENT_H

#include <GoPxLSdk/GoGdpMsg/GoGdpMsg.h>

class GoGdpMsgTests;

namespace GoPxLSdk
{

    class GoPxLSdkClass GoGdpMeasurement : public GoGdpMsg
    {
    public:
        /**
        * Constructs GoGdpMeasurement.
        * 
        * @public                @memberof GoGdpMeasurement
        * @version               Introduced in 0.2.1.53.
        */
        GoGdpMeasurement();
        ~GoGdpMeasurement() = default;

        /**
        * Deserialize measurement message.
        * 
        * @public                @memberof GoGdpMeasurement
        * @version               Introduced in 0.2.1.53.
        * @param serializer      The serializer to read.
        * @throws Go::Exception  If failed to deserialize measurement message.
        */
        void Deserialize(kSerializer serializer) override;

        /**
        * Gets the value stored in the measurement message.
        *
        * @public                @memberof GoGdpMeasurement
        * @version               Introduced in 1.0.104.64.
        * @return                The measurement value.
        */
        const k64f Value() const;

        /**
        * Gets the decision value stored in the measurement message.
        *
        * @public                @memberof GoGdpMeasurement
        * @version               Introduced in 1.0.104.64.
        * @return                The measurement decision value.
        */
        const k8u Decision() const;

    private:
        k64f value = 0;
        k8u decision = 0;
        friend class ::GoGdpMsgTests;
    };
}


#endif
