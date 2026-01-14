/*
 * @file            ReceiveImage.cpp
 * @brief           Demonstrates receiving heightmap image data via GoPxL Data Protocol (GDP)
 *
 * @details         This sample shows how to receive heightmap images from a Gocator sensor.
 *                  It demonstrates how to:
 *                  - Configure sensor for Image scan mode
 *                  - Enable GoPxL Data Protocol (GDP)
 *                  - Add heightmap image output to GDP output map
 *                  - Connect to GDP server and receive image data synchronously
 *                  - Process heightmap image data and extract pixel values
 *                  - Save heightmap as BMP file for visualization
 *
 *                  Heightmap images encode Z-height information as pixel intensity values to
 *                  provide a visual representation of the scanned surface. The sample
 *                  receives data via GDP and can save it in various pixel formats (Mono8,
 *                  Mono12, Mono16, Bgr8, etc.) as BMP files.
 *
 * GoPxLSdk Sample
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 *
 * Licensed under the MIT License.
 * Redistributions of files must retain the above copyright notice.
 */

#include <GoApi/GoApiLib.h>
#include <kApi/kApiDef.h>
#include <kApi/Data/kArray2.h>
#include <GoPxLSdk/GoSystem.h>
#include <GoPxLSdk/GoChannelError.h>
#include <GoPxLSdk/GoRequestError.h>
#include "../Common/src/SampleUtils.h"

#include <GoPxLSdk/GoGdpClient.h>
#include <GoPxLSdk/GoGdpMsg/GoGdpStamp.h>
#include <GoPxLSdk/GoGdpMsg/GoGdpImage.h>
#include <GoPxLSdk/GoGdpMsg/GoGdpPixelFormat.h>
#include <fstream> // for file handling

using namespace std;
using std::string;
using namespace GoPxLSdk;
using namespace GoPxLSdkSamplesCommon;

// Image Paths
constexpr int IMAGE_MODE                    = 0;

// Control and Replay Paths
const string GOCATOR_CONTROL_PATH           = "/controls/gocator";
const string GOCATOR_OUTPUT_PATH            = GOCATOR_CONTROL_PATH + "/outputs";
const string GOCATOR_ADD_OUTPUT_PATH        = GOCATOR_OUTPUT_PATH + "/commands/add";
const string REPLAY_PATH                    = "/replay/playback";

// Command Timeouts
constexpr int RECEIVE_DATA_TIMEOUT_MSEC     = 60000;

// Network Configuration Addresses
// If sensor is remote-controlled (previously referred to as "accelerated"), 
// use the IP address of the remote controller, typically 127.0.0.1.
constexpr const kChar* SENSOR_IP            = "192.168.1.10";
// The control port is 3600 by default, but could vary for each GoPxL instance.
// To check ports in GoPxL Manager, select an instance, click Modify, and hover over "Base port";
// or check in GoPxL Discovery, where all ports are listed in columns.
constexpr k16u CONTROL_PORT                 = GO_PXL_SDK_DEFAULT_CONTROL_PORT;

// Image File Parameters
constexpr const kChar* imageFilename        = "ImageOutput.bmp";
constexpr k32u BITPERBYTE                   = 8;
constexpr k32u MAX_12U                      = 4095;

