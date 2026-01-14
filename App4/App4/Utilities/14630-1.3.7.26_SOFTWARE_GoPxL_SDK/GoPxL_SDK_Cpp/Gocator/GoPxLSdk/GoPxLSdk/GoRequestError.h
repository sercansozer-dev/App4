/**
 * @file    GoRequestError.h
 * @brief   Declares the GoPxLSdk.GoRequestError class.
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#ifndef GO_PXL_SDK_REQUESTERROR_H
#define GO_PXL_SDK_REQUESTERROR_H

#include <stdexcept>
#include <GoPxLSdk/GoResponse.h>

namespace GoPxLSdk
{

class GoPxLSdkClass GoRequestError : public std::runtime_error
{
public:
    /**
     * Constructs GoRequestError.
     *
     * @public                @memberof GoRequestError
     * @version               Introduced in 0.2.1.53
     */
    GoRequestError();
    GoRequestError(const GoRequestResponse& response);

    /**
     * @public                @memberof GoRequestError
     * @version               Introduced in 0.2.1.53
     */
    GoRequestResponse GetResponse() const;

private:
    GoRequestResponse response;
};

}

#endif