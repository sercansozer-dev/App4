/**
 * @file    GoGdpTransform.cpp
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#include <GoPxLSdk/GoGdpMsg/GoGdpTransform.h>

namespace GoPxLSdk
{
    void GoGdpTransform::Deserialize(kSerializer serializer)
    {
        k32f fValue;
        try
        {
            GoTest(kSerializer_Read32f(serializer, &fValue));
            this->xx = (k64f)fValue;
            GoTest(kSerializer_Read32f(serializer, &fValue));
            this->xy = (k64f)fValue;
            GoTest(kSerializer_Read32f(serializer, &fValue));
            this->xz = (k64f)fValue;
            GoTest(kSerializer_Read32f(serializer, &fValue));
            this->xt = (k64f)fValue;

            GoTest(kSerializer_Read32f(serializer, &fValue));
            this->yx = (k64f)fValue;
            GoTest(kSerializer_Read32f(serializer, &fValue));
            this->yy = (k64f)fValue;
            GoTest(kSerializer_Read32f(serializer, &fValue));
            this->yz = (k64f)fValue;
            GoTest(kSerializer_Read32f(serializer, &fValue));
            this->yt = (k64f)fValue;

            GoTest(kSerializer_Read32f(serializer, &fValue));
            this->zx = (k64f)fValue;
            GoTest(kSerializer_Read32f(serializer, &fValue));
            this->zy = (k64f)fValue;
            GoTest(kSerializer_Read32f(serializer, &fValue));
            this->zz = (k64f)fValue;
            GoTest(kSerializer_Read32f(serializer, &fValue));
            this->zt = (k64f)fValue;
        }
        catch (const Go::Exception&)
        {
            GoRethrow("Failed to deserialize transform.");
        }
    }

    const k64f GoGdpTransform::XX() const
    {
        return this->xx;
    }

    const k64f GoGdpTransform::XY() const
    {
        return this->xy;
    }

    const k64f GoGdpTransform::XZ() const
    {
        return this->xz;
    }

    const k64f GoGdpTransform::XT() const
    {
        return this->xt;
    }

    const k64f GoGdpTransform::YX() const
    {
        return this->yx;
    }

    const k64f GoGdpTransform::YY() const
    {
        return this->yy;
    }

    const k64f GoGdpTransform::YZ() const
    {
        return this->yz;
    }

    const k64f GoGdpTransform::YT() const
    {
        return this->yt;
    }

    const k64f GoGdpTransform::ZX() const
    {
        return this->zx;
    }

    const k64f GoGdpTransform::ZY() const
    {
        return this->zy;
    }

    const k64f GoGdpTransform::ZZ() const
    {
        return this->zz;
    }

    const k64f GoGdpTransform::ZT() const
    {
        return this->zt;
    }

}