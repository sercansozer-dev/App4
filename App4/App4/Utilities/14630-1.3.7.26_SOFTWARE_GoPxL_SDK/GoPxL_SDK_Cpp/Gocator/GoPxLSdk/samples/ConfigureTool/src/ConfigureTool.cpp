/*
 * @file            ConfigureTool.cpp
 * @brief           Demonstrates tool creation, configuration, and data output setup
 *
 * @details         This sample shows how to work with measurement tools in the GoPxL SDK.
 *                  It demonstrates how to:
 *                  - Create a new tool (Profile Bounding Box)
 *                  - Configure tool properties (region, external ID)
 *                  - Connect tool inputs to data sources
 *                  - Enable tool regions and measurement outputs
 *                  - Read measurement values from tool outputs
 *                  - Send tool measurement data over the GoPxL Data Protocol (GDP)
 *
 *                  This sample provides a complete workflow for setting up measurement
 *                  tools, from creation through configuration to data output. The tool
 *                  is connected to a profile data source and configured to output
 *                  measurements.
 *
 * GoPxLSdk Sample
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 *
 * Licensed under the MIT License.
 * Redistributions of files must retain the above copyright notice.
 */

#include <kApi/kApiDef.h>
#include <GoApi/GoApiLib.h>
#include <GoPxLSdk/GoSystem.h>
#include <GoPxLSdk/GoChannelError.h>
#include <GoPxLSdk/GoRequestError.h>
#include "../Common/src/SampleUtils.h"

using std::string;
using namespace GoPxLSdk;
using namespace GoPxLSdkSamplesCommon;

// Tool Paths
const string TOOLS_PATH                 = "/tools/";
const string TOOL_TYPE                  = "ProfileBoundingBox";
const string TOOL_ID                    = "ProfileBoundingBox-demo";
const string TOOL_PATH                  = TOOLS_PATH + "extId=" + TOOL_ID;
const string METRICS_PATH               = TOOL_PATH + "/metrics";
const string TOOL_INPUTS_PATH           = TOOL_PATH + "/inputs";
const string PROFILE_INPUT_PATH         = TOOL_INPUTS_PATH + "/ProfileInput";

// Output Data Paths
const string OUTPUT                     = "X";
const string TOOL_OUTPUT_PATH           = TOOL_PATH + "/outputs/" + OUTPUT;
const string TOOL_OUTPUT_DATA_PATH      = "tools:" + TOOL_TYPE + "-0:outputs:" + OUTPUT;
const string TOP_UNIFORM_PROFILE_G2     = "scan:LMILaserLineProfiler:scanner-0:topUniformProfile";
const string TOP_UNIFORM_PROFILE_G5     = "scan:LMIConfocalLineProfiler:scanner-0:topUniformProfileLayer0";

// GoPxL Data Protocol Paths
const string DATA_SOURCE_PATH           = "/dataSources/";
const string GOCATOR_CONTROL_PATH       = "/controls/gocator";
const string GOCATOR_OUTPUT_PATH        = GOCATOR_CONTROL_PATH + "/outputs/commands/add";

// Network Configuration Addresses
// If sensor is remote-controlled (previously referred to as ""accelerated""), 
// use the IP address of the remote controller, typically 127.0.0.1.
constexpr const kChar* SENSOR_IP        = "192.168.1.10";
// The control port is 3600 by default, but could vary for each GoPxL instance.
// To check ports in GoPxL Manager, select an instance, click Modify, and hover over "Base port";
// or check in GoPxL Discovery, where all ports are listed in columns.
constexpr k16u CONTROL_PORT             = GO_PXL_SDK_DEFAULT_CONTROL_PORT;

