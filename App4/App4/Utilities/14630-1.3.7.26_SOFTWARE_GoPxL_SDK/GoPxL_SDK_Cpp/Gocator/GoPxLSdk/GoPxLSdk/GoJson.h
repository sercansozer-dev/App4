/**
 * @file    GoJson.h
 * @brief   Declares the GoPxLSdk.GoJson class.
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#ifndef GO_PXL_SDK_JSON_H
#define GO_PXL_SDK_JSON_H

#include <GoPxLSdk/GoJsonPointer.h>
#include <GoPxLSdk/GoJsonIterator.h>

/**
 * Macro used to explicitly instantiate GoJson templates.
 * @param type Type to use.
 */
#define GoJsonTypeSpec(type) \
                        template GoPxLSdkClass type GoJson::Get<type>(); \
                        template GoPxLSdkClass const type GoJson::Get<type>() const; \
                        template GoPxLSdkClass type GoJson::Get<type>(const std::string&); \
                        template GoPxLSdkClass const type GoJson::Get<type>(const std::string&) const; \
                        template GoPxLSdkClass type GoJson::Get<type>(const GoJsonPointer&); \
                        template GoPxLSdkClass const type GoJson::Get<type>(const GoJsonPointer&) const; \
                        template GoPxLSdkClass bool operator==<type>(const GoJson&, const type&); \
                        template GoPxLSdkClass void GoJson::operator=<type>(const type&); \
                        template GoPxLSdkClass void GoJson::Set<type>(const std::string&, const type&); \
                        template GoPxLSdkClass void GoJson::Set<type>(const GoJsonPointer&, const type&); \
                        template GoPxLSdkClass void GoJson::Add<type>(const std::string&, const type&); \
                        template GoPxLSdkClass void GoJson::Replace<type>(const std::string&, const type&);

namespace GoPxLSdk
{
/**
 * Simplify working with json data.
 */
class GoPxLSdkClass GoJson
{
public:
    /**
     * Creates empty GoJson object.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     */
    GoJson();

    /**
     * Creates GoJson object based on nlohmann::json.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @param json nlohmann::json json.
     */
    explicit GoJson(std::shared_ptr<nlohmann::json> json);

    /**
     * Copy constructor.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @param json GoJson json.
     */
    GoJson(const GoJson& json);

    /**
     * Creates GoJson object.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @param data JSON data stored as string.
     * @throw GoJsonError if data is not valid json string
     */
    explicit GoJson(const std::string& data);

    /**
     * Destroys GoJson object.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     */
    ~GoJson();

    /**
     * Gets an iterator to the first element.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @returns Iterator object
     */
    GoJsonIterator Begin();

    /**
    * Gets an iterator to the first element.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
    * @returns Iterator object
    */
    const GoJsonIterator Begin() const;

    /**
     * Gets an iterator to the last element.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @returns Iterator object.
     */
    const GoJsonIterator End() const;

    /**
     * Gets value.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @param path Path
     * @throw GoJsonError if path does not exist.
     * @throw GoJsonError if type is not compatible with the json's type.
     * @return valid JSON data.
     */
    template<typename T>
    T Get(const std::string& path);

    /**
    * Gets value.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @throw GoJsonError if type is not compatible with the json's type.
     * @return valid JSON data.
     */
    template<typename T>
    T Get();

    /**
     * Gets value.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @param path Path
     * @throw GoJsonError if path does not exist.
     * @throw GoJsonError if type is not compatible with the json's type.
     * @return valid JSON data.
     */
    template<typename T>
    const T Get(const std::string& path) const;

    /**
     * Gets value.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @param pointer Json pointer
     * @throw GoJsonError if pointer does not exist.
     * @throw GoJsonError if type is not compatible with the json's type.
     * @return valid JSON data.
     */
    template<typename T>
    T Get(const GoJsonPointer& pointer);

    /**
     * Gets value.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @param pointer Json pointer
     * @throw GoJsonError if pointer does not exist.
     * @throw GoJsonError if type is not compatible with the json's type.
     * @return valid JSON data.
     */
    template<typename T>
    const T Get(const GoJsonPointer& pointer) const;

    /**
     * Gets value.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @throw GoJsonError if type is not compatible with the json's type.
     * @return valid JSON data.
     */
    template<typename T>
    const T Get() const;

    /**
     * Gets binary value.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 1.0.106.80.
     * @throw GoJsonError if json is not binary.
     * @return ByteArray of valid JSON data.
     */
    ByteArray GetBinary();

    /**
     * Sets value.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @param path Path.
     * @param val Value to be stored. Must be valid JSON data type.
     */
    template <typename T>
    void Set(const std::string& path, const T& val);

