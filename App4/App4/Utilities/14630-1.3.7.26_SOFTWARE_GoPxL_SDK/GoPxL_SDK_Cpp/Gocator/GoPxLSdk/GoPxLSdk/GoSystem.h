/**
 * @file    GoSystem.h
 * @brief   Declares the GoPxLSdk.GoSystem class.
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#ifndef GO_PXL_SDK_SYSTEM_H
#define GO_PXL_SDK_SYSTEM_H

#include <GoPxLSdk/Def.h>
#include <GoPxLSdk/GoRestClient.h>

#include <kApi/kApiDef.h>
#include <kApi/Io/kNetwork.h>

namespace GoPxLSdk
{
/**
 * Represents a single Gocator system.
 *
 * A Gocator system may have multiple sensors/devices.
 * 
 * NOTE: Before the declaration of GoSystem the GoPxl API core framework must be constructed using GoApiLib_Construct().
 * For examples please refer to the sample applications provided.
 */
class GoPxLSdkClass GoSystem
{
/*
 * Constant member value declarations;
 */
private:

    /**
     * Constants used by operations available from this class to specify how long
     * to wait for a response from the requested operation. If no response to
     * the operation is received, then the operation fails with a timeout error.
     *
     * Define individual timeouts for each operation, rather than just one generic timeout
     * for all operations, because operations are not guaranteed to require the same amount
     * of time to complete.
     *
     * The GoSystem APIs are convenience functions that wrap around the underlying REST APIs.
     * Therefore, as convenience functions, they should be made to work as simply as possible
     * without needing the SDK user to configure or specifying anything to make these convenience
     * operations work.
     */
    static constexpr k64u GDP_TIMEOUT_MSEC              = 15000;
    static constexpr k64u START_TIMEOUT_MSEC            = 15000;
    static constexpr k64u STOP_TIMEOUT_MSEC             = 15000;
    static constexpr k64u RUNNING_STATE_TIMEOUT_MSEC    = 15000;
    static constexpr k64u SENSOR_PATH_TIMEOUT_MSEC      = 15000;
    static constexpr k64u CHILD_PATH_TIMEOUT_MSEC       = 15000;
    static constexpr k64u QUICKEDIT_TIMEOUT_MSEC        = 15000;

public:
    enum class State
    {
        Ready = 0,
        Running = 1,
        Conflict = 2
    };

    /**
     * GoSystem default constructor.
     * This constructor automatically creates a GoRestClient instance.
     * However, the IP address and port to which the GoRestClient should connect to
     * still has to be set using the SetAddress() and SetControlPort() function.
     * Otherwise the client will fail to connect to the remote device.
     * when the Connect() API is called.
     *
     * @public                              @memberof GoSystem
     * @version                             Introduced in 0.2.1.53.
     */
    GoSystem() = default;

    /**
     * Constructs a GoSystem object with the given IP address and control port number.
     * This constructor automatically creates a GoRestClient instance,
     * and initialize the IP address and port the client should connect to when the
     * Connect() API is called.
     *
     * @public                              @memberof GoSystem
     * @version                             Introduced in 1.1.50.15
     * @param address                       The IPv4 address of the Gocator remote device.
     * @param port                          The IP control port number of the Gocator device.
     *                                      A port value of zero (0) is invalid.
     * @throws Go::Exception                If arguments are invalid.
     */
    explicit GoSystem(const kIpAddress& address, k16u port = GO_PXL_SDK_DEFAULT_CONTROL_PORT);

    /**
     * GoSystem destructor
     *
     * @public                              @memberof GoSystem
     * @version                             Introduced in 0.2.1.53.
     */
    ~GoSystem() = default;

    /**
     * Sets the ipAddress used in Connect(). IP address must be a IPv4 address.
     *
     * @public                              @memberof GoSystem
     * @version                             Introduced in 0.2.1.53.
     * @param ipAddress                     IPv4 address of the destination device to which the client should connect.
     * @throws Go::Exception                If argument is invalid.
     */
    void SetAddress(const kIpAddress& ipAddress);

    /**
     * Gets the ipAddress used in Connect().
     *
     * @public                              @memberof GoSystem
     * @version                             Introduced in 0.2.1.53.
     */
    kIpAddress Address() const;

    /**
     * Sets the port used in Connect(). Port number cannot be zero (0).
     *
     * @public                              @memberof GoSystem
     * @version                             Introduced in 0.2.1.53.
     * @param port                          Port number of the control port of the destination device to which the client should connect.
     *                                      Value cannot be 0.
     * @throws Go::Exception                If argument is invalid.
     */
    void SetControlPort(k16u port);

    /**
     * Gets the port used in Connect().
     *
     * @public                              @memberof GoSystem
     * @version                             Introduced in 0.2.1.53.
     */
    k16u ControlPort() const;

    /**
     * Gets the GoPxL Data Protocol (GDP) port.
     * This function throws a GoPxLSdk::GoRequestError if the path is invalid.
     * This function throws a GoPxLSdk::GoChannelError if the operation is not completed within the timeout period.
     *
     * @public                              @memberof GoSystem
     * @version                             Introduced in 1.2.30.46
     */
    k16u GdpPort();

    /**
     * Connects to a system. Waits until the connection is established.
     *
     * @public                              @memberof GoSystem
     * @version                             Introduced in 0.2.1.53.
     * @throws Go::Exception                If the connection attempt failed.
     */
    void Connect();

