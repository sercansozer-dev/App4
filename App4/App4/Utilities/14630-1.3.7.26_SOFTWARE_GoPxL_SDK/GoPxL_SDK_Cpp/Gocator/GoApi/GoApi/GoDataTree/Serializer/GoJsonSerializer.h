/**@file    GoJsonSerializer.h
 * Defines the GoJsonSerializer class.
 */

#ifndef GOAPI_GODATATREE_SERIALIZER_GOJSONSERIALIZER_H
#define GOAPI_GODATATREE_SERIALIZER_GOJSONSERIALIZER_H

#include <GoApi/GoDataTree/GoDataTree.h>
#include <GoApi/GoDataTree/Serializer/IGoDataTreeSerializer.h>
#include <GoApi/GoDataTree/Serializer/GoDataTreeSerializerOutputWriter.h>
#include <GoApi/GoDataTree/Serializer/GoDataTreeSerializerInputIterator.h>

#include <nlohmann/detail/conversions/to_chars.hpp>

namespace GoApi
{

constexpr k64u JSON_BUFFER_SIZE = 512;
constexpr const char STRING_NULL_TERMINATOR = '\0';
constexpr const char JSON_NULL_KEYWORD[5] = "null";
constexpr const char* JSON_INTLIKE_FLOAT_APPEND = ".0";
constexpr const char JSON_NUMBER_FRACTION_PREFIX = '.';

class GoApiClass GoJsonSerializer : public IGoDataTreeSerializer<GoJsonFormat>
{
private:
    using OutputWriter = IGoDataTreeSerializerOutputWriter;
    using InputIterator = IGoDataTreeSerializerInputIterator;

public:
    // GOS-10532: Even though deserialization of JSON data format string uses 64-bits to store
    // floating point numbers, 32-bit floating point type is still required for serialization.
    // This is because some Go::Params data structure may have declared k32f fields so these
    // fields are marked as k32f fields in the GoDataTree.
    enum class ValueType
    {
        String,
        Number,
        True,
        False,
        Null,
        Object,
        Array,
        Binary,     /// Custom helper type.
        Primitive,  /// Custom helper type.
        Number8u,   /// Custom helper type.
        Number8s,   /// Custom helper type.
        Number16u,  /// Custom helper type.
        Number16s,  /// Custom helper type.
        Number32u,  /// Custom helper type.
        Number32s,  /// Custom helper type.
        Number32f,  /// Custom helper type.
        Number64u,  /// Custom helper type.
        Number64s,  /// Custom helper type.
        Number64f,  /// Custom helper type.
    };

public:
    /**
     * GoJsonSerializer constructor.
     *
     * @param prettyPrint                       Whether to pretty print JSON output.
     */
    GoJsonSerializer(bool prettyPrint = true);

    /**
     * Serializes GoDataTree into JSON.
     *
     * @param source                            GoDataTree input.
     * @param out                               GoJsonFormat output.
     */
    void Serialize(const GoDataTree& source, GoJsonFormat& out) override;

    /**
     * Serializes GoDataTree into kStream.
     *
     * @param source                            GoDataTree input.
     * @param out                               Output stream or kSerializer to write into.
     */
    void Serialize(const GoDataTree& source, kObject out) override;

    /**
     * Deserializes GoJsonFormat into GoDataTree.
     *
     * @param source                            GoJsonFormat input.
     * @param out                               GoDataTree output.
     */
    void Deserialize(const GoJsonFormat& source, GoDataTree& out) override;

    /**
     * Deserializes std::vector<kByte> into GoDataTree.
     *
     * @param source                            std::vector<kByte> input.
     * @param out                               GoDataTree output.
     */
    void Deserialize(const std::vector<kByte>& source, GoDataTree& out);

    /**
     * Deserializes JSON stream into GoDataTree.
     *
     * @param source                            Input stream.
     * @param size                              Input size.
     * @param out                               GoDataTree output.
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

    /// Gets the GoDataTree item type as JSON ValueType.
    ValueType GetType(const GoDataTree& tree);

    /**
     * Serializes JSON. Use serializeSibling argument to indicate whether to serialize single item from within GoDataTree or the whole GoDataTree.
     *
     * @param it                                Input iterator.
     * @param writer                            Output writer.
     * @param serializeSibling                  Whether to serialize single item or item and its sibling.
     */
    void SerializeJson(GoDataTreeIterator&& it, OutputWriter& writer, bool serializeSibling = true);

    /// Serializes JSON array.
    void SerializeArray(GoDataTreeIterator& it, OutputWriter& writer);

