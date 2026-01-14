/**
* @file     StringConvert.h
* @brief    Declares string conversion functions used for serialization.
*/

#ifndef GOAPI_STRING_CONVERT_H
#define GOAPI_STRING_CONVERT_H

#include <GoApi/Properties/Nodes/TypeInfo.h>
#include <sstream>

namespace Go
{
namespace Properties
{
namespace SerializersInternal
{

// Default implementation
template <typename T, typename = void>
struct StringFormatter
{
    static std::string ToString(const T& val)
    {
        std::stringstream stream;
        stream << val;
        return stream.str();
    }

    static void FromString(const std::string& input, T& val)
    {
        std::stringstream stream(input);
        stream >> val;
    }
};

// For std::string, because stream extractor extracts the first word only.
template <typename T>
struct StringFormatter<T, std::enable_if_t<std::is_same<T, std::string>::value>>
{
    static std::string ToString(const T& val)
    {
        return val;
    }

    static void FromString(const std::string& input, T& val)
    {
        val = input;
    }
};

// For enums
template <typename T>
struct StringFormatter<T, std::enable_if_t<std::is_enum<T>::value>>
{
    static std::string ToString(const T& val)
    {
        return StringFormatter<_intrinsic_t>::ToString(static_cast<_intrinsic_t>(val));
    }

    static void FromString(const std::string& input, T& val)
    {
        _intrinsic_t intrinsicVal;

        StringFormatter<_intrinsic_t>::FromString(input, intrinsicVal);
        val = static_cast<T>(intrinsicVal);
    }

private:
    using _intrinsic_t = typename std::underlying_type<T>::type;
};

// For binary
template <>
struct StringFormatter<ByteVector>
{
    static std::string ToString(const ByteVector& val)
    {
        return std::string((const char*)kArray1_Data(val), kArray1_Count(val));
    }

    static void FromString(const std::string input, ByteVector& val)
    {
        val = ByteVector((void*)input.data(), input.size());
    }
};

}}} //Namespaces

#endif
