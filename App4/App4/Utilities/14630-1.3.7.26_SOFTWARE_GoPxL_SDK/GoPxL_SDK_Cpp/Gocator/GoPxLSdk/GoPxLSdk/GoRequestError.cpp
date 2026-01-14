/**
 * @file    GoRequestError.cpp
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#include <GoPxLSdk/Def.h>
#include <GoPxLSdk/GoRequestError.h>

namespace GoPxLSdk
{

GoRequestError::GoRequestError() : std::runtime_error("GoRequestError") { }

GoRequestError::GoRequestError(const GoRequestResponse& response) : std::runtime_error("GoRequestError")
{
    this->response = response;
}

GoRequestResponse GoRequestError::GetResponse() const
{
    return this->response;
}

}