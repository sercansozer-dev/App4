/**
 * @file    GoGdpMsgDef.h
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#ifndef GO_PXL_SDK_GOGDPMSGDEF_H
#define GO_PXL_SDK_GOGDPMSGDEF_H

#include <GoPxLSdk/Def.h>
#include <GoPxLSdk/GoJson.h>

namespace GoPxLSdk
{
    const k64f SCALE_FACTOR_MM_TO_NM = 1000000.0; //Convert all units to nanometers from millimeters

    /**
     * List of enums representing types available to be serialized out through GDP
     * This list is essentially the same as GoCore::MessageType.
     *
     * Although the list is similar to PipeDataType in GoPipe::PipeDataType,
     * The key difference is that MessageType represents types to be serialized out.
     * These enums should be kept as close to each other as possible.
     */
    enum struct MessageType : k16u
    {
        //---------------------------------------------------------------------
        // Control types
        //---------------------------------------------------------------------
        SIGNAL                  = 1, /// Signals to clients that data on a stream is invalidated.
        // Reserved for control type expansion.
        //---------------------------------------------------------------------
        // Common data types.
        //---------------------------------------------------------------------
        NULL_TYPE               = 10, /// Null data type that contains a status code. 
        STAMP                   = 11, /// Stamp information.
        UNIFORM_PROFILE         = 12, /// Uniform profile data.
        PROFILE_POINT_CLOUD     = 13, /// Raw profile data.
        UNIFORM_SURFACE         = 14, /// Uniform surface data.
        SURFACE_POINT_CLOUD     = 15, /// Raw surface data.
        IMAGE                   = 16, /// Image data.
        SPOTS                   = 17, /// Spot data associated with the image.
        MESH                    = 18, /// Mesh data.
        MEASUREMENT             = 19, /// Measurement data.
        STRING                  = 20, /// String data.
        // Reserved for common data type expansion.
        //---------------------------------------------------------------------
        // Types containing graphical and feature data.
        //---------------------------------------------------------------------
        RENDERING               = 70, /// Graphical data associated with an output.
        POINT_FEATURE           = 71, /// Point feature data.
        LINE_FEATURE            = 72, /// Line feature data.
        PLANE_FEATURE           = 73, /// Plane feature data.
        CIRCLE_FEATURE          = 74, /// Circle feature data.
        // Reserved for feature data type expansion.
        //---------------------------------------------------------------------
        // Serialization only types. (not a framework type)
        //---------------------------------------------------------------------
        HEALTH                  = 100
    };

}

#endif
