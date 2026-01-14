/**
* @file    TypeInfo.h
* @brief   Defines type traits used by the properties system.
*
*/

#ifndef GOAPI_PROPERTIES_TYPE_INFO_H
#define GOAPI_PROPERTIES_TYPE_INFO_H

#include <GoApi/GoApi.h>
#include <kApi/Data/kArray1.h>
#include <kApi/Data/kArrayList.h>
#include <array>
#include <vector>
#include <deque>
#include <unordered_set>
#include <list>
#include <string>
#include <array>
#include <set>
#include <map>
#include <unordered_map>
#include <forward_list>

namespace Go
{
namespace Properties
{

// Type to define a binary type. Serializers will serialize this into binary type.
// std::vector cannot be type-cast to ByteVector; the copy constructor can be used instead.
class GoApiClass ByteVector
{
public:
    /**
     * Constructs an empty ByteVector.
     */
    ByteVector();

    /**
     * Constructs a ByteVector and copies data from std::vector<kByte>.
     * Warning! This will create multiple copies in memory. 
     * Use another API with kArray1 as the source data type to avoid any memory copy.
     * 
     * @param other                             The source std::vector to copy the data from.
     */
    ByteVector(const std::vector<kByte>& other);

    /**
     * Constructs a ByteVector and copies data from other source.
     * Warning! This will create multiple copies in memory.
     * Use another API with kArray1 as the source data type to avoid any memory copy.
     *
     * @param ptr                               The source data.
     * @param size                              The source data size.
     */
    ByteVector(void* ptr, kSize size);

    /**
     * Constructs a ByteVector. If input object is kArray1 it's shared, if input object is kArrayList, data is copied.
     * Warning! This may create multiple copies in memory.
     * Use kArray1 as the source data type to avoid any memory copy.
     * 
     * @param bytes                             The source kArray1 or kArrayList object.
     */
    ByteVector(kObject bytes);

    /**
     * Constructs a ByteVector. If input object is kArray1 it's shared, if input object is kArrayList, data is copied.
     * Warning! This may create multiple copies in memory.
     * Use const Go::Object<kArray1>& as the source data type to avoid any memory copy.
     * 
     * @param bytes                             The source kArray1 or kArrayList object.
     */
    ByteVector(const Go::Object<kObject>& bytes);

    /**
     * ByteVector destructor.
     */
    ~ByteVector();

    /**
     * Resizes ByteVector.
     * 
     * @param length                            New byte vector length.
     */
    void Resize(kSize length);

    /**
     * Counts bytes in ByteVector.
     *
     * @return                                  Number of bytes in the ByteVector.
    */
    kSize Count() const;

    /**
     * Returns raw pointer to bytes.
     *
     * @return                                  Pointer to bytes.
    */
    void* Data();
   
    /**
     * Returns raw pointer to bytes.
     *
     * @return                                  Pointer to bytes.
    */
    const void* Data() const;

    /**
     * Casts ByteVector to Go::Object<kArray1>.
     *
     * @return                                  Go::Object.
    */
    operator Go::Object<kObject>() const;

    /**
     * Casts ByteVector to kArray1.
     *
     * @return                                  kArray1.
    */
    operator kArray1() const;

    /**
     * Casts ByteVector to std::vector<kByte>. ByteVector data is copied into new std::vector.
     *
     * @return                                  std::vector<kByte>.
    */
    operator std::vector<kByte>() const;
    
    /**
     * Assigns Go::Object to ByteVector.
     * Warning! This may create multiple copies in memory.
     * Use const Go::Object<kArray1>& as the source data type to avoid any memory copy.
     * 
     * @param other                             The source of Go::Object object.
     * 
     * @return                                  std::vector<kByte>.
    */
    ByteVector& operator=(const Go::Object<kObject>& other);

    /**
     * Compares two byte vectors.
     *
     * @param other                             Other ByteVector to compare.
     * @return                                  True if ByteVectors are equal, false otherwise.
    */
    bool operator==(const ByteVector& other) const;

    /**
     * Compares two byte vectors.
     *
     * @param other                             Other ByteVector to compare.
     * @return                                  True if ByteVectors are not equal, false otherwise.
    */
    bool operator!=(const ByteVector& other) const;

    /**
     * Compares two byte vectors.
     *
     * @param other                             Other ByteVector to compare.
     * @return                                  True if ByteVector is lesser than another, false otherwise.
    */
    bool operator<(const ByteVector& other) const;

