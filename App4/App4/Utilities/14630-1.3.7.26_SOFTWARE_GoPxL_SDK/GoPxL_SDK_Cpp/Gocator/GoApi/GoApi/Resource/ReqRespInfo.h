/**\file    ReqRespInfo.h
 */

#ifndef GOAPI_REQ_RESP_INFO_H
#define GOAPI_REQ_RESP_INFO_H

#include <GoApi/GoApiDef.h>
#include <GoApi/GoDataTree/GoDataTree.h>

#include <map>
#include <nlohmann/json.hpp>

using GoDataTree = GoApi::GoDataTree;

namespace GoApi
{

using ResourceKeys = std::map<std::string, std::string>;

/**
 * Internal struct representing data carrying vector of bytes and offset and size information.
 * This structure is used to represent a data buffer as well as a subsection within the data buffer.
 * The subsection can be accessed with an offset and a size.
 * It is assumed the subsection starts with certain offset bytes, and ends within the data buffer.
 */
struct DataBuffer
{
private:
    // This is the data buffer.
    std::shared_ptr<std::vector<kByte>> data;

    // This is the address difference between the start of the data buffer and the subsection of the buffer.
    size_t offset;

    // This is the size for the subsection of the data.
    size_t size;

public:
    // Constructors
    DataBuffer() :
        data(nullptr),
        offset(0),
        size(0)
    {
    }

    DataBuffer(std::shared_ptr<std::vector<kByte>> data) :
        data(data),
        offset(0),
        size(0)
    {
    }

    DataBuffer(std::shared_ptr<std::vector<kByte>> data, size_t offset, size_t size) :
        data(data),
        offset(offset),
        size(size)
    {
    }

    /**
     * Checks whether data contains any valid buffer object.
     *
     * @return              Whether data buffer is valid.
     */
    bool HasData()
    {
        return true;
    }

    /**
     * Gets the original data buffer.
     *
     * @return              The data buffer.
     */
    std::shared_ptr<std::vector<kByte>> GetData()
    {
        return data;
    }

    /**
     * Gets the address for the subsection of the data buffer.
     *
     * @return              The address for the subsection.
     */
    kByte* GetSubData()
    {
        if (data)
        {
            return data->data() + offset;
        }
        else
        {
            return nullptr;
        }
    }

    /**
     * Gets the size in bytes of the subsection of the data buffer.
     *
     * @return              The size in byte.
     */
    size_t GetSubDataSize()
    {
        if (data)
        {
            return size;
        }
        else
        {
            return 0;
        }
    }
};

/**
* @struct RequestInfo
*
* @brief Represents informations that is passed when performing a request
* @ingroup GoApi-Resources
*/
class GoApiClass RequestInfo
{
public:
    /**
     * Resource keys used to get collection ids.
     */
    ResourceKeys keys;

    /**
     * Optional JSON arguments used in the request.
     */
    nlohmann::json args;

    /**
     * Boolean describing if schema should be included.
     * Only used for Read.
     */
    bool includeSchema;

    /**
     * Optional data container used to carry the binary data when in use.
     */
    DataBuffer msgPackData;

    /**
     * RequestInfo constructor for an empty request.
     */
    RequestInfo();

    /**
     * RequestInfo constructor with keys.
     *
     * @param keys              ResourceKeys.
     */
    RequestInfo(const ResourceKeys& keys);

    /**
     * RequestInfo constructor with keys and JSON args.
     *
     * @param keys              ResourceKeys.
     * @param args              JSON arguments to be used.
     */
    RequestInfo(const ResourceKeys& keys, const nlohmann::json& args);

    /**
     * RequestInfo constructor with keys and JSON args and MsgPack formatted data buffer for memory efficiency.
     *
     * @param keys              ResourceKeys.
     * @param args              JSON arguments to be used.
     * @param msgPackData       Optional MsgPack formatted data used to carry the binary data when in use.
     */
    RequestInfo(const ResourceKeys& keys, const nlohmann::json& args, DataBuffer msgPackData);

    /**
     * RequestInfo constructor with keys and JSON args and includeSchema flag for reading.
     *
     * @param keys              ResourceKeys.
     * @param args              JSON arguments to be used.
     * @param includeSchema     includeSchema flag.
     */
    RequestInfo(const ResourceKeys& keys, const nlohmann::json& args, bool includeSchema);

