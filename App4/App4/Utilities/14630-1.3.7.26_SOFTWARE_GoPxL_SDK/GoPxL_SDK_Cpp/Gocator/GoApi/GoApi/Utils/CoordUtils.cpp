#include "CoordUtils.h"

namespace Go
{
//-----------------------------------------------------------------------------
// 3D and 2D coordinates related utility functions.
//-----------------------------------------------------------------------------

kRect64f CoordUtils::Extract2DRect(const kRect3d64f& rect3d)
{
    kRect64f rect; 

    rect.x = rect3d.x;
    rect.y = rect3d.z;
    rect.width = rect3d.width;
    rect.height = rect3d.depth;
    
    return rect;
}

} // Namespaces
