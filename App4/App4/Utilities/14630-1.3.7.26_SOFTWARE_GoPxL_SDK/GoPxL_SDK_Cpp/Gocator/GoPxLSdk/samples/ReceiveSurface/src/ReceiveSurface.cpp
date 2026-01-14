/*
 * @file            ReceiveSurface.cpp
 * @brief           Demonstrates receiving surface data via GoPxL Data Protocol (GDP)
 *
 * @details         This sample shows how to receive surface data from a Gocator sensor.
 *                  It demonstrates how to:
 *                  - Configure sensor for Surface scan mode
 *                  - Enable intensity data acquisition
 *                  - Enable GoPxL Data Protocol (GDP)
 *                  - Add surface output to GDP output map
 *                  - Connect to GDP server and receive surface data synchronously
 *                  - Process both uniform and point cloud surface formats
 *                  - Transform surface data to real-world coordinates (X, Y, Z)
 *                  - Extract intensity values from surface data
 *
 *                  Surface data is received via GDP as 2D arrays of height values, which
 *                  are transformed using offset and resolution values to obtain real-world
 *                  coordinates in millimeters. The sample handles both uniform surfaces
 *                  and point cloud surfaces.
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
#include <GoPxLSdk/GoGdpMsg/GoGdpSurfaceUniform.h>
#include <GoPxLSdk/GoGdpMsg/GoGdpSurfacePointCloud.h>

using std::string;
using namespace GoPxLSdk;
using namespace GoPxLSdkSamplesCommon;

// Surface Mode Parameters and Paths
constexpr int SURFACE_MODE                      = 3;
const string SURFACE_SOURCE_KEY                 = "UniformSurface";
const string SURFACE_SOURCE_COMPONENT           = (ENGINE_ID == "LMIFringeSnapshot") ? SENSOR_ID : "top";
const string SURFACE_SOURCE_ID                  = "\"scan:" + ENGINE_ID + ":" + SCANNER_ID + ":" + SURFACE_SOURCE_COMPONENT + SURFACE_SOURCE_KEY + (ENGINE_ID == "LMIConfocalLineProfiler" ? "Layer0\"" : "\"");

// Control Paths
const string GOCATOR_CONTROL_PATH               = "/controls/gocator";
const string GOCATOR_OUTPUT_PATH                = GOCATOR_CONTROL_PATH + "/outputs";
const string GOCATOR_ADD_OUTPUT_PATH            = GOCATOR_OUTPUT_PATH + "/commands/add";

// Replay Paths
const string REPLAY_PATH                        = "/replay/playback";

// Command Timeouts
constexpr int RECEIVE_DATA_TIMEOUT_MSEC         = 6'000;

// Network Configuration Addresses
// If sensor is remote-controlled (previously referred to as ""accelerated""), 
// use the IP address of the remote controller, typically 127.0.0.1.
constexpr const kChar* SENSOR_IP                = "192.168.1.10";
// The control port is 3600 by default, but could vary for each GoPxL instance.
// To check ports in GoPxL Manager, select an instance, click Modify, and hover over "Base port";
// or check in GoPxL Discovery, where all ports are listed in columns.
constexpr k16u CONTROL_PORT                     = GO_PXL_SDK_DEFAULT_CONTROL_PORT;

// Represents a point in a surface.
typedef struct SurfacePoint
{
    double x; // x coordinate - position along laser line (mm).
    double y; // y coordinate - position along the direction of travel (mm).
    double z; // z coordinate - height at a given x position(mm).
    unsigned char intensity;
} SurfacePoint;

typedef struct UniformSurface
{
    /* The x and y coordinates of a given point in the height map can be calculated as following:
     * for z value of HeightMap[i][j]
     * double x = xOffset + xResolution * i;
     * double y = yOffset + yResolution * j;
     */

    UniformSurface(
        unsigned int length, 
        unsigned int width, 
        unsigned int intensityLength, 
        unsigned int intensityWidth, 
        double XOffset, 
        double YOffset, 
        double XResolution, 
        double YResolution
    )
    {
        HeightMap.resize(length);
        for (unsigned int i = 0; i < length; i++)
        {
            HeightMap[i] = std::vector<double>(width);
        }
        IntensityMap.resize(intensityLength);
        for (unsigned int i = 0; i < intensityLength; i++)
        {
            IntensityMap[i] = std::vector<unsigned char>(intensityWidth);
        }
        xOffset = XOffset;
        yOffset = YOffset;
        xResolution = XResolution;
        yResolution = YResolution;
    }

    std::vector<std::vector<double>> HeightMap;                 // Z coordinates - height at a given x and y position (mm).
    std::vector<std::vector<unsigned char>> IntensityMap;       // Intensities - intensity at a given x and y position (mm).
    double xOffset;                                             // x Offset - offset position along laser line (mm).
    double yOffset;                                             // y Offset - offset position along the direction of travel (mm).
    double xResolution;                                         // x Resolution - resolution along laser line (mm).
    double yResolution;                                         // y resolution - resolution along the direction of travel(mm).

} UniformSurface;

