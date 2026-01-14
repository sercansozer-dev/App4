/*
 * @file            SaveJob.cpp
 * @brief           Demonstrates job file management operations
 *
 * @details         This sample shows how to manage job files on a Gocator sensor.
 *                  It demonstrates how to:
 *                  - List all saved jobs with expand level parameter
 *                  - Save current configuration as a job file
 *                  - Rename existing job files
 *                  - Download job file (.gpjob) from sensor
 *                  - Upload job file back to sensor
 *                  - Load a job to make it active
 *                  - Use API's 'expand levels' to retrieve embedded resources
 *
 *                  Job files (.gpjob) contain sensor configuration including tools,
 *                  measurements, and scan settings. This sample demonstrates the
 *                  workflow for job file management including save, rename, download, upload,
 *                  and load operations.
 *
 * GoPxLSdk Sample
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 *
 * Licensed under the MIT License.
 * Redistributions of files must retain the above copyright notice.
 */

#include <fstream>
#include <filesystem>

#include <kApi/kApiDef.h>
#include <GoApi/GoApiLib.h>
#include <GoPxLSdk/GoSystem.h>
#include <GoPxLSdk/GoChannelError.h>
#include <GoPxLSdk/GoRequestError.h>
#include "../Common/src/SampleUtils.h"

using std::string;
using namespace GoPxLSdk;
using namespace GoPxLSdkSamplesCommon;

// Job Paths
const string JOBS_PATH                  = "/jobs";
const string JOB_FILES_PATH             = JOBS_PATH + "/files";
const string JOB_COMMAND_PATH           = JOBS_PATH + "/commands";
const string SAVE_JOB_PATH              = JOB_COMMAND_PATH + "/save";
const string LOAD_JOB_PATH              = JOB_COMMAND_PATH + "/load";
const string RENAME_JOB_PATH            = JOB_COMMAND_PATH + "/rename";
const string LOCAL_JOB_FILE_PATH        = "./sample_job.gpjob";
const string LOADED_JOB_PATH            = "/loadedJob";

// Job Names
const string JOB_NAME_0                 = "SDK-demo";
const string JOB_NAME_1                 = "SDK-demo-job";
const string JOB_NAME_2                 = "SDK-local-job-demo";
const string READ_DATA_PATH             = JOB_FILES_PATH + "/" + JOB_NAME_1 + "/data";

// Command Timeouts
constexpr int RECEIVE_DATA_TIMEOUT_MSEC = 20000;     // Receiving data might require a higher timeout value.c

// Network Configuration Addresses
// If sensor is remote-controlled (previously referred to as ""accelerated""), 
// use the IP address of the remote controller, typically 127.0.0.1.
constexpr const kChar* SENSOR_IP        = "192.168.1.10";
// The control port is 3600 by default, but could vary for each GoPxL instance.
// To check ports in GoPxL Manager, select an instance, click Modify, and hover over "Base port";
// or check in GoPxL Discovery, where all ports are listed in columns.
constexpr k16u CONTROL_PORT             = GO_PXL_SDK_DEFAULT_CONTROL_PORT;

int SaveJob()
{
    GoSystem system;
    GoJson response;
    GoJson payload;

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

    // Get the list of jobs.
    // Jobs contain embedded resources that are partially represented.
    // These embedded resources can be expanded via a parameter, "expandLevel", which determines 
    // how many levels of embedded resources to expand. Note that this parameter is optional; 
    // omitting it is equivalent to setting it to 0.
    auto expandLevel = GoJson(R"({
        "expandLevel" : 1
    })");

    try
    {
        response = system.Client().Read(JOB_FILES_PATH, {}, expandLevel).GetResponse().Payload();
        if (response.HasKey("_embedded") && response.At("_embedded").HasKey("item")) {
            GoJson jobs = response.At("_embedded/item");
            std::cout << "\nList of saved jobs: " << std::endl;
            for (auto it = jobs.Begin(); it != jobs.End(); it.operator++(0))
            {
                std::cout << it.Value().At("jobName") << std::endl;
            }
        }
        else {
            std::cout << "\nCurrently GoPxL does not contain any saved job." << std::endl;
        }
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what() << " - Failed to get jobs list."
            << std::endl;
    }

    // Save job as specific name.
    payload = GoJson(R"({
        "name" : ")" + JOB_NAME_0 + R"("
    })");

    std::cout << "\nSaving current job as " << payload.At("name") << "..." << std::endl;
    system.Client().Call(SAVE_JOB_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);

    // Rename job.
    auto renameJobContent = GoJson(R"({
        "sourceName" : ")" + JOB_NAME_0 + R"(",
        "destName" : ")" + JOB_NAME_1 + R"("
    })");
    
    std::cout << "\nRenaming job to " << renameJobContent.At("destName") << "..." << std::endl;
    try
    {
        system.Client().Call(RENAME_JOB_PATH, renameJobContent).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what() << " - Failed to rename the job. "
                  << "Ensure job names are correct and destination name does not already exist, "
                  << "or try increasing timeout value." << std::endl;
    }

    // Download job.
    response = system.Client().Read(READ_DATA_PATH, payload).GetResponse(RECEIVE_DATA_TIMEOUT_MSEC).Payload();
    ByteArray jobData = response.At("content").GetBinary();
    try
    {
        char* buffer = (char*)jobData.data();
        std::ofstream recFile(LOCAL_JOB_FILE_PATH, std::ios::out | std::ios::binary);
        std::cout << "\nJob downloaded to " << std::filesystem::absolute(LOCAL_JOB_FILE_PATH) << std::endl;
        if (recFile.is_open()) {
            recFile.write(buffer, jobData.size());
            recFile.close();
        }
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what() << " - Failed to download the job to "
                  << std::filesystem::absolute(LOCAL_JOB_FILE_PATH) << std::endl;
        return ERROR_STATUS;
    }

    // Upload job.
    std::ifstream jobFile(LOCAL_JOB_FILE_PATH, std::ios::binary);
    try
    {
        if (jobFile.is_open()) {
            std::vector<k8u> jobData(std::istreambuf_iterator<char>(jobFile), {});
            GoJson localJobContent;
            localJobContent.Set("fromLive", false);
            localJobContent.Set("name", JOB_NAME_2);
            localJobContent.Set("content", jobData);

            std::cout << "\nUploading job as " << localJobContent.At("name") << " from "
                      << std::filesystem::absolute(LOCAL_JOB_FILE_PATH) << "..." << std::endl;
            system.Client().Create(JOB_FILES_PATH, localJobContent).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
        }
        else {
            std::cerr << "\nFailed to upload file from " << std::filesystem::absolute(LOCAL_JOB_FILE_PATH) << std::endl;
        }
    }
    catch (const std::exception& e)
    {
        std::cerr << "Error: " << e.what() << " - Failed to upload the job. Ensure job name does not already exist, "
                  << "or try increasing timeout value." << std::endl;
        return ERROR_STATUS;
    }

    // Load the uploaded job.
    payload = GoJson(R"({
        "name" : ")" + JOB_NAME_2 + R"("
    })");

    system.Client().Call(LOAD_JOB_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
    response = system.Client().Read(JOBS_PATH, {}, expandLevel).GetResponse().Payload().At(LOADED_JOB_PATH);
    std::cout << "\nLoaded job: " << response.Get<string>() << std::endl;

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
        status = SaveJob();
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