int ConfigureTool()
{
    GoSystem system;
    GoJson response;

    // Connect to a sensor system.
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

    // Configuration 1: Add a "Profile Bounding Box" tool.
    auto payload = GoJson(R"({
        "type" : ")" + TOOL_TYPE + R"(",
        "autoConnect" : false,
        "position" : 0
    })");
    
    // Add tool.
    std::cout << "\nAdding new tool: " << payload.At("type") << "..." << std::endl;
    try
    {
        system.Client().Create(TOOLS_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what() << " - Failed to add tool. Check API path and tool type." << std::endl;
        return ERROR_STATUS;
    }

    // Determine path of added tool.
    response = system.Client().Read(TOOLS_PATH).GetResponse().Payload();
    std::string original_tool_path = response.At("_embedded/item").Begin().Value().At("_links/self/href").ToString();
    original_tool_path = original_tool_path.erase(0, 2);
    original_tool_path = original_tool_path.erase(original_tool_path.size() - 1);

    // Checks if region is already enabled.
    response = system.Client().Read(original_tool_path).GetResponse().Payload();
    bool useRegion = response.At("parameters/UseRegion").Get<bool>();

    // Enables region if region is not already enabled.
    if (!useRegion)
    {
        payload = GoJson(R"({
            "parameters" : {
                "UseRegion" : true
            }
        })");

        std::cout << "\nEnabling tool region..." << std::endl;
        try
        {
            system.Client().Update(original_tool_path, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
            std::string toolID = system.Client().Read(original_tool_path).GetResponse().Payload().Get<std::string>("id");
            std::cout << "Tool ID: " << toolID << std::endl;
        }
        catch (const std::exception& e)
        {
            std::cerr << "Error: " << e.what()
                << " - Failed to enable tool region. Check the existing tool ID and try again, "
                << "or try increasing timeout value."
                << std::endl;
            return ERROR_STATUS;
        } 
    }
    else
    {
        std::cout << "\nTool region already enabled." << std::endl;
    }

    // Configuration 2: Update the tool id.
    payload = GoJson(R"({
        "extId" : ")" + TOOL_ID + R"("
    })");

    std::cout << "\nUpdating tool ID..." << std::endl;
    try
    {
        system.Client().Update(original_tool_path, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
        std::cout << "Tool ID: " << payload.At("extId") << std::endl;
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what()
                  << " - Failed to update tool ID. Check that there isn't an existing tool with the extId and try again, "
                  << "or try increasing timeout value."
                  << std::endl;
        return ERROR_STATUS;
    }

    // Configuration 3: Modify tool region.
    payload = GoJson(R"({
        "parameters" : {
            "Region" : {
                "height" : 10.0,
                "width" : 10.0,
                "x" : 5.0,
                "z" : 5.0
            }
        }
    })");

    std::cout << "\nModifying tool region..." << std::endl;
    try
    {
        system.Client().Update(TOOL_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what()
            << " - Failed to modify tool region. Check the existing tool ID and try again, "
            << "or try increasing timeout value."
            << std::endl;
        return ERROR_STATUS;
    }

    // Configuration 4: Connect tool to a data source.
    // Read all available outputs (data sources).
    response = system.Client().Read(DATA_SOURCE_PATH).GetResponse(REST_COMMAND_TIMEOUT_MSEC).Payload();
    auto dataSources = response.At("_embedded/item");

    string dataSourceId;
    std::vector<string> dataSourceIds;
    std::cout << "\nData sources:" << std::endl;
    for (auto it = dataSources.Begin(); it != dataSources.End(); it.operator++(0))
    {
        dataSourceId = it.Value().At("/_links/self/href").Get<string>();
        std::cout << dataSourceId << std::endl;
        dataSourceIds.push_back(dataSourceId);
    }

    // Read all available tool inputs.
    response = system.Client().Read(TOOL_INPUTS_PATH).GetResponse().Payload();
    auto inputs = response.At("_embedded/item");

    string inputId;
    std::cout << "\nTool inputs:" << std::endl;
    for (auto it = inputs.Begin(); it != inputs.End(); it.operator++(0))
    {
        inputId = it.Value().At("/_links/self/href").Get<string>();
        std::cout << inputId << std::endl;
    }

    // Connect tool input to a data source (the uniform profile).
    payload = GoJson(R"({
        "dataSource" : ")" + TOP_UNIFORM_PROFILE_G2 + R"("
    })");

    std::cout << "\nConnecting tool input..." << std::endl;
    try
    {
        system.Client().Update(PROFILE_INPUT_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what()
                  << " - Failed to update tool input. Check the tool ID and API path, "
                  << "or try increasing timeout value."
                  << std::endl;
        return ERROR_STATUS;
    }

    // Configuration 5: Enable a measurement output.
    payload = GoJson(R"({
        "enabled" : true
    })");

    std::cout << "\nEnabling measurement output..." << std::endl;
    try
    {
        system.Client().Update(TOOL_OUTPUT_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what()
                  << " - Failed to enable the output. Check the tool ID and API path, "
                  << "or try increasing timeout value."
                  << std::endl;
        return ERROR_STATUS;
    }
    // Configuration 6: Send measurement value over GDP.
    // Read measurement output.
    try
    {
        response = system.Client().Read(METRICS_PATH).GetResponse().Payload();
        std::cout << "\nMeasurement value: " 
                  << response.At("/outputsByExtId/" + OUTPUT + "/value") << std::endl;
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what()
                  << " - Failed to read value. Check API path and ensure scan data is available."
                  << std::endl;
        return ERROR_STATUS;
    }
    // Enable Gocator Protocol.
    std::cout << "\nEnabling Gocator Protocol..." << std::endl; 
    system.Client().Update(GOCATOR_CONTROL_PATH, GoJson("{\"enabled\":true}")).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
    
    // Add measurement value to output.
    // source: data source to add
    // outputId: output identifier (auto-assigned if not provided)
    // autoshift: true = shift overlapping entries, false = reject on overlap
    payload = GoJson(R"({
        "source" : ")" + TOOL_OUTPUT_DATA_PATH + R"(",
        "outputId" : 1,
        "autoshift" : true
    })");

    std::cout << "\nAdding measurement value to output..." << std::endl;
    try
    {
        system.Client().Call(GOCATOR_OUTPUT_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what()
                  << " - Failed to send the data. Check the tool name and API path, "
                  << "or try increasing timeout value."
                  << std::endl;
        return ERROR_STATUS;
    }

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
            std::cout << "Error: " << gpApiLibConstructionStatus 
                      << " - Failed to construct GoApiLib." << std::endl;
            return ERROR_STATUS;
        }
        status = ConfigureTool();
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
    std::cout << "\nPress Enter to exit the program..." << std::endl;
    std::ignore = getchar(); 

    return status;
}
