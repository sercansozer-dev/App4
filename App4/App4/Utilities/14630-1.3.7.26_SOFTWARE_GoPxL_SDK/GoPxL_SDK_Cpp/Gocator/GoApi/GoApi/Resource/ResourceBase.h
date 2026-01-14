/**\file    ResourceBase.h
 * Resource Manager class
 */

#ifndef GOAPI_RESOURCE_BASE_H
#define GOAPI_RESOURCE_BASE_H

#include <GoApi/GoApiDef.h>
#include <GoApi/Properties/Nodes.h>
#include <GoApi/Properties/Serializers/JsonSerializer.h>
#include <GoApi/Properties/Serializers/GoDataTreeSerializer.h>
#include <GoApi/Resource/IResource.h>
#include <GoApi/Resource/Path.h>

namespace GoApi
{

/*! \class ResourceBase ResourceBase.h
*  \ingroup GoApi-Resources
*/
class GoApiClass ResourceBase : public IResource
{
protected:
    ResourceNotificationEvent notificationEvent;
public:
    IParentResource* ParentProvider() override;

    IDocumentResource* DocumentProvider() override;

    ICallableResource* CallableProvider() override;

    IStreamableResource* StreamableProvider() override;

    IResource::ResourceNotificationEvent* NotificationEvent() override;
};

/*! \class DocumentResourceBase ResourceBase.h
*  \ingroup GoApi-Resources
*/
class GoApiClass DocumentResourceBase : public ResourceBase, public IDocumentResource
{
public:
    IDocumentResource* DocumentProvider() override;

    virtual void UpdateDocument(const RequestInfo& info, const nlohmann::json& data);

protected:
    /**
     * Creates and returns a json object constructed from the passed node. reqInfo may include additional parameters that influence what is/isn't included.
     */
    nlohmann::json NodeToJson(const Go::Properties::Node& node, const RequestInfo& reqInfo);

    /**
     * Creates and returns a GoDataTree object constructed from the passed node. reqInfo may include additional parameters that influence what is/isn't included.
     */
    void NodeToGoDataTree(const Go::Properties::Node& node, const RequestInfoEx& reqInfo, GoDataTree reply);

    /**
     * Overwrites the data in node with the json data.
     */
    void NodeFromJson(Go::Properties::Node& node, const nlohmann::json& data);

    /**
     * Overwrites the data in node with the GoDataTree data.
     */
    void NodeFromGoDataTree(Go::Properties::Node& node, const GoDataTree& data);

    /**
     * Validates the json data using the validation criteria in node.
     */
    void ValidateJsonFromNode(const Go::Properties::Node& node, const nlohmann::json& data);

    /**
     * Validates the GoDataTree data using the validation criteria in node.
     */
    void ValidateGoDataTreeFromNode(const Go::Properties::Node& node, const GoDataTree& data);

    /**
     * Validates the json data from node then writes the data to the node if successful.
     */
    void NodeFromJsonAndValidate(Go::Properties::Node& node, const nlohmann::json& data);

    /**
     * Validates the GoDataTree data from node then writes the data to the node if successful.
     */
    void NodeFromGoDataTreeAndValidate(Go::Properties::Node& node, const GoDataTree& data);
};

/*! \class CommandResourceBase ResourceBase.h
*  \ingroup GoApi-Resources
*/
class GoApiClass CommandResourceBase : public ResourceBase, public ICallableResource
{
public:
    ICallableResource * CallableProvider() override;
};

/*! \class StreamResourceBase ResourceBase.h
*  \ingroup GoApi-Resources
*/
class GoApiClass StreamResourceBase : public IStreamableResource
{
protected:
    StreamStartEvent streamStartEvent;
public:
    StreamStartEvent* StartEvent() override;


};

} // Namespace

#endif  // GOAPI_RESOURCE_BASE_H