    /**
     * Sets value.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @param path Path.
     * @param val Value to be stored (array of chars). Must be valid JSON data type.
     */
    template <std::size_t N>
    void Set(const std::string& path, const char(&val)[N])
    {
        Set(path, std::string(val));
    }

    /**
     * Sets value.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @param ptr GoJsonPointer ptr.
     * @param val Value to be stored. Must be valid JSON data type.
     */
    template <typename T>
    void Set(const GoJsonPointer& ptr, const T& val);

    /**
     * Sets value.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @param ptr GoJsonPointer ptr.
     * @param val Value to be stored (array of chars). Must be valid JSON data type.
     */
    template <std::size_t N>
    void Set(const GoJsonPointer& ptr, const char(&val)[N])
    {
        Set(ptr, std::string(val));
    }

    /**
     * Sets value.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @param path Path.
     * @param val GoJson to be stored. Must be valid JSON data type.
     */
    void Set(const std::string& path, const GoJson& val);

    /**
     * Sets value.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @param ptr GoJsonPointer.
     * @param val GoJson to be stored. Must be valid JSON data type.
     */
    void Set(const GoJsonPointer& ptr, const GoJson& val);

    /**
     * Sets value.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @param path Path.
     * @param val ByteArray to be stored.
     */
    void Set(const std::string& path, const std::vector<k8u>& val);

    /**
     * Sets value.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @param ptr GoJsonPointer.
     * @param val ByteArray to be stored.
     */
    void Set(const GoJsonPointer& ptr, const std::vector<k8u>& val);

    /**
     * Adds value if not exists.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @param path            JSON pointer path of the new field to add.
     *                        This path MUST begin with a "/" to be a proper JSON pointer path.
     * @param val             Value to be stored. Must be valid JSON data type.
     * @throw GoJsonError when handle cannot be unflatted.
     */
    template<typename T>
    void Add(const std::string& path, const T& val);

    /**
     * Adds value if not exists.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @param path            JSON pointer path of the new field to add.
     *                        This path MUST begin with a "/" to be a proper JSON pointer path.
     * @param val             Value to be stored. Must be valid JSON data type.
     *                        Specify the number of characters in the template argument.
     * @throw GoJsonError when handle cannot be unflatted.
     */
    template <std::size_t N>
    void Add(const std::string& path, const char(&val)[N])
    {
        Add(path, std::string(val));
    }

    /**
     * Replaces value.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @param path Path.
     * @param val Value to be stored. Must be valid JSON data type.
     * @throw GoJsonError
     */
    template<typename T>
    void Replace(const std::string& path, const T& val);

    /**
     * Adds value if not exists.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @param path Path.
     * @param val Value to be stored. Must be valid JSON data type.
     * @throw GoJsonError when handle cannot be unflatted.
     */
    template <std::size_t N>
    void Replace(const std::string& path, const char(&val)[N])
    {
        Replace(path, std::string(val));
    }

    /**
     * Checks if object contains key.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @param key Key.
     * @return true if key exists.
     */
    bool HasKey(const std::string& key) const;

    /**
     * Copies JSON object.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @param source Source path.
     * @param destination Destination path.
     * @throw GoJsonError
     */
    void Copy(const std::string& source, const std::string& destination);

    /**
     * Moves JSON object. Source object will be removed.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @param source Source path.
     * @param destination Destination path.
     * @throw GoJsonError
     */
    void Move(const std::string& source, const std::string& destination);

    /**
     * Removes JSON object.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @throw GoJsonError
     * @param path Path to be removed.
     */
    void Remove(const std::string& path);

    /**
     * Patches JSON object.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @throw GoJsonError
     * @param obj A valid GoJson patch object (RFC 6902)[https://tools.ietf.org/html/rfc6902]
     */
    void Patch(const GoJson& obj);

    /**
     * Merges JSON object.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @throw GoJsonError
     * @param obj A GoJson object to merge with.
     */
    void Merge(const GoJson& obj);

    /**
     * Checks if GoJson object is empty.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @return true if is empty, otherwise false.
     */
    const bool Empty() const;

    /**
     * Finds key in object
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @throw GoJsonError
     * @param key String key
     * @return Iterator value iterator
     */
    GoJsonIterator Find(const std::string& key);

    /**
     * UNSUPPORTED!
     * Finds key in object
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @throw GoJsonError
     * @param pointer Pointer valid json pointer
     * @return Iterator value iterator
     */
    GoJsonIterator Find(const GoJsonPointer& pointer);

    /**
     * Finds key in object
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @throw GoJsonError
     * @param key String key
     * @return Iterator value iterator
     */
    const GoJsonIterator Find(const std::string& key) const;

