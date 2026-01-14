#include "ResourceBase.h"

namespace GoApi
{

// ResourceBase methods.
IParentResource* ResourceBase::ParentProvider()
{
    return nullptr;
}

IDocumentResource* ResourceBase::DocumentProvider()
{
    return nullptr;
}

ICallableResource* ResourceBase::CallableProvider()
{
    return nullptr;
}

IStreamableResource* ResourceBase::StreamableProvider()
{
    return nullptr;
}

IResource::ResourceNotificationEvent* ResourceBase::NotificationEvent()
{
    return &notificationEvent;
}

// DocumentResourceBase methods.
IDocumentResource* DocumentResourceBase::DocumentProvider()
{
    return this;
}

void DocumentResourceBase::UpdateDocument(const RequestInfo& info, const nlohmann::json& data)
{
    throw Go::Exception(kERROR_UNIMPLEMENTED, "Tried to call unimplemented DocumentResourceBase::UpdateDocument");
}

nlohmann::json DocumentResourceBase::NodeToJson(const Go::Properties::Node& node, const RequestInfo& reqInfo)
{
    Go::Properties::JsonSerializer serializer;
    auto result = serializer.Serialize(node);

    if (reqInfo.includeSchema)
    {
        result["_schema"] = serializer.SerializeSchema(node);
    }

    return result;
}

void DocumentResourceBase::NodeToGoDataTree(const Go::Properties::Node& node, const RequestInfoEx& reqInfo, GoDataTree reply)
{
    Go::Properties::GoDataTreeSerializer serializer;

    serializer.Serialize(node, reply);

    if (reqInfo.includeSchema)
    {
        serializer.SerializeSchema(node, reply["_schema"]);
    }
}

void DocumentResourceBase::NodeFromJson(Go::Properties::Node& node, const nlohmann::json& data)
{
    Go::Properties::JsonSerializer serializer;

    serializer.Deserialize(node, data);
}

void DocumentResourceBase::NodeFromGoDataTree(Go::Properties::Node& node, const GoDataTree& data)
{
    Go::Properties::GoDataTreeSerializer serializer;

    serializer.Deserialize(node, data);
}

void DocumentResourceBase::ValidateJsonFromNode(const Go::Properties::Node& node, const nlohmann::json& data)
{
    Go::Properties::JsonSerializer serializer;

    serializer.ValidateJson(node, data);
}

void DocumentResourceBase::ValidateGoDataTreeFromNode(const Go::Properties::Node& node, const GoDataTree& data)
{
    Go::Properties::GoDataTreeSerializer serializer;

    serializer.ValidateDataTree(node, data);
}

void DocumentResourceBase::NodeFromJsonAndValidate(Go::Properties::Node& node, const nlohmann::json& data)
{
    ValidateJsonFromNode(node, data);
    NodeFromJson(node, data);
}

void DocumentResourceBase::NodeFromGoDataTreeAndValidate(Go::Properties::Node& node, const GoDataTree& data)
{
    ValidateGoDataTreeFromNode(node, data);
    NodeFromGoDataTree(node, data);
}

// CommandResourceBase methods.
ICallableResource* CommandResourceBase::CallableProvider()
{
    return this;
}

// StreamableResourceBase methods.
IStreamableResource::StreamStartEvent* StreamResourceBase::StartEvent()
{
    return &streamStartEvent;
}

} // namespace
