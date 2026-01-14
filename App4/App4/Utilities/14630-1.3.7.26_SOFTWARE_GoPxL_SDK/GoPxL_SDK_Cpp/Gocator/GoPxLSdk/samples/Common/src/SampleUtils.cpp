#include "SampleUtils.h"

namespace GoPxLSdkSamplesCommon {

    // Verifies that the device is not remote controlled.
    int VerifyConnection(GoSystem& system)
    {
        // Different application types
        const std::map<int, string> applicationTypes{
            {SENSOR_APPLICATION_TYPE, "Gocator Sensor"},
            {PC_APPLICATION_TYPE, "GoPxL on PC"},
            {GOMAX_APPLICATION_TYPE, "GoMax"},
            {DAEMON_APPLICATION_TYPE, "GoPxL Daemon"}
        };

        GoJson response;

        // Print out API version.
        try
        {
            response = system.Client().Read(API_VERSION_PATH).GetResponse(REST_COMMAND_TIMEOUT_MSEC).Payload();
            auto apiVersion = response.At("/apiVersion").Get<string>();
            std::cout << "\nAPI version is " << apiVersion << "." << std::endl;
        }
        catch (const std::exception& e)
        {
            std::cerr << "Error: " << e.what() << " - Failed to read API version. Check API path." << std::endl;
            return ERROR_STATUS;
        }

        // Check for a sensor, PC instance of GoPxL, or GoMax.
        try
        {
            response = system.Client().Read(ENVIRON_INFO_PATH).GetResponse(REST_COMMAND_TIMEOUT_MSEC).Payload();
            auto applicationType = response.At("/applicationType").Get<int>();
            auto serialNumber = response.At("/serialNumber").Get<string>();
            auto model = response.At("/model").Get<string>();

            // Print sensor information associated with the PC instance of GoPxL.
            if (applicationType == SENSOR_APPLICATION_TYPE)
            {
                std::cout << "\nThis device is a " << applicationTypes.at(applicationType)
                    << " model " << model << " with serial number " << serialNumber << "." << std::endl;
            }
            else
            {
                if (applicationType == PC_APPLICATION_TYPE)
                {
                    std::cout << "\nThis device is a " << applicationTypes.at(applicationType) << "." << std::endl;
                }
                else if (applicationType == GOMAX_APPLICATION_TYPE)
                {
                    std::cout << "\nThis device is a " << applicationTypes.at(applicationType)
                        << " model " << model << " with serial number " << serialNumber << "." << std::endl;
                }
                else if (applicationType == DAEMON_APPLICATION_TYPE)
                {
                    std::cout << "\nThis device is a " << applicationTypes.at(applicationType) << "." << std::endl;
                }

                // Read serial number from sensor.
                try
                {
                    response = system.Client().Read(SENSOR_PATH).GetResponse(REST_COMMAND_TIMEOUT_MSEC).Payload();
                    auto serialNumber = response.At("/serialNumber").Get<string>();
                    std::cout << "The serial number of " << SCANNER_ID << " " << SENSOR_ID
                        << " is " << serialNumber << "." << std::endl;
                }
                catch (const std::exception& e)
                {
                    std::cerr << "Error: " << e.what() << " - Failed to read serial number of " << SENSOR_ID << " of " << SCANNER_ID << ". "
                        << "\nCheck if scanner (sensor group) is connected and verify scanner ID and engine ID. "
                        << "Also check if sensor is connected and verify sensor ID."
                        << std::endl;
                    return ERROR_STATUS;
                }
            }
        }
        catch (const std::exception& e)
        {
            std::cerr << "Error: " << e.what() << " - Failed to read environment information. Check API path." << std::endl;
            return ERROR_STATUS;
        }

        // Check for a remote controller.
        try
        {
            response = system.Client().Read(REMOTE_CONTROLLER_PATH).GetResponse(REST_COMMAND_TIMEOUT_MSEC).Payload();
            auto remoteConnected = response.At("/remoteConnected").Get<bool>();

            if (remoteConnected)
            {
                auto ipAddress = response.At("/ipAddress").Get<string>();
                auto controlPort = response.At("/controlPort").Get<int>();
                std::cout << "\nThis device is controlled by a remote controller at IP " << ipAddress
                    << " with control port " << controlPort << "." << std::endl;
                std::cout << "Please use the IP address of the remote controller (previously called accelerator)." << std::endl;
                return ERROR_STATUS;
            }
        }
        catch (const std::exception& e)
        {
            std::cerr << "Error: " << e.what() << " - Failed to read remote controller information. Check API path." << std::endl;
            return ERROR_STATUS;
        }

        return OK_STATUS;
    }

} // end GoPxLSdkSamplesCommon
