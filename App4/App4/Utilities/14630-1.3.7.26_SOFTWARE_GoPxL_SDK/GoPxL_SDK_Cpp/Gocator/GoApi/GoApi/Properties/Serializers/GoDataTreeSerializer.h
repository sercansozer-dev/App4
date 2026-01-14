/**
* @file     GoDataTreeSerializer.h
* @brief    Declares the GoDataTreeSerializer class.
*/

#ifndef GOAPI_PROPERTIES_GODATATREE_SERIALIZER_H
#define GOAPI_PROPERTIES_GODATATREE_SERIALIZER_H

#include <GoApi/Properties/Nodes.h>
#include <GoApi/GoDataTree/GoDataTree.h>

using GoDataTree = GoApi::GoDataTree;

namespace Go
{
namespace Properties
{

/**
* @class   GoDataTreeSerializer
*/
class GoApiClass GoDataTreeSerializer
{
public:    
    /**
     * Serializes node data into GoDataTree.
     * 
     * @param node                      Node input.
     * @param tree                      GoDataTree output.
     */
    void Serialize(const Node& node, GoDataTree tree);

    /**
     * Deserializes GoDataTree data into a node.
     * 
     * @param node                      Node output.
     * @param tree                      GoDataTree input.
     */
    void Deserialize(Node& node, const GoDataTree tree);

    /**
     * Checks that the data in GoDataTree satisfies the validation criteria in node. Throws on failure.
     * 
     * @param node                      Node input.
     * @param tree                      GoDataTree input.
     */
    void ValidateDataTree(const Node& node, const GoDataTree tree);

    /**
     * Serializes node schema data into GoDataTree.
     * 
     * @param node                      Node input.
     * @param tree                      GoDataTree output.
     */
    void SerializeSchema(const Node& node, GoDataTree tree);

    /**
     * Binary can arrive in a couple of different formats. Decipher and convert into a ByteVector.
     * 
     * @param binary                    GoDataTree item containing binary data.
     * @return                          ByteVector containing binary data.
     */
    ByteVector DecipherBinaryJson(const GoDataTree& binary);
};

}} // namespaces

#endif // GOAPI_PROPERTIES_GODATATREE_SERIALIZER_H