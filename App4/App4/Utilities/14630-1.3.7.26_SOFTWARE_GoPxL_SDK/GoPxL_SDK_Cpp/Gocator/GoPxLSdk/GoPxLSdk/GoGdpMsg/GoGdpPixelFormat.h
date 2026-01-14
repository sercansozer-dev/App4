/**
 * @file    GoGdpPixelFormat.h
 * @brief   Declares the GoPxLSdk.GoGdpPixelFormat class.
 *
 * @internal
 * Copyright (C) 2024-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#ifndef GO_PXL_SDK_GOGDPPIXELFORMAT_H
#define GO_PXL_SDK_GOGDPPIXELFORMAT_H

#include <GoPxLSdk/GoGdpMsg/GoGdpMsg.h>

class GoGdpMsgTests;

namespace GoPxLSdk
{
    class GoPxLSdkClass GoGdpPixelFormat 
    {
    public:
    // Enums

    /**
     * \brief Indicates if pixel is monochrome or RGB.
     */
    enum ColorType : k32s
    {
        Mono = 0x01000000,                                               //!< Monochrome pixel
        Color = 0x02000000                                               //!< Pixel bearing color information
    };

    /**
    * \brief Indicates number of bits for a pixel. Needed for building values of FormatType.
    */
    enum PixelSize : k32s
    {
        Size_8_Bit = 0x00080000,                                         //!< Pixel effectively occupies 8 bits
        Size_10_Bit = 0x000A0000,                                        //!< Pixel effectively occupies 10 bits
        Size_12_Bit = 0x000C0000,                                        //!< Pixel effectively occupies 12 bits
        Size_14_Bit = 0x000E0000,                                        //!< Pixel effectively occupies 14 bits
        Size_16_Bit = 0x00100000,                                        //!< Pixel effectively occupies 16 bits
        Size_24_Bit = 0x00180000,                                        //!< Pixel effectively occupies 24 bits
        Size_32_Bit = 0x00200000,                                        //!< Pixel effectively occupies 32 bits
        Size_48_Bit = 0x00300000,                                        //!< Pixel effectively occupies 48 bits
        Size_64_Bit = 0x00400000,                                        //!< Pixel effectively occupies 64 bits
    };

    /**
    * \brief Pixel format types.
    * As far as possible, the Pixel Format Naming Convention (PFNC) has been followed, allowing a few deviations.
    * If data spans more than one byte, it is always LSB aligned, except if stated differently.
    * 
    * NOTE: Pixel formats from GEV specifications are marked as "legacy" in PFNC
    * 
    * "Packed": each channel occupies exactly the number of bits specified; unused bits used by next channel (whether same pixel or not).
    * "Unpacked": number of bits occupied by each channel is rounded up to multiple of 8 (full byte)
    */
    enum FormatType : k32s
    {
        //kPixelFormats
        Null_Format             = 0,                                                  // Unknown pixel format.
        Greyscale_8BPP          = 1,                                                  // 8-bit greyscale (k8u)
        CFA_8BPP                = 2,                                                  // 8-bit color filter array (k8u)
        BGRX_8BPC               = 3,                                                  // 8-bits-per-channel ColorType::Color with 4 channels (blue/green/red/unused)(kRgb)
        Greyscale_1BPP          = 4,                                                  // 1-bit greyscale, 8 packed pixels per image element (k8u)
        Greyscale_16BPP         = 5,                                                  // 16-bit greyscale (k16u)
        BGR_8BPC                = 6,                                                  // 8-bits-per-channel color with 3 channels (blue/green/red)(kRgb24)

        // mono formats
        Mono8                   = ColorType::Mono  | PixelSize::Size_8_Bit  | 0x0001, //!< Monochrome,  8 bits          (PFNC: Mono8)
        Mono10                  = ColorType::Mono  | PixelSize::Size_16_Bit | 0x0003, //!< Monochrome, 10 bits unpacked (PFNC: Mono10)
        Mono10p                 = ColorType::Mono  | PixelSize::Size_10_Bit | 0x0046, //!< Monochrome, 10 bits packed   (PFNC: Mono10p)
        Mono12                  = ColorType::Mono  | PixelSize::Size_16_Bit | 0x0005, //!< Monochrome, 12 bits unpacked (PFNC: Mono12)
        Mono12Packed            = ColorType::Mono  | PixelSize::Size_12_Bit | 0x0006, //!< Monochrome, 12 bits packed   (GEV:  Mono12Packed)
        Mono12p                 = ColorType::Mono  | PixelSize::Size_12_Bit | 0x0047, //!< Monochrome, 12 bits packed   (PFNC: Mono12p)
        Mono14                  = ColorType::Mono  | PixelSize::Size_16_Bit | 0x0025, //!< Monochrome, 14 bits unpacked (PFNC: Mono14)
        Mono16                  = ColorType::Mono  | PixelSize::Size_16_Bit | 0x0007, //!< Monochrome, 16 bits          (PFNC: Mono16)

        // bayer formats
        BayerGR8                = ColorType::Mono  | PixelSize::Size_8_Bit  | 0x0008, //!< Bayer-color,  8 bits, starting with GR line          (PFNC: BayerGR8)
        BayerRG8                = ColorType::Mono  | PixelSize::Size_8_Bit  | 0x0009, //!< Bayer-color,  8 bits, starting with RG line          (PFNC: BayerRG8)
        BayerGB8                = ColorType::Mono  | PixelSize::Size_8_Bit  | 0x000A, //!< Bayer-color,  8 bits, starting with GB line          (PFNC: BayerGB8)
        BayerBG8                = ColorType::Mono  | PixelSize::Size_8_Bit  | 0x000B, //!< Bayer-color,  8 bits, starting with BG line          (PFNC: BayerBG8)
        BayerGR10               = ColorType::Mono  | PixelSize::Size_16_Bit | 0x000C, //!< Bayer-color, 10 bits unpacked, starting with GR line (PFNC: BayerGR10)
        BayerRG10               = ColorType::Mono  | PixelSize::Size_16_Bit | 0x000D, //!< Bayer-color, 10 bits unpacked, starting with RG line (PFNC: BayerRG10)
        BayerGB10               = ColorType::Mono  | PixelSize::Size_16_Bit | 0x000E, //!< Bayer-color, 10 bits unpacked, starting with GB line (PFNC: BayerGB10)
        BayerBG10               = ColorType::Mono  | PixelSize::Size_16_Bit | 0x000F, //!< Bayer-color, 10 bits unpacked, starting with BG line (PFNC: BayerBG10)
        BayerGR12               = ColorType::Mono  | PixelSize::Size_16_Bit | 0x0010, //!< Bayer-color, 12 bits unpacked, starting with GR line (PFNC: BayerGR12)
        BayerRG12               = ColorType::Mono  | PixelSize::Size_16_Bit | 0x0011, //!< Bayer-color, 12 bits unpacked, starting with RG line (PFNC: BayerRG12)
        BayerGB12               = ColorType::Mono  | PixelSize::Size_16_Bit | 0x0012, //!< Bayer-color, 12 bits unpacked, starting with GB line (PFNC: BayerGB12)
        BayerBG12               = ColorType::Mono  | PixelSize::Size_16_Bit | 0x0013, //!< Bayer-color, 12 bits unpacked, starting with BG line (PFNC: BayerBG12)
        BayerGR12Packed         = ColorType::Mono  | PixelSize::Size_12_Bit | 0x002A, //!< Bayer-color, 12 bits packed, starting with GR line   (GEV:  BayerGR12Packed)
        BayerRG12Packed         = ColorType::Mono  | PixelSize::Size_12_Bit | 0x002B, //!< Bayer-color, 12 bits packed, starting with RG line   (GEV:  BayerRG12Packed)
        BayerGB12Packed         = ColorType::Mono  | PixelSize::Size_12_Bit | 0x002C, //!< Bayer-color, 12 bits packed, starting with GB line   (GEV:  BayerGB12Packed)
        BayerBG12Packed         = ColorType::Mono  | PixelSize::Size_12_Bit | 0x002D, //!< Bayer-color, 12 bits packed, starting with BG line   (GEV:  BayerBG12Packed)
        BayerGR10p              = ColorType::Mono  | PixelSize::Size_10_Bit | 0x0056, //!< Bayer-color, 12 bits packed, starting with GR line   (PFNC: BayerGR10p)
        BayerRG10p              = ColorType::Mono  | PixelSize::Size_10_Bit | 0x0058, //!< Bayer-color, 12 bits packed, starting with RG line   (PFNC: BayerRG10p)
        BayerGB10p              = ColorType::Mono  | PixelSize::Size_10_Bit | 0x0054, //!< Bayer-color, 12 bits packed, starting with GB line   (PFNC: BayerGB10p)
        BayerBG10p              = ColorType::Mono  | PixelSize::Size_10_Bit | 0x0052, //!< Bayer-color, 12 bits packed, starting with BG line   (PFNC: BayerBG10p)
        BayerGR12p              = ColorType::Mono  | PixelSize::Size_12_Bit | 0x0057, //!< Bayer-color, 12 bits packed, starting with GR line   (PFNC: BayerGR12p)
        BayerRG12p              = ColorType::Mono  | PixelSize::Size_12_Bit | 0x0059, //!< Bayer-color, 12 bits packed, starting with RG line   (PFNC: BayerRG12p)
        BayerGB12p              = ColorType::Mono  | PixelSize::Size_12_Bit | 0x0055, //!< Bayer-color, 12 bits packed, starting with GB line   (PFNC: BayerGB12p)
        BayerBG12p              = ColorType::Mono  | PixelSize::Size_12_Bit | 0x0053, //!< Bayer-color, 12 bits packed, starting with BG line   (PFNC: BayerBG12p)
        BayerGR16               = ColorType::Mono  | PixelSize::Size_16_Bit | 0x002E, //!< Bayer-color, 16 bits, starting with GR line          (PFNC: BayerGR16)
        BayerRG16               = ColorType::Mono  | PixelSize::Size_16_Bit | 0x002F, //!< Bayer-color, 16 bits, starting with RG line          (PFNC: BayerRG16)
        BayerGB16               = ColorType::Mono  | PixelSize::Size_16_Bit | 0x0030, //!< Bayer-color, 16 bits, starting with GB line          (PFNC: BayerGB16)
        BayerBG16               = ColorType::Mono  | PixelSize::Size_16_Bit | 0x0031, //!< Bayer-color, 16 bits, starting with BG line          (PFNC: BayerBG16)

        // rgb formats
        Rgb8                    = ColorType::Color | PixelSize::Size_24_Bit | 0x0014, //!< RGB,  8 bits x 3          (PFNC: RGB8)
        Bgr8                    = ColorType::Color | PixelSize::Size_24_Bit | 0x0015, //!< BGR,  8 bits x 3          (PFNC: BGR8)
        Rgb10                   = ColorType::Color | PixelSize::Size_48_Bit | 0x0018, //!< RGB, 10 bits unpacked x 3 (PFNC: RGB12)
        Bgr10                   = ColorType::Color | PixelSize::Size_48_Bit | 0x0019, //!< RGB, 10 bits unpacked x 3 (PFNC: RGB12)
        Rgb12                   = ColorType::Color | PixelSize::Size_48_Bit | 0x001A, //!< RGB, 12 bits unpacked x 3 (PFNC: RGB12)
        Bgr12                   = ColorType::Color | PixelSize::Size_48_Bit | 0x001B, //!< RGB, 12 bits unpacked x 3 (PFNC: RGB12)
        Rgb14                   = ColorType::Color | PixelSize::Size_48_Bit | 0x005E, //!< RGB, 14 bits unpacked x 3 (PFNC: RGB12)
        Bgr14                   = ColorType::Color | PixelSize::Size_48_Bit | 0x004A, //!< RGB, 14 bits unpacked x 3 (PFNC: RGB12)
        Rgb16                   = ColorType::Color | PixelSize::Size_48_Bit | 0x0033, //!< RGB, 16 bits x 3          (PFNC: RGB16)
        Bgr16                   = ColorType::Color | PixelSize::Size_48_Bit | 0x004B, //!< RGB, 16 bits x 3          (PFNC: RGB16)

        // rgba formats
        Rgba8                   = ColorType::Color | PixelSize::Size_32_Bit | 0x0016, //!< RGBA,  8 bits x 4          (PFNC: RGBa8)
        Bgra8                   = ColorType::Color | PixelSize::Size_32_Bit | 0x0017, //!< BGRA,  8 bits x 4          (PFNC: BGRa8)
        Rgba10                  = ColorType::Color | PixelSize::Size_64_Bit | 0x005F, //!< RGBA, 10 bits unpacked x 4 (PFNC: RGBa10)
        Bgra10                  = ColorType::Color | PixelSize::Size_64_Bit | 0x004C, //!< RGBA, 10 bits unpacked x 4 (PFNC: BGRa10)
        Rgba12                  = ColorType::Color | PixelSize::Size_64_Bit | 0x0061, //!< RGBA, 12 bits unpacked x 4 (PFNC: BGRa12)
        Bgra12                  = ColorType::Color | PixelSize::Size_64_Bit | 0x004E, //!< RGBA, 12 bits unpacked x 4 (PFNC: BGRa12)
        Rgba14                  = ColorType::Color | PixelSize::Size_64_Bit | 0x0063, //!< RGBA, 14 bits unpacked x 4 (PFNC: RGBa14)
        Bgra14                  = ColorType::Color | PixelSize::Size_64_Bit | 0x0050, //!< RGBA, 14 bits unpacked x 4 (PFNC: BGRa14)
        Rgba16                  = ColorType::Color | PixelSize::Size_64_Bit | 0x0064, //!< RGBA, 16 bits x 4          (PFNC: RGBa16)
        Bgra16                  = ColorType::Color | PixelSize::Size_64_Bit | 0x0051, //!< RGBA, 16 bits x 4          (PFNC: BGRa16)

        // yuv/ycbcr formats
        Yuv411                  = ColorType::Color | PixelSize::Size_12_Bit | 0x001E, //!< YUV   4:1:1 8 bit        (PFNC: YUV411_8_UYYVYY, GEV:YUV411Packed)
        Yuv422                  = ColorType::Color | PixelSize::Size_16_Bit | 0x001F, //!< YUV   4:2:2 8 bit        (PFNC: YUV422_8_UYVY,   GEV:YUV422Packed)
        Yuv444                  = ColorType::Color | PixelSize::Size_24_Bit | 0x0020, //!< YUV   4:4:4 8 bit        (PFNC: YUV8_UYV,        GEV:YUV444Packed)
        Yuv422_8                = ColorType::Color | PixelSize::Size_16_Bit | 0x0032, //!< YUV   4:2:2 8 bit        (PFNC: YUV422_8)
        YCbCr8_CbYCr            = ColorType::Color | PixelSize::Size_24_Bit | 0x003A, //!< YCbCr 4:4:4 8 bit        (PFNC: YCbCr8_CbYCr)
        YCbCr422_8              = ColorType::Color | PixelSize::Size_16_Bit | 0x003B, //!< YCbCr 4:2:2 8-bit        (PFNC: YCbCr422_8)
        YCbCr411_8_CbYYCrYY     = ColorType::Color | PixelSize::Size_12_Bit | 0x003C, //!< YCbCr 4:1:1 8 bit        (PFNC: YCbCr411_8_CbYYCrYY)
        YCbCr601_8_CbYCr        = ColorType::Color | PixelSize::Size_24_Bit | 0x003D, //!< YCbCr 4:4:4 8-bit BT.601 (PFNC: YCbCr601_8_CbYCr)
        YCbCr601_422_8          = ColorType::Color | PixelSize::Size_16_Bit | 0x003E, //!< YCbCr 4:2:2 8-bit BT.601 (PFNC: YCbCr601_422_8)
        YCbCr601_411_8_CbYYCrYY = ColorType::Color | PixelSize::Size_12_Bit | 0x003F, //!< YCbCr 4:1:1 8-bit BT.601 (PFNC: YCbCr601_411_8_CbYYCrYY)
        YCbCr709_8_CbYCr        = ColorType::Color | PixelSize::Size_24_Bit | 0x0040, //!< YCbCr 4:4:4 8-bit BT.709 (PFNC: YCbCr709_8_CbYCr)
        YCbCr709_422_8          = ColorType::Color | PixelSize::Size_16_Bit | 0x0041, //!< YCbCr 4:2:2 8-bit BT.709 (PFNC: YCbCr709_422_8)
        YCbCr709_411_8_CbYYCrYY = ColorType::Color | PixelSize::Size_12_Bit | 0x0042, //!< YCbCr 4:1:1 8-bit BT.709 (PFNC: YCbCr709_411_8_CbYYCrYY)
        YCbCr422_8_CbYCrY       = ColorType::Color | PixelSize::Size_16_Bit | 0x0043, //!< YCbCr 4:2:2 8 bit        (PFNC: YCbCr422_8_CbYCrY)
        YCbCr601_422_8_CbYCrY   = ColorType::Color | PixelSize::Size_16_Bit | 0x0044, //!< YCbCr 4:2:2 8-bit BT.601 (PFNC: YCbCr601_422_8_CbYCrY)
        YCbCr709_422_8_CbYCrY   = ColorType::Color | PixelSize::Size_16_Bit | 0x0045, //!< YCbCr 4:2:2 8-bit BT.709 (PFNC: YCbCr709_422_8_CbYCrY)
        YCbCr411_8              = ColorType::Color | PixelSize::Size_12_Bit | 0x005A, //!< YCbCr 4:1:1 8-bit        (PFNC: YCbCr411_8)
        YCbCr8                  = ColorType::Color | PixelSize::Size_24_Bit | 0x005B, //!< YCbCr 4:4:4 8-bit        (PFNC: YCbCr8)
    };

    public:
        // Disables all default constructors.
        GoGdpPixelFormat() = delete;
        GoGdpPixelFormat(const GoGdpPixelFormat&) = delete;
        GoGdpPixelFormat& operator=(const GoGdpPixelFormat) = delete; 

        /**
         * Gets the number of bits for the given pixel format.
         * 
         * @return                  Number of bits contained by the given pixel format.
         */
        static const kSize PixelBits(FormatType type);

        /**
         * Gets the number of bytes for the given pixel format.
         * This returns a double value as packed formats can have a decimal number
         * of bytes per pixel.
         *
         * @return                  Number of bytes contained by the given pixel format.
         */
        static const k64f PixelBytes(FormatType type);
    };
}

#endif // GO_PXL_SDK_GOGDPPIXELFORMAT_H
