/**
 * @file    GoUri.cpp
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#include <GoPxLSdk/Def.h>
#include <GoPxLSdk/GoUri.h>

#include <GoApi/Net/Url.h>

namespace GoPxLSdk
{

std::string GoUri::UrlEncode(const std::string& src, bool spaceAsPlus)
{
    return Go::Net::Url::Encode(src, spaceAsPlus);
}

std::string GoUri::UrlDecode(const std::string& src, bool spaceAsPlus)
{
    return Go::Net::Url::Decode(src, spaceAsPlus);
}

}