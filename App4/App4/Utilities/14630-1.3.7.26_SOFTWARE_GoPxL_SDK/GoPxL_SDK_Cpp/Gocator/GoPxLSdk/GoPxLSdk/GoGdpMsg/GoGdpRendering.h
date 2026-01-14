/**
 * @file    GoGdpRendering.h
 * @brief   Declares the GoPxLSdk.GdpRendering class.
 * 
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#ifndef GO_PXL_SDK_GOGDPRENDERING_H
#define GO_PXL_SDK_GOGDPRENDERING_H

#include <GoPxLSdk/GoGdpMsg/GoGdpMsg.h>

class GoGdpMsgTests;

namespace GoPxLSdk
{
    typedef struct GoPointSet
    {
        k32f size;
        k32u color;
        k32s shape;
        std::vector<kPoint3d32f> points;

        GoPointSet() = default;
        GoPointSet(k32f size, k32u color, k32s shape, std::vector<kPoint3d32f> points) :
            size(size), color(color), shape(shape), points(points) {}

    } PointSet;

    typedef struct GoLineSet
    {
        k32f width;
        k32u color;
        bool hasStartPointArrow;
        bool hasEndPointArrow;
        std::vector<kPoint3d32f> points;

        GoLineSet() = default;
        GoLineSet(k32f width, k32u color, bool hasStartPointArrow, bool hasEndPointArrow, std::vector<kPoint3d32f> points) :
            width(width), color(color), hasStartPointArrow(hasStartPointArrow), hasEndPointArrow(hasEndPointArrow), points(points) {}

    } GoLineSet;

    enum RegionType : k8u
    {
        Profile_Region_2d = 0,
        Region_3d = 1,
        Surface_Region_2d = 2,  // For future use.
    };

    struct GoRegion
    {
        virtual ~GoRegion()
        {};

        RegionType type;
    };

    struct GoProfileRegion2d : GoRegion
    {
        k64f x;
        k64f z;
        k64f width;
        k64f height;
        k64f angleY;
    };

    struct GoSurfaceRegion2d : GoRegion
    {
        k64f x;
        k64f y;
        k64f width;
        k64f length;
        k64f angleZ;
    };

    struct GoRegion3d : GoRegion
    {
        k64f x;
        k64f y;
        k64f z;
        k64f width;
        k64f length;
        k64f height;
        k64f angleZ;
    };

    struct GoPlane
    {
        k32f distance;
        kPoint3d32f normal;
    };

    struct GoRay
    {
        kPoint3d32f position;
        kPoint3d32f direction;
        k32f width;
        k32u color;
    };

    struct GoLabel
    {
        std::string text;
        kPoint3d64f position;
    };

    struct GoPosition
    {
        kPoint3d64f position;
        k8u type;
    };

    class GoPxLSdkClass GoGraphics
    {
    public:
        /**
        * Constructs GoGraphics.
        *
        * @public                @memberof GoGraphics
        * @version               Introduced in 0.2.1.53
        */
        GoGraphics() = default;
        ~GoGraphics() = default;

        /**
        * Deserialize the graphics object.
        *
        * @public                @memberof GoGraphics
        * @version               Introduced in 0.2.1.53
        * @param serializer      The serializer to read.
        * @throws Go::Exception  if failed to deserialize graphics.
        */
        void Deserialize(kSerializer serializer);

        /**
        * Get the number of point set items in the graphics object.
        *
        * @public                @memberof GoGraphics
        * @version               Introduced in 0.2.1.53
        * @return                the number of point set items.
        */
        const size_t PointSetCount() const;

        /**
        * Get the specified point set item in the point set list.
        *
        * @public                @memberof GoGraphics
        * @version               Introduced in 0.2.1.53
        * @param index           Specify which point set item to return.
        * @return                reference to the point set item.
        */
        const GoPointSet& PointSetAt(k32u index) const;

        /**
        * Get the number of line set items in the graphics object.
        *
        * @public                @memberof GoGraphics
        * @version               Introduced in 0.2.1.53
        * @return                the number of line set items.
        */
        const size_t LineSetCount() const;

        /**
        * Get the specified line set item in the line set list.
        *
        * @public                @memberof GoGraphics
        * @version               Introduced in 0.2.1.53
        * @param index           Specify which line set item to return.
        * @return                reference to the line set item.
        */
        const GoLineSet& LineSetAt(k32u index) const;

        /**
        * Get the number of graphics regions in the graphics object.
        *
        * @public                @memberof GoGraphics
        * @version               Introduced in 0.2.1.53
        * @return                the number of graphics regions.
        */
        const size_t RegionCount() const;

        /**
        * Get the specified graphics region in the region list.
        *
        * @public                @memberof GoGraphics
        * @version               Introduced in 0.2.1.53
        * @param index           Specify which graphics region to return.
        * @return                reference to the graphics region.
        */
        const GoRegion& RegionAt(k32u index) const;

        /**
        * Get the number of planes in the graphics object.
        *
        * @public                @memberof GoGraphics
        * @version               Introduced in 0.2.1.53
        * @return                the number of planes.
        */
        const size_t PlaneCount() const;

        /**
        * Get the specified plane in the planes list.
        *
        * @public                @memberof GoGraphics
        * @version               Introduced in 0.2.1.53
        * @param index           Specify which plane to return.
        * @return                reference to the plane.
        */
        const GoPlane& PlaneAt(k32u index) const;

        /**
        * Get the number of rays in the graphics object.
        *
        * @public                @memberof GoGraphics
        * @version               Introduced in 0.2.1.53
        * @return                the number of rays.
        */
        const size_t RayCount() const;

        /**
        * Get the specified ray in the rays list.
        *
        * @public                @memberof GoGraphics
        * @version               Introduced in 0.2.1.53
        * @param index           Specify which ray to return.
        * @return                reference to the ray.
        */
        const GoRay& RayAt(k32u index) const;

        /**
        * Get the number of labels in the graphics object.
        *
        * @public                @memberof GoGraphics
        * @version               Introduced in 0.2.1.53
        * @return                the number of labels.
        */
        const size_t LabelCount() const;

        /**
        * Get the specified label in the labels list.
        *
        * @public                @memberof GoGraphics
        * @version               Introduced in 0.2.1.53
        * @param index           Specify which label to return.
        * @return                reference to the label.
        */
        const GoLabel& LabelAt(k32u index) const;

        /**
        * Get the number of positions in the graphics object.
        *
        * @public                @memberof GoGraphics
        * @version               Introduced in 0.2.1.53
        * @return                the number of positions.
        */
        const size_t PositionCount() const;

        /**
        * Get the specified position in the positions list.
        *
        * @public                @memberof GoGraphics
        * @version               Introduced in 0.2.1.53
        * @param index           Specify which position to return.
        * @return                reference to the position.
        */
        const GoPosition& PositionAt(k32u index) const;

    private:
        // Helper functions to deserialize the different bytes of graphics types.
        void DeserializePointSets(kSerializer serializer, k16s count);
        void DeserializeLineSets(kSerializer serializer, k16s count);
        void DeserializeRegions(kSerializer serializer, k16s count);
        void DeserializePlanes(kSerializer serializer, k16s count);
        void DeserializeRays(kSerializer serializer, k16s count);
        void DeserializeLabels(kSerializer serializer, k16s count);
        void DeserializePositions(kSerializer serializer, k16s count);

        void ReadOnePointSet(kSerializer serializer, PointSet& pointSetEntry, k16u& pointCount);
        void ReadOneLineSet(kSerializer serializer, GoLineSet& lineSetEntry, k16u& pointCount);

        void ReadProfileRegion2d(kSerializer serializer, GoProfileRegion2d& region);
        void ReadSurfaceRegion2d(kSerializer serializer, GoSurfaceRegion2d& region);
        void ReadSurfaceRegion3d(kSerializer serializer, GoRegion3d& region);

    private:
        std::vector<GoPointSet> pointSet;
        std::vector<GoLineSet> lineSet;
        std::vector<std::shared_ptr<GoRegion>> regionSet;
        std::vector<GoPlane> planeSet;
        std::vector<GoRay> raySet;
        std::vector<GoLabel> labelSet;
        std::vector<GoPosition> positionSet;

        friend class ::GoGdpMsgTests;
    };

    class GoPxLSdkClass GoGdpRendering : public GoGdpMsg
    {
    public:
        /**
        * Constructs GoGdpRendering.
        */
        GoGdpRendering();
        ~GoGdpRendering() = default;

        /**
        * Deserialize rendering msg.
        * @param serializer The serializer to read.
        * @throws Go::Exception if unsuccessful.
        */
        void Deserialize(kSerializer serializer) override;

        /**
        * Get the graphics object in the rendering message.
        * @return the graphics object reference.
        */
        const GoGraphics& GetGraphics() const;

    private:
        GoGraphics graphics;

        kStatus DeserializeGraphic(kSerializer serializer);
        kStatus DeserializePointSets(kSerializer serializer, k16s count);
        kStatus DeserializeLineSets(kSerializer serializer, k16s count);
        kStatus DeserializeRegions(kSerializer serializer, k16s count);
        kStatus DeserializePlanes(kSerializer serializer, k16s count);
        kStatus DeserializeRays(kSerializer serializer, k16s count);
        kStatus DeserializeLabels(kSerializer serializer, k16s count);
        kStatus DeserializePositions(kSerializer serializer, k16s count);

        friend class ::GoGdpMsgTests;
    };
}

#endif
