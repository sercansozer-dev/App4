/**
 * @file    GoRequestMethod.cpp
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#include <GoPxLSdk/Def.h>
#include <GoPxLSdk/GoRequestMethod.h>

namespace GoPxLSdk
{

std::string GoRequestMethod::ToString()
{
    switch (method)
    {
        case Method::Call: return "call";
        case Method::Create: return "create";
        case Method::Delete: return "delete";
        case Method::Read: return "read";
        case Method::StartStream: return "stream";
        case Method::StopStream: return "cancelStream";
        case Method::Sub: return "sub";
        case Method::UnSub: return "unSub";
        case Method::Update: return "update";
        default: return "undefined";
    }
}

}