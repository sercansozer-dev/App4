/**
 * @file    GoJson.cpp
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#include <nlohmann/json.hpp>
#include <GoPxLSdk/GoJson.h>

namespace GoPxLSdk
{

GoJson::GoJson()
{
    jsonHandle = std::make_shared<nlohmann::json>(nlohmann::json::object());
}

GoJson::GoJson(std::shared_ptr<nlohmann::json> json)
{
    jsonHandle = std::make_shared<nlohmann::json>(*json);
}

GoJson::GoJson(const GoJson& json)
{
    jsonHandle = std::make_shared<nlohmann::json>(*json.GetHandle());
}

GoJson::GoJson(const std::string& data)
{
    try
    {
        jsonHandle = std::make_shared<nlohmann::json>(nlohmann::json::parse(data));
    }
    catch (nlohmann::json::exception& e)
    {
        throw GoJsonError(e.what());
    }
}

GoJson::~GoJson() = default;

GoJsonIterator GoJson::Begin()
{
    return GoJsonIterator(GetHandle()->begin());
}

const GoJsonIterator GoJson::Begin() const
{
    return GoJsonIterator(GetHandle()->begin());
}

const GoJsonIterator GoJson::End() const
{
    return GoJsonIterator(GetHandle()->end());
}

void GoJson::Set(const std::string& path, const GoJson& val)
{
    Set(GoJsonPointer(path), val);
}

void GoJson::Set(const GoJsonPointer& ptr, const GoJson& val)
{
    (*GetHandle())[*ptr.GetHandle()] = *val.GetHandle();
}

void GoJson::Set(const std::string& path, const std::vector<k8u>& val)
{
    Set(GoJsonPointer(path), val);
}

void GoJson::Set(const GoJsonPointer& ptr, const std::vector<k8u>& val)
{
    (*GetHandle())[*ptr.GetHandle()] = nlohmann::json::binary_t(val);
}

bool GoJson::HasKey(const std::string& key) const
{
    return GetHandle()->contains(key);
}

void GoJson::Copy(const std::string& source, const std::string& destination)
{
    Set(destination, GetHandle()->flatten()[source]);
}

void GoJson::Move(const std::string& source, const std::string& destination)
{
    Copy(source, destination);
    Remove(source);
}

void GoJson::Remove(const std::string& path)
{
    try
    {
        auto handle = GetHandle()->flatten();

        handle.erase(path);

        *GetHandle() = handle.unflatten();
    }
    catch (nlohmann::json::exception& e)
    {
        throw GoJsonError(e.what());
    }
}

void GoJson::Patch(const GoJson& obj)
{
    try
    {
        GetHandle() = std::make_shared<nlohmann::json>(GetHandle()->patch(*obj.GetHandle()));
    }
    catch (nlohmann::json::exception& e)
    {
        throw GoJsonError(e.what());
    }
}

void GoJson::Merge(const GoJson& obj)
{
    try
    {
        GetHandle()->merge_patch(*obj.GetHandle());
    }
    catch (nlohmann::json::exception& e)
    {
        throw GoJsonError(e.what());
    }
}

const bool GoJson::Empty() const
{
    return GetHandle()->empty();
}

GoJsonIterator GoJson::Find(const std::string& key)
{
    try
    {
        return GoJsonIterator(GetHandle()->find(key));
    }
    catch (nlohmann::json::exception& e)
    {
        throw GoJsonError(e.what());
    }
}

GoJsonIterator GoJson::Find(const GoJsonPointer& pointer)
{
    try
    {
        return GoJsonIterator(GetHandle()->find(*pointer.GetHandle()));
    }
    catch (nlohmann::json::exception& e)
    {
        throw GoJsonError(e.what());
    }
}

const GoJsonIterator GoJson::Find(const std::string& key) const
{
    try
    {
        return GoJsonIterator(GetHandle()->find(key));
    }
    catch (nlohmann::json::exception& e)
    {
        throw GoJsonError(e.what());
    }
}

const GoJsonIterator GoJson::Find(const GoJsonPointer& pointer) const
{
    try
    {
        return GoJsonIterator(GetHandle()->find(*pointer.GetHandle()));
    }
    catch (nlohmann::json::exception& e)
    {
        throw GoJsonError(e.what());
    }
}

GoJson GoJson::At(int index)
{
    try
    {
        return GoJson(std::make_shared<nlohmann::json>((*GetHandle()).at(index)));
    }
    catch (nlohmann::json::exception& e)
    {
        throw GoJsonError(e.what());
    }
}

const GoJson GoJson::At(int index) const
{
    try
    {
        return GoJson(std::make_shared<nlohmann::json>((*GetHandle()).at(index)));
    }
    catch (nlohmann::json::exception& e)
    {
        throw GoJsonError(e.what());
    }
}

GoJson GoJson::At(const std::string& path)
{
    return At(GoJsonPointer(path));
}

const GoJson GoJson::At(const std::string& path) const
{
    return At(GoJsonPointer(path));
}

GoJson GoJson::At(const GoJsonPointer& pointer)
{
    try
    {
        return GoJson(std::make_shared<nlohmann::json>((*GetHandle()).at(*pointer.GetHandle())));
    }
    catch (nlohmann::json::exception& e)
    {
        throw GoJsonError(e.what());
    }
}

const GoJson GoJson::At(const GoJsonPointer& pointer) const
{
    try
    {
        return GoJson(std::make_shared<nlohmann::json>((*GetHandle()).at(*pointer.GetHandle())));
    }
    catch (nlohmann::json::exception& e)
    {
        throw GoJsonError(e.what());
    }
}

const bool GoJson::Contains(const GoJsonPointer& pointer) const
{
    try
    {
        return GetHandle()->contains(*pointer.GetHandle());
    }
    catch (nlohmann::json::exception& e)
    {
        throw GoJsonError(e.what());
    }
}

const size_t GoJson::Size() const
{
    return GetHandle()->size();
}

const bool GoJson::IsObject() const
{
    return GetHandle()->is_object();
}

const bool GoJson::IsNumber() const
{
    return GetHandle()->is_number();
}

const bool GoJson::IsFloat() const
{
    return GetHandle()->is_number_float();
}

const bool GoJson::IsInteger() const
{
    return GetHandle()->is_number_integer();
}

const bool GoJson::IsUnsigned() const
{
    return GetHandle()->is_number_unsigned();
}

const bool GoJson::IsBoolean() const
{
    return GetHandle()->is_boolean();
}

const bool GoJson::IsString() const
{
    return GetHandle()->is_string();
}

const bool GoJson::IsArray() const
{
    return GetHandle()->is_array();
}

const bool GoJson::IsPrimitive() const
{
    return GetHandle()->is_primitive();
}

const bool GoJson::IsBinary() const
{
    return GetHandle()->is_binary();
}

const bool GoJson::IsDiscarded() const
{
    return GetHandle()->is_discarded();
}

const bool GoJson::IsNull() const
{
    return GetHandle()->is_null();
}

const bool GoJson::IsStructured() const
{
    return GetHandle()->is_structured();
}

const bool GoJson::IsObject(const std::string& path) const
{
    return (*GetHandle())[*GoJsonPointer(path).GetHandle()].is_object();
}

const bool GoJson::IsNumber(const std::string& path) const
{
    return (*GetHandle())[*GoJsonPointer(path).GetHandle()].is_number();
}

const bool GoJson::IsFloat(const std::string& path) const
{
    return (*GetHandle())[*GoJsonPointer(path).GetHandle()].is_number_float();
}

const bool GoJson::IsInteger(const std::string& path) const
{
    return (*GetHandle())[*GoJsonPointer(path).GetHandle()].is_number_integer();
}

const bool GoJson::IsUnsigned(const std::string& path) const
{
    return (*GetHandle())[*GoJsonPointer(path).GetHandle()].is_number_unsigned();
}

const bool GoJson::IsBoolean(const std::string& path) const
{
    return (*GetHandle())[*GoJsonPointer(path).GetHandle()].is_boolean();
}

const bool GoJson::IsString(const std::string& path) const
{
    return (*GetHandle())[*GoJsonPointer(path).GetHandle()].is_string();
}

const bool GoJson::IsArray(const std::string& path) const
{
    return (*GetHandle())[*GoJsonPointer(path).GetHandle()].is_array();
}

const bool GoJson::IsPrimitive(const std::string& path) const
{
    return (*GetHandle())[*GoJsonPointer(path).GetHandle()].is_primitive();
}

const bool GoJson::IsBinary(const std::string& path) const
{
    return (*GetHandle())[*GoJsonPointer(path).GetHandle()].is_binary();
}

const bool GoJson::IsDiscarded(const std::string& path) const
{
    return (*GetHandle())[*GoJsonPointer(path).GetHandle()].is_discarded();
}

const bool GoJson::IsNull(const std::string& path) const
{
    return (*GetHandle())[*GoJsonPointer(path).GetHandle()].is_null();
}

const bool GoJson::IsStructured(const std::string& path) const
{
    return (*GetHandle())[*GoJsonPointer(path).GetHandle()].is_structured();
}

void GoJson::Flatten()
{
    try
    {
        *GetHandle() = GetHandle()->flatten();
    }
    catch (nlohmann::json::exception& e)
    {
        throw GoJsonError(e.what());
    }
}

void GoJson::Unflatten()
{
    try
    {
        *GetHandle() = GetHandle()->unflatten();
    }
    catch (nlohmann::json::exception& e)
    {
        throw GoJsonError(e.what());
    }
}

const std::string GoJson::ToString()
{
    return GetHandle()->dump();
}

const std::string GoJson::ToString() const
{
    return GetHandle()->dump();
}

const ByteArray GoJson::ToBinary()
{
    return GetHandle() != nullptr ? nlohmann::json::to_msgpack(*GetHandle().get()) : ByteArray();
}

const std::shared_ptr<nlohmann::json> GoJson::GetHandle(const std::string& path) const
{
    return std::make_shared<nlohmann::json>((*GetHandle())[*GoJsonPointer(path).GetHandle()]);
}

const std::shared_ptr<nlohmann::json>& GoJson::GetHandle() const
{
    if (!jsonHandle)
    {
        jsonHandle = std::make_shared<nlohmann::json>(nlohmann::json::object());
    }

    return jsonHandle;
}

std::shared_ptr<nlohmann::json> GoJson::GetHandle(const std::string& path)
{
    return std::make_shared<nlohmann::json>((*GetHandle())[*GoJsonPointer(path).GetHandle()]);
}

std::shared_ptr<nlohmann::json>& GoJson::GetHandle()
{
    if (!jsonHandle)
    {
        jsonHandle = std::make_shared<nlohmann::json>(nlohmann::json::object());
    }

    return jsonHandle;
}

GoJson GoJson::ParseString(const std::string& data)
{
    return GoJson(data);
}

GoJson GoJson::ParseMsgPack(const ByteArray& data)
{
    try
    {
        return GoJson(std::make_shared<nlohmann::json>(nlohmann::json::from_msgpack(data)));
    }
    catch (const std::exception& e)
    {
        throw GoJsonError(e.what());
    }
}

template<typename T>
T GoJson::Get(const std::string& path)
{
    try
    {
        return GetHandle(path)->get<T>();
    }
    catch (nlohmann::json::exception& e)
    {
        throw GoJsonError(e.what());
    }
}

template <>
GoJson GoJson::Get(const std::string& path)
{
    try
    {
        auto json = GoJson();
        json.jsonHandle = std::make_shared<nlohmann::json>(GetHandle(path)->get<nlohmann::json>());

        return json;
    }
    catch (nlohmann::json::exception& e)
    {
        throw GoJsonError(e.what());
    }
}

template<typename T>
T GoJson::Get()
{
    try
    {
        return GetHandle()->get<T>();
    }
    catch (nlohmann::json::exception& e)
    {
        throw GoJsonError(e.what());
    }
}

ByteArray GoJson::GetBinary()
{
    try
    {
        return GetHandle()->get_binary();
    }
    catch (nlohmann::json::exception& e)
    {
        throw GoJsonError(e.what());
    }
}

template <>
GoJson GoJson::Get()
{
    try
    {
        auto json = GoJson();
        json.jsonHandle = std::make_shared<nlohmann::json>(GetHandle()->get<nlohmann::json>());

        return json;
    }
    catch (nlohmann::json::exception& e)
    {
        throw GoJsonError(e.what());
    }
}

template<typename T>
const T GoJson::Get(const std::string& path) const
{
    try
    {
        return GetHandle(path)->get<T>();
    }
    catch (nlohmann::detail::exception& e)
    {
        throw GoJsonError(e.what());
    }
}

template <>
const GoJson GoJson::Get(const std::string& path) const
{
    try
    {
        auto json = GoJson();
        json.jsonHandle = std::make_shared<nlohmann::json>(GetHandle(path)->get<nlohmann::json>());

        return json;
    }
    catch (nlohmann::json::exception& e)
    {
        throw GoJsonError(e.what());
    }
}

template<typename T>
T GoJson::Get(const GoJsonPointer& pointer)
{
    return At(pointer).Get<T>();
}

template <>
GoJson GoJson::Get(const GoJsonPointer& pointer)
{
    try
    {
        auto json = GoJson();
        json.jsonHandle = std::make_shared<nlohmann::json>(At(pointer).Get<nlohmann::json>());

        return json;
    }
    catch (nlohmann::json::exception& e)
    {
        throw GoJsonError(e.what());
    }
}

template<typename T>
const T GoJson::Get(const GoJsonPointer& pointer) const
{
    return At(pointer).Get<T>();
}

template <>
const GoJson GoJson::Get(const GoJsonPointer& pointer) const
{
    try
    {
        auto json = GoJson();
        json.jsonHandle = std::make_shared<nlohmann::json>(At(pointer).Get<nlohmann::json>());

        return json;
    }
    catch (nlohmann::json::exception& e)
    {
        throw GoJsonError(e.what());
    }
}

template<typename T>
const T GoJson::Get() const
{
    try
    {
        return GetHandle()->get<T>();
    }
    catch (nlohmann::json::exception& e)
    {
        throw GoJsonError(e.what());
    }
}

template <>
const GoJson GoJson::Get() const
{
    try
    {
        auto json = GoJson();
        json.jsonHandle = std::make_shared<nlohmann::json>(GetHandle()->get<nlohmann::json>());

        return json;
    }
    catch (nlohmann::json::exception& e)
    {
        throw GoJsonError(e.what());
    }
}

template <typename T>
void GoJson::Set(const std::string& path, const T& val)
{
    Set(GoJsonPointer(path), val);
}

template <>
void GoJson::Set(const std::string& path, const GoJson& val)
{
    Set(GoJsonPointer(path), val);
}

template <typename T>
void GoJson::Set(const GoJsonPointer& ptr, const T& val)
{
    (*GetHandle())[*ptr.GetHandle()] = val;
}

template <>
void GoJson::Set(const GoJsonPointer& ptr, const GoJson& val)
{
    (*GetHandle())[*ptr.GetHandle()] = *val.GetHandle();
}

template<typename T>
void GoJson::Add(const std::string& path, const T& val)
{
    try
    {
        auto handle = GetHandle()->flatten();
        handle.emplace(path, val);

        (*GetHandle()) = handle.unflatten();
    }
    catch (nlohmann::json::exception& e)
    {
        throw GoJsonError(e.what());
    }
}

template<>
void GoJson::Add(const std::string& path, const GoJson& val)
{
    try
    {
        auto handle = GetHandle()->flatten();
        handle.emplace(path, *val.GetHandle());

        (*GetHandle()) = handle.unflatten();
    }
    catch (nlohmann::json::exception& e)
    {
        throw GoJsonError(e.what());
    }
}

template<typename T>
void GoJson::Replace(const std::string& path, const T& val)
{
    Set(path, val);
}

template<>
void GoJson::Replace(const std::string& path, const GoJson& val)
{
    Set(path, val);
}

template<typename T>
bool operator==(const GoJson& left, const T& right)
{
    try
    {
        return (*left.GetHandle()) == right;
    }
    catch (nlohmann::json::exception& e)
    {
        throw GoJsonError(e.what());
    }
}

template<>
bool operator==(const GoJson& left, const GoJson& right)
{
    try
    {
        return *left.GetHandle() == *right.GetHandle();
    }
    catch (nlohmann::json::exception& e)
    {
        throw GoJsonError(e.what());
    }
}

template<typename T>
void GoJson::operator=(const T& left)
{
    *GetHandle() = left;
}

template<>
void GoJson::operator=(const GoJson& left)
{
    *GetHandle() = *left.GetHandle();
}

template GoPxLSdkClass void GoJson::operator=<char const*>(char const* const&);

GoJsonTypeSpec(int)
GoJsonTypeSpec(bool)
GoJsonTypeSpec(float)
GoJsonTypeSpec(double)
GoJsonTypeSpec(unsigned int)
GoJsonTypeSpec(int64_t)
GoJsonTypeSpec(uint64_t)
GoJsonTypeSpec(nullptr_t)
GoJsonTypeSpec(std::string)
GoJsonTypeSpec(std::vector<k8u>)
GoJsonTypeSpec(GoJson)

}