    /**
     * Returns the value of a key.
     *
     * @param   id      Id of the key.
     *
     * @return          Pointer to the value. nullptr if key is not found.
     */
    const std::string* FindKey(const std::string& id) const;
};

class GoApiClass RequestInfoEx
{
public:
    /**
     * Resource keys used to get collection ids.
     */
    ResourceKeys keys;

    /**
     * Optional GoDataTree arguments used in the request.
     */
    GoDataTree args;

    /**
     * Boolean describing if schema should be included.
     * Only used for Read.
     */
    bool includeSchema;

    /**
     * RequestInfoEx constructor for an empty request.
     */
    RequestInfoEx();

    /**
     * RequestInfoEx constructor with keys.
     *
     * @param keys              ResourceKeys.
     */
    RequestInfoEx(const ResourceKeys& keys);

    /**
     * RequestInfoEx constructor with keys and GoDataTree args.
     *
     * @param keys              ResourceKeys.
     * @param args              GoDataTree arguments to be used.
     */
    RequestInfoEx(const ResourceKeys& keys, const GoDataTree& args);

    /**
     * RequestInfoEx constructor with keys and GoDataTree args and includeSchema flag for reading.
     *
     * @param keys              ResourceKeys.
     * @param args              GoDataTree arguments to be used.
     * @param includeSchema     includeSchema flag.
     */
    RequestInfoEx(const ResourceKeys& keys, const GoDataTree& args, bool includeSchema);

    /**
     * Returns the value of a key.
     *
     * @param   id      Id of the key.
     *
     * @return          Pointer to the value. nullptr if key is not found.
     */
    const std::string* FindKey(const std::string& id) const;
};

/**
* Contains extra information returned by resources (other than the GoDataTree representation).
*/
class GoApiClass ResponseInfo
{
public:

    /**
    * Representations a path reference.
    */
    struct PathRef
    {
        std::string path;       /*!< The path as a string. */
        nlohmann::json extra;   /*!< Annotations for links, or extra properties for embedded items. */
    };

    /**
    * Representations a link relation as a collection.
    */
    struct RefCollection
    {
        bool forceArray = false;        /*!< Whether or not to force the relation as an array. */
        bool forceExpansion = false;    /*!< Whether or not the relation should always be expanded. */
        std::vector<PathRef> refs;      /*!< The list of references. */
    };

    using RefPtrMap = std::map<std::string, const RefCollection*>;

    /**
    * Add a link.
    *
    * Links are added to the "_links" property of the JSON representation, based on the HAL standard.
    *
    * For more information on HAL and examples, see https://tools.ietf.org/html/draft-kelly-json-hal-06.
    *
    * @param rel            The relation type of the link. If not using standard IANA types like "item", prefix with "go:", e.g. "go:toolInput".
    * @param path           The path of the link.
    * @param forceArray     Force the link relation to be an array. See @ref ForceLinkArray.
    * @param annotations    Optional annotations for the link. Properties are added under the relation object, i.e. next to "href".
    */
    void Link(const std::string& rel, const std::string& path, bool forceArray = false, const nlohmann::json& annotations = {});

    /**
    * Add an embedded resource as link.
    *
    * Embedded resources are added to the "_embedded" property of the JSON representation, based on the HAL standard.
    * The Read method can automatically expand embedded links recurisvely on demand.
    *
    * See @ref Link for more information on the HAL standard and examples.
    *
    * @param rel            The relation type of the link. Same as @ref Link.
    * @param path           The path of the link.
    * @param forceArray     Force the link relation to be an array. See @ref ForceEmbeddedArray.
    * @param extraProps     Optional properties to add to the embedded resource.
    */
    void Embed(const std::string& rel, const std::string& path, bool forceArray = false, const nlohmann::json& extraProps = {});

    /**
    * Force a link relation to be an array.
    *
    * When a link relation is forced to be an array, the resulting JSON is an array, even if there is only one item
    * In fact, an empty array is created even if no item has been added.
    *
    * @param rel        The relation type to make as array.
    */
    void ForceLinkArray(const std::string& rel);

    /**
    * Force an embedded relation to be an array.
    *
    * See @ref ForceLinkArray for more information.
    *
    * @param rel        The relation type to make as array.
    */
    void ForceEmbeddedArray(const std::string& rel);

