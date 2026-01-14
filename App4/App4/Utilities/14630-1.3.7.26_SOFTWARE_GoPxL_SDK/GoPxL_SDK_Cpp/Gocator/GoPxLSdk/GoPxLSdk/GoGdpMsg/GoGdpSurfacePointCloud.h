/**
 * @file    GoGdpSurfacePointCloud.h
 * @brief   Declares the GoPxLSdk.GoGdpSurfacePointCloud class.
 * 
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#ifndef GO_PXL_SDK_GOGDPSURFACEPOINTCLOUD_H
#define GO_PXL_SDK_GOGDPSURFACEPOINTCLOUD_H

#include <GoPxLSdk/GoGdpMsg/GoGdpMsg.h>
#include <GoPxLSdk/GoGdpMsg/GoGdpSurfaceBase.h>

class GoGdpMsgTests;

namespace GoPxLSdk
{
    class GoPxLSdkClass GoGdpSurfacePointCloud : public GoGdpSurfaceBase
    {
    public:
        /**
        * Constructs GoGdpSurfacePointCloud.
        * @public                @memberof GoGdpSurfacePointCloud
        * @version               Introduced in 0.2.1.53.
        */
        GoGdpSurfacePointCloud();
        ~GoGdpSurfacePointCloud() = default;

        /**
        * Deserialize surface point cloud message.
        *
        * @public                @memberof GoGdpSurfacePointCloud
        * @version               Introduced in 0.2.1.53.
        * @param serializer      The serializer to read.
        * @throws Go::Exception  If failed to deserialize surface point cloud message.
        */
        void Deserialize(kSerializer serializer) override;

        /**
        * Check if the data is adjacent/sorted.
        *
        * @public                @memberof GoGdpSurfacePointCloud
        * @version               Introduced in 0.2.1.53.
        * @return                True if is adjacant.
        */
        const bool IsAdjacent() const;

        /**
        * Get surface range array.
        *
        * @public                @memberof GoGdpSurfacePointCloud
        * @version               Introduced in 0.2.1.53.
        * @return                The surface range array.
        */
        const kArray2 Ranges() const;

        /**
        * Get surface intensity array.
        *
        * @public                @memberof GoGdpSurfacePointCloud
        * @version               Introduced in 0.2.1.53.
        * @return                The surface intensity array.
        */
        const kArray2 Intensities() const;

    private:
        Go::Object<kArray2> ranges;
        Go::Object<kArray2> intensities;
        kBool isAdjacent = 0;

        friend class ::GoGdpMsgTests;
    };
}

#endif
