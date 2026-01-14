/**
 * @file    GoGdpBoundingBox.cpp
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#include <GoPxLSdk/GoGdpMsg/GoGdpBoundingBox.h>

namespace GoPxLSdk
{
    void GoGdpBoundingBox::Deserialize(kSerializer serializer)
    {
        k32f point;
        try
        {
            GoTest(kSerializer_Read32f(serializer, &point));
            this->x = (k64f)point;
            GoTest(kSerializer_Read32f(serializer, &point));
            this->y = (k64f)point;
            GoTest(kSerializer_Read32f(serializer, &point));
            this->z = (k64f)point;

            GoTest(kSerializer_Read32f(serializer, &point));
            this->width = (k64f)point;
            GoTest(kSerializer_Read32f(serializer, &point));
            this->length = (k64f)point;
            GoTest(kSerializer_Read32f(serializer, &point));
            this->height = (k64f)point;
        }
        catch (const Go::Exception&)
        {
            GoRethrow("Failed to deserialize bounding box.");
        }

    }

    const k64f GoGdpBoundingBox::X() const
    {
        return this->x;
    }

    const k64f GoGdpBoundingBox::Y() const
    {
        return this->y;
    }

    const k64f GoGdpBoundingBox::Z() const
    {
        return this->z;
    }

    const k64f GoGdpBoundingBox::Width() const
    {
        return this->width;
    }

    const k64f GoGdpBoundingBox::Length() const
    {
        return this->length;
    }

    const k64f GoGdpBoundingBox::Height() const
    {
        return this->height;
    }
    
    GoGdpBoundingBox& GoGdpBoundingBox::operator= (const GoGdpBoundingBox& box)
    {
        this->x = box.x;
        this->y = box.y;
        this->z = box.z;
        this->width = box.width;
        this->length = box.length;
        this->height = box.height;
        return *this;
    }
}



