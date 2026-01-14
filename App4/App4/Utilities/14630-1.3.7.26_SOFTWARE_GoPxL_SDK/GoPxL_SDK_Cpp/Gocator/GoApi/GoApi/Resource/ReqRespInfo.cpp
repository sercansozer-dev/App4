#include "ReqRespInfo.h"

namespace GoApi
{

// RequestInfo methods

RequestInfo::RequestInfo() :
    includeSchema(false)
{
}

RequestInfo::RequestInfo(const ResourceKeys& keys) :
    keys(keys),
    includeSchema(false)
{
}

RequestInfo::RequestInfo(const ResourceKeys& keys, const nlohmann::json& args):
    keys(keys),
    args(args),
    includeSchema(false)
{
}

RequestInfo::RequestInfo(const ResourceKeys& keys, const nlohmann::json& args, DataBuffer msgPackData):
    keys(keys),
    args(args),
    includeSchema(false),
    msgPackData(msgPackData)
{
}

RequestInfo::RequestInfo(const ResourceKeys& keys, const nlohmann::json& args, bool includeSchema) :
    keys(keys),
    args(args),
    includeSchema(includeSchema)
{
}

const std::string* RequestInfo::FindKey(const std::string& id) const
{
    auto it = keys.find(id);

    return it == keys.end() ? nullptr : &it->second;
}

RequestInfoEx::RequestInfoEx() :
    includeSchema(false)
{ }

RequestInfoEx::RequestInfoEx(const ResourceKeys& keys) :
    keys(keys),
    includeSchema(false)
{ }

RequestInfoEx::RequestInfoEx(const ResourceKeys& keys, const GoDataTree& args) :
    keys(keys),
    args(args),
    includeSchema(false)
{ }

RequestInfoEx::RequestInfoEx(const ResourceKeys& keys, const GoDataTree& args, bool includeSchema) :
    keys(keys),
    args(args),
    includeSchema(includeSchema)
{ }

const std::string* RequestInfoEx::FindKey(const std::string& id) const
{
    auto it = keys.find(id);

    return it == keys.end() ? nullptr : &it->second;
}

// ResponseInfo methods

void ResponseInfo::ForceLinkArray(const std::string& rel)
{
    links[rel].forceArray = true;
}

void ResponseInfo::ForceEmbeddedArray(const std::string& rel)
{
    embeddings[rel].forceArray = true;
}

void ResponseInfo::ForceEmbeddedExpansion(const std::string& rel)
{
    embeddings[rel].forceExpansion = true;
}

void ResponseInfo::Link(const std::string& rel, const std::string& path, bool forceArray, const nlohmann::json& annotations)
{
    if (forceArray)
    {
        ForceLinkArray(rel);
    }

    AddRef(links, rel, path, annotations);
}

void ResponseInfo::Embed(const std::string& rel, const std::string& path, bool forceArray, const nlohmann::json& extraProps)
{
    if (forceArray)
    {
        ForceEmbeddedArray(rel);
    }

    AddRef(embeddings, rel, path, extraProps);
}

void ResponseInfo::AddRef(RefMap& refMap, const std::string& rel, const std::string& path, const nlohmann::json& extra)
{
    PathRef ref;
    ref.path = path;
    ref.extra = extra;

    auto& item = refMap[rel];
    item.refs.push_back(ref);
}

ResponseInfo::RefPtrMap ResponseInfo::GetLinks() const
{
    return GetRefPtrMap(links);
}

ResponseInfo::RefPtrMap ResponseInfo::GetEmbeddings() const
{
    return GetRefPtrMap(embeddings);
}

ResponseInfo::RefPtrMap ResponseInfo::GetRefPtrMap(const RefMap& refMap) const
{
    RefPtrMap results;

    for (auto& pair : refMap)
    {
        results[pair.first] = &pair.second;
    }

    return results;
}

void ResponseInfoEx::Link(const std::string& rel, const std::string& path, bool forceArray, const GoDataTree& annotations)
{
    if (forceArray)
    {
        ForceLinkArray(rel);
    }

    AddRef(links, rel, path, annotations);
}

void ResponseInfoEx::Embed(const std::string& rel, const std::string& path, bool forceArray, const GoDataTree& extraProps)
{
    if (forceArray)
    {
        ForceEmbeddedArray(rel);
    }

    AddRef(embeddings, rel, path, extraProps);
}

void ResponseInfoEx::ForceLinkArray(const std::string& rel)
{
    links[rel].forceArray = true;
}

void ResponseInfoEx::ForceEmbeddedArray(const std::string& rel)
{
    embeddings[rel].forceArray = true;
}

void ResponseInfoEx::ForceEmbeddedExpansion(const std::string& rel)
{
    embeddings[rel].forceExpansion = true;
}

ResponseInfoEx::RefPtrMap ResponseInfoEx::GetLinks() const
{
    return GetRefPtrMap(links);
}

ResponseInfoEx::RefPtrMap ResponseInfoEx::GetEmbeddings() const
{
    return GetRefPtrMap(embeddings);
}

void ResponseInfoEx::AddRef(RefMap& refMap, const std::string& rel, const std::string& path, const GoDataTree& extra)
{
    PathRef ref;
    ref.path = path;
    ref.extra = extra;

    auto& item = refMap[rel];
    item.refs.push_back(ref);
}

ResponseInfoEx::RefPtrMap ResponseInfoEx::GetRefPtrMap(const RefMap& refMap) const
{
    RefPtrMap results;

    for (auto& pair : refMap)
    {
        results[pair.first] = &pair.second;
    }

    return results;
}

} // namespace