    /// Serializes JSON object.
    void SerializeObject(GoDataTreeIterator& it, OutputWriter& writer);

    /// Serializes JSON binary object.
    void SerializeBinary(GoDataTreeIterator& it, OutputWriter& writer);

    /// Serializes k8u
    void Serialize8u(GoDataTreeIterator& it, OutputWriter& writer);

    /// Serializes k8s
    void Serialize8s(GoDataTreeIterator& it, OutputWriter& writer);

    /// Serializes k16u
    void Serialize16u(GoDataTreeIterator& it, OutputWriter& writer);

    /// Serializes k16s
    void Serialize16s(GoDataTreeIterator& it, OutputWriter& writer);

    /// Serializes k32u
    void Serialize32u(GoDataTreeIterator& it, OutputWriter& writer);

    /// Serializes k32s
    void Serialize32s(GoDataTreeIterator& it, OutputWriter& writer);

    /// Serializes k64u
    void Serialize64u(GoDataTreeIterator& it, OutputWriter& writer);

    /// Serializes k64s
    void Serialize64s(GoDataTreeIterator& it, OutputWriter& writer);

    // Serializes 32 or 64 bit float.
    template<typename T>
    void SerializeFloat(GoDataTreeIterator& it, OutputWriter& writer);

    // Serializes ieee_single_or_double 32 or 64 bit float.
    template<typename T>
    void SerializeFloat(GoDataTreeIterator& it, OutputWriter& writer, std::true_type);

    // Serializes not ieee_single_or_double 32 or 64 bit float.
    template<typename T>
    void SerializeFloat(GoDataTreeIterator& it, OutputWriter& writer, std::false_type);

    /// Serializes key name.
    void SerializeName(GoDataTreeIterator& it, OutputWriter& writer);

    /// Serializes JSON string.
    void SerializeString(OutputWriter& writer, const char* str, kSize length);

    void PrettyPrint(OutputWriter& writer);

    void WriteNewLine(OutputWriter& writer);

    void WriteTabs(OutputWriter& writer);

    ///
    /// Deserialization methods.
    ///

    /// Deserializes JSON.
    void DeserializeJson(InputIterator&& it, GoDataTree& tree);

    /// Deserializes JSON.
    void Deserialize(GoDataTree& tree, InputIterator& it, bool&& expectKeyname);

    /// Deserializes JSON object.
    void DeserializeObject(GoDataTree& tree, InputIterator& it);

    /// Deserializes JSON array.
    void DeserializeArray(GoDataTree& tree, InputIterator& it);

    /// Deserializes JSON primitive type (number, true, false, null).
    void DeserializePrimitive(GoDataTree& tree, InputIterator& it);

    /// Deserializes JSON number primitive.
    void DeserializeNumber(GoDataTree& tree, const std::string& numberString, bool isFloat, bool isUnsigned);

    /// Deserializes float string.
    void DeserializeFloat(GoDataTree& tree, const std::string& numberString);

    /// Deserializes signed integer string.
    void DeserializeSigned(GoDataTree& tree, const std::string& numberString);

    /// Deserializes unsigned integer string.
    void DeserializeUnsigned(GoDataTree& tree, const std::string& numberString);

    /// Increments the iterator and skips whitespaces and separators.
    void SkipSeparator(InputIterator& it);

    /// Gets the JSON key-value pair key.
    void GetKeyName(InputIterator& it, std::string& out);

    /// Gets the JSON key-value pair value type.
    ValueType GetValueType(InputIterator& it);

    /// Reads JSON string.
    void ReadString(InputIterator& it, std::string& out);

    /**
     * Reads JSON characters until the next separator token or a whitespace.
     *
     * @param it                                Input iterator.
     * @param type                              (out) Primitive type.
     * @param primitive                         (out) Primitive string. "true", "false", "null" or a number.
     * @param isFloat                           (out) Whether number is float. Used only if primitive type is Number.
     * @param isUnsigned                        (out) Whether number is unsigned. Used only if primitive type is Number.
     */
    void ReadPrimitive(InputIterator& it, GoJsonSerializer::ValueType& type, std::string& primitive, bool& isFloat, bool& isUnsigned);

    void TrimJsonInput(InputIterator&& inputIt, std::string& output);

    /// Limits the length of the output text to be no larger than the size of the output text buffer.
    /// If the output text length has to be reduced, a warning log is generated.
    void LimitNumBytesToOutput(kSize bufferLength, const kChar* warningText, k32s& numBytesInBufferToOutput);

private:
    bool prettyPrint;
    k32u jsonNestLevel;

