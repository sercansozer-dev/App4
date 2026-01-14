/**
 * @file    GoGdpStamp.h
 * @brief   Declares the GoPxLSdk.GoGdpStamp class.
 * 
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#ifndef GO_PXL_SDK_GOGDPSTAMP_H
#define GO_PXL_SDK_GOGDPSTAMP_H

#include <GoPxLSdk/GoGdpMsg/GoGdpMsg.h>

class GoGdpMsgTests;

namespace GoPxLSdk
{

    class GoPxLSdkClass GoGdpStamp : public GoGdpMsg
    {
    public:
        /**
        * Constructs GoGdpStamp.
        *
        * @public                @memberof GoGdpStamp
        * @version               Introduced in 0.2.1.53.
        */
        GoGdpStamp();
        ~GoGdpStamp() = default;
        
        /**
        * Deserialize stamp message.
        *
        * @public                @memberof GoGdpStamp
        * @version               Introduced in 0.2.1.53.
        * @param serializer      The serializer to read.
        * @throws Go::Exception  If failed to deserialize stamp message.
        */
        void Deserialize(kSerializer serializer) override;

        /**
        * Gets the frame index stored in the stamp message.
        *
        * @public                @memberof GoGdpStamp
        * @version               Introduced in 1.0.104.64.
        * @return                The frame index.
        */
        const k64u FrameIndex() const;

        /**
        * Gets the timestamp stored in the stamp message.
        *
        * @public                @memberof GoGdpStamp
        * @version               Introduced in 1.0.104.64.
        * @return                The timestamp.
        */
        const k64u Timestamp() const;


        /**
        * Gets the encoder stored in the stamp message.
        *
        * @public                @memberof GoGdpStamp
        * @version               Introduced in 1.0.104.64.
        * @return                The encoder value.
        */
        const k64s Encoder() const;

        /**
        * Gets the encoder at z stored in the stamp message.
        *
        * @public                @memberof GoGdpStamp
        * @version               Introduced in 1.0.104.64.
        * @return                The encoder at z value.
        */
        const k64s EncoderAtZ() const;

        /**
        * Gets the status stored in the stamp message.
        *
        * @public                @memberof GoGdpStamp
        * @version               Introduced in 1.0.104.64.
        * @return                The status value.
        */
        const k64u Status() const;

        /**
        * Gets the system time seconds stored in the stamp message.
        *
        * @public                @memberof GoGdpStamp
        * @version
        * @return                The seconds component of the system time.
        */
        const k64u SystemTimeSeconds() const;
        
        /**
        * Gets the system time nanoseconds stored in the stamp message.
        *
        * @public                @memberof GoGdpStamp
        * @version
        * @return                The nanoseconds component of the sytem time.
        */
        const k64u SystemTimeNanoseconds() const;
        
    private:
        k64u frameIndex = 0;
        k64u timestamp = 0;
        k64s encoder = 0;
        k64s encoderAtZ = 0;
        k64u status = 0;
        k64u systemTimeSeconds;
        k64u systemTimeNanoseconds;

        friend class ::GoGdpMsgTests;
    };
}

#endif
