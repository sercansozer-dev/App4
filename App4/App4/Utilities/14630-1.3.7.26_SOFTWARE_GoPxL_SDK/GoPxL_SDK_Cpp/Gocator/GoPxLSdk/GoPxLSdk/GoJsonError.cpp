/**
 * @file    GoJsonError.cpp
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#include <GoPxLSdk/Def.h>
#include <GoPxLSdk/GoJsonError.h>

namespace GoPxLSdk
{

GoJsonError::GoJsonError() : std::runtime_error("GoJsonError") { }

GoJsonError::GoJsonError(const char* message) : std::runtime_error(message) { }

}