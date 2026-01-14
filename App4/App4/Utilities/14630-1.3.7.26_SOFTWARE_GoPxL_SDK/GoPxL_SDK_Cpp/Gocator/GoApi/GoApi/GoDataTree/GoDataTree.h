/**@file    GoDataTree.h
 * Defines the GoDataTree class.
 */

#ifndef GOAPI_GODATATREE_H
#define GOAPI_GODATATREE_H

#include <GoApi/GoApi.h>
#include <GoApi/GoApiDef.h>
#include <kApi/Data/kArray1.h>
#include <kApi/Data/kDataTree.h>
#include <kApi/kValue.h>

kDeclareValueEx(GoApi, GoDataTreeArray, kValue)
kDeclareValueEx(GoApi, GoDataTreeObject, kValue)

namespace Go
{
namespace Properties
{
    class GoDataTreeValueWriter;
    class GoDataTreeSerializerImpl;
}
}

namespace GoApi
{

using GoMsgPackFormat = std::vector<kByte>;
using GoJsonFormat = std::string;

class GoDataTreeIterator;

/* kDataTree wrapper class. */
class GoApiClass GoDataTree
{

public:
    static constexpr const char* GO_DATA_TREE_NAME_EMPTY = "";

public:
    /**
     * Constructs GoDataTree object.
     * 
     * @param allocator                         kAlloc object. Defaults to "kAlloc_App()".
     */
    GoDataTree(kAlloc allocator = kAlloc_App());

    /**
     * Copy constructor. It only shares the underlying kDataTree object using "kObject_Share()" API. 
     * It does NOT make a copy of underlying kDataTree object. To make a copy, use explicit "GoDataTree::Clone()" method.
     * 
     * @param dataTree                          GoDataTree object.
     */
    GoDataTree(const GoDataTree& dataTree);

    /**
     * Destructs GoDataTree object.
     */
    ~GoDataTree();

    /**
     * Clones GoDataTree.
     * 
     * @return                                  Clone of GoDataTree object.
     */
    GoDataTree Clone();

    /**
     * Returns GoDataTree item at specified index. If item at index does not exist, it will be created.
     * 
     * @param index                             Item index.
     * @return                                  GoDataTree object.
     * 
     * @throw kERROR_STATE if GoDataTree is not an array.
     * @throw kERROR_NOT_FOUND if index is out of range.
     */
    GoDataTree At(k32u index) const;

    /**
     * Pushes a value into the GoDataTree array.
     * 
     * @param value                             T.
     * @throws kERROR_STATE if GoDataTree is not an array.
     */
    template <typename T>
    void PushBack(const T& value);

    /**
     * Resizes GoDataTree array. If size < Count(), items will be removed. If size > Count(), empty items will be created.
     * 
     * @param size                              New array size.
     */
    void Resize(k64u size);

    /**
     * Sets GoDataTree item value.
     * 
     * @param value                             T.
     * 
     * @throw kERROR_UNIMPLEMENTED if T is not supported.
     */
    template <typename T>
    void Set(const T& value);

    /**
     * Sets GoDataTree child item value.
     * 
     * @param key                               Child item name.
     * @param value                             T.
     * 
     * @throw kERROR_UNIMPLEMENTED if T is not supported.
     */
    template <typename T>
    void SetChild(const std::string& key, const T& value);

    /**
     * Sets GoDataTree item binary array.
     * 
     * @param bytes                             Vector of kBytes.
     */
    void SetBinary(const std::vector<kByte>& bytes);

    /**
     * Sets GoDataTree item binary array.
     * 
     * @param bytes                             kArray1 of kBytes.
     * 
     * @throw kERROR_PARAMETER if kArray1 data type is not kByte.
     */
    void SetBinary(kArray1 bytes);

    /**
     * Gets GoDataTree item binary array.
     * 
     * @return                                  kArray1 of kBytes.
     * 
     * @throw kERROR_PARAMETER if data tree item is not kArray1 or if kArray1 data type is not kByte.
     */
    Go::Object<kArray1> GetBinary() const;

    /**
     * Gets GoDataTree item value as T.
     * 
     * @return                                  Stored value as T.
     * 
     * @throw kERROR_PARAMETER if T is not stored item type.
     */
    template <typename T>
    T Get() const;

    /**
     * Gets GoDataTree item value as T or returns the default value.
     *
     * @param defaultValue                      Default value to return if child item does not exist.
     * @return                                  T value.
     */
    template <typename T>
    T Value(const T& defaultValue);

