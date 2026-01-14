/**
 * @file    Version.cpp
 *
 * @internal
 * Copyright (C) 2023-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#include "Version.h"

namespace GoPxLSdk
{

// The SDK version is the build version that generated the SDK package.
#define GO_PXL_SDK_BUILD_VERSION kVersion_Stringify_(GO_PXL_SDK_VERSION_MAJOR, GO_PXL_SDK_VERSION_MINOR, GO_PXL_SDK_VERSION_RELEASE, GO_PXL_SDK_VERSION_BUILD)

#define SDK_STRINGIFY(MAJOR, MINOR, RELEASE)               \
    xkStringize(MAJOR) "." xkStringize(MINOR) "." xkStringize(RELEASE)

#define GO_PXL_SDK_API_VERSION SDK_STRINGIFY(GO_PXL_SDK_API_VERSION_MAJOR, GO_PXL_SDK_API_VERSION_MINOR, GO_PXL_SDK_API_VERSION_PATCH)

#define GO_PXL_VERSION kVersion_Stringify_(GO_PXL_VERSION_MAJOR, GO_PXL_VERSION_MINOR, GO_PXL_VERSION_RELEASE, GO_PXL_VERSION_BUILD)

GoPxLSdkCppFx(kVersion) BuildVersionNumber()
{
    return kVersion_Create(GO_PXL_SDK_VERSION_MAJOR, GO_PXL_SDK_VERSION_MINOR, GO_PXL_SDK_VERSION_RELEASE, GO_PXL_SDK_VERSION_BUILD);
}

GoPxLSdkCppFx(const kChar *) BuildVersion()
{
    return GO_PXL_SDK_BUILD_VERSION;
}

GoPxLSdkCppFx(const kChar*) ApiVersion()
{
    return GO_PXL_SDK_API_VERSION;
}

GoPxLSdkCppFx(const kChar*) ReleaseVersion()
{
    return GO_PXL_VERSION;
}

} // namespace