    // Character separating every 3 decimal digits such as "1,000", or "1,000,000".
    kChar thousandsSeparator = STRING_NULL_TERMINATOR;
    kChar decimalPoint = STRING_NULL_TERMINATOR;
};

template<typename T>
inline void GoJsonSerializer::SerializeFloat(GoDataTreeIterator& it, OutputWriter& writer)
{
    // NaN / inf
    if (!std::isfinite((*it).Get<T>()))
    {
        writer.WriteTextArray((kByte*)JSON_NULL_KEYWORD, sizeof(JSON_NULL_KEYWORD) - 1);

        return;
    }

    // If T is an IEEE-754 single or double precision number,
    // use the Grisu2 algorithm to produce short numbers which are
    // guaranteed to round-trip, using strtof and strtod, resp.
    static constexpr bool is_ieee_single_or_double =
        (std::numeric_limits<T>::is_iec559 && std::numeric_limits<T>::digits == 24 && std::numeric_limits<T>::max_exponent == 128) ||
        (std::numeric_limits<T>::is_iec559 && std::numeric_limits<T>::digits == 53 && std::numeric_limits<T>::max_exponent == 1024);

    SerializeFloat<T>(it, writer, std::integral_constant<bool, is_ieee_single_or_double>());
}

template<typename T>
inline void GoJsonSerializer::SerializeFloat(GoDataTreeIterator& it, OutputWriter& writer, std::true_type /*is_ieee_single_or_double*/)
{
    std::vector<kChar> buffer;
    buffer.resize(JSON_BUFFER_SIZE);

    char* begin = buffer.data();
    // GOS-12709: This is a lossy call when converting k32f > string > k64f.
    // Always treating this as a k64f gets around this issue until we pivot to GoDataTree entirely in GOS-12655.
    char* end = nlohmann::detail::to_chars(begin, begin + buffer.size(), (*it).Get<k64f>());

    writer.WriteTextArray((kByte*)buffer.data(), static_cast<size_t>(end - begin));
}

template<typename T>
inline void GoJsonSerializer::SerializeFloat(GoDataTreeIterator& it, OutputWriter& writer, std::false_type /*is_ieee_single_or_double*/)
{
    static constexpr k32s numberOfDigits = std::numeric_limits<T>::max_digits10;

    std::string buffer;
    buffer.resize(JSON_BUFFER_SIZE);

    T value = (*it).Get<T>();
    k32s length = snprintf(buffer.data(), buffer.size(), "%.*g", numberOfDigits, value);

    // Negative value indicates an error.
    GoThrowMsgIf(length < 0, kERROR, "Failed to serialize \"%s\" field: %d.", (*it).Name(), length);

    // Check if buffer was large enough.
    GoThrowMsgIf(static_cast<std::size_t>(length) > buffer.size(), kERROR, "Buffer was not large enough to serialize \"%s\" field value.", (*it).Name());

    // Erase thousands separator.
    if (thousandsSeparator != STRING_NULL_TERMINATOR)
    {
        const auto end = std::remove(buffer.begin(), buffer.begin() + length, thousandsSeparator);

        std::fill(end, buffer.end(), STRING_NULL_TERMINATOR);

        GoThrowMsgIf((end - buffer.begin()) > length, kERROR, "Failed to remove thousands separator for \"%s\" field.", (*it).Name());

        length = (k32s)(end - buffer.begin());
    }

    // Convert decimal point to JSON_NUMBER_FRACTION_PREFIX.
    if (decimalPoint != STRING_NULL_TERMINATOR && decimalPoint != JSON_NUMBER_FRACTION_PREFIX)
    {
        const auto decPosition = std::find(buffer.begin(), buffer.end(), decimalPoint);

        if (decPosition != buffer.end())
        {
            *decPosition = JSON_NUMBER_FRACTION_PREFIX;
        }
    }

    // Determine if we need to append ".0".
    const bool valueIsIntLike =
        std::none_of(buffer.begin(), buffer.begin() + length + 1,
            [](char c)
            {
                return c == '.' || c == 'e';
            });

    writer.WriteTextArray((kByte*)buffer.data(), length);

    if (valueIsIntLike)
    {
        writer.WriteTextArray((kByte*)JSON_INTLIKE_FLOAT_APPEND, kStrLength(JSON_INTLIKE_FLOAT_APPEND));
    }
}

} // namespace

#endif // GOAPI_GODATATREE_SERIALIZER_GOJSONSERIALIZER_H
