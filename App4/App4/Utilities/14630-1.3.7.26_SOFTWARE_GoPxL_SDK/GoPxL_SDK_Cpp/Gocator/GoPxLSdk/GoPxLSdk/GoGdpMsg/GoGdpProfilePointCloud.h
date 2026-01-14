/**
 * @file    GoGdpProfilePointCloud.h
 * @brief   Declares the GoPxLSdk.GoGdpProfilePointCloud class.
 * 
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#ifndef GO_PXL_SDK_GOGDPPROFILEPOINTCLOUD_H
#define GO_PXL_SDK_GOGDPPROFILEPOINTCLOUD_H

#include <GoPxLSdk/GoGdpMsg/GoGdpMsg.h>
#include <GoPxLSdk/GoGdpMsg/GoGdpProfileBase.h>

class GoGdpMsgTests;

namespace GoPxLSdk
{
    class GoPxLSdkClass GoGdpProfilePointCloud : public GoGdpProfileBase
    {
    public:
        /**
        * Constructs GoGdpProfilePointCloud.
        *
        * @public                @memberof GoGdpProfilePointCloud
        * @version               Introduced in 0.2.1.53
        */
        GoGdpProfilePointCloud();
        ~GoGdpProfilePointCloud() = default;

        /**
        * Deserialize profile point cloud message.
        *
        * @public                @memberof GoGdpProfilePointCloud
        * @version               Introduced in 0.2.1.53
        * @param serializer      The serializer to read.
        * @throws Go::Exception  If failed to deserialize profile point cloud message.
        */
        void Deserialize(kSerializer serializer) override;

        /**
        * Get profile range array.
        *
        * @public                @memberof GoGdpProfilePointCloud
        * @version               Introduced in 0.2.1.53
        * @return                The profile range array.
        */
        const kArray1 Ranges() const;

        /**
        * Get profile intensity array.
        *
        * @public                @memberof GoGdpProfilePointCloud
        * @version               Introduced in 0.2.1.53
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


