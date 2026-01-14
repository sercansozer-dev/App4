#include <GoApi/GoDataTree/Serializer/GoJsonSerializer.h>
#include <kApi/Io/kStream.h>
#include <kApi/Data/kString.h>

namespace GoApi
{

constexpr k64u JSON_SERIALIZATION_TEMP_VALUE_RESERVE = 512;
constexpr k64u JSON_SERIALIZATION_INITIAL_JSON_RESERVE = 64 * 1024; // 64 KB.

// GOS-10532: Prefer to use exponential output format for floating point numbers display because of
// its compactness, but we also want avoid using exponents for small numbers, like
// 1.0 (1.0e0).
// These thresholds numbers are used to determine whether to use the %f or the %e
// format specifier. For small numbers, use %f. For larger numbers, use %e to
// reduce the amount of digits that are outputted.
// The lower limit has the same number of digits to the right of the decimal point as the
// precision used for printing floating point numbers.
// The upper limit is derived from the number of characters used for the exponent, which is 4.
//    For example: "e+00"
// So a number with more than 4 digits to the right of the decimal place should use exponents
// in the output.
constexpr k32f JSON_SERIALIZATION_32_BIT_FLOAT_USE_EXPONENT_OUTPUT_LOWER_THRESHOLD_VALUE = 0.000000001F;        // k32F_DIGITS to the right of the decimal place.
constexpr k32f JSON_SERIALIZATION_32_BIT_FLOAT_USE_EXPONENT_OUTPUT_UPPER_THRESHOLD_VALUE = 100000.0F;
constexpr k64f JSON_SERIALIZATION_64_BIT_FLOAT_USE_EXPONENT_OUTPUT_LOWER_THRESHOLD_VALUE = 0.00000000000000001; // k64F_DIGITS to the right of the decimal place.
constexpr k64f JSON_SERIALIZATION_64_BIT_FLOAT_USE_EXPONENT_OUTPUT_UPPER_THRESHOLD_VALUE = 100000.0;

// GOS-10532: Number of decimal places to output if using exponential output representation
// for a floating point number. The number of decimal places must satisfy the criteria
// that:
//   a. If an IEEE 754 single-precision number is converted to a decimal string with at least 9 significant digits,
//      and then converted back to single-precision representation, the final result must match the original number.
//        - from https://en.wikipedia.org/wiki/Single-precision_floating-point_format
//   b. If an IEEE 754 double-precision number is converted to a decimal string with at least 17 significant digits,
//      and then converted back to double-precision representation, the final result must match the original number
//        - from https://en.wikipedia.org/wiki/Double-precision_floating-point_format
// So for 32-bit float (single precision), specify 9 decimal digits in the output.
// For 64-bit float (double precision), specify 17 decimal digits in the output.
constexpr k32u JSON_SERIALIZATION_32_BIT_FLOAT_PRECISION = k32F_DIGITS;
constexpr k32u JSON_SERIALIZATION_64_BIT_FLOAT_PRECISION = k64F_DIGITS;

constexpr const char JSON_NEW_LINE = '\n';
constexpr const char JSON_TAB = '\t';

constexpr const char JSON_VALUE_SEPARATOR = ',';

constexpr const char JSON_NUMBER_EXP_PREFIX = 'e';
constexpr const char JSON_NUMBER_PLUS_SIGN = '+';
constexpr const char JSON_NUMBER_MINUS_SIGN = '-';
constexpr const char JSON_NUMBER_NEGATIVE_PREFIX = '-';

constexpr const char JSON_ARRAY_PREFIX = '[';
constexpr const char JSON_ARRAY_POSTFIX = ']';

constexpr const char JSON_OBJECT_PREFIX = '{';
constexpr const char JSON_OBJECT_POSTFIX = '}';

constexpr const char JSON_KEY_VALUE_SEPARATOR = ':';

constexpr const char JSON_STRING_PREFIX = '"';
constexpr const char JSON_STRING_POSTFIX = '"';

constexpr const char JSON_ESCAPE_CHARACTER = '\\';

constexpr const char JSON_TRUE_KEYWORD[5] = "true";
constexpr const char JSON_FALSE_KEYWORD[6] = "false";

GoJsonSerializer::GoJsonSerializer(bool prettyPrint) :
    prettyPrint(prettyPrint),
    jsonNestLevel(0)
{ 
    std::lconv* locale = std::localeconv();

    thousandsSeparator = locale->thousands_sep == nullptr ? STRING_NULL_TERMINATOR : std::char_traits<char>::to_char_type(*(locale->thousands_sep));
    decimalPoint = locale->decimal_point == nullptr ? STRING_NULL_TERMINATOR : std::char_traits<char>::to_char_type(*(locale->decimal_point));
}

void GoJsonSerializer::Serialize(const GoDataTree& source, GoJsonFormat& out)
{
    GoDataTreeSerializerOutputContainerWriter writer(&out);

    // TODO: Calculate approx. size of the data tree.
    out.reserve(JSON_SERIALIZATION_INITIAL_JSON_RESERVE);
    
    SerializeJson(source.Iterator(), writer, source.IsRoot());
}

void GoJsonSerializer::Serialize(const GoDataTree& source, kObject out)
{
    if (kType_Is(kObject_Type(out), kTypeOf(kSerializer)))
    {
        GoDataTreeSerializerOutputWriter writer(out);

        SerializeJson(source.Iterator(), writer, source.IsRoot());
    }
    else if (kType_Is(kObject_Type(out), kTypeOf(kStream)))
    {
        GoDataTreeSerializerOutputStreamWriter writer(out);

        SerializeJson(source.Iterator(), writer, source.IsRoot());

        GoTest(kStream_Flush(out));
    }
    else
    {
        GoThrowMsg(kERROR_PARAMETER, "Expected kStream or kSerializer, got %s.", kType_Name(kObject_Type(out)));
    }
}

void GoJsonSerializer::Deserialize(const GoJsonFormat& source, GoDataTree& out)
{
    std::string trimmedSource;

    TrimJsonInput(GoDataTreeSerializerInputContainerIterator(source.begin(), source.end()), trimmedSource);

    DeserializeJson(GoDataTreeSerializerInputContainerIterator(trimmedSource.begin(), trimmedSource.end()), out);
}

void GoJsonSerializer::Deserialize(const std::vector<kByte>& source, GoDataTree& out)
{
    std::string trimmedSource;

    TrimJsonInput(GoDataTreeSerializerInputContainerIterator(source.begin(), source.end()), trimmedSource);

    DeserializeJson(GoDataTreeSerializerInputContainerIterator(trimmedSource.begin(), trimmedSource.end()), out);
}

void GoJsonSerializer::Deserialize(kStream source, kSize size, GoDataTree& out)
{
    std::string trimmedSource;

    TrimJsonInput(GoDataTreeSerializerInputStreamIterator(source, size), trimmedSource);

    DeserializeJson(GoDataTreeSerializerInputContainerIterator(trimmedSource.begin(), trimmedSource.end()), out);
}

kSize GoJsonSerializer::SerializedOutputSize(const GoDataTree& source)
{
    GoDataTreeSerializerOutputCounter writer;

    SerializeJson(source.Iterator(), writer);

    return writer.Count();
}

GoJsonSerializer::ValueType GoJsonSerializer::GetType(const GoDataTree& tree)
{
    // Is an empty object.
    if (tree.IsRoot() && tree.Count() == 0)
    {
        return ValueType::Object;
    }
    if (tree.IsNull())
    {
        return ValueType::Null;
    }
    if (tree.IsBoolean())
    {
        return tree.Get<bool>() ? ValueType::True : ValueType::False;
    }
    else if (tree.Is8u())
    {
        return ValueType::Number8u;
    }
    else if (tree.Is8s())
    {
        return ValueType::Number8s;
    }
    else if (tree.Is16u())
    {
        return ValueType::Number16u;
    }
    else if (tree.Is16s())
    {
        return ValueType::Number16s;
    }
    else if (tree.Is32u())
    {
        return ValueType::Number32u;
    }
    else if (tree.Is32s())
    {
        return ValueType::Number32s;
    }
    else if (tree.Is32f())
    {
        return ValueType::Number32f;
    }
    else if (tree.Is64u())
    {
        return ValueType::Number64u;
    }
    else if (tree.Is64s())
    {
        return ValueType::Number64s;
    }
    else if (tree.Is64f())
    {
        return ValueType::Number64f;
    }
    else if (tree.IsString())
    {
        return ValueType::String;
    }
    else if (tree.IsArray())
    {
        return ValueType::Array;
    }
    else if (tree.IsObject())
    {
        return ValueType::Object;
    }
    else if (tree.IsBinary())
    {
        return ValueType::Binary;
    }

    GoThrow(kERROR_FORMAT);
}

void GoJsonSerializer::SerializeJson(GoDataTreeIterator&& it, OutputWriter& writer, bool serializeSibling)
{
    GoDataTreeIterator siblingIt = it;

    while (siblingIt)
    {
        // When serializing single item (serializeSibling=false), do not serialize its name. For example, for given tree:
        //     GoDataTree tree;
        //     tree["a"] = "A";
        //     tree["b"] = "B";
        // output of tree["b"] JSON serialization should be:
        //     "B" instead of { "b" : "B" }
        if (serializeSibling)
        {
            SerializeName(siblingIt, writer);
        }

        switch (GetType((*siblingIt)))
        {
            case ValueType::Null:
            {
                writer.WriteTextArray((kByte*)JSON_NULL_KEYWORD, sizeof(JSON_NULL_KEYWORD) - 1);

                break;
            }
            case ValueType::True:
            {
                writer.WriteTextArray((kByte*)JSON_TRUE_KEYWORD, sizeof(JSON_TRUE_KEYWORD) - 1);

                break;
            }
            case ValueType::False:
            {
                writer.WriteTextArray((kByte*)JSON_FALSE_KEYWORD, sizeof(JSON_FALSE_KEYWORD) - 1);

                break;
            }
            case ValueType::Number8u:
            {
                Serialize8u(siblingIt, writer);

                break;
            }
            case ValueType::Number8s:
            {
                Serialize8s(siblingIt, writer);

                break;
            }
            case ValueType::Number16u:
            {
                Serialize16u(siblingIt, writer);

                break;
            }
            case ValueType::Number16s:
            {
                Serialize16s(siblingIt, writer);

                break;
            }
            case ValueType::Number32u:
            {
                Serialize32u(siblingIt, writer);

                break;
            }
            case ValueType::Number32s:
            {
                Serialize32s(siblingIt, writer);

                break;
            }
            case ValueType::Number32f:
            {
                SerializeFloat<k32f>(siblingIt, writer);

                break;
            }
            case ValueType::Number64u:
            {
                Serialize64u(siblingIt, writer);

                break;
            }
            case ValueType::Number64s:
            {
                Serialize64s(siblingIt, writer);

                break;
            }
            case ValueType::Number64f:
            {
                SerializeFloat<k64f>(siblingIt, writer);

                break;
            }
            case ValueType::String:
            {
                std::string str = (*siblingIt).Get<std::string>();

                SerializeString(writer, str.c_str(), str.size());

                break;
            }
            case ValueType::Array:
            {
                SerializeArray(siblingIt, writer);

                break;
            }
            case ValueType::Object:
            {
                SerializeObject(siblingIt, writer);

                break;
            }
            case ValueType::Binary:
            {
                SerializeBinary(siblingIt, writer);

                break;
            }
            default:
            {
                GoThrow(kERROR_FORMAT);
            }
        }

        if (!serializeSibling)
        {
            break;
        }

        if (siblingIt.HasSibling())
        {
            writer.WriteByte(JSON_VALUE_SEPARATOR);

            PrettyPrint(writer);
        }

        siblingIt = siblingIt.NextSibling();
    }
}

void GoJsonSerializer::SerializeArray(GoDataTreeIterator& it, OutputWriter& writer)
{
    writer.WriteByte(JSON_ARRAY_PREFIX);

    jsonNestLevel++;

    PrettyPrint(writer);

    SerializeJson(it.FirstChild(), writer);

    jsonNestLevel--;

    PrettyPrint(writer);

    writer.WriteByte(JSON_ARRAY_POSTFIX);
}

void GoJsonSerializer::SerializeObject(GoDataTreeIterator& it, OutputWriter& writer)
{
    writer.WriteByte(JSON_OBJECT_PREFIX);

    jsonNestLevel++;

    PrettyPrint(writer);

    SerializeJson(it.FirstChild(), writer);

    jsonNestLevel--;

    PrettyPrint(writer);

    writer.WriteByte(JSON_OBJECT_POSTFIX);
}

void GoJsonSerializer::SerializeBinary(GoDataTreeIterator& it, OutputWriter& writer)
{
    kArray1 binary = (*it).Get<kArray1>();

    Go::Object<kString> base64;
    GoTest(kString_Construct(base64.Ref(), kNULL, kNULL));
    GoTest(kBase64Encode((const kByte*)kArray1_Data(binary), kArray1_Count(binary), base64.Get()));

    SerializeString(writer, kString_Chars(base64.Get()), kString_Length(base64.Get()));
}

void GoJsonSerializer::Serialize8u(GoDataTreeIterator& it, OutputWriter& writer)
{
    kText256 buffer;

    k32s length = snprintf((char*)buffer, kCountOf(buffer), "%u", (*it).Get<k8u>());

    // Handle the case of the formatted output being longer than the buffer.
    // In that case, cap the value of length to just the number of characters excluding
    // the null terminator, which means cap the length to one less than the
    // size of the buffer.
    LimitNumBytesToOutput(kCountOf(buffer), "Serialization of unsigned byte truncated", length);

    writer.WriteTextArray((kByte*) buffer, length);
}

void GoJsonSerializer::Serialize8s(GoDataTreeIterator& it, OutputWriter& writer)
{
    kText256 buffer;

    k32s length = snprintf((char*)buffer, kCountOf(buffer), "%d", (*it).Get<k8s>());

    // Handle the case of the formatted output being longer than the buffer.
    // In that case, cap the value of length to just the number of characters excluding
    // the null terminator, which means cap the length to one less than the
    // size of the buffer.
    LimitNumBytesToOutput(kCountOf(buffer), "Serialization of signed byte truncated", length);

    writer.WriteTextArray((kByte*) buffer, length);
}

void GoJsonSerializer::Serialize16u(GoDataTreeIterator& it, OutputWriter& writer)
{
    kText256 buffer;

    k32s length = snprintf((char*)buffer, kCountOf(buffer), "%u", (*it).Get<k16u>());

    // Handle the case of the formatted output being longer than the buffer.
    // In that case, cap the value of length to just the number of characters excluding
    // the null terminator, which means cap the length to one less than the
    // size of the buffer.
    LimitNumBytesToOutput(kCountOf(buffer), "Serialization of unsigned 16-bit value truncated", length);

    writer.WriteTextArray((kByte*) buffer, length);
}

void GoJsonSerializer::Serialize16s(GoDataTreeIterator& it, OutputWriter& writer)
{
    kText256 buffer;

    k32s length = snprintf((char*)buffer, kCountOf(buffer), "%d", (*it).Get<k16s>());

    // Handle the case of the formatted output being longer than the buffer.
    // In that case, cap the value of length to just the number of characters excluding
    // the null terminator, which means cap the length to one less than the
    // size of the buffer.
    LimitNumBytesToOutput(kCountOf(buffer), "Serialization of signed 16-bit value truncated", length);

    writer.WriteTextArray((kByte*) buffer, length);
}

void GoJsonSerializer::Serialize32u(GoDataTreeIterator& it, OutputWriter& writer)
{
    kText256 buffer;

    k32s length = snprintf((char*)buffer, kCountOf(buffer), "%u", (*it).Get<k32u>());

    // Handle the case of the formatted output being longer than the buffer.
    // In that case, cap the value of length to just the number of characters excluding
    // the null terminator, which means cap the length to one less than the
    // size of the buffer.
    LimitNumBytesToOutput(kCountOf(buffer), "Serialization of unsigned 32-bit value truncated", length);

    writer.WriteTextArray((kByte*) buffer, length);
}

void GoJsonSerializer::Serialize32s(GoDataTreeIterator& it, OutputWriter& writer)
{
    kText256 buffer;

    k32s length = snprintf((char*)buffer, kCountOf(buffer), "%d", (*it).Get<k32s>());

    // Handle the case of the formatted output being longer than the buffer.
    // In that case, cap the value of length to just the number of characters excluding
    // the null terminator, which means cap the length to one less than the
    // size of the buffer.
    LimitNumBytesToOutput(kCountOf(buffer), "Serialization of signed 32-bit value truncated", length);

    writer.WriteTextArray((kByte*) buffer, length);
}

void GoJsonSerializer::Serialize64u(GoDataTreeIterator& it, OutputWriter& writer)
{
    kText256 buffer;

    k32s length = snprintf((char*)buffer, kCountOf(buffer), "%llu", (*it).Get<k64u>());

    // Handle the case of the formatted output being longer than the buffer.
    // In that case, cap the value of length to just the number of characters excluding
    // the null terminator, which means cap the length to one less than the
    // size of the buffer.
    LimitNumBytesToOutput(kCountOf(buffer), "Serialization of unsigned 64-bit value truncated", length);

    writer.WriteTextArray((kByte*) buffer, length);
}

void GoJsonSerializer::Serialize64s(GoDataTreeIterator& it, OutputWriter& writer)
{
    kText256 buffer;

    k32s length = snprintf((char*)buffer, kCountOf(buffer), "%lld", (*it).Get<k64s>());

    // Handle the case of the formatted output being longer than the buffer.
    // In that case, cap the value of length to just the number of characters excluding
    // the null terminator, which means cap the length to one less than the
    // size of the buffer.
    LimitNumBytesToOutput(kCountOf(buffer), "Serialization of signed 64-bit value truncated", length);

    writer.WriteTextArray((kByte*) buffer, length);
}

void GoJsonSerializer::SerializeName(GoDataTreeIterator& it, OutputWriter& writer)
{
    const char* name = (*it).Name();
    kSize length = kStrLength(name);

    if (kStrLength(name) > 0)
    {
        SerializeString(writer, name, length);

        writer.WriteByte(JSON_KEY_VALUE_SEPARATOR);
    }
}

void GoJsonSerializer::SerializeString(OutputWriter& writer, const char* str, kSize length)
{
    writer.WriteByte(JSON_STRING_PREFIX);

    for (kSize index = 0; index < length; index++)
    {
        // Certain characters should be replaced by an escape sequence.
        // The full list of characters are listed here https://www.technology.org/how-and-why/escape-special-characters-json-string/.
        // Some of those are optional. In order to maintain parity with nlohmann::json, solidus (/) and \u are not included as nlohmann::json didn't escape them.
        switch (str[index])
        {
        case '\b':
            writer.WriteByte('\\');
            writer.WriteByte('b');
            break;
        case '\f':
            writer.WriteByte('\\');
            writer.WriteByte('f');
            break;
        case '\n':
            writer.WriteByte('\\');
            writer.WriteByte('n');
            break;
        case '\r':
            writer.WriteByte('\\');
            writer.WriteByte('r');
            break;
        case '\t':
            writer.WriteByte('\\');
            writer.WriteByte('t');
            break;
        case '"':
        case '\\':
            writer.WriteByte('\\');
            [[fallthrough]];
        default:
            writer.WriteByte(str[index]);
        }
    }

    writer.WriteByte(JSON_STRING_POSTFIX);
}

void GoJsonSerializer::PrettyPrint(OutputWriter& writer)
{
    if (prettyPrint)
    {
        WriteNewLine(writer);
        WriteTabs(writer);
    }
}

void GoJsonSerializer::WriteNewLine(OutputWriter& writer)
{
    writer.WriteByte(JSON_NEW_LINE);
}

void GoJsonSerializer::WriteTabs(OutputWriter& writer)
{
    for (k32u i = 0; i < jsonNestLevel; i++)
    {
        writer.WriteByte(JSON_TAB);
    }
}

void GoJsonSerializer::DeserializeJson(InputIterator&& it, GoDataTree& tree)
{
    while (it)
    {
        Deserialize(tree, it, false);
    }
}

void GoJsonSerializer::Deserialize(GoDataTree& tree, InputIterator& it, bool&& expectKeyname)
{
    std::string key;

    if (expectKeyname)
    {
        GetKeyName(it, key);
    }

    GoDataTree currentTree = !expectKeyname ? tree : tree[key];

    switch (GetValueType(it))
    {
        case ValueType::Primitive:
        {
            DeserializePrimitive(currentTree, it);

            break;
        }
        case ValueType::String:
        {
            std::string str;

            ReadString(it, str);

            currentTree.Set<std::string>(str);

            break;
        }
        case ValueType::Object:
        {
            ++it;

            currentTree.MarkAsObject();
            DeserializeObject(currentTree, it);

            break;
        }
        case ValueType::Array:
        {
            ++it;

            currentTree.MarkAsArray();
            DeserializeArray(currentTree, it);

            break;
        }
        default:
        {
            GoThrow(kERROR_FORMAT);
        }
    }

    expectKeyname = true;
    SkipSeparator(it);
}

void GoJsonSerializer::DeserializeObject(GoDataTree& tree, InputIterator& it)
{
    bool foundObjectPostfix = false;

    while (it)
    {
        if (*it == JSON_OBJECT_POSTFIX)
        {
            ++it;
            foundObjectPostfix = true;
            break;
        }

        Deserialize(tree, it, true);
    }

    GoThrowIf(!foundObjectPostfix, kERROR_FORMAT);
}

void GoJsonSerializer::DeserializeArray(GoDataTree& tree, InputIterator& it)
{
    bool foundArrayPostfix = false;
    k32u index = 0;

    while (it)
    {
        if (*it == JSON_ARRAY_POSTFIX)
        {
            ++it;
            foundArrayPostfix = true;
            break;
        }

        GoDataTree child = tree[index];

        Deserialize(child, it, false);

        index++;
    }

    GoThrowIf(!foundArrayPostfix, kERROR_FORMAT);
}

void GoJsonSerializer::DeserializePrimitive(GoDataTree& tree, InputIterator& it)
{
    ValueType primitiveType;

    bool isFloat = false;
    bool isUnsigned = true;
    std::string primitive;

    ReadPrimitive(it, primitiveType, primitive, isFloat, isUnsigned);

    switch (primitiveType)
    {
        case ValueType::True:
        {
            tree.Set<bool>(true);

            break;
        }
        case ValueType::False:
        {
            tree.Set<bool>(false);

            break;
        }
        case ValueType::Null:
        {
            tree.Set(nullptr);

            break;
        }
        case ValueType::Number:
        {
            DeserializeNumber(tree, primitive, isFloat, isUnsigned);

            break;
        }
        default:
        {
            GoThrow(kERROR_PARAMETER);
        }
    }
}

void GoJsonSerializer::DeserializeNumber(GoDataTree& tree, const std::string& numberString, bool isFloat, bool isUnsigned)
{
    if (isFloat)
    {
        DeserializeFloat(tree, numberString);
    }
    else if (isUnsigned)
    {
        DeserializeUnsigned(tree, numberString);
    }
    else
    {
        DeserializeSigned(tree, numberString);
    }
}

void GoJsonSerializer::DeserializeFloat(GoDataTree& tree, const std::string& numberString)
{
    long double number = std::stold(numberString);

    // GOS-10532: Avoid losing precision by always representing a floating point number with the
    // biggest numeric type possible, which in this case is 64-bits.
    // Don't use 32-bits for represent a floating point number as this can cause a
    // loss of precision compared to Nlohmann JSON, for exmaple, which always uses 64-bits
    // for all its floating point numbers.
    // For example, the bit patterns and most accurate representations for the value of
    // k32F_MAX (3.402823466e+38) are:
    //   32-bit: 0x7F7F FFFF           = 3.40282346638528859811704183485E38
    //   64-bit: 0x47EF FFFF DFF0 7036 = 3.40282346600000016151267322115E38
    // Representing this number as a 32-bit floating point number is different from a
    // 64-bit floating point number.
    tree.Set<k64f>((k64f)number);
}

void GoJsonSerializer::DeserializeSigned(GoDataTree& tree, const std::string& numberString)
{
    k64s number = std::stoll(numberString);

    if (number >= k8S_MIN && number <= k8S_MAX)
    {
        tree.Set<k8s>((k8s)number);
    }
    else if (number >= k16S_MIN && number <= k16S_MAX)
    {
        tree.Set<k16s>((k16s)number);
    }
    else if (number >= k32S_MIN && number <= k32S_MAX)
    {
        tree.Set<k32s>((k32s)number);
    }
    else
    {
        tree.Set<k64s>(number);
    }
}

void GoJsonSerializer::DeserializeUnsigned(GoDataTree& tree, const std::string& numberString)
{
    k64u number = std::stoull(numberString);

    if (number <= k8U_MAX)
    {
        tree.Set<k8u>((k8u)number);
    }
    else if (number <= k16U_MAX)
    {
        tree.Set<k16u>((k16u)number);
    }
    else if (number <= k32U_MAX)
    {
        tree.Set<k32u>((k32u)number);
    }
    else
    {
        tree.Set<k64u>(number);
    }
}

void GoJsonSerializer::SkipSeparator(InputIterator& it)
{
    if (it && *it == JSON_VALUE_SEPARATOR)
    {
        ++it;
    }
}

void GoJsonSerializer::GetKeyName(InputIterator& it, std::string& out)
{
    ReadString(it, out);

    if (*it == JSON_KEY_VALUE_SEPARATOR)
    {
        ++it;
    }
    else
    {
        GoThrow(kERROR_FORMAT);
    }
}

GoJsonSerializer::ValueType GoJsonSerializer::GetValueType(InputIterator& it)
{
    if (*it == JSON_ARRAY_PREFIX)
    {
        return ValueType::Array;
    }

    if (*it == JSON_OBJECT_PREFIX)
    {
        return ValueType::Object;
    }

    if (*it == JSON_STRING_PREFIX)
    {
        return ValueType::String;
    }

    return ValueType::Primitive;
}

void GoJsonSerializer::ReadString(InputIterator& it, std::string& out)
{
    GoThrowIf((it && *it != JSON_STRING_PREFIX) || !(it), kERROR_FORMAT);

    // Skip iterator's JSON_STRING_PREFIX.
    ++it;

    // Handle JSON string escape sequences. Example:
    // For { "description": "These are \"string\" \"escape\" \"sequences\"" } JSON,
    // Input string will be { \"description\": \"These are \\\"string\\\" \\\"escape\\\" \\\"sequences\\\"\" }.
    while (it)
    {
        kSize escapeCharPos = it.Find(JSON_ESCAPE_CHARACTER);
        kSize postfixCharPos = it.Find(JSON_STRING_POSTFIX);

        if (escapeCharPos < postfixCharPos)
        {
            // String contains an escaped character.

            // First read up to the escape character.
            out.resize(out.size() + escapeCharPos);
            it.ReadBytes((kByte*)&out[out.size() - escapeCharPos], escapeCharPos);

            // Skip the escape character.
            ++it;

            // Parse the escaped character into its actual character and write it into the output.
            switch (*it)
            {
            case 'b': // Backspace
                out.push_back('\b');
                break;
            case 'f': // Form feed
                out.push_back('\f');
                break;
            case 'n': // Line feed
                out.push_back('\n');
                break;
            case 'r': // Carriage return
                out.push_back('\r');
                break;
            case 't': // Tab
                out.push_back('\t');
                break;
            case '"': // Quote
            case '\\': // Slash
                // For quote and slash, use the original character with the escape character removed.
            default: // Unused escape character. Just ignore it.
                out.push_back(*it);
            }

            // Skip the parsed escaped character.
            ++it;
        }
        else if (postfixCharPos < escapeCharPos)
        {
            // The rest of the string does not contain any escaped characters.

            // Read the rest of the string into the buffer.
            out.resize(out.size() + postfixCharPos);
            it.ReadBytes((kByte*)&out[out.size() - postfixCharPos], postfixCharPos);

            // Skip the JSON_STRING_POSTFIX.
            ++it;

            // Done parsing the string.
            break;
        }
        else // This can occur if both are kSIZE_NULL.
        {
            GoThrowMsg(kERROR_FORMAT, bepgettext("sensor", "Error parsing string: missing string postfix '%c'."), JSON_STRING_POSTFIX);
        }
    }
}

void GoJsonSerializer::ReadPrimitive(InputIterator& it, GoJsonSerializer::ValueType& type, std::string& primitive, bool& isFloat, bool& isUnsigned)
{
    kSize primitiveLength = it.FindIf([](const kByte & byte) { return byte == JSON_VALUE_SEPARATOR || byte == JSON_ARRAY_POSTFIX || byte == JSON_OBJECT_POSTFIX; });

    GoThrowIf(primitiveLength == kSIZE_NULL, kERROR_FORMAT);

    primitive.resize(primitiveLength);

    it.ReadBytes((kByte*)primitive.data(), primitiveLength);

    if (primitive == JSON_NULL_KEYWORD)
    {
        type = ValueType::Null;

        return;
    }
    else if (primitive == JSON_TRUE_KEYWORD)
    {
        type = ValueType::True;

        return;
    }
    else if (primitive == JSON_FALSE_KEYWORD)
    {
        type = ValueType::False;

        return;
    }

    // Assume primitive is a number and validate.
    char previousCharacter = 0;
    for (char c : primitive)
    {
        bool isDigit = isdigit(c);
        bool isFractionPrefix = c == JSON_NUMBER_FRACTION_PREFIX;
        bool isExpPrefix =
            tolower(c) == JSON_NUMBER_EXP_PREFIX ||
            (tolower(previousCharacter) == JSON_NUMBER_EXP_PREFIX && (c == JSON_NUMBER_PLUS_SIGN || c == JSON_NUMBER_MINUS_SIGN));
        bool isNegative = c == JSON_NUMBER_NEGATIVE_PREFIX;

        GoThrowIf(!(isDigit || isFractionPrefix || isExpPrefix || isNegative), kERROR_FORMAT);

        // GOS-10532: If not parsing the exponent, check if the sign of the number is consistent.
        // If parsing the exponent, the sign check for the number is not applicable because a positive number
        // can have a negative exponent and vice versa.
        GoThrowIf(((!isExpPrefix && (!isUnsigned && isNegative)) || (isFloat && isFractionPrefix)), kERROR_FORMAT);

        // GOS-10532: The flag "isUnsigned" is used to indicate the sign of the number and not the exponent.
        // Update "isUnsigned" only for negative numbers, but not for negative exponents.
        // For exponents, don't change "isUnsigned".
        if (isNegative && (!isExpPrefix))
        {
            isUnsigned = false;
        }

        if (isFractionPrefix)
        {
            isFloat = true;
        }

        previousCharacter = c;
    }

    type = ValueType::Number;
}

void GoJsonSerializer::TrimJsonInput(InputIterator&& inputIt, std::string& output)
{
    output.reserve(inputIt.Size());

    bool isString = false;

    for (kSize i = 0; i < inputIt.Size(); i++)
    {
        if (*inputIt == JSON_ESCAPE_CHARACTER)
        {
            // Skip the escaped character even if it's JSON_STRING_PREFIX or a space.
            output.push_back(*inputIt);
            ++inputIt;

            i++;
            if (i == inputIt.Size())
            {
                break;
            }

            output.push_back(*inputIt);
            ++inputIt;

            continue;
        }
        else if (*inputIt == JSON_STRING_PREFIX)
        {
            isString = !isString;
        }

        if (!isString && isspace(*inputIt))
        {
            ++inputIt;

            continue;
        }

        output.push_back(*inputIt);

        ++inputIt;
    }
}

void GoJsonSerializer::LimitNumBytesToOutput(kSize bufferLength, const kChar* warningText, k32s& numBytesInBufferToOutput)
{
    if (numBytesInBufferToOutput < 0)
    {
        // Error. Nothing to output.
        numBytesInBufferToOutput = 0;
    }
    else if (numBytesInBufferToOutput >= (k32s) bufferLength)
    {
        GoLogWarn(warningText);

        numBytesInBufferToOutput = (k32s)(bufferLength - 1);
    }

}

}  // namespace GoApi