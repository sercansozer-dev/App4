/**
 * @file    GoGdpPixelFormat.cpp
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#include <GoPxLSdk/GoGdpMsg/GoGdpPixelFormat.h>
#include <kApi/Data/kImage.h>

namespace GoPxLSdk
{
    const kSize GoGdpPixelFormat::PixelBits(FormatType type)
    {
        switch (type)
        {
            //kPixelFormats
            case FormatType::Null_Format:       return  0;
            case FormatType::Greyscale_8BPP:
            case FormatType::CFA_8BPP:          return  8;
            case FormatType::BGRX_8BPC:         return 32;
            case FormatType::Greyscale_1BPP:    return  1;
            case FormatType::Greyscale_16BPP:   return 16;
            case FormatType::BGR_8BPC:          return 24;
            //New style pixel formats following PFNC. Size (bits) is encoded in second most-significant byte
            default: return (type >> 16) & 0xFF;
        }
    }

    const k64f GoGdpPixelFormat::PixelBytes(FormatType type)
    {
        kSize bitsPerPixel = PixelBits(type);

        // Divide the number of bits per pixel by 8 and return the double value.
        k64f bytesPerPixel = (k64f)bitsPerPixel / 8;

        return bytesPerPixel;
    }
}
