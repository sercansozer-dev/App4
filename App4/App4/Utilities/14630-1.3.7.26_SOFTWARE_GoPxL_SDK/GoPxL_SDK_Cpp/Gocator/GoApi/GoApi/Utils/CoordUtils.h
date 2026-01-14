/**@file    CoordUtils.h
 * 2D and 3D coordinates related utilities and classes. 
 */

#ifndef GOAPI_UTILS_COORDUTILS_H
#define GOAPI_UTILS_COORDUTILS_H

#include <GoApi/GoApiDef.h>
#include <kApi/kApiDef.h>

//! namespace used for all GoApi related classes.
namespace Go
{
    class GoApiClass CoordUtils
    {
    public:
        static kRect64f Extract2DRect(const kRect3d64f& rect3d);
    };

} // Namespace

#endif
