/**
* KukaSnapshot_EyeOnHand.cpp
* An example to doing a eye-on-hand calibration on a Kuka Robot with a Gocator Snapshot Sensor mounted on the robot arm.
*
* @public
* Copyright (C) 2017-2020 by LMI Technologies Inc.
* Licensed under the MIT License.
* Redistributed files must retain the above copyright notice.
*/

#include <iostream>
#include <vector>
#include <GoSdk/GoSdk.h>
#include <GoRobot/GoRobot.h>
#include <RobotDrivers/KukaRobotDriver.h>
#include <string>
#include <windows.h>

#define SENSOR_IP_ADDRESS   "192.168.1.10"      // Change this to the Sensor's IP address
#define ROBOT_IP_ADDRESS    "192.168.1.20"      // Change this to the Robot's IP address
#define ROBOT_PORT          54600               // Change this to the Robot's Connection Port (specified in the XmlServerFollow.xml)
        
#define NUMBER_OF_POSES     5

using namespace std;

void printMatrix(const GoRobot::Matrix &m, bool multiline = false)
{
    if (multiline)
    {
        printf("%12.3e, %12.3e, %12.3e, %12.3e, \n", m.Ix, m.Jx, m.Kx, m.Tx);
        printf("%12.3e, %12.3e, %12.3e, %12.3e, \n", m.Iy, m.Jy, m.Ky, m.Ty);
        printf("%12.3e, %12.3e, %12.3e, %12.3e, \n", m.Iz, m.Jz, m.Kz, m.Tz);
        printf("%12.3e, %12.3e, %12.3e, %12.3e, \n",  0.0,  0.0,  0.0,  1.0);
    }
    else
    {
        printf("%12.3e, %12.3e, %12.3e, "  , m.Ix, m.Iy, m.Iz);
        printf("%12.3e, %12.3e, %12.3e, "  , m.Jx, m.Jy, m.Jz);
        printf("%12.3e, %12.3e, %12.3e, "  , m.Kx, m.Ky, m.Kz);
        printf("%12.3e, %12.3e, %12.3e \n", m.Tx, m.Ty, m.Tz);
    }
}

/***
* This fucntion performs a EyeOnHand calibration for a G3 sensor mounted on the Kuka arm
*
* @param    system  The Gocator System object
* @param    sensor  The Gocator Sensor object
* @param    sensor  A pointer to the KukaRobotDriver object
* @param    handEye Return parameter of the hand eye calibration matrix
*
* @return   Status indicator
***/
kStatus Calibrate(GoSystem system, GoSensor sensor, GoRobot::Driver* robotDriver, 
    GoRobot::Matrix* handEye)
{
    GoRobot::Matrix           initPose = GoRobot::KukaPose(345, 0, 220 , 180, 0, 0).toMatrix();
    vector<GoRobot::Matrix>   sPoses;
    vector<GoRobot::Matrix>   rPoses(NUMBER_OF_POSES);
    vector<GoRobot::Matrix>   results(rPoses.size());
    GoDataSet                 dataset = kNULL;
    GoRobot::Matrix           poseMatrix;

    kTry
    {
        // The function EyeOnHandCalibrationPoses returns a list of poses on a dome looking down on an object in the center. 
        // Which is useful for taking multiple scans of the same object (ball bar) from different angles.
        // Note the ballbar should be laying flat at around Z=0
        GoRobot::EyeOnHandCalibrationPoses(rPoses.size(), 10, initPose, rPoses.data());
        for (GoRobot::Matrix pose : rPoses)
        {
            robotDriver->move(&pose, 1);
            Sleep(500);
            kTest(GoSensor_Trigger(sensor));
            kTest(GoSystem_ReceiveData(system, &dataset, 20000000));
            poseMatrix = GoRobot::GetBallBarPoseMeasurement(dataset, 0);
            sPoses.push_back(poseMatrix);

            kDestroyRef(&dataset);
        }
        robotDriver->move(&initPose);

        // Perform the calibration based on the list of robot poses and the ballbar poses
        *handEye = GoRobot::CalibEyeOnHandSystem(rPoses.data(), sPoses.data(), (k32s)rPoses.size());

        cout << "Hand Eye Calibration Matrix:" << endl;
        printMatrix(*handEye, true);
        cout << endl;
    }
    kFinally
    {
        kDestroyRef(&dataset);
        kEndFinally();
    }

    return kOK;
}

