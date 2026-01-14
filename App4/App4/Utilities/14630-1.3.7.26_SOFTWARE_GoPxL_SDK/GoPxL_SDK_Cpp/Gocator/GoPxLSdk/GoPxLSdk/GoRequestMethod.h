/**
 * @file    GoRequestMethod.h
 * @brief   Declares the GoPxLSdk.GoRequestMethod class.
 * 
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#ifndef GO_PXL_SDK_REQUESTMETHOD_H
#define GO_PXL_SDK_REQUESTMETHOD_H

#include <kApi/kApiDef.h>

namespace GoPxLSdk
{

class GoPxLSdkClass GoRequestMethod
{
public:
    enum Method : k32s 
    {
        Create,             ///< A Create request
        Read,               ///< A Read request
        Update,             ///< A Update request
        Delete,             ///< A Delete request
        Call,               ///< A Call request
        Sub,                ///< A Subscribe request
        UnSub,              ///< An Unsubscribe request
        StartStream,        ///< A Start stream request
        StopStream          ///< A Stop stream request
    };

    GoRequestMethod() = default;

    constexpr GoRequestMethod(Method method) : method(method) { }

    operator Method() const { return method; }
    constexpr bool operator==(GoRequestMethod r) const { return method == r.method; }
    constexpr bool operator!=(GoRequestMethod r) const { return method != r.method; }

    std::string ToString();

private:
    Method method;
};

}

#endif
