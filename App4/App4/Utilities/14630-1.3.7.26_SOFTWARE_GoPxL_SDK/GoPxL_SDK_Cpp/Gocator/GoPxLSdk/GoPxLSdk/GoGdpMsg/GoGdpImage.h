/**
 * @file    GoGdpImage.h
 * @brief   Declares the GoPxLSdk.GoGdpImage class.
 * 
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#ifndef GO_PXL_SDK_GOGDPIMAGE_H
#define GO_PXL_SDK_GOGDPIMAGE_H

#include <GoPxLSdk/GoGdpMsg/GoGdpMsg.h>
#include <GoPxLSdk/GoGdpMsg/GoGdpPixelFormat.h>

class GoGdpMsgTests;

namespace GoPxLSdk
{
    class GoPxLSdkClass GoGdpImage : public GoGdpMsg
    {
    public:
        /**
        * Constructs GoGdpImage.
        *
        * @public                @memberof GoGdpImage
        * @version               Introduced in 0.2.1.53.
        */
        GoGdpImage();
        ~GoGdpImage() = default;

        /**
        * Deserialize video/image message.
        *
        * @public                @memberof GoGdpImage
        * @version               Introduced in 0.2.1.53.
        * @param serializer      The serializer to read.
        * @throws Go::Exception  If failed to deserialize video/image message.
        */
        void Deserialize(kSerializer serializer) override;

        /**
        * Get image height.
        *
        * @public                @memberof GoGdpImage
        * @version               Introduced in 0.2.1.53.
        * @return                The image height value.
        */
        const k32u Height() const;

        /**
        * Get image width.
        *
        * @public                @memberof GoGdpImage
        * @version               Introduced in 0.2.1.53.
        * @return                The image width value.
        */
        const k32u Width() const;

        /**
        * Get image pixel size.
        *
        * @public                @memberof GoGdpImage
        * @version               Introduced in 0.2.1.53.
        * @return                The image pixel size.
        */
        const k32u PixelSize() const;

        /**
        * Get image pixel format.
        *
        * @public                @memberof GoGdpImage
        * @version               Introduced in 1.1.50.15
        * @return                The image pixel format.
        */
        const GoGdpPixelFormat::FormatType PixelFormat() const;

        /**
        * Get image color filter.
        * Color filter array alignment:
        * 0 - None
        * 1 - Bayer BG/GR
        * 2 - Bayer GB/RG
        * 3 - Bayer RG/GB
        * 4 - Bayer GR/BG
        *
        * @public                @memberof GoGdpImage
        * @version               Introduced in 0.2.1.53.
        * @return                The color filter value.
        */
        const kCfa ColorFilter() const;

        /**
        * Get image exposure.
        *
        * @public                @memberof GoGdpImage
        * @version               Introduced in 0.2.1.53.
        * @return                The image exposure value in nanoseconds.
        */
        const k32f Exposure() const;

        /**
        * Get flag to indicate if image is flipped about X-axis.
        *
        * @public                @memberof GoGdpImage
        * @version               Introduced in 0.2.1.53.
        * @return                True if flipped, false otherwise.
        */
        const bool FlippedX() const;

        /**
        * Get flag to indicate if image is flipped about Y-axis.
        *
        * @public                @memberof GoGdpImage
        * @version               Introduced in 0.2.1.53.
        * @return                True if flipped, false otherwise.
        */
        const bool FlippedY() const;

        /**
        * Get flag to indicate if the image is column based or not.
        *
        * @public                @memberof GoGdpImage
        * @version               Introduced in 0.2.1.53.
        * @return                True if column based, false otherwise.
        */
        const bool ColumnBased() const;

        /**
        * Get image scale point.
        *
        * @public                @memberof GoGdpImage
        * @return                The image resolution.
        */
        const kPoint64f Resolution() const;

        /**
        * Get image offset point.
        *
        * @public                @memberof GoGdpImage
        * @return                The image offset.
        */
        const kPoint64f Offset() const;

        /**
        * Get the image data.
        *
        * @public                @memberof GoGdpImage
        * @version               Introduced in 1.1.50.15
        * @return                The kArray2 object containing the data.
        */
        const kArray2 Pixels() const;

    private:
        /**
        * Gets the number of bytes per row.
        * 
        * @remarks This will return the number of bytes per row. 
        *          If image is kPixelFormat types, size is image's width x pixel size.
        *          If image is other types, size is image's width x pixel size determined by PixelSize().
        */
        const kSize RowSize() const;

    private:
        k32u width = 0;
        k32u height = 0;
        k32u pixelSize = 0;
        GoGdpPixelFormat::FormatType pixelFormat = GoGdpPixelFormat::FormatType::Null_Format;
        kCfa colorFilter = 0;
        k32f exposure = 0.0;
        bool flippedX = false;
        bool flippedY = false;
        bool columnBased = false;

        kPoint64f resolution = { 0.0 };
        kPoint64f offset = { 0.0 };

        Go::Object<kArray2> pixels;

        friend class ::GoGdpMsgTests;
    };
}

#endif // GO_PXL_SDK_GOGDPIMAGE_H