    /**
     * UNSUPPORTED!
     * Finds key in object
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @throw GoJsonError
     * @param pointer Pointer valid json pointer
     * @return Iterator value iterator
     */
    const GoJsonIterator Find(const GoJsonPointer& pointer) const;

    /**
     * Gets object using pointer.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @param index array index.
     * @throw GoJsonError.
     * @return GoJson object
     */
    GoJson At(int index);

    /**
     * Gets object using pointer.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @param index array index.
     * @throw GoJsonError.
     * @return GoJson object
     */
    const GoJson At(int index) const;

    /**
     * Gets object using pointer.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @param path json data path.
     * @throw GoJsonError.
     * @return GoJson object
     */
    GoJson At(const std::string& path);

    /**
     * Gets object using pointer.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @param path json data path.
     * @throw GoJsonError.
     * @return GoJson object
     */
    const GoJson At(const std::string& path) const;

    /**
     * Gets object using pointer.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @param pointer GoJson::Pointer json data pointer.
     * @throw GoJsonError.
     * @return GoJson object
     */
    GoJson At(const GoJsonPointer& pointer);

    /**
     * Gets object using pointer.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @param pointer GoJson::Pointer json data pointer.
     * @throw GoJsonError.
     * @return GoJson object
     */
    const GoJson At(const GoJsonPointer& pointer) const;

    /**
     * Checks if Json contains data.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @param pointer GoJson::Pointer json data pointer.
     * @throw GoJsonError.
     * @return true if contains.
     */
    const bool Contains(const GoJsonPointer& pointer) const;

    /**
     * Gets size of the array.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @return size
     */
    const size_t Size() const;

    /**
     * Checks if GoJson is an object.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @return true if is object, otherwise false.
     */
    const bool IsObject() const;

    /**
     * Checks if GoJson is any number.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @return true if is number, otherwise false.
     */
    const bool IsNumber() const;

    /**
     * Checks if GoJson is float.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @return true if is float, otherwise false.
     */
    const bool IsFloat() const;

    /**
     * Checks if GoJson is integer.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @return true if is integer, otherwise false.
     */
    const bool IsInteger() const;

    /**
     * Checks if GoJson is unsigned.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @return true if is unsigned, otherwise false.
     */
    const bool IsUnsigned() const;

    /**
     * Checks if GoJson is boolean.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @return true if is bool, otherwise false.
     */
    const bool IsBoolean() const;

    /**
     * Checks if GoJson is the string.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @return true if is string, otherwise false.
     */
    const bool IsString() const;

    /**
     * Checks if GoJson is an array.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @return true if is array, otherwise false.
     */
    const bool IsArray() const;

    /**
     * Checks if GoJson is a primitive type.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @return true if is primitive, otherwise false.
     */
    const bool IsPrimitive() const;

    /**
     * Checks if GoJson is binary.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @return true if is binary, otherwise false.
     */
    const bool IsBinary() const;

    /**
     * Checks if GoJson value was discarded during parsing.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @return true if discarded, otherwise false.
     */
    const bool IsDiscarded() const;

    /**
     * Checks if GoJson is null.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @return true if is null, otherwise false.
     */
    const bool IsNull() const;

    /**
     * Checks if GoJson is structured (array or object).
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @return true if is structured, otherwise false.
     */
    const bool IsStructured() const;

    /**
     * Checks if GoJson is an object.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @param path Path.
     * @return true if is object, otherwise false.
     */
    const bool IsObject(const std::string& path) const;

    /**
     * Checks if GoJson is any number.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @param path Path.
     * @return true if is number, otherwise false.
     */
    const bool IsNumber(const std::string& path) const;

    /**
     * Checks if GoJson is float.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @param path Path.
     * @return true if is float, otherwise false.
     */
    const bool IsFloat(const std::string& path) const;

    /**
     * Checks if GoJson is integer.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @param path Path.
     * @return true if is integer, otherwise false.
     */
    const bool IsInteger(const std::string& path) const;

    /**
     * Checks if GoJson is unsigned.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @param path Path.
     * @return true if is unsigned, otherwise false.
     */
    const bool IsUnsigned(const std::string& path) const;

    /**
     * Checks if GoJson is boolean.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @param path Path.
     * @return true if is bool, otherwise false.
     */
    const bool IsBoolean(const std::string& path) const;

    /**
     * Checks if GoJson is the string.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @param path Path.
     * @return true if is string, otherwise false.
     */
    const bool IsString(const std::string& path) const;

    /**
     * Checks if GoJson is an array.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @param path Path.
     * @return true if is array, otherwise false.
     */
    const bool IsArray(const std::string& path) const;

    /**
     * Checks if GoJson is a primitive type.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @param path Path.
     * @return true if is primitive, otherwise false.
     */
    const bool IsPrimitive(const std::string& path) const;