    /**
     * Gets GoDataTree child item value as T.
     * 
     * @param key                               Child item name.
     * @return                                  Stored value as T.
     * 
     * @throw kERROR_PARAMETER if T is not stored child item type.
     */
    template <typename T>
    T GetChild(const std::string& key) const;

    /**
     * Gets GoDataTree child item value as T or returns the default value.
     *
     * @param key                               Child item name.
     * @param defaultValue                      Default value to return if child item does not exist.
     * @return                                  T value.
     */
    template <typename T>
    T Value(const std::string& key, const T& defaultValue);

    /**
     * Removes GoDataTree child item.
     * 
     * @param key                               Child item name.
     */
    void Remove(const std::string& key);

    /**
     * Removes GoDataTree child item at index.
     * 
     * @param index                             Child item index.
     */
    void Remove(k32u index);

    /**
     * Checks if GoDataTree child item exists.
     * 
     * @param key                               Child item name.
     * @return                                  True if child item exists, false otherwise.
     */
    bool Contains(const std::string& key) const;

    /**
     * Resets the GoDataTree to other value.
     * 
     * @param other                             Tree to assign.
     */
    void Reset(const GoDataTree& other);

    /**
     * Clears GoDataTree object.
     */
    void Clear();

    /**
     * Gets the number of GoDataTree items.
     * 
     * @return                                  Number of tree items.
     */
    kSize Count() const;

    /**
     * Checks if GoDataTree is empty (Count() == 0).
     *
     * @return                                  True if empty, false otherwise.
     */
    bool IsEmpty() const;

    /**
     * Gets the type of GoDataTree item.
     * 
     * @return                                  Type of the item.
     */
    kType Type() const;

    /**
     * Gets the type of GoDataTree child item.
     * 
     * @param key                               Child item name.
     * @return                                  Type of the child item.
     */
    kType ChildType(const std::string& key)  const;

    /**
     * Checks if GoDataTree item is a root node.
     * 
     * @return                                  True if GoDataTree item is a root node, false otherwise.
     */
    bool IsRoot() const;

    /**
     * Checks if GoDataTree item is null (there's no value assigned).
     * 
     * @return                                  True if GoDataTree item is null, false otherwise.
     */
    bool IsNull() const;

    /**
     * Checks if GoDataTree item is an object (contains a named children).
     * 
     * @return                                  True if GoDataTree item is an object, false otherwise.
     */
    bool IsObject() const;

    /**
     * Checks if GoDataTree item is an array. GoDataTree array represents a list of objects of any type except binary.
     *
     * @return                                  True if GoDataTree item is an array, false otherwise.
     */
    bool IsArray() const;

    /**
     * Checks if GoDataTree item is an kArray1 holding kBytes.
     *
     * @return                                  True if GoDataTree item is a kArray1 holding kBytes, false otherwise.
     */
    bool IsBinary() const;

    /**
     * Checks if GoDataTree item is a number (k8u or k8s or k16u or k16s or k32u or k32s or k32f or k64u or k64s or k64f).
     *
     * @return                                  True if GoDataTree item is a number, false otherwise.
     */
    bool IsNumber() const;

    /**
     * Checks if GoDataTree item is a negative number (value < 0).
     *
     * @return                                  True if GoDataTree item is a negative number, false otherwise.
     */
    bool IsNegativeNumber() const;
    
    /**
     * Checks if GoDataTree item is k8u.
     *
     * @return                                  True if GoDataTree item is k8u, false otherwise.
     */
    bool Is8u() const;

    /**
     * Checks if GoDataTree item is k8s.
     *
     * @return                                  True if GoDataTree item is k8s, false otherwise.
     */
    bool Is8s() const;

    /**
     * Checks if GoDataTree item is k16u.
     *
     * @return                                  True if GoDataTree item is k16u, false otherwise.
     */
    bool Is16u() const;

    /**
     * Checks if GoDataTree item is k16s.
     *
     * @return                                  True if GoDataTree item is k16s, false otherwise.
     */
    bool Is16s() const;
    
    /**
     * Checks if GoDataTree item is k32u.
     *
     * @return                                  True if GoDataTree item is k32u, false otherwise.
     */
    bool Is32u() const;
    
    /**
     * Checks if GoDataTree item is k32s.
     *
     * @return                                  True if GoDataTree item is k32s, false otherwise.
     */
    bool Is32s() const;
    
