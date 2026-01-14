/**
 * @file    GoGdpProfileBase.h
 * @brief   Declares the GoPxLSdk.GoGdpProfileBase class.
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#ifndef GO_PXL_SDK_GOGDPPROFILEBASE_H
#define GO_PXL_SDK_GOGDPPROFILEBASE_H

#include <GoPxLSdk/GoGdpMsg/GoGdpMsg.h>

class GoGdpMsgTests;

namespace GoPxLSdk
{
    class GoPxLSdkClass GoGdpProfileBase : public GoGdpMsg
    {
    public:
        /**
        * Get number of points per profile array.
        * 
        * @public                @memberof GoGdpProfileBase
        * @version               Introduced in 0.2.1.53.
        * @return                The profile width.
        */
        const k32u Width() const;

        /**
        * Get number of values per profile intensity array.
        * 
        * @public                @memberof GoGdpProfileBase
        * @version               Introduced in 0.2.1.53.
        * @return                The profile array width.
        */
        const k32u IntensityWidth() const;

        /**
        * Get profile resolution.
        * 
        * @public                @memberof GoGdpProfileBase
        * @version               Introduced in 0.2.1.53.
        * @return                The resolution value.
        */
        const kPoint3d64f Resolution() const;

        /**
        * Get profile offset.
        * 
        * @public                @memberof GoGdpProfileBase
        * @version               Introduced in 0.2.1.53.
        * @return                The offset value.
        */
        const kPoint3d64f Offset() const;

        /**
        * Get profile exposure. 
        * 
        * @public                @memberof GoGdpProfileBase
        * @version               Introduced in 0.2.1.53.
        * @return                The exposure value in nanoseconds.
        */
        const k32f Exposure() const;

    protected:
        /**
        * Constructs GoGdpProfileBase.
        * 
        * @public                @memberof GoGdpProfileBase
        * @version               Introduced in 0.2.1.53.
        * @param type            The message type.
        */
        GoGdpProfileBase(MessageType type);
        ~GoGdpProfileBase() = default;

        /**
        * Deserialize information common to all profile messages.
        * 
        * @public                @memberof GoGdpProfileBase
        * @version               Introduced in 0.2.1.53.
        * @param serializer      The serializer to read.
        * @throws Go::Exception  If failed to deserialize profile base message.
        */
        void Deserialize(kSerializer serializer) override;

    private:
        const k64f OffsetScaled = 1000.0;
        const k64f ResolutionScaled = 1000000.0;

        k32u width = 0;
        k32u intensityWidth = 0;

        // The actual resolution/offset is a 2-d value. 
        // However, the Firesync 2-d structure kPoint64f only has X and Y members.
        // The customer documentation refers to the resolution/scale and offset as Z scale and Z offset.
        // To be consistent with the documentation, use a 3-d structure kPoint3d64f that has a member called "Z", 
        // instead of using the 2-d kPoint64f structure which does not have a "Z" member.
        // The Y-member in the kPoint3d64f used for resolution and offset are set to 0.
        kPoint3d64f resolution = { 0.0 };
        kPoint3d64f offset = { 0.0 };

        k32f exposure = 0.0;
        
        friend class ::GoGdpMsgTests;
    };
}

#endif