    /**
     * Compares two byte vectors.
     *
     * @param other                             Other ByteVector to compare.
     * @return                                  True if ByteVector is greater than another, false otherwise.
    */
    bool operator>(const ByteVector& other) const;

    /**
     * Compares two byte vectors.
     *
     * @param other                             Other ByteVector to compare.
     * @return                                  True if ByteVector is lesser or equal than another, false otherwise.
    */
    bool operator<=(const ByteVector& other) const;

    /**
     * Compares two byte vectors.
     *
     * @param other                             Other ByteVector to compare.
     * @return                                  True if ByteVector is greater or equal than another, false otherwise.
    */
    bool operator>=(const ByteVector& other) const;

private:
    Go::Object<kArray1> data;
};

namespace Internal
{

// IsTypeArray
template<typename T, typename = void>
struct IsTypeArray
{
    static constexpr bool value = false;
};

template<typename T, size_t SIZE>
struct IsTypeArray<std::array<T, SIZE>>
{
    static constexpr bool value = true;
};

// There are more generic ways of doing this. But this should suffice for our purposes.
#define DECLARE_TYPE_AS_ARRAY(TYPE)             \
template<typename T>                            \
struct IsTypeArray<TYPE<T>>                     \
{                                               \
    static constexpr bool value = true;         \
};

DECLARE_TYPE_AS_ARRAY(std::vector)
DECLARE_TYPE_AS_ARRAY(std::deque)
DECLARE_TYPE_AS_ARRAY(std::forward_list)
DECLARE_TYPE_AS_ARRAY(std::list)
DECLARE_TYPE_AS_ARRAY(std::set)
DECLARE_TYPE_AS_ARRAY(std::multiset)
DECLARE_TYPE_AS_ARRAY(std::unordered_multiset)
DECLARE_TYPE_AS_ARRAY(std::unordered_set)

// IsTypeCArray
template<typename T, typename = void>
struct IsTypeCArray
{
    static constexpr bool value = false;
};

template<typename T, size_t SIZE>
struct IsTypeCArray<std::array<T, SIZE>>
{
    static constexpr bool value = true;
};

// IsTypeObject
template<typename T, typename = void>
struct IsTypeObject
{
    static constexpr bool value = false;
};

#define DECLARE_TYPE_AS_OBJECT(TYPE)            \
template<typename... T>                         \
struct IsTypeObject<TYPE<T...>>                 \
{                                               \
    static constexpr bool value = true;         \
};

DECLARE_TYPE_AS_OBJECT(std::map)
DECLARE_TYPE_AS_OBJECT(std::unordered_map)
DECLARE_TYPE_AS_OBJECT(std::multimap)
DECLARE_TYPE_AS_OBJECT(std::unordered_multimap)

// TypeValueName
template<typename T, typename = void>
struct TypeValueName
{
    static constexpr const char* value = "unknown";
};

template<typename T>
struct TypeValueName<T, std::enable_if_t<std::is_integral<T>::value && !std::is_same<T, bool>::value>>
{
    static constexpr const char* value = "integer";
};

template<typename T>
struct TypeValueName<T, std::enable_if_t<std::is_enum<T>::value>>
{
    static constexpr const char* value = "integer";
};

template<typename T>
struct TypeValueName<T, std::enable_if_t<std::is_floating_point<T>::value>>
{
    static constexpr const char* value = "number";
};

template<typename T>
struct TypeValueName<T, std::enable_if_t<std::is_same<T, bool>::value>>
{
    static constexpr const char* value = "boolean";
};

template<typename T>
struct TypeValueName<T, std::enable_if_t<std::is_same<T, std::string>::value>>
{
    static constexpr const char* value = "string";
};

template<typename T>
struct TypeValueName<T, std::enable_if_t<std::is_same<T, ByteVector>::value>>
{
    static constexpr const char* value = "binary";
};

template<typename T>
struct TypeValueName<T, std::enable_if_t<IsTypeArray<T>::value>>
{
    static constexpr const char* value = "array";
};

template<typename T>
struct TypeValueName<T, std::enable_if_t<IsTypeObject<T>::value>>
{
    static constexpr const char* value = "object";
};

// ArrayItemType
template<typename T, typename = void>
struct ArrayItemType
{
    using type = void;
};

template<typename T>
struct ArrayItemType<T, std::enable_if_t<IsTypeArray<T>::value>>
{
    using type = typename T::value_type;
};

}}} //Namespaces

#endif