    /**
     * Checks if GoDataTree item is k32f.
     *
     * @return                                  True if GoDataTree item is k32f, false otherwise.
     */
    bool Is32f() const;
    
    /**
     * Checks if GoDataTree item is k64u.
     *
     * @return                                  True if GoDataTree item is k64u, false otherwise.
     */
    bool Is64u() const;
    
    /**
     * Checks if GoDataTree item is k64s.
     *
     * @return                                  True if GoDataTree item is k64s, false otherwise.
     */
    bool Is64s() const;
    
    /**
     * Checks if GoDataTree item is k64f.
     *
     * @return                                  True if GoDataTree item is k64f, false otherwise.
     */
    bool Is64f() const;
    
    /**
     * Checks if GoDataTree item is boolean.
     *
     * @return                                  True if GoDataTree item is boolean, false otherwise.
     */
    bool IsBoolean() const;
    
    /**
     * Checks if GoDataTree item is string.
     *
     * @return                                  True if GoDataTree item is string, false otherwise.
     */
    bool IsString() const;

    /**
     * Gets the GoDataTree item name.
     *
     * @return                                  GoDataTree item name.
     */
    const char* Name() const;

    /**
     * Gets the GoDataTree iterator.
     * 
     * @return                                  GoDataTreeIterator object.
     */
    GoDataTreeIterator Iterator() const;

    /**
     * Searches for the key in GoDataTree.
     * 
     * @return                                  GoDataTreeIterator object.
     */
    GoDataTreeIterator Find(const std::string& key) const;

    /**
     * Gets the GoDataTree allocator.
     *
     * @return                                  kAlloc object.
     */
    kAlloc Allocator() const;

    /**
     * Deserializes GoDataTree to JSON data format and returns it as a string.
     * 
     * @param prettyPrint                       (optional) Whether to pretty print JSON output.
     * @return                                  GoDataTree as string.
     */
    std::string Dump(bool prettyPrint = true) const;

    /**
     * Gets the GoDataTree child item. If requested child is not found, new child item will be created.
     *
     * @param key                               Child item name.
     * @return                                  GoDataTree item.
     * 
     * @throw kERROR_STATE if GoDataTree is not an object.
     */
    GoDataTree operator [] (const std::string& key);

    /**
     * Gets the GoDataTree child item.
     *
     * @param key                               Child item name.
     * @return                                  GoDataTree item.
     * 
     * @throw kERROR_STATE if GoDataTree is not an object.
     * @throw kERROR_PARAMETER if child item is not found.
     */
    GoDataTree operator [] (const std::string& key) const;

    /**
     * Gets the GoDataTree child item at index. If index is out of range, new child items will be created.
     *
     * @param index                             Child item index.
     * @return                                  GoDataTree item.
     * 
     * @throw kERROR_STATE if GoDataTree is not an array.
     */
    GoDataTree operator [] (k32u index);

    /**
     * Gets the GoDataTree child item at index.
     *
     * @param index                             Child item index.
     * @return                                  GoDataTree item.
     *      
     * @throw kERROR_STATE if GoDataTree is not an array.
     * @throw kERROR_PARAMETER if index is out of range.
     */
    GoDataTree operator [] (k32u index) const;

    /**
     * Assigns the GoDataTree object.
     * Currently unsupported.
     *
     * @param other                             GoDataTree object to assign.
     * 
     * @throw kERROR_PARAMETER                  If other data tree is NULL.
     */
    GoDataTree& operator=(const GoDataTree& other);

    /**
     * Assigns the value to GoDataTree item.
     * 
     * @param value                             T.
     * 
     * @throw kERROR_UNIMPLEMENTED              If T is not supported.
     */
    template <typename T>
    void operator = (T value);

public:
    /**
     * Returns GoDataTree array.
     * 
     * @return                                  GoDataTree array.
     */
    static GoDataTree Array();

    /**
     * Returns GoDataTree object.
     *
     * @return                                  GoDataTree object.
     */
    static GoDataTree Object();

    /**
     * Serializes GoDataTree into message pack.
     *
     * @param tree                              GoDataTree to serialize.
     * @param out                               GoMsgPackFormat output.
     */
    static void ToMsgPack(const GoDataTree& tree, GoMsgPackFormat& out);

    /**
     * Serializes GoDataTree into message pack stream.
     *
     * @param tree                              GoDataTree to serialize.
     * @param out                               kStream output or kSerializer to write into.
     */
    static void ToMsgPack(const GoDataTree& tree, kObject out);

