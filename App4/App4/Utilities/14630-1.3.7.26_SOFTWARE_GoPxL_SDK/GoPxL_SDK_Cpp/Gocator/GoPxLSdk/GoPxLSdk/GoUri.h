/**
 * @file    GoUri.h
 * @brief   Declares the GoPxLSdk.GoUri class.
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#ifndef GO_PXL_SDK_GOURI_H
#define GO_PXL_SDK_GOURI_H

namespace GoPxLSdk
{
/* 
 * Uri helper methods.
 */
class GoPxLSdkClass GoUri
{
public:
    /**
     * Encodes given string.
     *
     * @public                @memberof GoUri
     * @version               Introduced in 0.2.1.53
     * @param src string to encode.
     * @param spaceAsPlus use plus sign as space.
     */
    static std::string UrlEncode(const std::string& src, bool spaceAsPlus = false);

    /**
     * Decodes given string.
     *
     * @public                @memberof GoUri
     * @version               Introduced in 0.2.1.53
     * @param src string to encode.
     * @param spaceAsPlus use plus sign as space.
     */
    static std::string UrlDecode(const std::string& src, bool spaceAsPlus = false);
};

}

#endif