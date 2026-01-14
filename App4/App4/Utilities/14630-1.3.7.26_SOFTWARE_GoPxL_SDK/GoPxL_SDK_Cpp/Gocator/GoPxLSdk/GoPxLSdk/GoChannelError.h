/**
 * @file    GoChannelError.h
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#ifndef GO_PXL_SDK_CHANNELERROR_H
#define GO_PXL_SDK_CHANNELERROR_H

#include <stdexcept>

namespace GoPxLSdk
{

class GoPxLSdkClass GoChannelError final : public std::runtime_error 
{
public:
    /**
     * Constructs GoChannelError.
     *
     * @public                @memberof GoChannelError
     * @version               Introduced in 0.2.1.53
     */
    GoChannelError();

    GoChannelError(const char* message);
};

}

#endif