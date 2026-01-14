/**
 * @file    GoChannelError.cpp
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#include <GoPxLSdk/Def.h>
#include <GoPxLSdk/GoChannelError.h>

namespace GoPxLSdk
{

GoChannelError::GoChannelError() : std::runtime_error("GoChannelError") { }

GoChannelError::GoChannelError(const char* message) : std::runtime_error(message) { }

}