    /**
    * Force an embedded relation to always be expanded.
    *
    * By default, an embedded resource contains only a link, and is only expanded
    * when requested by the user. This function overrides that behavior and ensures
    * the embedded resources are always expanded.
    *
    * @param rel        The relation type to expand.
    */
    void ForceEmbeddedExpansion(const std::string& rel);

    /**
    * Retrieve all added links.
    *
    * @return       A map of rel -> reference items.
    */
    RefPtrMap GetLinks() const;

    /**
    * Retrieve all added embedded items.
    *
    * @return       A map of rel -> reference items.
    */
    RefPtrMap GetEmbeddings() const;

private:
    using RefMap = std::map<std::string, RefCollection>;

    RefMap links;
    RefMap embeddings;

    void AddRef(RefMap& refMap, const std::string& rel, const std::string& path, const nlohmann::json& extra);
    RefPtrMap GetRefPtrMap(const RefMap& refMap) const;
};

class GoApiClass ResponseInfoEx
{
public:

    /**
    * Representations a path reference.
    */
    struct PathRef
    {
        std::string path;       /*!< The path as a string. */
        GoDataTree extra;       /*!< Annotations for links, or extra properties for embedded items. */
    };

    /**
    * Representations a link relation as a collection.
    */
    struct RefCollection
    {
        bool forceArray = false;        /*!< Whether or not to force the relation as an array. */
        bool forceExpansion = false;    /*!< Whether or not the relation should always be expanded. */
        std::vector<PathRef> refs;      /*!< The list of references. */
    };

    using RefPtrMap = std::map<std::string, const RefCollection*>;

    /**
    * Add a link.
    *
    * Links are added to the "_links" property of the GoDataTree representation, based on the HAL standard.
    *
    *
    * @param rel            The relation type of the link. If not using standard IANA types like "item", prefix with "go:", e.g. "go:toolInput".
    * @param path           The path of the link.
    * @param forceArray     Force the link relation to be an array. See @ref ForceLinkArray.
    * @param annotations    Optional annotations for the link. Properties are added under the relation object, i.e. next to "href".
    */
    void Link(const std::string& rel, const std::string& path, bool forceArray = false, const GoDataTree& annotations = {});

    /**
    * Add an embedded resource as link.
    *
    * Embedded resources are added to the "_embedded" property of the GoDataTree representation, based on the HAL standard.
    * The Read method can automatically expand embedded links recursively on demand.
    *
    * See @ref Link for more information on the HAL standard and examples.
    *
    * @param rel            The relation type of the link. Same as @ref Link.
    * @param path           The path of the link.
    * @param forceArray     Force the link relation to be an array. See @ref ForceEmbeddedArray.
    * @param extraProps     Optional properties to add to the embedded resource.
    */
    void Embed(const std::string& rel, const std::string& path, bool forceArray = false, const GoDataTree& extraProps = {});

    /**
    * Force a link relation to be an array.
    *
    * When a link relation is forced to be an array, the resulting GoDataTree is an array, even if there is only one item
    * In fact, an empty array is created even if no item has been added.
    *
    * @param rel        The relation type to make as array.
    */
    void ForceLinkArray(const std::string& rel);

    /**
    * Force an embedded relation to be an array.
    *
    * See @ref ForceLinkArray for more information.
    *
    * @param rel        The relation type to make as array.
    */
    void ForceEmbeddedArray(const std::string& rel);

    /**
    * Force an embedded relation to always be expanded.
    *
    * By default, an embedded resource contains only a link, and is only expanded
    * when requested by the user. This function overrides that behavior and ensures
    * the embedded resources are always expanded.
    *
    * @param rel        The relation type to expand.
    */
    void ForceEmbeddedExpansion(const std::string& rel);

    /**
    * Retrieve all added links.
    *
    * @return       A map of rel -> reference items.
    */
    RefPtrMap GetLinks() const;

    /**
    * Retrieve all added embedded items.
    *
    * @return       A map of rel -> reference items.
    */
    RefPtrMap GetEmbeddings() const;

private:
    using RefMap = std::map<std::string, RefCollection>;

    RefMap links;
    RefMap embeddings;

    void AddRef(RefMap& refMap, const std::string& rel, const std::string& path, const GoDataTree& extra);

    RefPtrMap GetRefPtrMap(const RefMap& refMap) const;
};

} // Namespace

#endif  // GOAPI_REQ_RESP_INFO_H
