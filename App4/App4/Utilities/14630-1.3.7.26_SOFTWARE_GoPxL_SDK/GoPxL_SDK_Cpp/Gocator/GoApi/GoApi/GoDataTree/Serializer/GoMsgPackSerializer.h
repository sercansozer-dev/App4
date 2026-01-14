/**@file    GoMsgPackSerializer.h
 * Defines the GoMsgPackSerializer class.
 */

#ifndef GOAPI_GODATATREE_SERIALIZER_GOMSGPACKSERIALIZER_H
#define GOAPI_GODATATREE_SERIALIZER_GOMSGPACKSERIALIZER_H

#include <GoApi/GoDataTree/GoDataTree.h>
#include <GoApi/GoDataTree/Serializer/IGoDataTreeSerializer.h>
#include <GoApi/GoDataTree/Serializer/GoDataTreeSerializerOutputWriter.h>
#include <GoApi/GoDataTree/Serializer/GoDataTreeSerializerInputIterator.h>

namespace GoApi
{

/* GoDataTree message pack serializer */
class GoApiClass GoMsgPackSerializer : public IGoDataTreeSerializer<GoMsgPackFormat>
{
private:
    using OutputWriter = IGoDataTreeSerializerOutputWriter;
    using InputIterator = IGoDataTreeSerializerInputIterator;

public:
    /**
     * Message pack types.
     */
    enum class Type
    {
        PositiveFixInt,
        NegativeFixInt,
        Map,           
        FixMap,
        Array,
        FixArray,
        FixStr,
        Nil,
        False,
        True,
        Bin,
        Bin8,
        Bin16,
        Bin32,
        Ext,
        Ext8,
        Ext16,
        Ext32,
        Float32,
        Float64,
        UInt8,
        UInt16,
        UInt32,
        UInt64,
        Int8,
        Int16,
        Int32,
        Int64,
        FixExt1,
        FixExt2,
        FixExt4,
        FixExt8,
        FixExt16,
        Str,
        Str8,
        Str16,
        Str32,
        Array16,
        Array32,
        Map16,
        Map32,
    };

public:
    /**
     * Serializes GoDataTree into GoMsgPackFormat.
     *
     * @param source                        GoDataTree input.
     * @param out                           GoMsgPackFormat output.
     */
    void Serialize(const GoDataTree& source, GoMsgPackFormat& out) override;

    /**
     * Serializes GoDataTree into kStream.
     *
     * @param source                        GoDataTree input.
     * @param out                           Output stream.
     */
    void Serialize(const GoDataTree& source, kObject out) override;
    
    /**
     * Deserializes GoMsgPackFormat into GoDataTree.
     *
     * @param source                        GoMsgPackFormat input.
     * @param out                           Output tree.
     */
    void Deserialize(const GoMsgPackFormat& source, GoDataTree& out) override;

    /**
     * Deserializes message pack stream into GoDataTree.
     *
     * @param source                        Message pack input stream.
     * @param size                          Input size.
     * @param out                           Output tree.
     */
    void Deserialize(kStream source, kSize size, GoDataTree& out) override;

    /**
     * Calculates serialized output size in bytes.
     *
     * @param source                            Input tree.
     * @return                                  Output size.
     */
    kSize SerializedOutputSize(const GoDataTree& source) override;

private:
    ///
    /// Serialization methods.
    ///

    // Serializes current GoDataTree item.
    void SerializeMessagePack(GoDataTreeIterator&& it, OutputWriter& writer);

    // Serializes GoDataTree object.
    void SerializeMap(GoDataTreeIterator& it, OutputWriter& writer);

    // Serializes GoDataTree array.
    void SerializeArray(GoDataTreeIterator& it, OutputWriter& writer);

    // Serializes GoDataTree binary.
    void SerializeBin(kArray1 binary, OutputWriter& writer);

    // Serializes extension. Currently unsupported.
    void SerializeExt(GoDataTreeIterator& it, OutputWriter& writer);

    // Retrieves GoDataTree item name and writes it into output.
    void SerializeName(const kChar* name, OutputWriter& writer);

    // Retrieves GoDataTree item string value and writes it into output.
    void SerializeString(const kChar* value, kSize length, OutputWriter& writer);

    ///
    /// Deserialization methods.
    ///
    
    // Gets the message pack item type.
    Type GetType(kByte byte);

    // Gets the message pack object length.
    kSize GetLength(InputIterator& it);

    // Deserializes message pack.
    void DeserializeMessagePack(InputIterator&& it, GoDataTree& tree);

    // Deserializes message pack.
    void Deserialize(InputIterator& it, GoDataTree& tree);

    // Deserializes message pack map.
    void DeserializeMap(InputIterator& it, GoDataTree& tree);

    // Deserializes message pack array.
    void DeserializeArray(InputIterator& it, GoDataTree& tree);

    // Deserializes message pack binary.
    void DeserializeBin(InputIterator& it, GoDataTree& tree);

    // Deserializes message pack extension. Currently unsupported.
    void DeserializeExt(InputIterator& it, GoDataTree& tree);
    
    // Deserializes 5-bit negative integer.
    k32s DeserializeNegativeFixInt(InputIterator& it);
    
    // Deserializes 7-bit positive integer.
    k32u DeserializePositiveFixInt(InputIterator& it);

    // Deserializes message pack string.
    void DeserializeString(InputIterator& it, std::string& out);

    template <typename T>
    constexpr kByte* ConvertToBytes(T&& value);
};

template<typename T>
inline constexpr kByte* GoMsgPackSerializer::ConvertToBytes(T&& value)
{
    return reinterpret_cast<kByte*>(&value);
}

} // namespace

#endif // GOAPI_GODATATREE_SERIALIZER_GOMSGPACKSERIALIZER_H
