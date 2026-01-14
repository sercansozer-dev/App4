#ifndef GOAPI_UNSIGNEDVALUEMIN_H
#define GOAPI_UNSIGNEDVALUEMIN_H

namespace Go
{
namespace Params
{
    /**
     * GOS-3518: Set minimum of unsigned integers to 0.
     * Must be done via compile-time branching, otherwise compile-time errors accrue due to setting a non-numeric minVal to 0,
     * even if it would never occur in practice.
     */

    // Default
    template <typename T, typename = void>
    struct UnsignedValueMin
    {
        static bool SetUnsignedMin(T& minVal)
        {
            // Do not set minimum in defult case.
            return false;
        }
    };

    // Unsigned integer
    template <typename T>
    struct UnsignedValueMin<T, std::enable_if_t<
        std::is_unsigned<T>::value &&
        !std::is_same<T, bool>::value>> // Boolean is considered unsigned, resulting in minimum = false. Not a huge issue, but unnecessary.
    {
        static bool SetUnsignedMin(T& minVal)
        {
            // Set minimum to 0 if unsigned.
            minVal = 0;
            return true;
        }
    };

}
}

#endif