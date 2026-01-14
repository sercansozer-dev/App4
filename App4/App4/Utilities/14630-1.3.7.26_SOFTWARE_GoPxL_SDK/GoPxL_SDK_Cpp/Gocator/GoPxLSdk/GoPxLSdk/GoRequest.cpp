/**
 * @file    GoRequest.cpp
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#include <GoPxLSdk/Def.h>
#include <GoPxLSdk/GoRequest.h>

namespace GoPxLSdk
{

k64u GoRequest::nextId = 0;

GoRequest::GoRequest(GoRequestMethod method, const std::string& uri) : id(nextId++)
{
    this->method = method;
    this->uri = uri;
    this->content = GoJson();
    this->args = GoJson();
}

GoRequest::GoRequest(GoRequestMethod method, const std::string& uri, const GoJson& content) : id(nextId++)
{
    this->method = method;
    this->uri = uri;
    this->content = content;
    this->args = GoJson();
}

GoRequest::GoRequest(GoRequestMethod method, const std::string& uri, const GoJson& content, const GoJson& args) : id(nextId++)
{
    this->method = method;
    this->uri = uri;
    this->content = content;
    this->args = args;
}

k64u GoRequest::Id() const
{
    return id;
}

GoRequestMethod GoRequest::Method() const
{
    return method;
}

const std::string& GoRequest::Uri() const
{
    return uri;
}

const GoJson& GoRequest::Arguments() const
{
    return args;
}

const GoJson& GoRequest::Content() const
{
    return content;
}

const ByteArray GoRequest::ToByteArray() const
{
    auto jsonRequest = GoJson();

    jsonRequest.Set("method", Method().ToString());
    jsonRequest.Set("path", Uri());
    jsonRequest.Set("payload", Content());
    jsonRequest.Set("args", Arguments());
   
    return jsonRequest.ToBinary();
}

}
