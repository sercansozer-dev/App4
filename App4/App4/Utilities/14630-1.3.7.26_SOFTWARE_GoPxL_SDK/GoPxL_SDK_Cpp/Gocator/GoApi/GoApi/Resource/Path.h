/**@file    Path.h
 * Defines the Path class.
 */

#ifndef GOREST_PATH_H
#define GOREST_PATH_H

#include <GoApi/GoApiDef.h>
#include <string>

namespace GoApi
{

/**
 * Functions and definitions for resource paths.
 */
class GoApiClass Path
{
public:

    /**
     * Creates a path using a path template and a series of keys.
     *
     * Accepts a path in the form given to @ref ResourceRouter::Register, and
     * a series of keys to create a concrete path.
     *
     * For example, the following code produces the path "/tools/ProfilePosition-0/inputs/Profile":
     * @code
     * FormatPath("/tools/:tool/inputs/:input", {"ProfilePosition-0", "Profile"});
     * @endcode
     *
     * It is acceptable to have fewer keys than placeholders in the path; this
     * produces a new template with fewer placeholders. Having more keys than
     * placeholders will result in an exception.
     *
     * @param   pathTemplate        Path with key placeholders.
     * @param   keys                List of keys to apply to pathTemplate.
     *
     * @return                      The result after applying keys to pathTemplate.
     */
    static std::string Format(const std::string& pathTemplate, std::initializer_list<std::string> keys);
};

} // Namespace

#endif
