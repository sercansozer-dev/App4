/**
 * @file    GoJsonIterator.cpp
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#include <nlohmann/json.hpp>

#include <GoPxLSdk/GoJsonIterator.h>
#include <GoPxLSdk/GoJson.h>


namespace GoPxLSdk
{

GoJsonIterator::GoJsonIterator(GoJsonIterator::RawIterator iterator)
{
    this->iterator = std::make_shared<RawIterator>(iterator);
}

std::string GoJsonIterator::Key()
{
    try
    {
        return GetHandle()->key();
    }
    catch (nlohmann::json::exception& e)
    {
        throw GoJsonError(e.what());
    }
}

const std::string GoJsonIterator::Key() const
{
    try
    {
        return GetHandle()->key();
    }
    catch (nlohmann::json::exception& e)
    {
        throw GoJsonError(e.what());
    }
}

GoJson GoJsonIterator::Value()
{
    try
    {
        return GoJson(GetHandle()->value().dump());
    }
    catch (nlohmann::json::exception& e)
    {
        throw GoJsonError(e.what());
    }
}

const GoJson GoJsonIterator::Value() const
{
    try
    {
        return GoJson(GetHandle()->value().dump());
    }
    catch (nlohmann::json::exception& e)
    {
        throw GoJsonError(e.what());
    }
}

void GoJsonIterator::SetValue(const GoJson& value)
{
    try
    {
        (nlohmann::json&)((*(*iterator))) = *value.GetHandle();
    }
    catch (nlohmann::json::exception& e)
    {
        throw GoJsonError(e.what());
    }
}

std::shared_ptr<GoJsonIterator::RawIterator>& GoJsonIterator::GetHandle()
{
    return iterator;
}

const std::shared_ptr<GoJsonIterator::RawIterator>& GoJsonIterator::GetHandle() const
{
    return iterator;
}

GoJsonIterator& GoJsonIterator::operator=(const GoJsonIterator& other)
{
    *GetHandle() = *other.GetHandle();

    return *this;
}

bool GoJsonIterator::operator==(const GoJsonIterator other) const
{
    return *GetHandle() == *other.GetHandle();
}

bool GoJsonIterator::operator!=(const GoJsonIterator other) const
{
    return *GetHandle() != *other.GetHandle();
}

bool GoJsonIterator::operator<(const GoJsonIterator other) const
{
    return *GetHandle() < *other.GetHandle();
}

bool GoJsonIterator::operator<=(const GoJsonIterator other) const
{
    return *GetHandle() <= *other.GetHandle();
}

bool GoJsonIterator::operator>(const GoJsonIterator other) const
{
    return *GetHandle() > *other.GetHandle();
}

bool GoJsonIterator::operator>=(const GoJsonIterator other) const
{
    return *GetHandle() >= *other.GetHandle();
}

GoJson GoJsonIterator::operator*() const
{
    return GoJson(Value());
}

GoJsonIterator& GoJsonIterator::operator+(int val)
{
    *GetHandle() = *GetHandle() + val;

    return *this;
}

GoJsonIterator& GoJsonIterator::operator+=(int val)
{
    *GetHandle() = *GetHandle() += val;

    return *this;
}

GoJsonIterator& GoJsonIterator::operator++(int)
{
    (*GetHandle())++;

    return *this;
}

GoJsonIterator& GoJsonIterator::operator-(int val)
{
    *GetHandle() = *GetHandle() - val;

    return *this;
}

GoJsonIterator& GoJsonIterator::operator-=(int val)
{
    *GetHandle() = *GetHandle() -= val;

    return *this;
}

GoJsonIterator& GoJsonIterator::operator--(int)
{
    (*GetHandle())--;

    return *this;
}

}