// https://en.wikipedia.org/wiki/BMP_file_format
// Bitmap header structure.
typedef struct BmpHeader {
    kByte bitmapSignatureBytes[2] = {'B', 'M'};
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

void WriteOutputToBmpFile(const GoGdpImage & imageSrc)
{
    ofstream fout(imageFilename, ios::binary);

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

    // Write bitmap data.
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

        // Downscale pixel values.
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

        // Write pixel values.
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

int ReceiveImage()
{
    GoSystem system;
    GoJson response;
    GoJson payload;

    std::vector<std::vector<kByte>> ImageBuffer;

    // Connect to sensor.
    kIpAddress systemIpAddress;
    kIpAddress_Parse(&systemIpAddress, SENSOR_IP);
    system.SetAddress(systemIpAddress);
    system.SetControlPort(CONTROL_PORT);

    kChar ipAddress[16];
    kIpAddress_Format(system.Address(), ipAddress, sizeof(ipAddress));

    std::cout << "\nConnecting to " << ipAddress << ":" << system.ControlPort() << "..." << std::endl;
    try
    {
        system.Connect();
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what() << " - Connection failed. Check if sensor is powered on, connected, and using correct IP/port."
                  << std::endl;
        return ERROR_STATUS;
    }

    // Verify that the device is not remote controlled.
    if (VerifyConnection(system) == ERROR_STATUS)
    {
        return ERROR_STATUS;
    }

    // Check if sensor is using replay or live data
    bool replayDataEnabled;
    try
    {
        response = system.Client().Read(REPLAY_PATH).GetResponse().Payload();
        replayDataEnabled = response.Get<bool>("enabled");
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what() << " - Check API path." << std::endl;
        return ERROR_STATUS;
    }

    if (replayDataEnabled)
    {
        std::cout << "\nUsing replay data" << std::endl;
    }
    else
    {
        std::cout << "\nUsing live data" << std::endl;

        // Run the sensor.
        if (system.RunningState() == GoSystem::State::Ready)
        {
            std::cout << "\nStarting system..." << std::endl;
            try
            {
                system.Start();
            }
            catch (const std::exception& e)
            {
                std::cerr << "Error: " << e.what()
                    << " - Failed to run sensor. Check if sensor is powered on and connected."
                    << std::endl;
                return ERROR_STATUS;
            }
        }

        // Change scan mode to Image.
        response = system.Client().Read(SCANNER_PATH).GetResponse().Payload();
        auto scanModeValue = response.At(SCAN_MODE_PATH).Get<int>();

        if (scanModeValue != IMAGE_MODE) {
            payload = GoJson(R"({
                "parameters" : {
                    "scanModeSettings" : {
                        "scanMode" : )" + std::to_string(IMAGE_MODE) + R"(
                    }
                }
            })");

            std::cout << "\nSetting scan mode to Image..." << std::endl;
            try
            {
                system.Client().Update(SCANNER_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
            }
            catch (const std::exception& e)
            {
                std::cerr << "Error: " << e.what()
                    << " - Check if API path is valid or try increasing timeout value."
                    << std::endl;
                return ERROR_STATUS;
            }
        }
    }

    // Enable Gocator Protocol.
    std::cout << "\nEnabling Gocator Protocol..." << std::endl;
    try
    {
        system.Client().Update(GOCATOR_CONTROL_PATH, GoJson("{\"enabled\":true}")).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what() << " - Check timeout value or API path." << std::endl;
        return ERROR_STATUS;
    }

    // Determine the correct data source key depending on engine.
    std::map<std::string, std::string> dataSourceKeys = {
        { "LMILaserLineProfiler", "Image" },
        { "LMIConfocalLineProfiler", "Image" },
        { "LMIFringeSnapshot", "Camera" }
    };

    std::string data_source_key = dataSourceKeys[ENGINE_ID];
    std::string image_data_source_id = "\"scan:" + ENGINE_ID + ":" + SCANNER_ID + ":" + SCAN_ENGINE_COMPONENT + data_source_key + "0\"";

    // Check if image already added to Gocator Protocol output
    bool imageOutputAdded = false;

    try
    {
        response = system.Client().Read(GOCATOR_OUTPUT_PATH).GetResponse().Payload();
        GoJson map = response.Get<GoJson>("map");

        for (GoJsonIterator i = map.Begin(); i != map.End(); i++)
        {
            GoJson value = i.Value();
            if (value.Get<std::string>("source").find(data_source_key))
            {
                imageOutputAdded = true;
                break;
            }
        }
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what() << " - Check API path." << std::endl;
        return ERROR_STATUS;
    }

    if (!imageOutputAdded)
    {
        // Add image to Gocator Protocol output.
        try
        {
            payload = GoJson(R"({
                "source" : )" + image_data_source_id + R"(,
                "outputId" : 0,
                "autoShift" : true
            })");

            system.Client().Call(GOCATOR_ADD_OUTPUT_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
        }
        catch (const std::exception& e)
        {
            std::cerr << "Error: " << e.what() << " - Failed to add profile to Gocator Protocol output. "
                << "Check API path and ensure uniform spacing is enabled, "
                << "or try increasing timeout value."
                << std::endl;
            return ERROR_STATUS;
        }
    }

    // Connect to GDP server.
    std::cout << "\nConnecting to Gocator Protocol..." << std::endl;
    auto gdpClient = std::make_unique<GoGdpClient>();
    try
    {
        gdpClient->Connect(system.Address(), system.GdpPort());
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what() << " - Failed to connect to GDP server." << std::endl;
        return ERROR_STATUS;
    }

    // Receive data synchronously.
    try
    {
        gdpClient->ReceiveDataSync(RECEIVE_DATA_TIMEOUT_MSEC);
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what()
            << " - Failed to receive data via synchronous connection. "
            << "Check that image output is available or try increasing timeout value."
            << std::endl;
        return ERROR_STATUS;
    }

    const GoDataSet& receivedDataSet = gdpClient->DataSet();

    std::cout << "\nTotal number of messages in this data set: " << receivedDataSet.Count() << std::endl;

    // Loop through messages in the dataSet.
    for (size_t msgIndex = 0; msgIndex < receivedDataSet.Count(); msgIndex++)
    {
        std::cout << std::string(64, '-') << " GDP Output Source " << msgIndex + 1 << std::endl;
        const auto& msg = receivedDataSet.GdpMsgAt(msgIndex);

            switch (msg.Type())
        {
            case GoPxLSdk::MessageType::IMAGE:
            {
                const GoGdpImage& imageMsg = static_cast<const GoGdpImage&>(msg);
                std::cout << "\nMessage type: Image" << std::endl;
                std::cout << "GDP ID: " << imageMsg.GdpId() << std::endl;
                std::cout << "Date Source ID: " << imageMsg.DataSourceId() << std::endl;
            
                if (imageMsg.ArrayedCount() == 0) {
                    std::cout << "Not arrayed" << std::endl;
                }
                else {
                    std::cout << "Arrayed" << std::endl;
                    std::cout << "\tCount: " << imageMsg.ArrayedCount() << std::endl;
                    std::cout << "\tIndex: " << imageMsg.ArrayedIndex() << std::endl;
                }

                std::cout << "FlippedX: " << imageMsg.FlippedX() << std::endl;
                std::cout << "FlippedY: " << imageMsg.FlippedY() << std::endl;

                std::cout << "Height: " << imageMsg.Height() << std::endl;
                std::cout << "Width: " << imageMsg.Width() << std::endl;
                std::cout << "PixelSize: " << imageMsg.PixelSize() << std::endl;
                std::cout << "PixelFormat: " << imageMsg.PixelFormat() << std::endl;
                std::cout << "ColorFilter: " << imageMsg.ColorFilter() << std::endl;

                std::cout << "Resolution X: " << imageMsg.Resolution().x << std::endl;
                std::cout << "Resolution Y: " << imageMsg.Resolution().y << std::endl;
                std::cout << "Offset X: " << imageMsg.Offset().x << std::endl;
                std::cout << "Offset Y: " << imageMsg.Offset().y << std::endl;

                if (imageMsg.PixelFormat() == GoGdpPixelFormat::FormatType::Greyscale_8BPP ||
                    imageMsg.PixelFormat() == GoGdpPixelFormat::FormatType::Mono8 ||
                    imageMsg.PixelFormat() == GoGdpPixelFormat::FormatType::Bgr8 ||
                    imageMsg.PixelFormat() == GoGdpPixelFormat::FormatType::Mono12 ||
                    imageMsg.PixelFormat() == GoGdpPixelFormat::FormatType::Mono12p ||
                    imageMsg.PixelFormat() == GoGdpPixelFormat::FormatType::Mono16)
                {
                    WriteOutputToBmpFile(imageMsg);
                    std::cout << "\nImage saved as '" << imageFilename << "'" << std::endl;
                }

                break;
            }
            default:
                std::cout << "\nNo image found in the message. "
                    << "Check that scan data is being collected and that scan target is in range.\n" 
                    << std::endl;
                break;
        }
    }

    std::ignore = getchar();
    gdpClient->Close();
    system.Stop();
    system.Disconnect();
    return OK_STATUS;
}

int main(int argc, char** argv)
{
    int status;
    kStatus gpApiLibConstructionStatus;
    kAssembly goApiLib = kNULL;

    try
    {
        // Constructs GoPxL API core framework.
        // GOS-7780: It is important to construct goApiLib before declaration of GoSystem, 
        // this is because GoSystem implicitly calls constructor for GoRestClient. 
        if ((gpApiLibConstructionStatus = GoApiLib_Construct(&goApiLib)) != kOK)
        {
            std::cout << "Error: " << gpApiLibConstructionStatus << " - Failed to construct GoApiLib." << std::endl;
            return ERROR_STATUS;
        }
        status = ReceiveImage();
    }
    catch (const GoRequestError& e)
    {
        string errorMessage = "";

        const GoJson& payload = e.GetResponse().Payload();
        if (!payload.Empty() && payload.HasKey("errors"))
        {
            GoJson errors = payload.At("errors");
            errorMessage.append(errors.At("/0/status").ToString());
            errorMessage.append(" ");
            errorMessage.append(errors.At("/0/description").ToString());
        }

        std::cerr << "GoRequestError: " << errorMessage << std::endl;
        std::cerr << "Error sending a REST command to " << e.GetResponse().Path() << std::endl;
        status = ERROR_STATUS;
    }
    catch (const GoChannelError& e)
    {
        std::cerr << "Error: " << e.what()
                  << " - Check sensor status, ensure it is connected, or try increasing timeout value."
                  << std::endl;
        status = ERROR_STATUS;
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what() << std::endl;
        status = ERROR_STATUS;
    }

    kDestroyRef(&goApiLib);
    return status;
}
