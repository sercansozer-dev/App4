#include "ResourceRouter.h"
#include "ResourceDef.h"

namespace GoApi
{
//-----------------------------------------------------------------------------
// ResolvedResource
//-----------------------------------------------------------------------------
ResolvedResource::ResolvedResource() : resource(nullptr), hasNullKey(false)
{
}

//-----------------------------------------------------------------------------
// PathTokenTree
//-----------------------------------------------------------------------------
PathTokenTree::PathTokenTree() : resource(nullptr)
{
}

//-----------------------------------------------------------------------------
// PathToken
//-----------------------------------------------------------------------------
PathToken::PathToken(const std::string& str)
{
    // GOS-10590: Added to handle edge case of the root resource path.
    if (str == GOAPI_ROOT_RESOURCE_PATH)
    {
        type = PathToken::Root;
        value = str;
    }
    else if (str[0] == GOAPI_RESOURCE_PATH_KEY_PREFIX_CHAR)
    {
        type = PathToken::Key;
        value = str.substr(1);
    }
    else
    {
        type = PathToken::Name;
        value = str;
    }
}

PathToken::PathToken(Type type, const std::string& value) :
    type(type),
    value(value)
{
}

bool PathToken::operator<(const PathToken& other) const
{
    if (type < other.type)
    {
        return true;
    }
    else if (type > other.type)
    {
        return false;
    }
    else
    {
        return value < other.value;
    }
}

//-----------------------------------------------------------------------------
// ResourceNotification
//-----------------------------------------------------------------------------
ResourceNotification::ResourceNotification(GoApi::EventType eventType, const std::string& path) :
    eventType(eventType),
    path(path)
{
}

GoApi::EventType ResourceNotification::EventType() const
{
    return eventType;
}

const std::string& ResourceNotification::Path() const
{
    return path;
}

//-----------------------------------------------------------------------------
// DeregisteredResource
//-----------------------------------------------------------------------------
DeregisteredResource::DeregisteredResource(const std::string& path, bool isRecursive) :
    path(path),
    isRecursive(isRecursive)
{
}

const std::string& DeregisteredResource::Path() const
{
    return path;
}

bool DeregisteredResource::IsRecursive() const
{
    return isRecursive;
}

//-----------------------------------------------------------------------------
// ResourceRouter
//-----------------------------------------------------------------------------

void ResourceRouter::Register(const std::string& location, IResource& resource)
{
    // Split up the location string with respect to the path delimiter.
    std::vector<PathToken> tokens = TokenizePath(location);

    // Pointer to the current node of the resource path tree (starting at the root node).
    PathTokenTree* currentNodePtr = &tokenTree;

    // Traverse the resource path tree to find the provided path token nodes.
    for (auto& token : tokens)
    {
        // GOS-10590: Added to handle edge case of registering the root resource.
        if (token.type == PathToken::Root)
        {
            break;
        }

        // Look for the path resource token in the list of child nodes.
        auto found = currentNodePtr->children.find(token);

        // Could not find the path in the list of the current node's children.
        if (found == currentNodePtr->children.end())
        {
            // Create/add a new leaf node to the tree.
            currentNodePtr->children[token];
            found = currentNodePtr->children.find(token);

            // Add keyed resource to the list to indicate path is enumerable.
            if (token.type == PathToken::Key)
            {
                currentNodePtr->keyList.push_back(found);
            }
        }
        // Update the tree pointer to the current leaf node.
        currentNodePtr = &found->second;
    }

    // Make sure the resource has not been previously assigned.
    kAssert(currentNodePtr->resource == nullptr);
    currentNodePtr->resource = &resource;

    if (resource.NotificationEvent())
    {
        //Bind the callback with an extra "path" argument as context, as the resources themselves do not know their path.
        resource.NotificationEvent()->AddListener(std::bind(&ResourceRouter::OnNotificationEvent, this, std::placeholders::_1,
            std::placeholders::_2, std::placeholders::_3));
    }
}

static void DeleteToken(PathTokenTree& root,
    std::vector<PathToken>::const_iterator tokenIt,
    const std::vector<PathToken>& tokens,
    bool& endToken,
    int tokenIndex,
    bool recurseSubPaths)
{
    if (tokenIt == tokens.end())
    {
        // Set end node flag and delete flag
        endToken = true;
        return;
    }

    auto found = root.children.find(tokens[tokenIndex]);

    if (found != root.children.end())
    {
        DeleteToken(found->second, tokenIt + 1,tokens, endToken, (tokenIndex + 1), recurseSubPaths);
    }
    else
    {
        //*deleteNode = false;
        endToken = false;
        return;
    }

    // Clear all the resource and all other token from this resource
    if (endToken)
    {
        // There is two case to erase a token.
        // Case-1
        // Token should be at the end of the resource path. This is the resource actuually requested for delete
        // Case-2
        // Delete the token if there is only one child and there is no resources attached to that node.
        if ((tokenIt + 1) == tokens.end() || (found->second.resource == nullptr))
        {
            if (!recurseSubPaths)
            {
                // Just delete the resource, keep all child nodes
                auto childNode = root.children.find(tokens[tokenIndex]);
                childNode->second.resource = nullptr;
            }
            else
            {
                root.children.erase(tokens[tokenIndex]);
            }

            if (tokens[tokenIndex].type == PathToken::Key)
            {
                root.keyList.pop_back();
            }
        }

        if ((root.children.size() >= 1) || (!recurseSubPaths))
        {
            endToken = false;
        }
    }
}

void ResourceRouter::Deregister(const std::string& location, bool recurseSubPaths)
{
    std::vector<PathToken> tokens = TokenizePath(location);
    bool endToken = false;

    deregisteredEvent.Notify(DeregisteredResource(location, recurseSubPaths));

    // GOS-10590: Added to handle edge case of deregistering the root resource.
    if (!tokens.empty() && tokens.at(0).type == PathToken::Root)
    {
        tokenTree.resource = nullptr;
    }
    else if ((Resolve(location).resource != nullptr))
    {
        DeleteToken(tokenTree, tokens.begin(), tokens, endToken, 0, recurseSubPaths);
    }
}

void ResourceRouter::Clear()
{
    tokenTree.children.clear();
    tokenTree.resource = nullptr;
    tokenTree.keyList.clear();
}

static void ResolveFrom(const PathTokenTree& root,
    std::vector<std::string>::const_iterator tokenIt,
    const ResolvedResource* result,
    const std::vector<std::string>& tokens,
    std::vector<ResolvedResource>& results)
{
    if (tokenIt == tokens.end())
    {
        if (root.resource != nullptr)
        {
            results.emplace_back(*result);
            results.back().resource = root.resource;
        }

        // Try match a parametric resource using a null key
        // e.g. requesting "/tools" on "/tools/:id".
        if (root.keyList.size() == 1 && root.keyList[0]->second.resource != nullptr)
        {
            results.emplace_back(*result);
            results.back().resource = root.keyList[0]->second.resource;
            results.back().hasNullKey = true;
        }

        return;
    }

    // First try to find an exact match
    PathToken exactMatchToken(PathToken::Name, *tokenIt);

    auto foundIt = root.children.find(exactMatchToken);
    if (foundIt != root.children.end())
    {
        auto& foundTree = foundIt->second;

        ResolveFrom(foundTree, tokenIt + 1, result, tokens, results);
    }

    // All keyed resources can match any path component
    for (auto possibleKey : root.keyList)
    {
        auto& possibleKeyToken = possibleKey->first;
        auto& possibleKeyTree = possibleKey->second;

        // Branch as a possible result
        ResolvedResource nextResult = *result;

        nextResult.keys[possibleKeyToken.value] = *tokenIt;

        ResolveFrom(possibleKeyTree, tokenIt + 1, &nextResult, tokens, results);
    }
}

ResolvedResource ResourceRouter::Resolve(const std::string& path)
{
    std::vector<std::string> tokens = TokenizeConcretePath(path);
    std::vector<ResolvedResource> results;
    ResolvedResource mainResult;

    // GOS-10590: Added to handle edge case of resolving the root resource path.
    // This is done because we are using TokenizeConcretePath() instead of TokenizePath().
    // TODO: Streamline the implementation not to rely on two seperate tokenize functions
    // and to instead use the PathToken::Root type to determine the root path resource.
    if (path == GOAPI_ROOT_RESOURCE_PATH)
    {
        mainResult.resource = tokenTree.resource;
        return mainResult;
    }

    ResolveFrom(tokenTree, tokens.begin(), &mainResult, tokens, results);

    // GOS-10590: Added check for empty tokens as a workaround for ResolveFrom() returning
    // the Root resource in certain edge cases due to the changes made for this ticket.
    if (results.empty() || tokens.empty())
    {
        ResolvedResource emptyResult;
        return emptyResult;
    }

    // Find a result with the fewest number of keys as "best".
    // This is the result that is most specificly defined.
    ResolvedResource* bestResult = &results.front();

    for (auto& result : results)
    {
        if (result.keys.size() < bestResult->keys.size())
        {
            bestResult = &result;
        }
        else if (result.keys.size() == bestResult->keys.size())
        {
            // Prefer a result that doesn't have a null key, i.e. it is more specific.
            if (bestResult->hasNullKey && !result.hasNullKey)
            {
                bestResult = &result;
            }
        }
    }

    return *bestResult;
}

std::vector<PathToken> ResourceRouter::TokenizePath(const std::string& location)
{
    std::vector<PathToken> tokens;

    ParsePath(location, [&tokens](const std::string& tokenStr)
    {
        tokens.emplace_back(tokenStr);
    });

    return tokens;
}

std::vector<std::string> ResourceRouter::TokenizeConcretePath(const std::string& location)
{
    std::vector<std::string> tokens;

    ParsePath(location, [&tokens](const std::string& tokenStr)
    {
        tokens.emplace_back(tokenStr);
    });

    return tokens;
}

void ResourceRouter::ParsePath(const std::string& path, std::function<void(const std::string&)> handler)
{
    size_t start = 0;

    // GOS-10590: Workaround to handle edge case of registering the root path.
    // Bypasses parsing, passing the root path directly to the handler to return a single PathToken item.
    if (path == GOAPI_ROOT_RESOURCE_PATH)
    {
        handler(path);
    }
    else
    {
        while (start < path.size())
        {
            size_t end = path.find_first_of(GOAPI_RESOURCE_PATH_SEPARATOR, start);

            if (end == path.npos)
            {
                end = path.size();
            }

            if (end > start)
            {
                std::string tokenStr = path.substr(start, end - start);

                handler(tokenStr);
            }

            start = end + 1;
        }
    }
}

void ResourceRouter::OnNotificationEvent(IResource& resource, EventType eventType, const std::string& path)
{
    notificationEvent.Notify(ResourceNotification(eventType, path));
}

ResourceRouter::ResourceNotificationEvent& ResourceRouter::NotificationEvent()
{
    return notificationEvent;
}

ResourceRouter::ResourceDeregisteredEvent& ResourceRouter::DeregisteredEvent()
{
    return deregisteredEvent;
}

} // Namespaces