    /**
     * Calculates GoDataTree size in message pack format.
     *
     * @param tree                              GoDataTree input.
     * @return                                  Message pack size.
     */
    static kSize CalculateMsgPackSize(const GoDataTree& tree);

    /**
     * Serializes GoDataTree into JSON.
     *
     * @param tree                              GoDataTree to serialize.
     * @param out                               GoJsonFormat output.
     * @param prettyPrint                       (optional) Whether to pretty print JSON output.
     */
    static void ToJson(const GoDataTree& tree, GoJsonFormat& out, bool prettyPrint = true);

    /**
     * Serializes GoDataTree into JSON.
     *
     * @param tree                              GoDataTree to serialize.
     * @param out                               kStream output or kSerializer to write into.
     * @param prettyPrint                       (optional) Whether to pretty print JSON output.
     */
    static void ToJson(const GoDataTree& tree, kObject out, bool prettyPrint = true);

    /**
     * Calculates GoDataTree size in json format.
     *
     * @param tree                              GoDataTree input.
     * @param prettyPrint                       (optional) Whether to calculate the size with "pretty print" characters.
     * @return                                  JSON size.
     */
    static kSize CalculateJsonSize(const GoDataTree& tree, bool prettyPrint = true);

    /**
     * Deserializes message pack into GoDataTree.
     * 
     * @param msgPack                           GoMsgPackFormat message pack.
     * @param output                            GoDataTree object.
     */
    static void FromMsgPack(const GoMsgPackFormat& msgPack, GoDataTree& output);

    /**
     * Deserializes message pack stream into GoDataTree.
     *
     * @param stream                            Message pack stream.
     * @param size                              Message pack size.
     * @param output                            GoDataTree object.
     */
    static void FromMsgPack(kStream stream, kSize size, GoDataTree& output);

    /**
     * Deserializes JSON into GoDataTree.
     *
     * @param json                              GoJsonFormat JSON.
     * @param output                            GoDataTree object.
     */
    static void FromJson(const GoJsonFormat& json, GoDataTree& output);

    /**
     * Deserializes JSON into GoDataTree.
     *
     * @param json                              std::vector<kByte> JSON.
     * @param output                            GoDataTree object.
     */
    static void FromJson(const std::vector<kByte>& json, GoDataTree& output);

    /**
     * Deserializes JSON stream into GoDataTree.
     *
     * @param stream                            JSON stream.
     * @param size                              Message pack size.
     * @param output                            GoDataTree object.
     */
    static void FromJson(kStream stream, kSize size, GoDataTree& output);

private:
    kType Type(kDataTreeItem item) const;

    void SetNull();
    void SetNull(const std::string& key);

    void Set8u(const k8u& value);
    void Set8u(const std::string& key, const k8u& value);
    void Set8s(const k8s& value);
    void Set8s(const std::string& key, const k8s& value);
    void Set16u(const k16u& value);
    void Set16u(const std::string& key, const k16u& value);
    void Set16s(const k16s& value);
    void Set16s(const std::string& key, const k16s& value);
    void Set32u(const k32u& value);
    void Set32u(const std::string& key, const k32u& value);
    void Set32s(const k32s& value);
    void Set32s(const std::string& key, const k32s& value);
    void Set32f(const k32f& value);
    void Set32f(const std::string& key, const k32f& value);
    void Set64u(const k64u& value);
    void Set64u(const std::string& key, const k64u& value);
    void Set64s(const k64s& value);
    void Set64s(const std::string& key, const k64s& value);
    void Set64f(const k64f& value);
    void Set64f(const std::string& key, const k64f& value);
    void SetBoolean(const bool& value);
    void SetBoolean(const std::string& key, const bool& value);
    void SetString(const std::string& value);
    void SetString(const std::string& key, const std::string& value);
    void SetArray(const kArray1& value);
    void SetArray(const std::string& key, const kArray1& value);
    void SetItem(const GoDataTree& value);
    void SetChild(const std::string& key, const GoDataTree& value);

