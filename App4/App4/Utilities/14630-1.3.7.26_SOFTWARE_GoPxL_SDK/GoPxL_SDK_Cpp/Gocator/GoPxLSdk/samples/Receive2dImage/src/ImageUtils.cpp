#include "ImageUtils.h"
#include <kApi/Data/kArray2.h>
#include <kApi/Data/kImage.h>
#include <fstream> // for file handling

using namespace GoPxLSdk;

constexpr k32u BITPERBYTE = 8;
constexpr k32u MAX_12U = 4095;

// https://en.wikipedia.org/wiki/BMP_file_format
// Bitmap header structure.
typedef struct BmpHeader {
    kByte bitmapSignatureBytes[2] = { 'B', 'M' };
    k32u sizeOfBitmapFile = 54 + 30000;
    k32u reservedBytes = 0;
    k32u pixelDataOffset = 54;
} BmpHeader;

// Bitmap info header structure.
typedef struct BmpInfoHeader {
    k32u sizeOfThisHeader = 40;
    k32u width = 100; // in pixels
    k32u height = 100; // in pixels
    k16u numberOfColorPlanes = 1; // must be 1
    k16u colorDepth = 24;
    k32u compressionMethod = 0;
    k32u rawBitmapDataSize = 0; // generally ignored
    k32u horizontalResolution = 512; // in pixel per meter
    k32u verticalResolution = 512; // in pixel per meter
    k32u colorTableEntries = 0;
    k32u importantColors = 0;

    // helper fields, not part of the actual BMP header
    // Always has 3 colors, Blue/Green/Red.
    k32u numberOfChannels = 3;
} BmpInfoHeader;

