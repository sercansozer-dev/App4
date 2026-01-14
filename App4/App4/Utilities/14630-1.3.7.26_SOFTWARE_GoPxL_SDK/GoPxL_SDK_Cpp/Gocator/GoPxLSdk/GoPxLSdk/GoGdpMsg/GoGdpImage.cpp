/**
 * @file    GoGdpImage.cpp
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#include <GoPxLSdk/GoGdpMsg/GoGdpImage.h>
#include <GoPxLSdk/GoGdpMsg/GoGdpPixelFormat.h>
#include <kApi/Data/kArray2.h>

namespace GoPxLSdk
{
    GoGdpImage::GoGdpImage() :
        GoGdpMsg(MessageType::IMAGE),
        resolution({ k64F_NULL, k64F_NULL }),
        offset({ k64F_NULL, k64F_NULL })
    {
    }

    void GoGdpImage::Deserialize(kSerializer serializer)
    {
        kSize rowSize;
        bool needEndRead = false;

        GoGdpMsg::Deserialize(serializer);

        try
        {
            k16u reserved16;
            k8u byteValue;

            GoTest(kSerializer_BeginRead(serializer, kTypeOf(k16u), kTRUE));
            needEndRead = true;
            GoTest(kSerializer_Read32u(serializer, &height));
            GoTest(kSerializer_Read32u(serializer, &width));
            GoTest(kSerializer_Read32u(serializer, &pixelSize));
            GoTest(kSerializer_Read32s(serializer, (k32s*)&pixelFormat));
            GoTest(kSerializer_Read32s(serializer, &colorFilter));
            GoTest(kSerializer_Read16u(serializer, &reserved16));

            GoTest(kSerializer_Read32f(serializer, &exposure));
            GoTest(kSerializer_Read8u(serializer, &byteValue));
            flippedX = (byteValue > 0);
            GoTest(kSerializer_Read8u(serializer, &byteValue));
            flippedY = (byteValue > 0);
            GoTest(kSerializer_Read8u(serializer, &byteValue));
            columnBased = (byteValue > 0);

            // These fields were added in GoPxL 1.3. Messages from older versions will not contain them.
            if (!kSerializer_ReadCompleted(serializer))
            {
                GoTest(kSerializer_Read64f(serializer, &resolution.x));
                GoTest(kSerializer_Read64f(serializer, &resolution.y));

                GoTest(kSerializer_Read64f(serializer, &offset.x));
                GoTest(kSerializer_Read64f(serializer, &offset.y));
            }

            GoTest(kSerializer_EndRead(serializer));
            needEndRead = false;

            rowSize = RowSize();

            // GOS-12890 : Check whether the current section has at least the number of bytes left to be deserialized for pixel data.
            k64u bytesRemaining = kSerializer_ReadBytesLeft(serializer);
            k64u bytesNeeded = height * rowSize * kType_Size(kTypeOf(k8u));

            GoThrowMsgIf(bytesRemaining < bytesNeeded, 
                kERROR_INCOMPLETE,
                "Insufficient bytes left in the current section to deserialize image pixel data. %llu bytes are needed, but only %llu bytes are left.",
                bytesNeeded, bytesRemaining);

            GoTest(kArray2_Construct(pixels.Ref(), kTypeOf(k8u), height, rowSize, kAlloc_App()));
            GoTest(kSerializer_Read8uArray(serializer, kArray2_DataT(pixels, k8u), height * rowSize));
        }
        catch (const Go::Exception&)
        {
            if (needEndRead)
            {
                kSerializer_EndRead(serializer);
            }

            GoRethrow("Failed to deserialize Image GDP message.");
        }
    }

    const k32u GoGdpImage::Height() const
    {
        return height;
    }

    const k32u GoGdpImage::Width() const
    {
        return width;
    }

    const k32u GoGdpImage::PixelSize() const
    {
        return pixelSize;
    }

    const GoGdpPixelFormat::FormatType GoGdpImage::PixelFormat() const
    {
        return pixelFormat;
    }

    const kCfa GoGdpImage::ColorFilter() const
    {
        return colorFilter;
    }

    const k32f GoGdpImage::Exposure() const
    {
        return exposure;
    }

    const bool GoGdpImage::FlippedX() const
    {
        return flippedX;
    }

    const bool GoGdpImage::FlippedY() const
    {
        return flippedY;
    }

    const bool GoGdpImage::ColumnBased() const
    {
        return columnBased;
    }

    const kPoint64f GoGdpImage::Resolution() const
    {
        return resolution;
    }

    const kPoint64f GoGdpImage::Offset() const
    {
        return offset;
    }

    const kArray2 GoGdpImage::Pixels() const
    {
        return pixels;
    }

    const kSize GoGdpImage::RowSize() const
    {
        if (pixelSize == 0 && colorFilter == kCFA_NONE)
        {
            return (kSize)std::ceil(width * GoGdpPixelFormat::PixelBytes(pixelFormat));
        }
        else
        {
            return width * pixelSize;
        }
    }

}
