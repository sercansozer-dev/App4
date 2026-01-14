/**
 * @file    GoGdpRendering.cpp
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#include <GoPxLSdk/GoGdpMsg/GoGdpRendering.h>

namespace GoPxLSdk
{
    GoGdpRendering::GoGdpRendering()
        : GoGdpMsg(MessageType::RENDERING)
    {}

    void GoGdpRendering::Deserialize(kSerializer serializer)
    {
        GoGdpMsg::Deserialize(serializer);

        try
        {
            graphics.Deserialize(serializer);
        }
        catch (const Go::Exception&)
        {
            GoRethrow("Failed to deserialize Rendering GDP message.");
        }
    }

    const GoGraphics& GoGdpRendering::GetGraphics() const
    {
        return graphics;
    }

    // ------------------ GoGraphics object member functions -------------------
    const size_t GoGraphics::PointSetCount() const
    {
        return pointSet.size();
    }

    const GoPointSet& GoGraphics::PointSetAt(k32u index) const
    {
        GoThrowIf(index >= pointSet.size(), kERROR_PARAMETER);

        return pointSet.at(index);
    }
    const size_t GoGraphics::LineSetCount() const
    {
        return lineSet.size();
    }

    const GoLineSet& GoGraphics::LineSetAt(k32u index) const
    {
        GoThrowIf(index >= lineSet.size(), kERROR_PARAMETER);

        return lineSet.at(index);
    }

    const size_t GoGraphics::RegionCount() const
    {
        return regionSet.size();
    }

    const GoRegion& GoGraphics::RegionAt(k32u index) const
    {
        GoThrowIf(index >= regionSet.size(), kERROR_PARAMETER);

        // The region set is a vector of pointers, so derefernce the pointer to
        // get at the Region object.
        return *regionSet.at(index);
    }

    const size_t GoGraphics::PlaneCount() const
    {
        return planeSet.size();
    }

    const GoPlane& GoGraphics::PlaneAt(k32u index) const
    {
        GoThrowIf(index >= planeSet.size(), kERROR_PARAMETER);

        return planeSet.at(index);
    }

    const size_t GoGraphics::RayCount() const
    {
        return raySet.size();
    }

    const GoRay& GoGraphics::RayAt(k32u index) const
    {
        GoThrowIf(index >= raySet.size(), kERROR_PARAMETER);

        return raySet.at(index);
    }

    const size_t GoGraphics::LabelCount() const
    {
        return labelSet.size();
    }

    const GoLabel& GoGraphics::LabelAt(k32u index) const
    {
        GoThrowIf(index >= labelSet.size(), kERROR_PARAMETER);

        return labelSet.at(index);
    }

    const size_t GoGraphics::PositionCount() const
    {
        return positionSet.size();
    }

    const GoPosition& GoGraphics::PositionAt(k32u index) const
    {
        GoThrowIf(index >= positionSet.size(), kERROR_PARAMETER);

        return positionSet.at(index);
    }

    void GoGraphics::Deserialize(kSerializer serializer)
    {
        k16u pointSetCount, lineSetCount, regionCount, planeCount,
            rayCount, labelCount, positionCount;
        bool needEndRead = false;

        try
        {
            // Read sectioned attribute section containing all graphic counts.
            GoTest(kSerializer_BeginRead(serializer, kTypeOf(k16u), kTRUE));
            needEndRead = true;

            GoTest(kSerializer_Read16u(serializer, &pointSetCount));
            GoTest(kSerializer_Read16u(serializer, &lineSetCount));
            GoTest(kSerializer_Read16u(serializer, &regionCount));
            GoTest(kSerializer_Read16u(serializer, &planeCount));
            GoTest(kSerializer_Read16u(serializer, &rayCount));
            GoTest(kSerializer_Read16u(serializer, &labelCount));
            GoTest(kSerializer_Read16u(serializer, &positionCount));

            GoTest(kSerializer_EndRead(serializer));
            needEndRead = false;

            // After the sectioned attribute section contains all the
            // graphic data.
            DeserializePointSets(serializer, pointSetCount);
            DeserializeLineSets(serializer, lineSetCount);
            DeserializeRegions(serializer, regionCount);
            DeserializePlanes(serializer, planeCount);
            DeserializeRays(serializer, rayCount);
            DeserializeLabels(serializer, labelCount);
            DeserializePositions(serializer, positionCount);
        }
        catch (const Go::Exception&)
        {
            if (needEndRead)
            {
                kSerializer_EndRead(serializer);
            }
            GoRethrow("Failed to deserialize graphics.");
        }


    }

    void GoGraphics::DeserializePointSets(kSerializer serializer, k16s count)
    {
        pointSet.clear();

        for (auto i = 0; i < count; i++)
        {
            GoPointSet pointSetEntry;
            k16u pointCount;

            try
            {
                ReadOnePointSet(serializer, pointSetEntry, pointCount);

                for (auto j = 0; j < pointCount; j++)
                {
                    kPoint3d32f point;

                    GoTest(kSerializer_Read32f(serializer, &point.x));
                    GoTest(kSerializer_Read32f(serializer, &point.y));
                    GoTest(kSerializer_Read32f(serializer, &point.z));

                    pointSetEntry.points.push_back(point);
                }

                pointSet.push_back(pointSetEntry);
            }
            catch (const Go::Exception&)
            {
                GoRethrow("Failed to deserialize point sets.");
            }
        }
    }

    void GoGraphics::DeserializeLineSets(kSerializer serializer, k16s count)
    {
        lineSet.clear();

        for (auto i = 0; i < count; i++)
        {
            GoLineSet lineSetEntry;
            k16u pointCount;

            try
            {
                ReadOneLineSet(serializer, lineSetEntry, pointCount);

                for (auto j = 0; j < pointCount; j++)
                {
                    kPoint3d32f point;

                    GoTest(kSerializer_Read32f(serializer, &point.x));
                    GoTest(kSerializer_Read32f(serializer, &point.y));
                    GoTest(kSerializer_Read32f(serializer, &point.z));

                    lineSetEntry.points.push_back(point);
                }

                lineSet.push_back(lineSetEntry);
            }
            catch (const Go::Exception&)
            {
                GoRethrow("Failed to deserialize line sets.");
            }
        }
    }

    void GoGraphics::DeserializeRegions(kSerializer serializer, k16s count)
    {
        regionSet.clear();

        for (auto i = 0; i < count; i++)
        {
            k8u type;

            try
            {
                GoTest(kSerializer_Read8u(serializer, &type));

                switch (type)
                {
                case Profile_Region_2d:
                {
                    auto region = std::make_shared<GoProfileRegion2d>();

                    region->type = (RegionType)type;

                    ReadProfileRegion2d(serializer, *region);

                    regionSet.push_back(region);
                    break;
                }
                case Surface_Region_2d:
                {
                    auto region = std::make_shared<GoSurfaceRegion2d>();

                    region->type = (RegionType)type;

                    ReadSurfaceRegion2d(serializer, *region);

                    regionSet.push_back(region);
                    break;
                }
                case Region_3d:
                {
                    auto region = std::make_shared<GoRegion3d>();

                    region->type = (RegionType)type;

                    ReadSurfaceRegion3d(serializer, *region);

                    regionSet.push_back(region);
                    break;
                }
                default:
                    break;
                }
            }
            catch (const Go::Exception&)
            {
                GoRethrow("Failed to deserialize regions.");
            }
        }
    }

    void GoGraphics::DeserializePlanes(kSerializer serializer, k16s count)
    {
        bool needEndRead = false;

        planeSet.clear();

        for (auto i = 0; i < count; i++)
        {
            GoPlane planeEntry;

            try
            {
                GoTest(kSerializer_BeginRead(serializer, kTypeOf(k16u), kTRUE));
                needEndRead = true;

                GoTest(kSerializer_Read32f(serializer, &planeEntry.distance));
                GoTest(kSerializer_Read32f(serializer, &planeEntry.normal.x));
                GoTest(kSerializer_Read32f(serializer, &planeEntry.normal.y));
                GoTest(kSerializer_Read32f(serializer, &planeEntry.normal.z));

                GoTest(kSerializer_EndRead(serializer));
                needEndRead = false;
            }
            catch (const Go::Exception&)
            {
                if (needEndRead)
                {
                    kSerializer_EndRead(serializer);
                }
                GoRethrow("Failed to deserialize planes.");
            }

            planeSet.push_back(planeEntry);
        }
    }

    void GoGraphics::DeserializeRays(kSerializer serializer, k16s count)
    {
        bool needEndRead = false;

        raySet.clear();

        for (auto i = 0; i < count; i++)
        {
            GoRay rayEntry;

            try
            {
                GoTest(kSerializer_BeginRead(serializer, kTypeOf(k16u), kTRUE));
                needEndRead = true;

                GoTest(kSerializer_Read32f(serializer, &rayEntry.position.x));
                GoTest(kSerializer_Read32f(serializer, &rayEntry.position.y));
                GoTest(kSerializer_Read32f(serializer, &rayEntry.position.z));

                GoTest(kSerializer_Read32f(serializer, &rayEntry.direction.x));
                GoTest(kSerializer_Read32f(serializer, &rayEntry.direction.y));
                GoTest(kSerializer_Read32f(serializer, &rayEntry.direction.z));

                GoTest(kSerializer_Read32f(serializer, &rayEntry.width));

                GoTest(kSerializer_Read32u(serializer, &rayEntry.color));

                GoTest(kSerializer_EndRead(serializer));
                needEndRead = false;
            }
            catch (const Go::Exception&)
            {
                if (needEndRead)
                {
                    kSerializer_EndRead(serializer);
                }
                GoRethrow("Failed to deserialize rays.");
            }

            raySet.push_back(rayEntry);
        }
    }

    void GoGraphics::DeserializeLabels(kSerializer serializer, k16s count)
    {
        bool needEndRead = false;

        labelSet.clear();

        for (auto i = 0; i < count; i++)
        {
            GoLabel labelEntry;
            k16u length;
            kText128 text;
            kChar* longText = nullptr;

            try
            {
                GoTest(kSerializer_BeginRead(serializer, kTypeOf(k16u), kTRUE));

                GoTest(kSerializer_Read16u(serializer, &length));
                if (length > 0)
                {
                    if (length < (k16u) kCountOf(text))
                    {
                        GoTest(kSerializer_ReadCharArray(serializer, text, length));
                        text[length] = '\0';  // Add null terminator.
                        labelEntry.text = text;
                    }
                    else
                    {
                        kStatus status;

                        // Add one byte for null terminator. Buffer is initialized with zero so
                        // string will always be null terminated.
                        GoTest(kMemAllocZero(length+1, &longText));
                        status = kSerializer_ReadCharArray(serializer, longText, length);
                        if (kSuccess(status))
                        {
                            labelEntry.text = longText;
                        }
                        // Must free memory before continuing. Ignore return status for the free call.
                        kMemFree(longText);
                        GoTest(status);
                    }
                }

                GoTest(kSerializer_Read64f(serializer, &labelEntry.position.x));
                GoTest(kSerializer_Read64f(serializer, &labelEntry.position.y));
                GoTest(kSerializer_Read64f(serializer, &labelEntry.position.z));

                GoTest(kSerializer_EndRead(serializer));
                needEndRead = false;
            }
            catch (const Go::Exception&)
            {
                if (needEndRead)
                {
                    kSerializer_EndRead(serializer);
                }
                GoRethrow("Failed to deserialize labels.");
            }

            labelSet.push_back(labelEntry);
        }
    }

    void GoGraphics::DeserializePositions(kSerializer serializer, k16s count)
    {
        bool needEndRead = false;

        positionSet.clear();

        for (auto i = 0; i < count; i++)
        {
            GoPosition positionEntry;

            try
            {
                GoTest(kSerializer_BeginRead(serializer, kTypeOf(k16u), kTRUE));
                needEndRead = true;

                GoTest(kSerializer_Read64f(serializer, &positionEntry.position.x));
                GoTest(kSerializer_Read64f(serializer, &positionEntry.position.y));
                GoTest(kSerializer_Read64f(serializer, &positionEntry.position.z));

                GoTest(kSerializer_Read8u(serializer, &positionEntry.type));

                GoTest(kSerializer_EndRead(serializer));
                needEndRead = false;
            }
            catch (const Go::Exception&)
            {
                if (needEndRead)
                {
                    kSerializer_EndRead(serializer);
                }
                GoRethrow("Failed to deserialize positions.");
            }

            positionSet.push_back(positionEntry);
        }
    }

    void GoGraphics::ReadOnePointSet(kSerializer serializer, PointSet& pointSetEntry, k16u& pointCount)
    {
        bool needEndRead = false;

        try
        {
            GoTest(kSerializer_BeginRead(serializer, kTypeOf(k16u), kTRUE));
            needEndRead = true;

            GoTest(kSerializer_Read32f(serializer, &pointSetEntry.size));
            GoTest(kSerializer_Read32u(serializer, &pointSetEntry.color));
            GoTest(kSerializer_Read32s(serializer, &pointSetEntry.shape));
            GoTest(kSerializer_Read16u(serializer, &pointCount));

            GoTest(kSerializer_EndRead(serializer));
            needEndRead = false;
        }
        catch (const Go::Exception&)
        {
            if (needEndRead)
            {
                kSerializer_EndRead(serializer);
            }
            GoRethrow("Failed to deserialize point set.");
        }
    }

    void GoGraphics::ReadOneLineSet(kSerializer serializer, GoLineSet& lineSetEntry, k16u& pointCount)
    {
        k8u value;
        bool needEndRead = false;

        try
        {
            GoTest(kSerializer_BeginRead(serializer, kTypeOf(k16u), kTRUE));
            needEndRead = true;

            GoTest(kSerializer_Read32f(serializer, &lineSetEntry.width));
            GoTest(kSerializer_Read32u(serializer, &lineSetEntry.color));

            GoTest(kSerializer_Read8u(serializer, &value));
            lineSetEntry.hasStartPointArrow = (value > 0);

            GoTest(kSerializer_Read8u(serializer, &value));
            lineSetEntry.hasEndPointArrow = (value > 0);

            GoTest(kSerializer_Read16u(serializer, &pointCount));

            GoTest(kSerializer_EndRead(serializer));
            needEndRead = false;
        }
        catch (const Go::Exception&)
        {
            if (needEndRead)
            {
                kSerializer_EndRead(serializer);
            }
            GoRethrow("Failed to deserialize line set.");
        }
    }

    void GoGraphics::ReadProfileRegion2d(kSerializer serializer, GoProfileRegion2d& region)
    {
        bool needEndRead = false;
        try
        {
            GoTest(kSerializer_BeginRead(serializer, kTypeOf(k16u), kTRUE));
            needEndRead = true;

            GoTest(kSerializer_Read64f(serializer, &region.x));
            GoTest(kSerializer_Read64f(serializer, &region.z));
            GoTest(kSerializer_Read64f(serializer, &region.width));
            GoTest(kSerializer_Read64f(serializer, &region.height));
            GoTest(kSerializer_Read64f(serializer, &region.angleY));

            GoTest(kSerializer_EndRead(serializer));
            needEndRead = false;
        }
        catch (const Go::Exception&)
        {
            if (needEndRead)
            {
                kSerializer_EndRead(serializer);
            }
            GoRethrow("Failed to deserialize profile region 2d.");
        }
    }

    void GoGraphics::ReadSurfaceRegion2d(kSerializer serializer, GoSurfaceRegion2d& region)
    {
        bool needEndRead = false;

        try
        {
            GoTest(kSerializer_BeginRead(serializer, kTypeOf(k16u), kTRUE));
            needEndRead = true;

            GoTest(kSerializer_Read64f(serializer, &region.x));
            GoTest(kSerializer_Read64f(serializer, &region.y));
            GoTest(kSerializer_Read64f(serializer, &region.width));
            GoTest(kSerializer_Read64f(serializer, &region.length));
            GoTest(kSerializer_Read64f(serializer, &region.angleZ));

            GoTest(kSerializer_EndRead(serializer));
            needEndRead = false;
        }
        catch (const Go::Exception&)
        {
            if (needEndRead)
            {
                kSerializer_EndRead(serializer);
            }
            GoRethrow("Failed to deserialize surface region 2d.");
        }
    }

    void GoGraphics::ReadSurfaceRegion3d(kSerializer serializer, GoRegion3d& region)
    {
        bool needEndRead = false;

        try
        {
            GoTest(kSerializer_BeginRead(serializer, kTypeOf(k16u), kTRUE));
            needEndRead = true;

            GoTest(kSerializer_Read64f(serializer, &region.x));
            GoTest(kSerializer_Read64f(serializer, &region.y));
            GoTest(kSerializer_Read64f(serializer, &region.z));
            GoTest(kSerializer_Read64f(serializer, &region.width));
            GoTest(kSerializer_Read64f(serializer, &region.length));
            GoTest(kSerializer_Read64f(serializer, &region.height));
            GoTest(kSerializer_Read64f(serializer, &region.angleZ));

            GoTest(kSerializer_EndRead(serializer));
            needEndRead = false;
        }
        catch (const Go::Exception&)
        {
            if (needEndRead)
            {
                kSerializer_EndRead(serializer);
            }
            GoRethrow("Failed to deserialize surface region 2d.");
        }
    }
}