    k8u Get8u() const;
    k8u Get8u(const std::string& key) const;
    k8s Get8s() const;
    k8s Get8s(const std::string& key) const;
    k16u Get16u() const;
    k16u Get16u(const std::string& key) const;
    k16s Get16s() const;
    k16s Get16s(const std::string& key) const;
    k32u Get32u() const;
    k32u Get32u(const std::string& key) const;
    k32s Get32s() const;
    k32s Get32s(const std::string& key) const;
    k32f Get32f() const;
    k32f Get32f(const std::string& key) const;
    k64u Get64u() const;
    k64u Get64u(const std::string& key) const;
    k64s Get64s() const;
    k64s Get64s(const std::string& key) const;
    k64f Get64f() const;
    k64f Get64f(const std::string& key) const;
    bool GetBoolean() const;
    bool GetBoolean(const std::string& key) const;
    std::string GetString() const;
    std::string GetString(const std::string& key) const;
    kArray1 GetArray() const;
    kArray1 GetArray(const std::string& key) const;
    GoDataTree GetChild(const std::string& key) const;

    void MarkAsArray();
    void MarkAsObject();
    void MarkAsObject(kDataTreeItem item);

    kDataTreeItem GetChildItem(const std::string& key) const;
    kDataTreeItem EnsureChildExists(k32u index) const;
    kDataTreeItem Find(const std::string& key, kDataTreeItem item) const;

    /**
     * Returns numeric value and casts it to specified type.
     */
    template <typename T>
    T GetNumericValue() const;

    template <typename T>
    T GetNumericValue(const std::string& key) const;

    /**
     * Special version of kObject_Clone. It uses kObject_Share for binary objects (kArray1) to avoid additional copies.
     */
    void CloneEx(kDataTreeItem* object, kDataTreeItem source);

    void* GetItemNameValuePair(const kChar** name, kType* type);

private:
    // Main kDataTree object.
    kDataTreeItem tree;

    /// kDataTreeItem object. 
    /// It's used as a reference point to an "active" item within the tree.
    /// For example:
    /// GoDataTree x;                        -> item points to the tree root.
    /// GoDataTree y = x["example"];         -> item points to "example" child within the "x" tree.
    kDataTreeItem item;

    friend class GoMsgPackSerializer;
    friend class GoJsonSerializer;
    friend class GoDataTreeIterator;
    friend class Go::Properties::GoDataTreeValueWriter;
    friend class Go::Properties::GoDataTreeSerializerImpl;
};

/* GoDataTree iterator */
class GoApiClass GoDataTreeIterator
{
public:
    /**
     * Constructs GoDataTree iterator.
     * 
     * @param tree                              GoDataTree object to construct an iterator for.
     */
    explicit GoDataTreeIterator(const GoDataTree& tree);

    /**
     * Returns the next sibling item iterator.
     * 
     * @return                                  Next sibling iterator.
     */
    GoDataTreeIterator NextSibling();

    /**
     * Returns the previous sibling item iterator.
     *
     * @return                                  Previous sibling iterator.
     */
    GoDataTreeIterator PreviousSibling();

    /**
     * Check if current item has any sibling.
     *
     * @return                                  True if sibling exists, false otherwise.
     */
    bool HasSibling();

    /**
     * Returns the first child item iterator.
     *
     * @return                                  First child iterator.
     */
    GoDataTreeIterator FirstChild();

    /**
     * Returns the last child item iterator.
     *
     * @return                                  Last child iterator.
     */
    GoDataTreeIterator LastChild();

    /**
     * Checks if current item has any children.
     *
     * @return                                  True if child exists, false otherwise.
     */
    bool HasChild();

    /**
     * Returns the parent item iterator.
     *
     * @return                                  Parent item iterator.
     */
    GoDataTreeIterator Parent();

    /**
     * Returns GoDataTree object iterator points to.
     *
     * @return                                  GoDataTree object.
     */
    GoDataTree operator*();

    /**
     * Assigns the iterator.
     * 
     * @param other                             GoDataTreeIterator object.
     */
    GoDataTreeIterator& operator=(const GoDataTreeIterator& other);

