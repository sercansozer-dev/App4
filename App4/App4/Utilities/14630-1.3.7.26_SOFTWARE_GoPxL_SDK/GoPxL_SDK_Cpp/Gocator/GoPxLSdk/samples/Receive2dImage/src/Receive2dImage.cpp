/*
 * @file            Receive2dImage.cpp
 * @brief           Demonstrates receiving 2D camera images via GoPxL Data Protocol (GDP)
 *
 * @details         This sample shows how to receive 2D camera image data from a direct
 *                  Ethernet connection to a sensor.
 *                  It demonstrates how to:
 *                  - Load a specific job configuration
 *                  - Configure camera acquisition mode and pixel format (RGB8)
 *                  - Set exposure time for the camera
 *                  - Set up a Script tool for image processing
 *                  - Enable GoPxL Data Protocol (GDP)
 *                  - Add 2D image output to GDP output map
 *                  - Connect to GDP server and receive image data synchronously
 *                  - Process and save received image data as BMP file
 *
 *                  This sample uses direct Ethernet connection and demonstrates camera
 *                  configuration, script tool setup for image manipulation, and image
 *                  processing via GDP. The Script tool processes images and generates
 *                  statistics (min, max, mean intensity) as measurements.
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
#include "ImageUtils.h"

using std::string;
using namespace GoPxLSdk;
using namespace GoPxLSdkSamplesCommon;

// Image Paths
constexpr int IMAGE_MODE                        = 0;
const string IMAGE_DATA_SOURCE_KEY              = "Image";
const string IMAGE_DATA_SOURCE_ID               = OUTPUTS_PATH + "/" + SENSOR_ID + IMAGE_DATA_SOURCE_KEY + "0";

// Job Paths
const string JOB_NAME                           = "job01";
const string LOAD_JOB_CMD                       = "/jobs/commands/load";

// Control Paths
const string GOCATOR_CONTROL_PATH               = "/controls/gocator";
const string GOCATOR_ADD_OUTPUT_PATH            = GOCATOR_CONTROL_PATH + "/outputs/commands/add";

// Command Timeouts
constexpr int RECEIVE_DATA_TIMEOUT_MSEC         = 20000;

// Network Configuration Addresses
// If sensor is remote-controlled (previously referred to as ""accelerated""), 
// use the IP address of the remote controller, typically 127.0.0.1.
constexpr const kChar* SENSOR_IP                = "192.168.1.10";
// The control port is 3600 by default, but could vary for each GoPxL instance.
// To check ports in GoPxL Manager, select an instance, click Modify, and hover over "Base port";
// or check in GoPxL Discovery, where all ports are listed in columns.
constexpr k16u CONTROL_PORT                     = GO_PXL_SDK_DEFAULT_CONTROL_PORT;

class Receive2dImage
{

public:

    void Connect(const char* ip, int port)
    {
        GoJson response;
        GoJson payload;

        // Connect to sensor.
        kIpAddress systemIpAddress;
        kIpAddress_Parse(&systemIpAddress, SENSOR_IP);
        goSystem.SetAddress(systemIpAddress);
        goSystem.SetControlPort(CONTROL_PORT);

        kChar ipAddress[16];
        kIpAddress_Format(goSystem.Address(), ipAddress, sizeof(ipAddress));

        std::cout << "\nConnecting to " << ipAddress << ":" << goSystem.ControlPort() << "..." << std::endl;
        try
        {
            goSystem.Connect();
        }
        catch (const std::exception& e)
        {
            std::cerr << "Error: " << e.what() << " - Connection failed. Check if sensor is powered on, connected, and using correct IP/port."
                << std::endl;
            throw;
        }

        // Verify that the device is not remote controlled.
        if (VerifyConnection(goSystem) == ERROR_STATUS)
        {
            throw;
        }

        // Stop the system before making any changes
        goSystem.Stop();
    }

    void ChangeJob(const std::string& jobName)
    {
        GoJson payload = GoJson(R"({
            "name" : ")" + jobName + R"(",
            "enabled" : true
        })");

        try
        {
            goSystem.Client().Call(LOAD_JOB_CMD, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
        }
        catch (std::exception& ex)
        {
            std::cerr << "Error changing job file: " << ex.what() << std::endl;
            std::cerr << "Check that the job '" << jobName << "' is available." << std::endl;
            throw;
        }
    }

    void SetContinuousAcquisitionMode()
    {
        GoJson payload = GoJson(R"({
            "parameters" : {
                "allFeatures" : {
                    "Camera" : {
                        "AcquisitionControl" : {
                            "AcquisitionMode" : 1
                        }
                    }
                }
            }
        })");

        try
        {
            goSystem.Client().Update(SENSOR_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
        }
        catch (const std::exception& ex)
        {
            std::cerr << "Error setting acquisition mode: " << ex.what() << std::endl;
            throw;
        }
    }

    void SetPixelFormat()
    {
        // First set the color filter to RGBl
        try
        {
            GoJson payload = GoJson(R"({
                "parameters" : {
                    "allFeatures" : {
                        "Camera" : {
                            "ImageFormatControl" : {
                                "PixelColorFilter" : 1
                            }
                        }
                    }
                }
            })");

            goSystem.Client().Update(SENSOR_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
        }
        catch (const std::exception& ex)
        {
            std::cerr << "Error setting pixel color filter: " << ex.what() << std::endl;
            throw;
        }

        // Then set the color pixel format to RGB8
        try
        {
            GoJson payload = GoJson(R"({
                "parameters" : {
                    "allFeatures" : {
                        "Camera" : {
                            "ImageFormatControl" : {
                                "PixelFormat" : )" + std::to_string((int)(GoGdpPixelFormat::Rgb8)) + R"(
                            }
                        }
                    }
                }
            })");

            goSystem.Client().Update(SENSOR_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
        }
        catch (const std::exception& ex)
        {
            std::cerr << "Error setting pixel format: " << ex.what() << std::endl;
            throw;
        }
    }

    void SetExposure(k64f exposure)
    {
        try
        {
            GoJson payload = GoJson(R"({
                "parameters" : {
                    "allFeatures" : {
                        "Camera" : {
                            "AcquisitionControl" : {
                                "ExposureTime" : )" + std::to_string(exposure) + R"(
                            }
                        }
                    }
                }
            })");

            goSystem.Client().Update(SENSOR_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
        }
        catch (const std::exception& ex)
        {
            std::cerr << "Error setting exposure value: " << ex.what() << std::endl;
            throw;
        }
    }

    void SetupScriptTool()
    {
        GoJson payload;

        std::string CODE = "\"";
        CODE += "im = get_image(0)\\n\\n";
        CODE += "new_pixels = im.pixels.copy()\\n\\n";
        CODE += "factor = 3\\n";
        CODE += "new_pixels = numpy.clip(new_pixels * float(factor), 0, 255).astype(numpy.uint8)\\n\\n";
        CODE += "min_intensity = numpy.min(new_pixels)\\n";
        CODE += "max_intensity = numpy.max(new_pixels)\\n";
        CODE += "mean_intensity = numpy.mean(new_pixels)\\n\\n";
        CODE += "send_image(0, new_pixels, PixelFormat.RGB_8)\\n\\n";
        CODE += "send_measurement(1, min_intensity)\\n";
        CODE += "send_measurement(2, max_intensity)\\n";
        CODE += "send_measurement(3, mean_intensity)\\n";
        CODE += "\"";

        payload = GoJson(R"({
            "type" : "Script",
            "autoConnect" : true,
            "position" : 0
        })");

        goSystem.Client().Create("/tools/", payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);

        payload = GoJson(R"({
            "parameters" : {
                "NumberOfOutputs" : 4
            }
        })");
        goSystem.Client().Update("/tools/Script-0", payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);

        // Set output types (18 is Image and 5 is Measurement)
        payload = GoJson(R"({
            "parameters" : {
                "Output0Type" : 18,
                "Output1Type" : 5, 
                "Output2Type" : 5, 
                "Output3Type" : 5
            }
        })");
        goSystem.Client().Update("/tools/Script-0", payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);

        // Setup code
        payload = GoJson(R"({
            "parameters" : {
                "Code" : {
                    "code" : )" + CODE + R"(
                }
            }
        })");
        goSystem.Client().Update("/tools/Script-0", payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
    }

    void Receive()
    {
        // Change scan mode to Image.
        auto response = goSystem.Client().Read(SCANNER_PATH).GetResponse().Payload();
        auto scanModeValue = response.At(SCAN_MODE_PATH).Get<int>();

        if (scanModeValue != IMAGE_MODE)
        {
            auto payload = GoJson(R"({
            "parameters" : {
                "scanModeSettings" : {
                    "scanMode" : )" + std::to_string(IMAGE_MODE) + R"(
                }
            }
        })");

            std::cout << "\nSetting scan mode to Image..." << std::endl;
            try
            {
                goSystem.Client().Update(SCANNER_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
            }
            catch (const std::exception& e)
            {
                std::cerr << "Error: " << e.what()
                    << " - Check if API path is valid or try increasing timeout value."
                    << std::endl;
                throw;
            }
        }

        // Enable Gocator Protocol.
        std::cout << "\nEnabling Gocator Protocol..." << std::endl;
        try
        {
            goSystem.Client().Update(GOCATOR_CONTROL_PATH, GoJson("{\"enabled\":true}")).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
        }
        catch (const std::exception& e)
        {
            std::cerr << "Error: " << e.what() << " - Failed to enable Gocator Protocol. Check timeout value or API path." << std::endl;
            throw;
        }

        // Add image to Gocator Protocol output.
        try
        {
            auto response = goSystem.Client().Read(IMAGE_DATA_SOURCE_ID).GetResponse().Payload();
            string surfaceDataSourceId = response.At("dataSourceId").ToString();
            std::cout << surfaceDataSourceId << std::endl;

            auto payload = GoJson(R"({
                "source" : )" + surfaceDataSourceId + R"(,
                "outputId" : 0,
                "autoShift" : true
            })");

            goSystem.Client().Call(GOCATOR_ADD_OUTPUT_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
        }
        catch (const std::exception& e)
        {
            std::cerr << "Error: " << e.what() << " - Failed to add image data to Gocator Protocol output. "
                << "Try increasing timeout value."
                << std::endl;
            throw;
        }

        // Connect to GDP server.
        std::cout << "\nConnecting to Gocator Protocol..." << std::endl;
        gdpClient = std::make_unique<GoGdpClient>();
        try
        {
            gdpClient->Connect(goSystem.Address(), goSystem.GdpPort());
        }
        catch (const std::exception& e)
        {
            std::cerr << "Error: " << e.what() << " - Failed to connect to GDP server." << std::endl;
            throw;
        }

        // Run the sensor.
        if (goSystem.RunningState() == GoSystem::State::Ready)
        {
            std::cout << "\nStarting system..." << std::endl;
            try
            {
                goSystem.Start();
            }
            catch (const std::exception& e)
            {
                std::cerr << "Error: " << e.what()
                    << " - Failed to run sensor. Check if sensor is powered on and connected."
                    << std::endl;
                throw;
            }
        }

        bool imageReceived = false;
        time_t timeout = time(NULL) + 10; // Define a 10-second timeout
        std::cout << "\nWaiting until a dataset with Image data is received within 10 seconds." << std::endl;

        // Repeat receiving data within the specified timeout until image data is received.
        while (!imageReceived && time(NULL) < timeout)
        {
            // Receive data synchronously.
            try
            {
                gdpClient->ReceiveDataSync(RECEIVE_DATA_TIMEOUT_MSEC);
            }
            catch (const std::exception& e)
            {
                std::cerr << "Error: " << e.what()
                    << " - Failed to receive data via synchronous connection. Try increasing timeout value."
                    << std::endl;
                throw;
            }

            std::cout << ".";

            const GoDataSet& receivedDataSet = gdpClient->DataSet();

            if (receivedDataSet.Count() < 1)
            {
                std::cout << "No data is received." << std::endl;
                throw;
            }

            // Loop through messages in the dataSet.
            for (size_t msgIndex = 0; msgIndex < receivedDataSet.Count(); msgIndex++)
            {
                if (receivedDataSet.GdpMsgAt(msgIndex).Type() == GoPxLSdk::MessageType::IMAGE)
                {
                    std::cout << "\nA dataset with an Image message has been received." << std::endl;
                    imageReceived = true;
                    break;
                }
            }
        }

        const GoDataSet& receivedDataSet = gdpClient->DataSet();

        std::cout << "\nTotal number of messages in this data set: " << receivedDataSet.Count() << std::endl;
        // Loop through messages in the dataSet.
        for (size_t msgIndex = 0; msgIndex < receivedDataSet.Count(); msgIndex++)
        {
            std::cout << "\nMessage " << msgIndex + 1 << " of " << receivedDataSet.Count() << std::endl;
            std::cout << "Message Type " << (int)receivedDataSet.GdpMsgAt(msgIndex).Type() << ": "
                << receivedDataSet.GdpMsgAt(msgIndex).DataSourceId() << std::endl;

            switch (receivedDataSet.GdpMsgAt(msgIndex).Type())
            {
            case GoPxLSdk::MessageType::STAMP:
            {
                const GoGdpStamp& stampMsg = (const GoGdpStamp&)receivedDataSet.GdpMsgAt(msgIndex);
                std::cout << "\nStamp" << std::endl;
                std::cout << "Frame index: " << stampMsg.FrameIndex() << std::endl;
                std::cout << "Timestamp: " << stampMsg.Timestamp() << std::endl;
                std::cout << "Encoder: " << stampMsg.Encoder() << std::endl;
                std::cout << "Encoder at Z: " << stampMsg.EncoderAtZ() << std::endl;
                std::cout << "Status: " << stampMsg.Status() << std::endl;
                std::cout << "System Time Sec: " << stampMsg.SystemTimeSeconds() << std::endl;
                std::cout << "System Time Nanosec: " << stampMsg.SystemTimeNanoseconds() << std::endl;
                break;
            }
            case GoPxLSdk::MessageType::IMAGE:
            {
                const GoGdpImage& imageMsg = (const GoGdpImage&)receivedDataSet.GdpMsgAt(msgIndex);
                std::cout << "\nImage" << std::endl;
                std::cout << "Date Source: " << imageMsg.DataSourceId() << std::endl;
                std::cout << "Array count: " << imageMsg.ArrayedCount() << std::endl;
                std::cout << "Array index: " << imageMsg.ArrayedIndex() << std::endl;

                WriteOutputToBmpFile(imageMsg);
                break;
            }
            default:
                std::cout << "\nUnsupported message received." << std::endl;
                break;
            }
        }
    }

    void Disconnect()
    {
        gdpClient->Close();
        goSystem.Stop();
        goSystem.Disconnect();
    }

private:
    GoSystem goSystem;
    std::unique_ptr<GoGdpClient> gdpClient;

};

int main(int argc, char** argv)
{
    int status = OK_STATUS;
    kStatus gpApiLibConstructionStatus;
    kAssembly goApiLib = kNULL;

    try
    {
        // Constructs GoPxL API core framework.
        // It is important to construct goApiLib before declaration of GoSystem, 
        // this is because GoSystem implicitly calls constructor for GoRestClient. 
        if ((gpApiLibConstructionStatus = GoApiLib_Construct(&goApiLib)) != kOK)
        {
            std::cout << "Error: " << gpApiLibConstructionStatus << " - Failed to construct GoApiLib." << std::endl;
            return ERROR_STATUS;
        }
        auto receiver = Receive2dImage();

        receiver.Connect(SENSOR_IP, CONTROL_PORT);
        receiver.ChangeJob(JOB_NAME);
        receiver.SetContinuousAcquisitionMode();
        receiver.SetPixelFormat();
        receiver.SetExposure(60.0);
        receiver.SetupScriptTool();
        receiver.Receive();
        receiver.Disconnect();
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
