#ifndef SAMPLE_UTILS_H
#define SAMPLE_UTILS_H

// This file is used to keep common function classes in GoPxLFirmware, GoMaxFirmware and GoPxLService projects.
#include <string>
#include <kApi/kApiDef.h>
#include <GoPxLSdk/GoSystem.h>

using std::string;
using namespace GoPxLSdk;

namespace GoPxLSdkSamplesCommon {

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
    const string ENGINE_ID                  = "LMILaserLineProfiler";
    const string SCANNER_ID                 = "scanner-0";
    const string SENSOR_ID                  = "sensor-0";

    // Version and Environment Paths
    const string API_VERSION_PATH           = "/version";
    const string ENVIRON_INFO_PATH          = "/environ/info";
    const string REMOTE_CONTROLLER_PATH     = "/environ/remoteController";
    const string VISIBLE_SENSORS_PATH       = "/scan/visibleSensors/";

    // Application Type Codes
    constexpr int SENSOR_APPLICATION_TYPE   = 0;
    constexpr int PC_APPLICATION_TYPE       = 1;
    constexpr int GOMAX_APPLICATION_TYPE    = 2;
    constexpr int DAEMON_APPLICATION_TYPE   = 3;

    // Scanner and Sensor API Paths
    const string ENGINE_PATH                = "/scan/engines/" + ENGINE_ID;
    const string SCANNER_PATH               = ENGINE_PATH  + "/scanners/" + SCANNER_ID;
    const string SCANNERS_PATH              = ENGINE_PATH + "/scanners";
    const string SENSOR_PATH                = SCANNER_PATH + "/sensors/" + SENSOR_ID;
    const string SENSORS_PATH               = SCANNER_PATH + "/sensors";
    const string SCAN_MODE_PATH             = "/parameters/scanModeSettings/scanMode";
    const string OUTPUTS_PATH               = SCANNER_PATH + "/outputs";
    const string SCAN_ENGINE_COMPONENT      = SENSOR_ID; // May be "top" depending on the workspace configuration.

    // Return Status Codes
    constexpr int ERROR_STATUS              = -1;
    constexpr int OK_STATUS                 = 0;

    // Command Timeouts
    // Note: Most operations complete within 3 seconds. For longer operations like alignment,
    // increase this timeout value. C# samples use 30000ms to accommodate G3 alignment delays.
    constexpr int REST_COMMAND_TIMEOUT_MSEC = 3000;

    // Verifies that the device is not remote controlled.
    int VerifyConnection(GoSystem& system);
}; // Namespace

#endif // GOSTUDIO_STUDIO_UTILS_H