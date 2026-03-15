/*
 * @file            SampleUtils.cs
 * Objective        Utilities used across GoPxL Sdk .NET samples.
 * 
 * GoPxLSdk Sample
 * Copyright (C) 2023-2025 by LMI Technologies Inc.
 *
 * Licensed under the MIT License.
 * Redistributions of files must retain the above copyright notice.
 */

using App4.Utilities;
using GoPxLSdk;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace GoPxLSdkSamplesCommon {
    internal class Utilities 
    {
        // Component Identification
        /*
         * Scan Engine IDs
         * +--------------------------------+-----------+---------------------------+
         * | Display Name                   | Family    | Engine ID                 |
         * +--------------------------------+-----------+---------------------------+
         * | "Gocator Laser Profiler"       | G2, G6    | LMILaserLineProfiler      |
         * | "Gocator Confocal Profiler"    | G4, G5    | LMIConfocalLineProfiler   |
         * | "Gocator Snapshot"             | G3        | LMIFringeSnapshot         |
         * +--------------------------------+-----------+---------------------------+
         */
        public const string ENGINE_ID               = "LMIFringeSnapshot";
        public const string SCANNER_ID              = "scanner-0";
        public const string SENSOR_ID               = "sensor-0";

        // Scanner and Sensor API Paths
        public const string ENGINE_PATH             = "/scan/engines/" + ENGINE_ID;
        public const string SCANNER_PATH            = ENGINE_PATH + "/scanners/" + SCANNER_ID;
        public const string SCANNERS_PATH           = ENGINE_PATH + "/scanners";
        public const string SENSOR_PATH             = SCANNER_PATH + "/sensors/" + SENSOR_ID;
        public const string SENSORS_PATH            = SCANNER_PATH + "/sensors";
        public const string SCAN_MODE_PATH          = "/parameters/scanModeSettings/scanMode";
        public const string OUTPUTS_PATH            = SCANNER_PATH + "/outputs";
        public const string SCAN_ENGINE_COMPONENT   = SENSOR_ID; // May be "top" depending on the workspace configuration.

        // Version and Environment Paths
        public const string API_VERSION_PATH        = "/version";
        public const string ENVIRON_INFO_PATH       = "/environ/info";
        public const string REMOTE_CONTROLLER_PATH  = "/environ/remoteController";

        // Application Type Codes
        public const int SENSOR_APPLICATION_TYPE    = 0;
        public const int PC_APPLICATION_TYPE        = 1;
        public const int GOMAX_APPLICATION_TYPE     = 2;
        public const int DAEMON_APPLICATION_TYPE    = 3;

        // Return Status Codes
        public const int ERROR_STATUS               = -1;
        public const int OK_STATUS                  = 0;

        // Command Timeouts
        // Alignment delays REST responses, and G3 alignment takes a few seconds, so an increased timeout is needed.
        public static int REST_COMMAND_TIMEOUT_MSEC => GlobalData.Gocator_RestTimeout;

        // Verifies that the device is not remote controlled.
        public static int VerifyConnection(GoSystem system)
        {
            // Different application types
            Dictionary<int, string> applicationTypes = new Dictionary<int, string>();
            applicationTypes.Add(SENSOR_APPLICATION_TYPE, "Gocator Sensor");
            applicationTypes.Add(PC_APPLICATION_TYPE, "GoPxL on PC");
            applicationTypes.Add(GOMAX_APPLICATION_TYPE, "GoMax");
            applicationTypes.Add(DAEMON_APPLICATION_TYPE, "GoPxL Daemon");

            JObject response;

            // Print out API version.
            try
            {
                response = system.Client().Read(API_VERSION_PATH).GetResponse(REST_COMMAND_TIMEOUT_MSEC).Payload;
                string apiVersion = (string)response["apiVersion"];
                Console.WriteLine($"\nAPI version is {apiVersion}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message} - Failed to read API version. Check API path.");
                return ERROR_STATUS;
            }

            // Check for a sensor, PC instance of GoPxL, or GoMax.
            try
            {
                response = system.Client().Read(ENVIRON_INFO_PATH).GetResponse(REST_COMMAND_TIMEOUT_MSEC).Payload;
                int applicationType = (int)response["applicationType"];
                string serialNumber = (string)response["serialNumber"];
                string model = (string)response["model"];

                // Print information of the sensor.
                if (applicationType == SENSOR_APPLICATION_TYPE)
                {
                    Console.WriteLine($"\nThis device is a {applicationTypes[applicationType]} model {model} with serial number {serialNumber}.");
                }
                else
                {
                    // Print sensor information associated with the PC instance of GoPxL.
                    if (applicationType == PC_APPLICATION_TYPE)
                    {
                        Console.WriteLine($"\nThis device is a {applicationTypes[applicationType]}.");
                    }

                    // Print sensor information associated with the GoMax.
                    else if (applicationType == GOMAX_APPLICATION_TYPE)
                    {
                        Console.WriteLine($"\nThis device is a {applicationTypes[applicationType]} model {model} with serial number {serialNumber}.");
                    }

                    // Print sensor information associated with the GoPxL Daemon.
                    else if (applicationType == DAEMON_APPLICATION_TYPE)
                    {
                        Console.WriteLine($"\nThis device is a {applicationTypes[applicationType]}.");
                    }

                    // Read serial number from sensor.
                    try
                    {
                        response = system.Client().Read(SENSOR_PATH).GetResponse(REST_COMMAND_TIMEOUT_MSEC).Payload;
                        serialNumber = (string)response["serialNumber"];
                        Console.WriteLine($"The serial number of {SCANNER_ID} {SENSOR_ID} is {serialNumber}.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message} - Failed to read serial number of {SENSOR_ID} of {SCANNER_ID}. "
                            + "\nCheck if scanner (sensor group) is connected and verify scanner ID and engine ID. "
                            + "Also check if sensor is connected and verify sensor ID.");
                        return ERROR_STATUS;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message} - Failed to read environment information. Check API path.");
                return ERROR_STATUS;
            }

            // Check for a remote controller.
            try
            {
                response = system.Client().Read(REMOTE_CONTROLLER_PATH).GetResponse(REST_COMMAND_TIMEOUT_MSEC).Payload;
                bool remoteConnected = (bool)response["remoteConnected"];

                if (remoteConnected)
                {
                    string ipAddress = (string)response["ipAddress"];
                    int controlPort = (int)response["controlPort"];
                    Console.WriteLine($"\nThis device is controlled by a remote controller at IP {ipAddress} with control port {controlPort}.");
                    Console.WriteLine("Please use the IP address of the remote controller (previously called accelerator).");
                    return ERROR_STATUS;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message} - Failed to read remote controller information. Check API path.");
                return ERROR_STATUS;
            }

            return OK_STATUS;
        }
    }
} // end GoPxLSdkSamplesCommon
