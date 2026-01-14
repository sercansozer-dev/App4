/**
 * @file    GoInstance.h
 * @brief   Declares the GoPxLSdk.GoInstance class.
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#ifndef GO_PXL_SDK_GOINSTANCE_H
#define GO_PXL_SDK_GOINSTANCE_H

#include <kApi/Io/kUdpClient.h>
#include <kApi/kApi.h>

namespace GoPxLSdk 
{
class GoPxLSdkClass GoInstance
{
public:
public:
    /**
     * Constructs GoInstance.
     *
     * @public                      @memberof GoInstance
     * @version                     Introduced in 0.2.1.53
     */
    GoInstance();
    ~GoInstance() = default;

    /**
     * Gets the IP address of GoPxL instance.
     * 
     * @public                      @memberof GoInstance
     * @version                     Introduced in 0.2.1.53
     * @return                      IP address of GoPxL instance.
     */
    const kIpAddress& GetIpAddress() const;

    /**
     * Sets the IP address of GoPxL instance.
     * 
     * @public                      @memberof GoInstance
     * @version                     Introduced in 0.2.1.53
     * @param ipAddress             IP address to be assigned to the GoPxL instance.
     */
    void SetIpAddress(kIpAddress ipAddress);
    
    /**
     * Gets the unique Application Id of GoPxL instance.
     * 
     * @public                      @memberof GoInstance
     * @version                     Introduced in 0.2.1.53
     * @return                      Application Id of GoPxL instance.
     */
    const std::string& GetAppId() const;

    /**
     * Sets the Application Id of GoPxL instance.
     * 
     * @public                      @memberof GoInstance
     * @version                     Introduced in 0.2.1.53
     * @param appId                 Application Id to be assigned to the GoPxL instance.
     */
    void SetAppId(std::string appId);

    /**
     * Gets the Application name of GoPxL instance.
     * 
     * @public                      @memberof GoInstance
     * @version                     Introduced in 0.2.1.53
     * @return                      Application name of GoPxL instance.
     */
    const std::string& GetAppName() const;

    /**
     * Sets the Application name of GoPxL instance.
     * 
     * @public                      @memberof GoInstance
     * @version                     Introduced in 0.2.1.53
     * @param appName               Application name to be assigned to the GoPxL instance.
     */
    void SetAppName(std::string appName);

    /**
     * Gets the Application version of GoPxL instance.
     * 
     * @public                      @memberof GoInstance
     * @version                     Introduced in 0.2.1.53
     * @return                      Application version of GoPxL instance.
     */
    const std::string& GetAppVersion() const;

    /**
     * Sets the Application version of GoPxL instance.
     * 
     * @public                      @memberof GoInstance
     * @version                     Introduced in 0.2.1.53
     * @param appVersion            Application version assigned to the GoPxL instance.
     */
    void SetAppVersion(std::string appVersion);
    
    /**
     * Gets the Control port of GoPxL instance.
     * 
     * @public                @memberof GoInstance
     * @version               Introduced in 1.1.50.15
     * @return                Control port of GoPxL instance.
     */
    const k16u GetControlPort() const;

    /**
     * Sets the Control port of GoPxL instance.
     * 
     * @public                      @memberof GoInstance
     * @param controlPort           Control port assigned to the GoPxL instance.
     * @version                     Introduced in 1.1.50.15
     */
    void SetControlPort(k16u controlPort);

    /**
     * Gets the Serial Number of GoPxL instance.
     * 
     * @public                      @memberof GoInstance
     * @version                     Introduced in 0.2.1.53
     * @return                      Serial Number of GoPxL instance.
     */
    const k32u& GetSerialNumber() const;

    /**
     * Sets the Serial Number of GoPxL instance.
     * 
     * @public                      @memberof GoInstance
     * @version                     Introduced in 0.2.1.53
     * @param serialNumber          Serial number assigned to the GoPxL instance.
     */
    void SetSerialNumber(k32u serialNumber);
    
    /**
     * Gets the Device model of GoPxL instance.
     * 
     * @public                      @memberof GoInstance
     * @version                     Introduced in 0.2.1.53
     * @return                      Device model of GoPxL instance.
     */
    const std::string& GetDeviceModel() const;

    /**
     * Sets the Device model of GoPxL instance.
     * 
     * @public                      @memberof GoInstance
     * @version                     Introduced in 0.2.1.53
     * @param deviceModel           Device Model assigned to the GoPxL instance.
     */
    void SetDeviceModel(std::string deviceModel);

    /**
     * Checks if the Dhcp of GoPxL instance is enabled or not.
     * 
     * @public                      @memberof GoInstance
     * @version                     Introduced in 0.2.1.53
     * @return                      True if enabled, false otherwise.
     */
    const bool GetIsDhcp() const;

    /**
     * Sets Dhcp of GoPxL instance to true if Dchp is enabled or false otherwise.
     * 
     * @public                      @memberof GoInstance
     * @version                     Introduced in 0.2.1.53
     * @param isDhcp                Flag indicating whether DHCP is enabled (true) or disabled.
     */
    void SetIsDhcp(bool isDhcp);
    
    /**
     * Gets the Gateway of GoPxL instance.
     * 
     * @public                      @memberof GoInstance
     * @version                     Introduced in 0.2.1.53
     * @return                      Gateway of GoPxL instance.
     */
    const kIpAddress& GetGateway() const;

    /**
     * Sets the Gateway of GoPxL instance.
     *
     * @public                      @memberof GoInstance
     * @version                     Introduced in 0.2.1.53
     * @param gateway               The IP address of the gateway.
     */
    void SetGateway(kIpAddress gateway);

    /**
     * Gets the GoPxL Data Protocol (Gdp) port of GoPxL instance.
     * 
     * @public                @memberof GoInstance
     * @version               Introduced in 1.1.50.15
     * @return                Gdp port of GoPxL instance.
     */
    const k16u GetGdpPort() const;

    /**
     * Sets the GoPxL Data Protocol (Gdp) port of GoPxL instance.
     * 
     * @public                      @memberof GoInstance
     * @version                     Introduced in 1.1.50.15
     * @param gdpPort               Port number to be used for GoPxL Data Protocol.
     */
    void SetGdpPort(k16u gdpPort);

    /**
     * Checks if there was an IP address conflict.
     * 
     * @public                      @memberof GoInstance
     * @version                     Introduced in 0.2.1.53
     * @return                      True if the IP address was automatically assigned due to an address conflict,
     *                              return false otherwise.
     */
    const bool GetIsAddressConflict() const;
    
    /**
     * Sets isAddressConflict value of GoPxL instance if there was an IP address conflict,
     * false otherwise.
     * 
     * @public                      @memberof GoInstance
     * @version                     Introduced in 0.2.1.53
     * @param isAddressConflict     Flag indicating whether an IP address conflict occurred.
     */
    void SetIsAddressConflict(bool isAddressConflict);

    /**
     * Checks if the GoPxL instance is connected remotely.
     * 
     * @public                      @memberof GoInstance
     * @version                     Introduced in 0.2.1.53
     * @return                      True if LMI device is remotely connected/controlled from another GoPxL instance,
     *                              return false otherwise.
     */
    const bool GetIsRemote() const;
    
    /**
     * Sets isRemote of GoPxL instance to true if GoPxL instance is connected remotely, 
     * false otherwise.
     * 
     * @public                      @memberof GoInstance
     * @version                     Introduced in 0.2.1.53
     * @param isRemote              Flag indicating whether GoPxL instance is connected remotely or locally.
     */
    void SetIsRemote(bool isRemote);

    /**
     * Gets the Mask of GoPxL instance.
     * 
     * @public                      @memberof GoInstance
     * @version                     Introduced in 0.2.1.53
     * @return                      Mask of GoPxL instance.
     */
    const kIpAddress& GetMask() const;

    /**
     * Sets the Mask of GoPxL instance.
     * 
     * @public                      @memberof GoInstance
     * @version                     Introduced in 0.2.1.53
     * @param mask                  The subnet mask IP address.
     */
    void SetMask(kIpAddress mask);

    /**
     * Gets the Web port of GoPxL instance.
     * 
     * @public                @memberof GoInstance
     * @version               Introduced in 1.1.50.15
     * @return                Webport of GoPxL instance.
     */
    const k16u GetWebPort() const;

    /**
     * Sets the Web port of GoPxL instance.
     * 
     * @public                @memberof GoInstance
     * @version               Introduced in 1.1.50.15
     * @param webPort         Port to be used for the web server.
     */
    void SetWebPort(k16u webPort);

    /**
    * Gets the HMI status of GoPxL instance.
    *
    * @public                @memberof GoInstance
    * @return                HMI status of GoPxL instance.
    */
    int GetHMIStatus() const;

    /**
    * Sets the HMI status of GoPxL instance.
    *
    * @public                @memberof GoInstance
    * @param status          The status of the HMI service.
    */
    void SetHMIStatus(int status);

private:
    kIpAddress ipAddress;
    std::string appId;
    std::string appName;
    std::string appVersion;
    k16u controlPort;
    k32u serialNumber;
    std::string deviceModel;
    bool isDhcp;
    kIpAddress gateway;
    k16u gdpPort;
    bool isAddressConflict;
    bool isRemote;
    kIpAddress mask;
    k16u webPort;
    int hmiStatus;
};
}
#endif