/***
* This fucntion is an application example to locating and moving to an object.
* It scans the ballbar again, and moves the TCP to be above the center of one of the balls
*
* @param    system      The Gocator System object
* @param    sensor      The Gocator Sensor object
* @param    sensor      A pointer to the KukaRobotDriver object
* @param    handEye     The hand eye calibration matrix, obtained from the Calibrate function
*
* @return   Status indicator
***/
kStatus MoveToBallBar(GoSystem system, GoSensor sensor, GoRobot::Driver* robotDriver, 
    GoRobot::Matrix* handEye)
{
    GoRobot::Matrix   scanPose = GoRobot::KukaPose(345, 0, 220, 180, 0, 0).toMatrix();
    GoRobot::Matrix   ballbar_in_sensor;
    GoRobot::Matrix   ballbar_in_base;
    GoRobot::Matrix   ballbar_in_base_flipped;
    GoRobot::Matrix   ballbar_in_base_flipped_above;
    GoDataSet         dataset = kNULL;

    kTry
    {
        robotDriver->move(&scanPose);
        Sleep(500);
        kTest(GoSensor_Trigger(sensor));
        kTest(GoSystem_ReceiveData(system, &dataset, 20000000));
        // Replace this function with the measurements of the feature you are trying to find
        ballbar_in_sensor = GoRobot::GetBallBarPoseMeasurement(dataset, 0);

        // "Locate" will transform the pose from Sensor coordinate system to Base coordinate system
        GoRobot::LocateEyeOnHandSystem(&scanPose, &ballbar_in_sensor, 1, *handEye, &ballbar_in_base);
        ballbar_in_base_flipped         = ballbar_in_base         * GoRobot::KukaPose(0, 0,   0, 180, 0, 0).toMatrix();
        ballbar_in_base_flipped_above   = ballbar_in_base_flipped * GoRobot::KukaPose(0, 0, -50,   0, 0, 0).toMatrix();

        robotDriver->move(&ballbar_in_base_flipped_above);
    }
    kFinally
    {
        kDestroyRef(&dataset);
        kEndFinally();
    }

    return kOK;
}

int main()
{
    kAssembly           api = kNULL;
    GoSystem            system = kNULL;
    GoSensor            sensor = kNULL;
    kIpAddress          ipAddress;

    GoRobot::Matrix     tcp = GoRobot::KukaPose(0, 0, 150, 0,0,0).toMatrix();
    GoRobot::Matrix     handEye;

    GoRobot::Driver*    robotDriver = kNULL;

    kTry
    {
        kTest(GoSdk_Construct(&api));

        robotDriver = (GoRobot::Driver*) new KukaRobotDriver(ROBOT_IP_ADDRESS, ROBOT_PORT, kObject_Alloc(api));

        robotDriver->set_base(GoRobot::MATRIX_UNITY);
        robotDriver->set_tcp(tcp);

        kTest(GoSystem_Construct(&system, kNULL));
        kTest(kIpAddress_Parse(&ipAddress, SENSOR_IP_ADDRESS));
        kTest(GoSystem_FindSensorByIpAddress(system, &ipAddress, &sensor));
        kTest(GoSensor_Connect(sensor));
        kTest(GoSystem_EnableData(system, kTRUE));

        // "SetupCalibrationJob" will create the calibration job on the sensor
        GoRobot::SetupCalibrationJob(sensor);

        kTest(GoSystem_Start(system));

        kTest(Calibrate(system, sensor, robotDriver, &handEye));
        kTest(MoveToBallBar(system, sensor, robotDriver, &handEye));

        kTest(GoSystem_Stop(system));
    }
    kFinally
    {
        delete robotDriver;
        kDestroyRef(&system);
        kDestroyRef(&api);
        kEndFinally();
    }
}