    /**
     * Checks if GoJson is binary.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @param path Path.
     * @return true if is binary, otherwise false.
     */
    const bool IsBinary(const std::string& path) const;

    /**
     * Checks if GoJson value was discarded during parsing.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @param path Path.
     * @return true if discarded, otherwise false.
     */
    const bool IsDiscarded(const std::string& path) const;

    /**
     * Checks if GoJson is null.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @param path Path.
     * @return true if is null, otherwise false.
     */
    const bool IsNull(const std::string& path) const;

    /**
     * Checks if GoJson is structured (array or object).
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @param path Path.
     * @return true if is structured, otherwise false.
     */
    const bool IsStructured(const std::string& path) const;

    /**
     * Flattens json.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @remark Example
     * Given JSON
     *    {"pi", 3.141},
     *    {"happy", true},
     *    {"name", "Niels"},
     *    {"nothing", nullptr},
     *    {
     *        "answer", {
     *            {"everything", 42}
     *        }
     *    },
     *    {"list", {1, 0, 2}},
     *    {
     *        "object", {
     *            {"currency", "USD"},
     *            {"value", 42.99}
     *        }
     *    }
     * Will become
     *    {
     *       "/answer/everything": 42,
     *       "/happy": true,
     *       "/list/0": 1,
     *       "/list/1": 0,
     *       "/list/2": 2,
     *       "/name": "Niels",
     *       "/nothing": null,
     *       "/object/currency": "USD",
     *       "/object/value": 42.99,
     *       "/pi": 3.141
     *    }
     */
    void Flatten();

    /**
     * Unflattens json.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @remark Example
     * Given flattened JSON
     *   {
     *        "/answer/everything": 42,
     *        "/happy": true,
     *        "/list/0": 1,
     *        "/list/1": 0,
     *        "/list/2": 2,
     *        "/name": "Niels",
     *        "/nothing": null,
     *        "/object/currency": "USD",
     *        "/object/value": 42.99,
     *        "/pi": 3.141
     *    }
     * Will become
     *     {"pi", 3.141},
     *     {"happy", true},
     *     {"name", "Niels"},
     *     {"nothing", nullptr},
     *     {
     *         "answer", {
     *             {"everything", 42}
     *         }
     *     },
     *     {"list", {1, 0, 2}},
     *     {
     *         "object", {
     *             {"currency", "USD"},
     *             {"value", 42.99}
     *         }
     *     }
     * @throw JsonError when value is not "flat".
     */
    void Unflatten();

    /**
     * Gets string formatted JSON.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @return stringified JSON.
     */
    const std::string ToString();

    /**
     * Gets string formatted JSON.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @return stringified JSON.
     */
    const std::string ToString() const;

    /**
     * Gets JSON as a ByteArray.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @return binary JSON.
     */
    const ByteArray ToBinary();

    /**
     * Parses JSON string data.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @throw GoJsonError if data is not valid json string
     * @return GoJson object.
     */
    static GoJson ParseString(const std::string& data);

    /**
     * Parses JSON binary data.
     *
     * @public                @memberof GoJson
     * @version               Introduced in 0.2.1.53
     * @throw GoJsonError if data is not valid
     * @return GoJson object.
     */
    static GoJson ParseMsgPack(const ByteArray& data);

    template<typename T>
    friend bool operator==(const GoJson& left, const T& right);

    template <std::size_t N>
    friend bool operator==(const GoJson& left, const char(&val)[N])
    {
        return left == std::string(val);
    }

    friend std::ostream& operator<<(std::ostream& os, const GoJson& json)
    {
        os << json.ToString();

        return os;
    }

    template<typename T>
    void operator=(const T& left);

    template <std::size_t N>
    void operator=(const char(&val)[N])
    {
        return *this = std::string(val);
    }

protected:
    /**
    * Gets JSON handle at specific path
    * @param path Path
    * @return JSON handle.
    */
    const std::shared_ptr<nlohmann::json> GetHandle(const std::string& path) const;

    /**
     * Gets handle to nlohmann::json.
     * @return nlohmann::json handle.
     */
    const std::shared_ptr<nlohmann::json>& GetHandle() const;

    /**
     * Gets JSON handle at specific path
     * @param path Path
     * @return JSON handle.
     */
    std::shared_ptr<nlohmann::json> GetHandle(const std::string& path);

    /**
     * Gets handle to nlohmann::json.
     * @return nlohmann::json handle.
     */
    std::shared_ptr<nlohmann::json>& GetHandle();

    friend class GoRequest;
    friend class GoJsonIterator;

private:
    mutable std::shared_ptr<nlohmann::json> jsonHandle;
};

}

#endif