    /**
     * Disconnects from a system. Waits until the disconnection is complete.
     *
     * @public                              @memberof GoSystem
     * @version                             Introduced in 0.2.1.53.
     * @throws Go::Exception                If unable to disconnect.
     */
    void Disconnect();

    /**
     * Checks if the system is connected or not.
     *
     * @public                              @memberof GoSystem
     * @version                             Introduced in 0.2.1.53.
     * @return                              True if connected, false otherwise.
     */
    bool IsConnected();

    /**
     * Starts the device.
     * This function throws a GoPxLSdk::GoChannelError if the operation is not completed within the timeout period.
     * Default timeout period is 15 seconds.
     *
     * @public                              @memberof GoSystem
     * @version                             Introduced in 0.2.1.53.
     * @throws GoPxLSdk::GoChannelError     If the request times out.
     * @throws GoPxLSdk::GoRequestError     If the server responds with a failed status for the request.
     */
    void Start();

    /**
     * Stops the device.
     * This function throws a GoPxLSdk::GoChannelError if the operation is not completed within the timeout period.
     * Default timeout period is 15 seconds.
     *
     * @public                              @memberof GoSystem
     * @version                             Introduced in 0.2.1.53.
     * @throws GoPxLSdk::GoChannelError     If the request times out.
     * @throws GoPxLSdk::GoRequestError     If the server responds with a failed status for the request.
     */
    void Stop();

    /**
     * Gets the system's current state.
     * This function throws a GoPxLSdk::GoChannelError if the operation is not completed within the timeout period.
     * Default timeout period is 15 seconds.
     *
     * @public                              @memberof GoSystem
     * @version                             Introduced in 0.2.1.53.
     * @throws GoPxLSdk::GoChannelError     If the request times out.
     * @throws GoPxLSdk::GoRequestError     If the server responds with a failed status for the request.
     * @return                              Current system state.
     */
    State RunningState();

    /**
     * Enables the quick edit mode.
     * Quick Edit mode allows configuration changes to be made, but disables recalculating the effects of the configuration changes on any existing scanned data. This means the user will not see changes in the Gocator UI visualizer that would normally be seen when a configuration change is made.
     * This function throws a GoPxLSdk::GoChannelError if the operation is not completed within the timeout period.
     * Default timeout period is 15 seconds.
     *
     * @public                              @memberof GoSystem
     * @version                             Introduced in 1.2.30.46
     * @throws GoPxLSdk::GoChannelError     If the request times out.
     * @throws GoPxLSdk::GoRequestError     If the server responds with a failed status for the request.
     */
    void EnableQuickEdit();

    /**
     * Disables the quick edit mode.
     * This function throws a GoPxLSdk::GoChannelError if the operation is not completed within the timeout period.
     * Default timeout period is 15 seconds.
     *
     * @public                              @memberof GoSystem
     * @version                             Introduced in 1.2.30.46
     * @throws GoPxLSdk::GoChannelError     If the request times out.
     * @throws GoPxLSdk::GoRequestError     If the server responds with a failed status for the request.
     */
    void DisableQuickEdit();

    /**
     * Checks if quick edit mode is enabled on the system.
     * This function throws a GoPxLSdk::GoChannelError if the operation is not completed within the timeout period.
     * Default timeout period is 15 seconds.
     *
     * @public                              @memberof GoSystem
     * @version                             Introduced in 1.2.30.46
     * @throws GoPxLSdk::GoChannelError     If the request times out.
     * @throws GoPxLSdk::GoRequestError     If the server responds with a failed status for the request.
     * @return                              True if quick edit mode is enabled, false otherwise.
     */
    bool QuickEditEnabled();

    /**
     * Returns a reference to the client. Most operations are done directly through this client.
     *
     * @public                              @memberof GoSystem
     * @version                             Introduced in 0.2.1.53.
     * @return                              GoRestClient reference.
     */
    GoRestClient& Client();

    /**
     * Given a sensor's serial number, returns its resource path.
     * This function throws a GoPxLSdk::GoChannelError if the operation is not completed within the timeout period.
     * Default timeout period is 15 seconds.
     *
     * @public                              @memberof GoSystem
     * @version                             Introduced in 0.2.1.53.
     * @param serialNum                     The serial number to look for.
     * @throws GoPxLSdk::GoRequestError     If the server responds with a failed status for the request.
     * @throws GoPxLSdk::GoChannelError     If the request times out.
     * @return                              Resource path of the sensor. If sensor with the specified serial number is not found, returns an empty string.
     */
    ResourcePath SensorPath(SerialNum serialNum);

// Private member variables.
private:
    GoRestClient restClient;

    kIpAddress address = { };
    k16u port = GO_PXL_SDK_DEFAULT_CONTROL_PORT;

// Private member functions.
private:
    /**
     * Given a resource path, returns a list of its child paths.
     * This function throws a GoPxLSdk::GoRequestError if the path is invalid.
     * This function throws a GoPxLSdk::GoChannelError if the operation is not completed within the timeout period.
     * Default timeout period is 15 seconds.
     *
     * @param path                          The path for which we want the children resource paths.
     * @throws GoPxLSdk::GoRequestError     If path is invalid.
     * @throws GoPxLSdk::GoChannelError     If the request times out.
     * @return                              List of child paths.
     */
    std::vector<ResourcePath> GetChildPaths(const ResourcePath& path);
};

}

#endif
