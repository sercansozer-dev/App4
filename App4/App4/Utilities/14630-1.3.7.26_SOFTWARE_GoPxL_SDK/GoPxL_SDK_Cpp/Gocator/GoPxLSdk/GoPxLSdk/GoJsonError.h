/**
 * @file    GoJsonError.h
 * @brief   Declares the GoPxLSdk.GoJsonError class.
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#ifndef GO_PXL_SDK_GOJSONERROR_H
#define GO_PXL_SDK_GOJSONERROR_H

#include <stdexcept>

namespace GoPxLSdk
{

class GoPxLSdkClass GoJsonError : public std::runtime_error
{
public:
    /**
     * Constructs GoJsonError.
     *
     * @public                @memberof GoJsonError
     * @version               Introduced in 0.2.1.53
     */
    GoJsonError();

    GoJsonError(const char* message);
};

}

#endif