    /**
     * Checks if current iterator points to valid GoDataTree item.
     */
    operator bool() const;

private:
    GoDataTree dataTree;
};

template<typename T>
inline void GoDataTree::PushBack(const T& value)
{
    (*this)[(k32u)Count()] = value;
}

template <typename T>
inline T GoDataTree::Value(const T& defaultValue)
{
    try
    {
        return Get<T>();
    }
    catch (const std::exception&)
    {
        return defaultValue;
    }
}

template <typename T>
inline T GoDataTree::Value(const std::string& key, const T& defaultValue)
{
    try
    {
        return GetChild<T>(key);
    }
    catch (const std::exception&)
    {
        return defaultValue;
    }
}

template<typename T>
inline void GoDataTree::operator = (T value)
{
    Set(value);
}

template<typename T>
inline T GoDataTree::GetNumericValue() const
{
    T value = 0;
    kType itemType = Type();

    if (kType_Is(itemType, kTypeOf(k8u)))
    {
        value = (T)Get8u();
    }
    else if (kType_Is(itemType, kTypeOf(k8s)))
    {
        value = (T)Get8s();
    }
    else if (kType_Is(itemType, kTypeOf(k16u)))
    {
        value = (T)Get16u();
    }
    else if (kType_Is(itemType, kTypeOf(k16s)))
    {
        value = (T)Get16s();
    }
    else if (kType_Is(itemType, kTypeOf(k32u)))
    {
        value = (T)Get32u();
    }
    else if (kType_Is(itemType, kTypeOf(k32s)))
    {
        value = (T)Get32s();
    }
    else if (kType_Is(itemType, kTypeOf(k32f)))
    {
        value = (T)Get32f();
    }
    else if (kType_Is(itemType, kTypeOf(k64u)))
    {
        value = (T)Get64u();
    }
    else if (kType_Is(itemType, kTypeOf(k64s)))
    {
        value = (T)Get64s();
    }
    else if (kType_Is(itemType, kTypeOf(k64f)))
    {
        value = (T)Get64f();
    }
    else
    {
        GoThrow(kERROR_PARAMETER);
    }

    return value;
}

template<typename T>
inline T GoDataTree::GetNumericValue(const std::string& key) const
{
    T value = 0;
    kType itemType = ChildType(key);

    if (kType_Is(itemType, kTypeOf(k8u)))
    {
        value = (T)Get8u(key);
    }
    else if (kType_Is(itemType, kTypeOf(k8s)))
    {
        value = (T)Get8s(key);
    }
    else if (kType_Is(itemType, kTypeOf(k16u)))
    {
        value = (T)Get16u(key);
    }
    else if (kType_Is(itemType, kTypeOf(k16s)))
    {
        value = (T)Get16s(key);
    }
    else if (kType_Is(itemType, kTypeOf(k32u)))
    {
        value = (T)Get32u(key);
    }
    else if (kType_Is(itemType, kTypeOf(k32s)))
    {
        value = (T)Get32s(key);
    }
    else if (kType_Is(itemType, kTypeOf(k32f)))
    {
        value = (T)Get32f(key);
    }
    else if (kType_Is(itemType, kTypeOf(k64u)))
    {
        value = (T)Get64u(key);
    }
    else if (kType_Is(itemType, kTypeOf(k64s)))
    {
        value = (T)Get64s(key);
    }
    else if (kType_Is(itemType, kTypeOf(k64f)))
    {
        value = (T)Get64f(key);
    }
    else
    {
        GoThrow(kERROR_PARAMETER);
    }

    return value;
}

template <>
inline void GoDataTree::Set(const std::nullptr_t&)
{
    SetNull();
}

template <>
inline void GoDataTree::SetChild(const std::string& key, const std::nullptr_t&)
{
    SetNull(key);
}

template <>
inline void GoDataTree::Set(const k8u& value)
{
    Set8u(value);
}

template <>
inline void GoDataTree::SetChild(const std::string& key, const k8u& value)
{
    Set8u(key, value);
}

template <>
inline void GoDataTree::Set(const k8s& value)
{
    Set8s(value);
}

template <>
inline void GoDataTree::SetChild(const std::string& key, const k8s& value)
{
    Set8s(key, value);
}

template <>
inline void GoDataTree::Set(const k16u& value)
{
    Set16u(value);
}

template <>
inline void GoDataTree::SetChild(const std::string& key, const k16u& value)
{
    Set16u(key, value);
}

template <>
inline void GoDataTree::Set(const k16s& value)
{
    Set16s(value);
}

template <>
inline void GoDataTree::SetChild(const std::string& key, const k16s& value)
{
    Set16s(key, value);
}

template <>
inline void GoDataTree::Set(const k32u& value)
{
    Set32u(value);
}

template <>
inline void GoDataTree::SetChild(const std::string& key, const k32u& value)
{
    Set32u(key, value);
}

template <>
inline void GoDataTree::Set(const k32s& value)
{
    Set32s(value);
}

template <>
inline void GoDataTree::SetChild(const std::string& key, const k32s& value)
{
    Set32s(key, value);
}

template <>
inline void GoDataTree::Set(const k32f& value)
{
    Set32f(value);
}

template <>
inline void GoDataTree::SetChild(const std::string& key, const k32f& value)
{
    Set32f(key, value);
}

template <>
inline void GoDataTree::Set(const k64u& value)
{
    Set64u(value);
}

template <>
inline void GoDataTree::SetChild(const std::string& key, const k64u& value)
{
    Set64u(key, value);
}

template <>
inline void GoDataTree::Set(const k64s& value)
{
    Set64s(value);
}

template <>
inline void GoDataTree::SetChild(const std::string& key, const k64s& value)
{
    Set64s(key, value);
}

template <>
inline void GoDataTree::Set(const k64f& value)
{
    Set64f(value);
}

template <>
inline void GoDataTree::SetChild(const std::string& key, const k64f& value)
{
    Set64f(key, value);
}

template <>
inline void GoDataTree::Set(const bool& value)
{
    SetBoolean(value);
}

template <>
inline void GoDataTree::SetChild(const std::string& key, const bool& value)
{
    SetBoolean(key, value);
}

template <>
inline void GoDataTree::Set(const std::string& value)
{
    SetString(value);
}

template <>
inline void GoDataTree::SetChild(const std::string& key, const std::string& value)
{
    SetString(key, value);
}

template <>
inline void GoDataTree::Set(const kArray1& value)
{
    SetArray(value);
}

template <>
inline void GoDataTree::SetChild(const std::string& key, const kArray1& value)
{
    SetArray(key, value);
}

template <>
inline void GoDataTree::Set(const GoDataTree& value)
{
    SetItem(value);
}

template <>
inline void GoDataTree::SetChild(const std::string& key, const GoDataTree& value)
{
    SetChild(key, value);
}

template <>
inline void GoDataTree::Set(const std::vector<kByte>& value)
{
    SetBinary(value);
}

template <>
inline void GoDataTree::SetChild(const std::string& key, const std::vector<kByte>& value)
{
    (*this)[key].SetBinary(value);
}

template <>
inline k8u GoDataTree::Get() const
{
    try
    {
        return Get8u();
    }
    catch (const std::exception&)
    {
        // Cast number to type. Throws kERROR_PARAMETER if value is not numeric.
        return GetNumericValue<k8u>();
    }
}

template <>
inline k8u GoDataTree::GetChild(const std::string& key) const
{
    try
    {
        return Get8u(key);
    }
    catch (const std::exception&)
    {
        // Cast number to type. Throws kERROR_PARAMETER if value is not numeric.
        return GetNumericValue<k8u>(key);
    }
}

template <>
inline k8s GoDataTree::Get() const
{
    try
    {
        return Get8s();
    }
    catch (const std::exception&)
    {
        // Cast number to type. Throws kERROR_PARAMETER if value is not numeric.
        return GetNumericValue<k8s>();
    }
}

template <>
inline k8s GoDataTree::GetChild(const std::string& key) const
{
    try
    {
        return Get8s(key);
    }
    catch (const std::exception&)
    {
        // Cast number to type. Throws kERROR_PARAMETER if value is not numeric.
        return GetNumericValue<k8s>(key);
    }
}

template <>
inline k16u GoDataTree::Get() const
{
    try
    {
        return Get16u();
    }
    catch (const std::exception&)
    {
        // Cast number to type. Throws kERROR_PARAMETER if value is not numeric.
        return GetNumericValue<k16u>();
    }
}

template <>
inline k16u GoDataTree::GetChild(const std::string& key) const
{
    try
    {
        return Get16u(key);
    }
    catch (const std::exception&)
    {
        // Cast number to type. Throws kERROR_PARAMETER if value is not numeric.
        return GetNumericValue<k16u>(key);
    }
}

template <>
inline k16s GoDataTree::Get() const
{
    try
    {
        return Get16s();
    }
    catch (const std::exception&)
    {
        // Cast number to type. Throws kERROR_PARAMETER if value is not numeric.
        return GetNumericValue<k16s>();
    }
}

template <>
inline k16s GoDataTree::GetChild(const std::string& key) const
{
    try
    {
        return Get16s(key);
    }
    catch (const std::exception&)
    {
        // Cast number to type. Throws kERROR_PARAMETER if value is not numeric.
        return GetNumericValue<k16s>(key);
    }
}

template <>
inline k32u GoDataTree::Get() const
{
    try
    {
        return Get32u();
    }
    catch (const std::exception&)
    {
        // Cast number to type. Throws kERROR_PARAMETER if value is not numeric.
        return GetNumericValue<k32u>();
    }
}

template <>
inline k32u GoDataTree::GetChild(const std::string& key) const
{
    try
    {
        return Get32u(key);
    }
    catch (const std::exception&)
    {
        // Cast number to type. Throws kERROR_PARAMETER if value is not numeric.
        return GetNumericValue<k32u>(key);
    }
}

template <>
inline k32s GoDataTree::Get() const
{
    try
    {
        return Get32s();
    }
    catch (const std::exception&)
    {
        // Cast number to type. Throws kERROR_PARAMETER if value is not numeric.
        return GetNumericValue<k32s>();
    }
}

template <>
inline k32s GoDataTree::GetChild(const std::string& key) const
{
    try
    {
        return Get32s(key);
    }
    catch (const std::exception&)
    {
        // Cast number to type. Throws kERROR_PARAMETER if value is not numeric.
        return GetNumericValue<k32s>(key);
    }
}

template <>
inline k32f GoDataTree::Get() const
{
    try
    {
        return Get32f();
    }
    catch (const std::exception&)
    {
        // Cast number to type. Throws kERROR_PARAMETER if value is not numeric.
        return GetNumericValue<k32f>();
    }
}

template <>
inline k32f GoDataTree::GetChild(const std::string& key) const
{
    try
    {
        return Get32f(key);
    }
    catch (const std::exception&)
    {
        // Cast number to type. Throws kERROR_PARAMETER if value is not numeric.
        return GetNumericValue<k32f>(key);
    }
}

template <>
inline k64u GoDataTree::Get() const
{
    try
    {
        return Get64u();
    }
    catch (const std::exception&)
    {
        // Cast number to type. Throws kERROR_PARAMETER if value is not numeric.
        return GetNumericValue<k64u>();
    }
}

template <>
inline k64u GoDataTree::GetChild(const std::string& key) const
{
    try
    {
        return Get64u(key);
    }
    catch (const std::exception&)
    {
        // Cast number to type. Throws kERROR_PARAMETER if value is not numeric.
        return GetNumericValue<k64u>(key);
    }
}

template <>
inline k64s GoDataTree::Get() const
{
    try
    {
        return Get64s();
    }
    catch (const std::exception&)
    {
        // Cast number to type. Throws kERROR_PARAMETER if value is not numeric.
        return GetNumericValue<k64s>();
    }
}

template <>
inline k64s GoDataTree::GetChild(const std::string& key) const
{
    try
    {
        return Get64s(key);
    }
    catch (const std::exception&)
    {
        // Cast number to type. Throws kERROR_PARAMETER if value is not numeric.
        return GetNumericValue<k64s>(key);
    }
}

template <>
inline k64f GoDataTree::Get() const
{
    try
    {
        return Get64f();
    }
    catch (const std::exception&)
    {
        // Cast number to type. Throws kERROR_PARAMETER if value is not numeric.
        return GetNumericValue<k64f>();
    }
}

template <>
inline k64f GoDataTree::GetChild(const std::string& key) const
{
    try
    {
        return Get64f(key);
    }
    catch (const std::exception&)
    {
        // Cast number to type. Throws kERROR_PARAMETER if value is not numeric.
        return GetNumericValue<k64f>(key);
    }
}

template <>
inline bool GoDataTree::Get() const
{
    return GetBoolean();
}

template <>
inline bool GoDataTree::GetChild(const std::string& key) const
{
    return GetBoolean(key);
}

template <>
inline std::string GoDataTree::Get() const
{
    return GetString();
}

template <>
inline std::string GoDataTree::GetChild(const std::string& key) const
{
    return GetString(key);
}

template <>
inline kArray1 GoDataTree::Get() const
{
    return GetArray();
}

template <>
inline kArray1 GoDataTree::GetChild(const std::string& key) const
{
    return GetArray(key);
}

template <>
inline GoDataTree GoDataTree::GetChild(const std::string& key) const
{
    return GetChild(key);
}

template<>
inline void GoDataTree::operator = (const char* value)
{
    Set(std::string(value));
}

} // namespace

typedef struct GoDataTreeArray {} GoDataTreeArray;
typedef struct GoDataTreeObject {} GoDataTreeObject;

#endif // GOAPI_GODATATREE_H
