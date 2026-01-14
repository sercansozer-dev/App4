/**
 * @file    GoJsonPointer.cpp
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#include <nlohmann/json.hpp>
#include <GoPxLSdk/GoJsonPointer.h>
#include <sstream>

namespace GoPxLSdk
{

GoJsonPointer::GoJsonPointer(const std::string& data)
{
    try
    {
        auto path = data;
        if (!data.empty() && data[0] != '/')
        {
            path = "/" + data;
        }

        handle = std::make_shared<nlohmann::json_pointer<nlohmann::json>>(path);
    }
    catch (nlohmann::json::exception& e)
    {
        throw GoJsonError(e.what());
    }
}

GoJsonPointer::GoJsonPointer(const GoJsonPointer& data)
{
    this->handle = std::make_shared<nlohmann::json_pointer<nlohmann::json>>(*data.GetHandle());
}

std::shared_ptr<nlohmann::json_pointer<nlohmann::json>>& GoJsonPointer::GetHandle()
{
    if (!handle) 
    {
        handle = std::make_shared<nlohmann::json_pointer<nlohmann::json>>();
    }

    return handle;
}

const std::shared_ptr<nlohmann::json_pointer<nlohmann::json>>& GoJsonPointer::GetHandle() const
{
    if (!handle)
    {
        handle = std::make_shared<nlohmann::json_pointer<nlohmann::json>>();
    }

    return handle;
}

void GoJsonPointer::PushBack(const std::string& key)
{
    if (key.empty())
    {
        GetHandle()->push_back(key);

        return;
    }

    for (auto const& token : Tokenize(key, '/'))
    {
        GetHandle()->push_back(token);
    }
}

void GoJsonPointer::PushBack(const GoJsonPointer& pointer)
{
    if (pointer.ToString().empty())
    {
        GetHandle()->push_back(pointer.ToString());
        
        return;
    }

    for (auto const& token : Tokenize(pointer.ToString(), '/'))
    {
        GetHandle()->push_back(token);
    }
}

void GoJsonPointer::PopBack()
{
    GetHandle()->pop_back();
}

const std::string GoJsonPointer::ToString() const
{
    return GetHandle()->to_string();
}

GoJsonPointer GoJsonPointer::Combine(const GoJsonPointer& target, const std::string& tail)
{
    auto copy = target;

    copy.PushBack(tail);
    return copy;
}

GoJsonPointer GoJsonPointer::Combine(const GoJsonPointer& target, const GoJsonPointer& tail)
{
    auto copy = target;

    copy.PushBack(tail);
    return copy;
}

GoJsonPointer GoJsonPointer::ParseString(const std::string& data)
{
    return GoJsonPointer(data);
}

std::vector<std::string> GoJsonPointer::Tokenize(const std::string& key, char delimiter)
{
    std::vector<std::string> tokens;

    std::stringstream ss(key);
    std::string token;
    while (std::getline(ss, token, delimiter))
    {
        if (!token.empty())
        {
            tokens.push_back(token);
        }
    }

    return tokens;
}

}