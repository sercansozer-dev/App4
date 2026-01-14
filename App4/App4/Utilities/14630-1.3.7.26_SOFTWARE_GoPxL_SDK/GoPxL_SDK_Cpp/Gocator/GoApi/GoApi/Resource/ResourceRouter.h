/**@file    ResourceRouter.h
 * Resource Router class
 */

#ifndef GOAPI_RESOURCE_ROUTER_H
#define GOAPI_RESOURCE_ROUTER_H

#include <GoApi/GoApiDef.h>
#include <GoApi/Resource/IResource.h>
#include <string>
#include <vector>
#include <map>
#include <set>
#include <regex>

namespace GoApi
{

/**
 * @struct ResolvedResource
 * @brief Represents a resource whose path has been resolved.
 * @ingroup GoApi-Resources
 */
struct GoApiClass ResolvedResource
{
    ResolvedResource();

    IResource* resource;

    /*
     * ResourceKeys used to identify specific resources in collections.
     */
    ResourceKeys keys;

    /*
     * The last key in the path is not specified.
     */
    bool hasNullKey;
};

/**
 * @struct PathToken
 * @brief Represents a resource path token.
 * @ingroup GoApi-Resources
 */
struct GoApiClass PathToken
{
    enum Type
    {
        Name, /**< Indicate token contains a resource path name.*/
        Key,  /**< Indicates a keyed resource that can be enumerated with numerical IDs.*/
        Root  /**< Special case used to indicate token is the Root resource.*/
    };

    PathToken(const std::string& str);
    PathToken(Type type, const std::string& value);

    Type type;
    std::string value;

    bool operator<(const PathToken& other) const;
};

/**
 * @struct PathTokenTree
 * @brief Represents a resource path token tree.
 * @ingroup GoApi-Resources
 */
struct GoApiClass PathTokenTree
{
    using TokenMap = std::map<PathToken, PathTokenTree>;

    PathTokenTree();

    TokenMap children;
    IResource* resource;

    std::vector<TokenMap::const_iterator> keyList;
};

/**
 * @class ResourceNotification
 * @brief Represents an event notification due to changes in resources.
 * @ingroup GoApi-Resources
 */
class GoApiClass ResourceNotification
{
public:
    ResourceNotification(GoApi::EventType eventType, const std::string& path);

    GoApi::EventType EventType() const;
    const std::string& Path() const;

private:
    GoApi::EventType eventType;
    std::string path;
};

// GOS-7650: This is passed through event notifications up to AsyncChannel so it can
// close any streams associated with the deregistered path.
/**
 * @class DeregisteredResource
 * @brief Represents a resource path that is being deregistered.
 * @ingroup GoApi-Resources
 */
class GoApiClass DeregisteredResource
{
public:
    DeregisteredResource(const std::string& path, bool isRecursive);

    /**
     * Returns the path that is being deregistered.
     *
     * @return                  The path being deregistered
     */
    const std::string& Path() const;

    /**
     * Returns true if the sub-paths of the main path are also
     * being deregistered. Returns false if only the base path
     * is being deregistered.
     *
     * @return                  Whether or not the sub-paths are being recursively deregistered as well.
     */
    bool IsRecursive() const;

private:
    std::string path;
    bool isRecursive;
};

/*! @class ResourceRouter ResourceRouter.h
 *  @brief ResourceRouter Class used to register and resolve resources to paths
 *  @ingroup GoApi-Resources
 */
class GoApiClass ResourceRouter
{
public:
    using ResourceNotificationEvent = Go::LockedEvent<ResourceNotification>;  // Resource Notification
    using ResourceDeregisteredEvent = Go::Event<DeregisteredResource>;

    /**
     * Register a resource path to a corresponding resource
     *
     * @param location          String of the path to resolve to the resource
     * @param provider          IResource object that will be the resource that is registered to the path
     */
    void Register(const std::string& location, IResource& provider);

    /**
     * Deregister a resource path to a corresponding resource.
     *
     * @param location          String of the path to resolve to the resource.
     * @param recurseSubPaths   False to only consider specific path; true to
     *                          recursively deregister all subpaths.
     */
    void Deregister(const std::string& location, bool recurseSubPaths = false);

    /**
     * Clear the resources registered
     */
    void Clear();

    /**
     * Resolve the path to get the corresponding resource
     *
     * @param path String containing the path of the resource to resolve to
     * @return A ResolvedResource object
     */
    ResolvedResource Resolve(const std::string& path);


    /**
     * This allows observers to listen for resource router notifications.
     *
     * @return              Notification event source of the resource router.
     *
     * @remark              The events from all registered
     *                      resources witin the system are available
     *                      from this one event source.
     */
    ResourceNotificationEvent& NotificationEvent();

    /**
     * This allows observers to listen for when a resource is
     * deregistered from the router.
     *
     * @return              Deregistered event source of the resource router.
     *
     * @remark              The event includes the deregistered path and
     *                      whether or not the deregister is recursive.
     */
    ResourceDeregisteredEvent& DeregisteredEvent();

private:
    PathTokenTree tokenTree;

    std::vector<PathToken> TokenizePath(const std::string& location);
    std::vector<std::string> TokenizeConcretePath(const std::string& location);
    ResourceNotificationEvent notificationEvent;
    ResourceDeregisteredEvent deregisteredEvent;
    size_t resourceSequence = 0;

    void ParsePath(const std::string& path, std::function<void(const std::string&)> handler);

    /**
     * Internally raise tool notification events.
     *
     * @param resource      Notification resource.
     * @param eventType     Notififation event type.
     * @param path          Notification path.
     */
    void OnNotificationEvent(IResource& resource, GoApi::EventType eventType, const std::string& path);
};

} // Namespace

#endif
