/**
 * @file    Version.h
 * @brief   Declares build and SDK API version numbers.
 *
 * @internal
 * Copyright (C) 2023-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#ifndef GOPXL_SDK_VERSION_H
#define GOPXL_SDK_VERSION_H

#include "Def.h"

// This constants defined in this file and the values listed in this confluence page
//     https://lmitechnologies.atlassian.net/wiki/spaces/GS/pages/1508127/Versions+and+Releases
// "Versions and Releases", must be in sync.
namespace GoPxLSdk
{
// The SDK version numbers describes the SDK build version digits.
// The backend build scripts will automatically replace these definitions
// with the actual version numbers for a build.
// For development, we should always set the Major and Minor numbers
// to the upcoming release in this file in the code repo.
//
// The build version is a 32-bit number made up of the digits as follows, where each digit
// occupies 8-bits in the version number:
//   version = MajorMinorReleaseBuild
#define GO_PXL_SDK_VERSION_MAJOR 8
#define GO_PXL_SDK_VERSION_MINOR 3
#define GO_PXL_SDK_VERSION_RELEASE 46
#define GO_PXL_SDK_VERSION_BUILD 26

// The SDK API version is the REST API version used by the SDK to communicate with the device.
// Changes to the REST API can affect whether the SDK application can communicate with the device,
// so it is important for SDK applications to know if the REST API used by the SDK application's SDK
// code matches the REST API of the device.
//
// Note this version should match the GOREST_API_CURRENT_VERSION.
//
// GoPxL 1.3 introduced breaking changes to the REST API so major version is incremented from 6 to 7.
#define GO_PXL_SDK_API_VERSION_MAJOR    7
#define GO_PXL_SDK_API_VERSION_MINOR    0
#define GO_PXL_SDK_API_VERSION_PATCH    0

// Specifies the GoPxL version aligned with the SDK release, updated before each release.
#define GO_PXL_VERSION_MAJOR        1
#define GO_PXL_VERSION_MINOR        2
#define GO_PXL_VERSION_RELEASE      30
#define GO_PXL_VERSION_BUILD        46

/**
* Returns the SDK build version as a 32-bit number in the format:
*    MMmmRRBB
* where:
*  MM = major number
*  mm = minor number
*  RR = release number
*  BB = build number
*
* The SDK build version is the build version that generated the SDK package.
*
* @return          Gocator SDK build version number.
*/
GoPxLSdkCppFx(kVersion) BuildVersionNumber();

/**
* Returns the SDK build version as a string. Format is:
*    MM.mm.RR.BB
* where:
*  MM = major number
*  mm = minor number
*  RR = release number
*  BB = build number
*
* The SDK build version is the build version that generated the SDK package.
*
* @return          Gocator SDK build version string.
*/
GoPxLSdkCppFx(const kChar*) BuildVersion();

/**
* Returns the SDK REST API version as string. Format is:
*   MM.mm.PN
* where:
*  MM = major number
*  mm = minor number
*  PN = patch number
*
* The SDK REST API version is the version of the SDK interfaces used to communicate with the device.
*
* @return          Gocator SDK REST API version string.
*/
GoPxLSdkCppFx(const kChar*) ApiVersion();

/**
* Returns the GoPxL release version of the SDK as string. Format is:
*    MM.mm.RR.BB
* where:
*  MM = major number
*  mm = minor number
*  RR = release number
*  BB = build number
*
* The GoPxL release version is the build version that generated the SDK package.
*
* @return          GoPxL release version number.
*/
GoPxLSdkCppFx(const kChar*) ReleaseVersion();

}
#endif
