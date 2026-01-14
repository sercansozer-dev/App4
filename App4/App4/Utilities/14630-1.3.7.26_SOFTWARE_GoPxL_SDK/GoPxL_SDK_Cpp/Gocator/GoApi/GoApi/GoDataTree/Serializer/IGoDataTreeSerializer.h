/**@file    IGoDataTreeSerializer.h
 * Defines the IGoDataTreeSerializer class.
 */

#ifndef GOAPI_GODATATREE_SERIALIZER_IGODATATREESERIALIZER_H
#define GOAPI_GODATATREE_SERIALIZER_IGODATATREESERIALIZER_H

#include <GoApi/GoApi.h>
#include <GoApi/GoApiDef.h>

namespace GoApi
{

template <typename T>
class GoApiClass IGoDataTreeSerializer
{
public:
    /**
     * Serializes GoDataTree into T.
     *
     * @param tree                              GoDataTree input.
     * @param out                               Output T.
     */
    virtual void Serialize(const GoDataTree& tree, T& out) = 0;

    /**
     * Serializes GoDataTree into kStream.
     *
     * @param tree                              GoDataTree input.
     * @param out                               Output stream or kSerializer to write into.
     */
    virtual void Serialize(const GoDataTree& tree, kObject out) = 0;

    /**
     * Deserializes T into GoDataTree.
     *
     * @param input                             Input T.
     * @param out                               Output tree.
     */
    virtual void Deserialize(const T& input, GoDataTree& out) = 0;

    /**
     * Deserializes kStream into GoDataTree.
     *
     * @param input                             Input stream.
     * @param size                              Input size.
     * @param out                               Output tree.
     */
    virtual void Deserialize(kStream input, kSize size, GoDataTree& out) = 0;

    /**
     * Calculates serialized output size in bytes. 
     * 
     * @param input                             Input tree.
     * @return                                  Output size.
     */
    virtual kSize SerializedOutputSize(const GoDataTree& input) = 0;
};

} // namespace

#endif // GOAPI_GODATATREE_SERIALIZER_IGODATATREESERIALIZER_H