void WriteOutputToBmpFile(const GoGdpImage& imageSrc)
{
    constexpr const kChar* imageFilename = "ImageOutput.bmp";

    std::ofstream fout(imageFilename, std::ios::binary);

    BmpHeader header;
    BmpInfoHeader infoHeader;

    infoHeader.colorDepth = (k16u)(BITPERBYTE * infoHeader.numberOfChannels);

    header.sizeOfBitmapFile = header.pixelDataOffset +
        imageSrc.Height() * imageSrc.Width() * infoHeader.numberOfChannels;
    infoHeader.height = imageSrc.Height();
    infoHeader.width = imageSrc.Width();

    // Write bitmap header.
    fout.write((char*)header.bitmapSignatureBytes, 2);
    fout.write((char*)&header.sizeOfBitmapFile, sizeof(k32u));
    fout.write((char*)&header.reservedBytes, sizeof(k32u));
    fout.write((char*)&header.pixelDataOffset, sizeof(k32u));

    // Write bitmap info.
    fout.write((char*)&infoHeader.sizeOfThisHeader, sizeof(k32u));
    fout.write((char*)&infoHeader.width, sizeof(k32u));
    fout.write((char*)&infoHeader.height, sizeof(k32u));
    fout.write((char*)&infoHeader.numberOfColorPlanes, sizeof(k16u));
    fout.write((char*)&infoHeader.colorDepth, sizeof(k16u));
    fout.write((char*)&infoHeader.compressionMethod, sizeof(k32u));
    fout.write((char*)&infoHeader.rawBitmapDataSize, sizeof(k32u));
    fout.write((char*)&infoHeader.horizontalResolution, sizeof(k32u));
    fout.write((char*)&infoHeader.verticalResolution, sizeof(k32u));
    fout.write((char*)&infoHeader.colorTableEntries, sizeof(k32u));
    fout.write((char*)&infoHeader.importantColors, sizeof(k32u));

    // WRite bitmap data.
    kSize numberOfPixels = infoHeader.width * infoHeader.height;

    const kArray2 pixelData = imageSrc.Pixels();
    k8u* pixel8bitBuffer = (k8u*)kArray2_Data(pixelData);
    k16u* pixel16bitBuffer = (k16u*)kArray2_Data(pixelData);

    // 1) The order of 3 colors per pixel is Blue/Green/Red.
    // 2) Because Bitmap format always uses B/G/R 3-color channels per pixel,
    //    for mono/greyscale image, we will use the same data for all 3 
    //    Blue/Green/Red channels per pixel.
    // 3) Bitmap format displays rows of data bottom-up, so image will appear as 
    //    flipped over x-axis. To fix this, row data will be accessed in reversed order.

    kSize row = 0;
    kSize flippedRow = 0;
    kSize pixelIndex = 0;

    // Mono12p is the 12bit packed data format.
    // Each pixel takes 12bits and leftover 4 bits is used by the next pixel.
    // 
    // Byte| MSB <------C------> LSB | MSB <------B------> LSB | MSB <------A------> LSB |
    // Bit:| c7 c6 c5 c4 c3 c2 c1 c0 | b7 b6 b5 b4 b3 b2 b1 b0 | a7 a6 a5 a4 a3 a2 a1 a0 |
    //     |              Pixel 1                 |                Pixel 0               |
    // 
    // A) To unpack the pixel 0 data:
    // 1) Use Byte B.
    //  -> | b7 b6 b5 b4 b3 b2 b1 b0 |
    // 2) Mask off first 4 MSB since they belong to next pixel.
    //  -> | 00 00 00 00 b3 b2 b1 b0 |
    // 3) Copy from 8bit buffer into 16bit buffer and left shift by 8.
    //  -> | 00 00 00 00 b3 b2 b1 b0 00 00 00 00 00 00 00 00 |
    // 4) Adds Byte A.
    //  -> +                       | a7 a6 a5 a4 a3 a2 a1 a0 |
    // 5) Gets the new pixe 0 value in 12bits representation in 16bit buffer.
    //  -> | 00 00 00 00 b3 b2 b1 b0 a7 a6 a5 a4 a3 a2 a1 a0 |

    // B) To unpack the pixel 1 data:
    // 1) Use Byte C.
    //  -> | c7 c6 c5 c4 c3 c2 c1 c0 |
    // 2) Copy from 8bit buffer into 16bit buffer and left shift by 4.
    //  -> | 00 00 00 00 c7 c6 c5 c4 c3 c2 c1 c0 00 00 00 00 |
    // 3) Use Byte B.
    //  -> | b7 b6 b5 b4 b3 b2 b1 b0 |
    // 4) Right shift by 4.
    //  -> | 00 00 00 00 b7 b6 b5 b4 |
    // 5) Adds 2) and 5)
    //  -> | 00 00 00 00 c7 c6 c5 c4 c3 c2 c1 c0 b7 b6 b5 b4 |

    // Lastly, because Bitmap format does not support 12bit representation of pixels,
    // we need to downscale all pixel values from 12bit range into 8bit range.
    // It is done by multiple the original value with a conversion factor between maximum value of 8bit vs 12bit.
    // ie, pixel value 0xD73, or 3443 in 12bit converts to 0xD6 or 214 in 8bit number.
    // or, 3443 * 255 / 4095 = 214.
    if (imageSrc.PixelFormat() == GoGdpPixelFormat::FormatType::Mono12p)
    {
        std::vector<kByte> flippedImageData;

        flippedImageData.reserve(imageSrc.Height() * imageSrc.Width() * infoHeader.numberOfChannels);

        for (kSize i = 0; i < numberOfPixels; i++)
        {
            k16u newPixel;

            if (i % 2 == 0)
            {
                pixelIndex = 1 + i / 2 * 3;
                newPixel = pixel8bitBuffer[pixelIndex] & 0xf;
                newPixel = (newPixel << 8) | (pixel8bitBuffer[pixelIndex - 1]);

                newPixel = (k32u)newPixel * k8U_MAX / MAX_12U;

                flippedImageData.push_back(newPixel & 0xff);
                flippedImageData.push_back(newPixel & 0xff);
                flippedImageData.push_back(newPixel & 0xff);
            }
            else
            {
                pixelIndex = 2 + (i - 1) / 2 * 3;
                newPixel = pixel8bitBuffer[pixelIndex];
                newPixel = (newPixel << 4) | (pixel8bitBuffer[pixelIndex - 1] >> 4);

                newPixel = (k32u)newPixel * k8U_MAX / MAX_12U;

                flippedImageData.push_back(newPixel & 0xff);
                flippedImageData.push_back(newPixel & 0xff);
                flippedImageData.push_back(newPixel & 0xff);
            }
        }

        for (kSize i = 0; i < numberOfPixels; i++)
        {
            row = i / infoHeader.width;
            flippedRow = infoHeader.height - row - 1;

            pixelIndex = flippedRow * infoHeader.width * infoHeader.numberOfChannels + i % infoHeader.width * infoHeader.numberOfChannels;

            fout.put(flippedImageData.at(pixelIndex + 0));
            fout.put(flippedImageData.at(pixelIndex + 1));
            fout.put(flippedImageData.at(pixelIndex + 2));
        }
    }
    // All unpacked formats.
    else
    {
        for (kSize i = 0; i < numberOfPixels; i++)
        {
            // Calculates the original row number and the 'flipped' row number
            // due to BMP being bottom-up in orientation.
            row = i / infoHeader.width;
            flippedRow = infoHeader.height - row - 1;

            if (imageSrc.PixelFormat() == GoGdpPixelFormat::FormatType::Greyscale_8BPP ||
                imageSrc.PixelFormat() == GoGdpPixelFormat::FormatType::Mono8)
            {
                pixelIndex = flippedRow * infoHeader.width + i % infoHeader.width;

                // Writes all 3 Blue/Green/Red the same value to emulate greyscale color for Bitmap.
                fout.put(pixel8bitBuffer[pixelIndex]);
                fout.put(pixel8bitBuffer[pixelIndex]);
                fout.put(pixel8bitBuffer[pixelIndex]);
            }
            else if (imageSrc.PixelFormat() == GoGdpPixelFormat::FormatType::Rgb8)
            {
                // RGB8 uses 3 different color channel for Red/Green/Blue thus 3 bytes per pixel.
                pixelIndex = flippedRow * infoHeader.width * infoHeader.numberOfChannels + i % infoHeader.width * infoHeader.numberOfChannels;

                // Writes each of the Red/Green/Blue per pixel.
                fout.put(pixel8bitBuffer[pixelIndex + 2]);
                fout.put(pixel8bitBuffer[pixelIndex + 1]);
                fout.put(pixel8bitBuffer[pixelIndex + 0]);
            }
            else if (imageSrc.PixelFormat() == GoGdpPixelFormat::FormatType::Bgr8)
            {
                // BGR8 uses 3 different color channel for Blue/Green/Red thus 3 bytes per pixel.
                pixelIndex = flippedRow * infoHeader.width * infoHeader.numberOfChannels + i % infoHeader.width * infoHeader.numberOfChannels;

                // Writes each of the Blue/Green/Red per pixel.
                fout.put(pixel8bitBuffer[pixelIndex + 0]);
                fout.put(pixel8bitBuffer[pixelIndex + 1]);
                fout.put(pixel8bitBuffer[pixelIndex + 2]);
            }
            // MONO12 uses 12 bits per pixel, thus 4 bits are padded with 0s to the full 16 bits size.
            // Because bitmap format does not support 16bit image, the 12bit value will be downscaled to 8bit.
            else if (imageSrc.PixelFormat() == GoGdpPixelFormat::FormatType::Mono12)
            {
                pixelIndex = flippedRow * infoHeader.width + i % infoHeader.width;

                // Downscaling the 12bit value to 8bit value.
                k16u newPixel = pixel16bitBuffer[pixelIndex];
                newPixel = (k32u)newPixel * k8U_MAX / MAX_12U;

                fout.put(newPixel & 0xff);
                fout.put(newPixel & 0xff);
                fout.put(newPixel & 0xff);
            }
            // MONO16 uses 16 bits per pixel.
            // Because bitmap format does not support 16bit image, the 12bit value will be downscaled to 8bit.
            else if (imageSrc.PixelFormat() == GoGdpPixelFormat::FormatType::Mono16)
            {
                pixelIndex = flippedRow * infoHeader.width + i % infoHeader.width;

                // Downscaling the 12bit value to 8bit value.
                k16u newPixel = pixel16bitBuffer[pixelIndex];
                newPixel = (k32u)newPixel * k8U_MAX / k16U_MAX;

                fout.put(newPixel & 0xff);
                fout.put(newPixel & 0xff);
                fout.put(newPixel & 0xff);
            }
        }
    }
    fout.close();
}
