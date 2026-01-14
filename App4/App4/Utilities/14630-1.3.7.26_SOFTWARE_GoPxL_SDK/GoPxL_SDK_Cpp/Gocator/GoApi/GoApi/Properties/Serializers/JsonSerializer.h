/**
* @file     JsonSerializer.h
* @brief    Declares the JsonSerializer class.
*/

#ifndef GOAPI_PROPERTIES_JSON_SERIALIZER_H
#define GOAPI_PROPERTIES_JSON_SERIALIZER_H

#include <GoApi/Properties/Nodes.h>
#include <nlohmann/json.hpp>

namespace Go
{
namespace Properties
{

/**
* @class   JsonSerializer
*/
class GoApiClass JsonSerializer
{
public:
    /**
     * Convert node's data into a json object.
     */
    nlohmann::json Serialize(const Node& node);

    /**
     * Replace node's data with json's data.
     */
    void Deserialize(Node& node, const nlohmann::json& json);

    /**
     * Check that the data in json satisfies the validation criteria in node. Throws on failure.
     */
    void ValidateJson(const Node& node, const nlohmann::json& json);

    nlohmann::json SerializeSchema(const Node& node);

    /**
     * Binary can arrive in a couple of different formats. Decipher and convert into a ByteVector.
     * Expose for use cases where full deserialization isn't necessary.
     */
    ByteVector DecipherBinaryJson(const nlohmann::json& binary);

private:
    // Copy of GcBase64_Decode. Remove once all requests are using GoDataTree.
    kStatus Base64Decode(const kChar* encoded, kArrayList* output, kAlloc alloc);
};

}
} //Namespaces

#endif