int ReceiveSurface()
{
    GoSystem system;
    GoJson response;
    GoJson payload;

    std::vector<std::vector<SurfacePoint>> surfaceBuffer;

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
        std::cerr << "Error: " << e.what() << " - Check timeout value or API path." << std::endl;
        return ERROR_STATUS;
    }

    if (replayDataEnabled)
    {
        std::cout << "\nUsing replay data" << std::endl;
    }
    else
    {
        std::cout << "\nUsing live data" << std::endl;

        // Read current scan mode.
        system.Client().Update(SCANNER_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
        response = system.Client().Read(SCANNER_PATH).GetResponse().Payload();
        auto scanModeValue = response.At(SCAN_MODE_PATH).Get<int>();

        // Set scan mode to Surface (3) if not set already.
        if (scanModeValue != SURFACE_MODE)
        {
            payload = GoJson(R"({
                "parameters" : {
                    "scanModeSettings" : {
                        "scanMode" : )" + std::to_string(SURFACE_MODE) + R"(
                    }
                }             
            })");

            std::cout << "\nSetting scan mode to Surface..." << std::endl;
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

    // Enable intensity.
    payload = GoJson(R"({
        "parameters" : {
            "scanModeSettings" : {
                "intensityEnabled" : true
            }
        }
    })");

    std::cout << "\nEnabling intensity..." << std::endl;
    try
    {
        system.Client().Update(SCANNER_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what() << " - Check timeout value or API path." << std::endl;
        return ERROR_STATUS;
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

    // Check if surface already added to Gocator Protocol output
    bool surfaceOutputAdded = false;

    try
    {
        response = system.Client().Read(GOCATOR_OUTPUT_PATH).GetResponse().Payload();
        GoJson map = response.Get<GoJson>("map");

        for (GoJsonIterator i = map.Begin(); i != map.End(); i++)
        {
            GoJson value = i.Value();
            if (value.Get<std::string>("source").find(SURFACE_SOURCE_KEY))
            {
                surfaceOutputAdded = true;
                break;
            }
        }
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what() << " - Check API path." << std::endl;
        return ERROR_STATUS;
    }

    if (!surfaceOutputAdded)
    {
        // Add surface output to Gocator Protocol.
        try
        {
            payload = GoJson(R"({
                "source" : )" + SURFACE_SOURCE_ID + R"(,
                "outputId" : 0,
                "autoShift" : true
            })");

            system.Client().Call(GOCATOR_ADD_OUTPUT_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
        }
        catch (const std::exception& e)
        {
            std::cerr << "Error: " << e.what() << " - Failed to add uniform surface output to Gocator Protocol. "
                << "Check if uniform/point cloud toggle is correctly set, "
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

    // Receive data synchronously.
    try
    {
        gdpClient->ReceiveDataSync(RECEIVE_DATA_TIMEOUT_MSEC);
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what()
            << " - Failed to receive data via synchronous connection. "
            << "Ensure surface data is available, check data source ID, or try increasing timeout value."
            << std::endl;
        return ERROR_STATUS;
    }

    const GoDataSet& receivedDataSet = gdpClient->DataSet();

    std::cout << "\nTotal number of messages: " << receivedDataSet.Count() << std::endl;

    // Loop through messages in the dataSet.
    for (size_t msgIndex = 0; msgIndex < receivedDataSet.Count(); msgIndex++)
    {
        std::cout << std::string(64, '-') << " GDP Output Source " << msgIndex + 1 << std::endl;
        const auto& msg = receivedDataSet.GdpMsgAt(msgIndex);

        switch (msg.Type())
        {
            case GoPxLSdk::MessageType::UNIFORM_SURFACE:
            {
                const auto& uniformSurfaceMsg = static_cast<const GoGdpSurfaceUniform&>(msg);

                // Output message information.
                std::cout << "Message type: Uniform Surface" << std::endl;
                std::cout << "GDP ID: " << uniformSurfaceMsg.GdpId() << std::endl;
                std::cout << "Data source ID: " << uniformSurfaceMsg.DataSourceId() << std::endl;

                if (uniformSurfaceMsg.ArrayedCount() == 0) {
                    std::cout << "Not arrayed" << std::endl;
                }
                else {
                    std::cout << "Arrayed" << std::endl;
                    std::cout << "\tCount: " << uniformSurfaceMsg.ArrayedCount() << std::endl;
                    std::cout << "\tIndex: " << uniformSurfaceMsg.ArrayedIndex() << std::endl;
                }

                // Transform to real world coordinates.
                unsigned int validPointCount = 0;
                unsigned int length = uniformSurfaceMsg.Length();
                unsigned int width = uniformSurfaceMsg.Width();
                auto intensityArray = uniformSurfaceMsg.Intensities();
                unsigned int intensityLength = uniformSurfaceMsg.IntensityLength();
                unsigned int intensityWidth = uniformSurfaceMsg.IntensityWidth();

                UniformSurface uniformSurface = UniformSurface(
                    length, 
                    width,
                    intensityLength, 
                    intensityWidth,
                    uniformSurfaceMsg.Offset().x,
                    uniformSurfaceMsg.Offset().y,
                    uniformSurfaceMsg.Resolution().x,
                    uniformSurfaceMsg.Resolution().y
                );

                /*  The surface data is sent in a 2D array of z values.
                 *  The x and y real-world coordinates can be calculated from the indices of the 2D array,
                 *  using the x and y offsets and resolutions.
                 *  The z values must be transformed into real world coordinates before being used.
                 */
                for (k32u j = 0; j < length; j++)
                {
                    for (k32u k = 0; k < width; k++)
                    {
                        k16s data;
                        kArray2_Item(uniformSurfaceMsg.Ranges(), j, k, &data);

                        if (data != k16S_NULL)
                        {
                            // Calculates the Z position in mm (Z offset + Z resolution * z)
                            uniformSurface.HeightMap[j][k] = uniformSurfaceMsg.Offset().z + uniformSurfaceMsg.Resolution().z * data;
                            validPointCount++;
                        }
                        else
                        {
                            uniformSurface.HeightMap[j][k] = k16S_NULL;
                        }
                    }
                }

                // Read intensities.
                if (intensityArray)
                {
                    for (k32u j = 0; j < intensityLength; j++)
                    {
                        for (k32u k = 0; k < intensityWidth; k++)
                        {
                            kArray2_Item(uniformSurfaceMsg.Intensities(), j, k, &(uniformSurface.IntensityMap[j][k]));
                        }
                    }
                }

                std::cout << "Surface data length: " << length << std::endl;
                std::cout << "Surface data width: " << width << std::endl;
                std::cout << "Intensity length: " << intensityLength << std::endl;
                std::cout << "Intensity width: " << intensityWidth << std::endl;
                std::cout << "Valid points count: " << validPointCount << std::endl;
                break;
            }
            case GoPxLSdk::MessageType::SURFACE_POINT_CLOUD:
            {
                const auto& pointCloudSurfaceMsg = static_cast<const GoGdpSurfacePointCloud&>(msg);

                // Output message information.
                std::cout << "Message type: Point Cloud Surface" << std::endl;
                std::cout << "GDP ID: " << pointCloudSurfaceMsg.GdpId() << std::endl;
                std::cout << "Data source ID: " << pointCloudSurfaceMsg.DataSourceId() << std::endl;

                if (pointCloudSurfaceMsg.ArrayedCount() == 0) {
                    std::cout << "Not arrayed" << std::endl;
                }
                else {
                    std::cout << "Arrayed" << std::endl;
                    std::cout << "\tCount: " << pointCloudSurfaceMsg.ArrayedCount() << std::endl;
                    std::cout << "\tIndex: " << pointCloudSurfaceMsg.ArrayedIndex() << std::endl;
                }

                // Transform to real world coordinates.
                unsigned int validPointCount = 0;
                unsigned int length = pointCloudSurfaceMsg.Length();
                unsigned int width = pointCloudSurfaceMsg.Width();
                auto intensityArray = pointCloudSurfaceMsg.Intensities();
                unsigned int intensityLength = pointCloudSurfaceMsg.IntensityLength();
                unsigned int intensityWidth = pointCloudSurfaceMsg.IntensityWidth();

                surfaceBuffer.resize(length);
                for (k32u row = 0; row < length; row++)
                {
                    surfaceBuffer[row] = std::vector<SurfacePoint>(width);
                }

                for (k32u j = 0; j < length; j++)
                {
                    for (k32u k = 0; k < width; k++)
                    {
                        kPoint3d16s point;
                        kArray2_Item(pointCloudSurfaceMsg.Ranges(), j, k, &point);
                        surfaceBuffer[j][k].x = pointCloudSurfaceMsg.Offset().x + pointCloudSurfaceMsg.Resolution().x * point.x;
                        surfaceBuffer[j][k].y = pointCloudSurfaceMsg.Offset().y + pointCloudSurfaceMsg.Resolution().y * point.y;

                        if (point.z != k16S_NULL)
                        {
                            surfaceBuffer[j][k].z = pointCloudSurfaceMsg.Offset().z + pointCloudSurfaceMsg.Resolution().z * point.z;
                            validPointCount++;
                        }
                        else
                        {
                            surfaceBuffer[j][k].z = k16S_NULL;
                        }
                    }
                }

                // Read intensities.
                if (intensityArray)
                {
                    for (k32u j = 0; j < intensityLength; j++)
                    {
                        for (k32u k = 0; k < intensityWidth; k++)
                        {
                            kArray2_Item(pointCloudSurfaceMsg.Intensities(), j, k, &(surfaceBuffer[j][k].intensity));
                        }
                    }
                }

                std::cout << "Surface data length: " << length << std::endl;
                std::cout << "Surface data width: " << width << std::endl;
                std::cout << "Intensity length: " << intensityLength << std::endl;
                std::cout << "Intensity width: " << intensityWidth << std::endl;
                std::cout << "Valid points count: " << validPointCount << std::endl;
                break;
            }
            default:
                std::cout << "\nNo surface found in the message.\n" << std::endl;
                break;
        }
    }

    std::cout << "\nPress Enter to exit the program..." << std::endl;
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
        // It is important to construct goApiLib before declaration of GoSystem, 
        // this is because GoSystem implicitly calls constructor for GoRestClient. 
        if ((gpApiLibConstructionStatus = GoApiLib_Construct(&goApiLib)) != kOK)
        {
            std::cout << "Error: " << gpApiLibConstructionStatus << " - Failed to construct GoApiLib." << std::endl;
            return ERROR_STATUS;
        }
        status = ReceiveSurface();
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
