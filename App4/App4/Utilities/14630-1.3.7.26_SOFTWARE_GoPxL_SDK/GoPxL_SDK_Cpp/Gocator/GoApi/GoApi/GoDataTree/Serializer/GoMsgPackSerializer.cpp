#include <GoApi/GoDataTree/Serializer/GoMsgPackSerializer.h>
#include <kApi/Io/kStream.h>

namespace GoApi
{
constexpr k64u MSGPACK_SERIALIZATION_INITIAL_MPACK_RESERVE  = 64 * 1024; // 64 KB.

// Constants taken from the msgpack documentation.
constexpr k64u MSGPACK_8_BIT_LENGTH_MAX                     = 255;
constexpr k64u MSGPACK_16_BIT_LENGTH_MAX                    = 65535;
constexpr k64u MSGPACK_32_BIT_LENGTH_MAX                    = 4294967295;

constexpr kByte MSGPACK_FORMAT_POSITIVE_FIXINT_LOW          = 0x00;
constexpr kByte MSGPACK_FORMAT_POSITIVE_FIXINT_HIGH         = 0x7f;
constexpr k64u MSGPACK_FORMAT_POSITIVE_FIXINT_VALUE_MAX     = 127;

constexpr kByte MSGPACK_FORMAT_NEGATIVE_FIXINT_LOW          = 0xe0;
constexpr kByte MSGPACK_FORMAT_NEGATIVE_FIXINT_HIGH         = 0xff;
constexpr k64s MSGPACK_FORMAT_NEGATIVE_FIXINT_VALUE_MIN     = -32;

constexpr kByte MSGPACK_FORMAT_FIXMAP_LOW                   = 0x80;
constexpr kByte MSGPACK_FORMAT_FIXMAP_HIGH                  = 0x8f;
constexpr k64u MSGPACK_FORMAT_FIXMAP_LENGTH_MAX             = MSGPACK_FORMAT_FIXMAP_HIGH - MSGPACK_FORMAT_FIXMAP_LOW;

constexpr kByte MSGPACK_FORMAT_FIXARRAY_LOW                 = 0x90;
constexpr kByte MSGPACK_FORMAT_FIXARRAY_HIGH                = 0x9f;
constexpr k64u MSGPACK_FORMAT_FIXARRAY_LENGTH_MAX           = MSGPACK_FORMAT_FIXARRAY_HIGH - MSGPACK_FORMAT_FIXARRAY_LOW;

constexpr kByte MSGPACK_FORMAT_FIXSTR_LOW                   = 0xa0;
constexpr kByte MSGPACK_FORMAT_FIXSTR_HIGH                  = 0xbf;
constexpr k64u MSGPACK_FORMAT_FIXSTR_LENGTH_MAX             = MSGPACK_FORMAT_FIXSTR_HIGH - MSGPACK_FORMAT_FIXSTR_LOW;

constexpr kByte MSGPACK_FORMAT_FIXEXT1                      = 0xd4;
constexpr k64u MSGPACK_FORMAT_FIXEXT1_LENGTH_MAX            = 1 + 1;
constexpr kByte MSGPACK_FORMAT_FIXEXT2                      = 0xd5;
constexpr k64u MSGPACK_FORMAT_FIXEXT2_LENGTH_MAX            = 2 + 1;
constexpr kByte MSGPACK_FORMAT_FIXEXT4                      = 0xd6;
constexpr k64u MSGPACK_FORMAT_FIXEXT4_LENGTH_MAX            = 4 + 1;
constexpr kByte MSGPACK_FORMAT_FIXEXT8                      = 0xd7;
constexpr k64u MSGPACK_FORMAT_FIXEXT8_LENGTH_MAX            = 8 + 1;
constexpr kByte MSGPACK_FORMAT_FIXEXT16                     = 0xd8;
constexpr k64u MSGPACK_FORMAT_FIXEXT16_LENGTH_MAX           = 16 + 1;

constexpr kByte MSGPACK_FORMAT_EXT8                         = 0xc7;
constexpr k64u MSGPACK_FORMAT_EXT8_LENGTH_MAX               = MSGPACK_8_BIT_LENGTH_MAX + 1;
constexpr kByte MSGPACK_FORMAT_EXT16                        = 0xc8;
constexpr k64u MSGPACK_FORMAT_EXT16_LENGTH_MAX              = MSGPACK_16_BIT_LENGTH_MAX + 1;
constexpr kByte MSGPACK_FORMAT_EXT32                        = 0xc9;
constexpr k64u MSGPACK_FORMAT_EXT32_LENGTH_MAX              = MSGPACK_32_BIT_LENGTH_MAX + 1;

constexpr kByte MSGPACK_FORMAT_NIL                          = 0xc0;

constexpr kByte MSGPACK_FORMAT_FALSE                        = 0xc2;
constexpr kByte MSGPACK_FORMAT_TRUE                         = 0xc3;

constexpr kByte MSGPACK_FORMAT_FLOAT32                      = 0xca;
constexpr kByte MSGPACK_FORMAT_FLOAT64                      = 0xcb;

constexpr kByte MSGPACK_FORMAT_UINT8                        = 0xcc;
constexpr kByte MSGPACK_FORMAT_UINT16                       = 0xcd;
constexpr kByte MSGPACK_FORMAT_UINT32                       = 0xce;
constexpr kByte MSGPACK_FORMAT_UINT64                       = 0xcf;

constexpr kByte MSGPACK_FORMAT_INT8                         = 0xd0;
constexpr kByte MSGPACK_FORMAT_INT16                        = 0xd1;
constexpr kByte MSGPACK_FORMAT_INT32                        = 0xd2;
constexpr kByte MSGPACK_FORMAT_INT64                        = 0xd3;

constexpr kByte MSGPACK_FORMAT_BIN8                         = 0xc4;
constexpr kByte MSGPACK_FORMAT_BIN16                        = 0xc5;
constexpr kByte MSGPACK_FORMAT_BIN32                        = 0xc6;

constexpr kByte MSGPACK_FORMAT_STR8                         = 0xd9;
constexpr kByte MSGPACK_FORMAT_STR16                        = 0xda;
constexpr kByte MSGPACK_FORMAT_STR32                        = 0xdb;

constexpr kByte MSGPACK_FORMAT_ARRAY16                      = 0xdc;
constexpr kByte MSGPACK_FORMAT_ARRAY32                      = 0xdd;

constexpr kByte MSGPACK_FORMAT_MAP16                        = 0xde;
constexpr kByte MSGPACK_FORMAT_MAP32                        = 0xdf;


void GoMsgPackSerializer::Serialize(const GoDataTree& source, GoMsgPackFormat& out)
{
    GoDataTreeSerializerOutputContainerWriter writer(&out);

    // TODO: Calculate approx. size of the data tree.
    out.reserve(MSGPACK_SERIALIZATION_INITIAL_MPACK_RESERVE);

    SerializeMessagePack(source.Iterator(), writer);
}

void GoMsgPackSerializer::Serialize(const GoDataTree& source, kObject out)
{
    if (kType_Is(kObject_Type(out), kTypeOf(kSerializer)))
    {
        GoDataTreeSerializerOutputWriter writer(out);

        SerializeMessagePack(source.Iterator(), writer);
    }
    else if (kType_Is(kObject_Type(out), kTypeOf(kStream)))
    {
        GoDataTreeSerializerOutputStreamWriter writer(out);

        SerializeMessagePack(source.Iterator(), writer);

        GoTest(kStream_Flush(out));
    }
    else
    {
        GoThrowMsg(kERROR_PARAMETER, "Expected kStream or kSerializer, got %s.", kType_Name(kObject_Type(out)));
    }
}

void GoMsgPackSerializer::Deserialize(const GoMsgPackFormat& source, GoDataTree& out)
{
    DeserializeMessagePack(GoDataTreeSerializerInputContainerIterator(source.begin(), source.end()), out);
}

void GoMsgPackSerializer::Deserialize(kStream source, kSize size, GoDataTree& out)
{
    DeserializeMessagePack(GoDataTreeSerializerInputStreamIterator(source, size), out);
}

kSize GoMsgPackSerializer::SerializedOutputSize(const GoDataTree& source)
{
    GoDataTreeSerializerOutputCounter writer;

    SerializeMessagePack(source.Iterator(), writer);

    return writer.Count();
}

void GoMsgPackSerializer::SerializeMessagePack(GoDataTreeIterator&& it, OutputWriter& writer)
{
    GoDataTreeIterator siblingIt = it;

    while (siblingIt)
    {
        GoDataTree sibling = *siblingIt;

        kType type = kNULL;
        const kChar* name = kNULL;
        void* value = sibling.GetItemNameValuePair(&name, &type);

        SerializeName(name, writer);

        if (kIsNull(type))
        {
            writer.WriteByte(MSGPACK_FORMAT_NIL);
        }
        else if (kType_Is(type, kTypeOf(kBool)))
        {
            writer.WriteByte((bool)*((kBool*)value) ? MSGPACK_FORMAT_TRUE : MSGPACK_FORMAT_FALSE);
        }
        else if (kType_Is(type, kTypeOf(k8u)))
        {
            writer.WriteByte(MSGPACK_FORMAT_UINT8);
            writer.WriteNumberBytes(ConvertToBytes(*((k8u*)value)), sizeof(k8u));
        }
        else if (kType_Is(type, kTypeOf(k8s)))
        {
            writer.WriteByte(MSGPACK_FORMAT_INT8);
            writer.WriteNumberBytes(ConvertToBytes(*((k8s*)value)), sizeof(k8s));
        }
        else if (kType_Is(type, kTypeOf(k16u)))
        {
            writer.WriteByte(MSGPACK_FORMAT_UINT16);
            writer.WriteNumberBytes(ConvertToBytes(*((k16u*)value)), sizeof(k16u));
        }
        else if (kType_Is(type, kTypeOf(k16s)))
        {
            writer.WriteByte(MSGPACK_FORMAT_INT16);
            writer.WriteNumberBytes(ConvertToBytes(*((k16s*)value)), sizeof(k16s));
        }
        else if (kType_Is(type, kTypeOf(k32u)))
        {
            if (*((k32u*)value) <= MSGPACK_FORMAT_POSITIVE_FIXINT_VALUE_MAX)
            {
                writer.WriteByte((k8u)(MSGPACK_FORMAT_POSITIVE_FIXINT_LOW + *((k32u*)value)));
            }
            else
            {
                writer.WriteByte(MSGPACK_FORMAT_UINT32);
                writer.WriteNumberBytes(ConvertToBytes(*((k32u*)value)), sizeof(k32u));
            }
        }
        else if (kType_Is(type, kTypeOf(k32s)))
        {
            if (*((k32s*)value) < 0 && *((k32s*)value) >= MSGPACK_FORMAT_NEGATIVE_FIXINT_VALUE_MIN)
            {
                writer.WriteByte((k8u)(MSGPACK_FORMAT_NEGATIVE_FIXINT_LOW | *((k32s*)value)));
            }
            else
            {
                writer.WriteByte(MSGPACK_FORMAT_INT32);
                writer.WriteNumberBytes(ConvertToBytes(*((k32s*)value)), sizeof(k32s));
            }
        }
        else if (kType_Is(type, kTypeOf(k32f)))
        {
            writer.WriteByte(MSGPACK_FORMAT_FLOAT32);
            writer.WriteNumberBytes(ConvertToBytes(*((k32f*)value)), sizeof(k32f));
        }
        else if (kType_Is(type, kTypeOf(k64u)))
        {
            writer.WriteByte(MSGPACK_FORMAT_UINT64);
            writer.WriteNumberBytes(ConvertToBytes(*((k64u*)value)), sizeof(k64u));
        }
        else if (kType_Is(type, kTypeOf(k64s)))
        {
            writer.WriteByte(MSGPACK_FORMAT_INT64);
            writer.WriteNumberBytes(ConvertToBytes(*((k64s*)value)), sizeof(k64s));
        }
        else if (kType_Is(type, kTypeOf(k64f)))
        {
            writer.WriteByte(MSGPACK_FORMAT_FLOAT64);
            writer.WriteNumberBytes(ConvertToBytes(*((k64f*)value)), sizeof(k64f));
        }
        else if (kType_Is(type, kTypeOf(kString)))
        {
            SerializeString(kString_Chars(value), kString_Length(value), writer);
        }
        else if (kType_Is(type, kTypeOf(kArray1)))
        {
            SerializeBin(value, writer);
        }
        else if (kType_Is(type, kTypeOf(GoDataTreeArray)))
        {
            SerializeArray(siblingIt, writer);
        }
        else if (kType_Is(type, kTypeOf(GoDataTreeObject)))
        {
            SerializeMap(siblingIt, writer);
        }
        else
        {
            GoThrow(kERROR_PARAMETER);
        }

        siblingIt = siblingIt.NextSibling();
    }
}

void GoMsgPackSerializer::SerializeMap(GoDataTreeIterator& it, OutputWriter& writer)
{
    kSize length = (*it).Count();

    if (length <= MSGPACK_FORMAT_FIXMAP_LENGTH_MAX)
    {
        writer.WriteByte((k8u)(MSGPACK_FORMAT_FIXMAP_LOW + length));
    }
    else if (length <= MSGPACK_16_BIT_LENGTH_MAX)
    {
        writer.WriteByte(MSGPACK_FORMAT_MAP16);
        writer.WriteNumberBytes(ConvertToBytes((k16u)length), sizeof(k16u));
    }
    else
    {
        writer.WriteByte(MSGPACK_FORMAT_MAP32);
        writer.WriteNumberBytes(ConvertToBytes((k32u)length), sizeof(k32u));
    }

    SerializeMessagePack(it.FirstChild(), writer);
}

void GoMsgPackSerializer::SerializeArray(GoDataTreeIterator& it, OutputWriter& writer)
{
    kSize length = (*it).Count();

    if (length <= MSGPACK_FORMAT_FIXARRAY_LENGTH_MAX)
    {
        writer.WriteByte((k8u)(MSGPACK_FORMAT_FIXARRAY_LOW + length));
    }
    else if (length <= MSGPACK_16_BIT_LENGTH_MAX)
    {
        writer.WriteByte(MSGPACK_FORMAT_ARRAY16);
        writer.WriteNumberBytes(ConvertToBytes((k16u)length), sizeof(k16u));
    }
    else
    {
        writer.WriteByte(MSGPACK_FORMAT_ARRAY32);
        writer.WriteNumberBytes(ConvertToBytes((k32u)length), sizeof(k32u));
    }

    SerializeMessagePack(it.FirstChild(), writer);
}

void GoMsgPackSerializer::SerializeBin(kArray1 value, OutputWriter& writer)
{
    GoThrowIf(kType_Is(kArray1_ItemType(value), kTypeOf(k8u)), kERROR_PARAMETER);

    kSize length = kArray1_Length(value);

    if (length <= MSGPACK_8_BIT_LENGTH_MAX)
    {
        writer.WriteByte(MSGPACK_FORMAT_BIN8);
        writer.WriteNumberBytes(ConvertToBytes((k8u)length), sizeof(k8u));
    }
    else if (length <= MSGPACK_16_BIT_LENGTH_MAX)
    {
        writer.WriteByte(MSGPACK_FORMAT_BIN16);
        writer.WriteNumberBytes(ConvertToBytes((k16u)length), sizeof(k16u));
    }
    else
    {
        writer.WriteByte(MSGPACK_FORMAT_BIN32);
        writer.WriteNumberBytes(ConvertToBytes((k32u)length), sizeof(k32u));
    }

    writer.WriteByteArray((kByte*)kArray1_Data(value), length);
}

void GoMsgPackSerializer::SerializeExt(GoDataTreeIterator& it, OutputWriter& writer)
{
    GoThrow(kERROR_UNIMPLEMENTED);
}

void GoMsgPackSerializer::SerializeName(const kChar* name, OutputWriter& writer)
{
    kSize length = kStrLength(name);

    if (length == 0)
    {
        return;
    }

    SerializeString(name, length, writer);
}

void GoMsgPackSerializer::SerializeString(const kChar* value, kSize length, OutputWriter& writer)
{
    if (length < MSGPACK_FORMAT_FIXSTR_LENGTH_MAX)
    {
        writer.WriteByte((k8u)(MSGPACK_FORMAT_FIXSTR_LOW + length));
    }
    else if (length < MSGPACK_8_BIT_LENGTH_MAX)
    {
        writer.WriteByte(MSGPACK_FORMAT_STR8);
        writer.WriteNumberBytes(ConvertToBytes((k8u)length), sizeof(k8u));
    }
    else if (length < MSGPACK_16_BIT_LENGTH_MAX)
    {
        writer.WriteByte(MSGPACK_FORMAT_STR16);
        writer.WriteNumberBytes(ConvertToBytes((k16u)length), sizeof(k16u));
    }
    else
    {
        writer.WriteByte(MSGPACK_FORMAT_STR32);
        writer.WriteNumberBytes(ConvertToBytes((k32u)length), sizeof(k32u));
    }

    writer.WriteTextArray((kByte*)value, length);
}

GoMsgPackSerializer::Type GoMsgPackSerializer::GetType(kByte byte)
{
    switch (byte)
    {
    case MSGPACK_FORMAT_NIL:
        return Type::Nil;
    case MSGPACK_FORMAT_FALSE:
        return Type::False;
    case MSGPACK_FORMAT_TRUE:
        return Type::True;
    case MSGPACK_FORMAT_BIN8:
        return Type::Bin8;
    case MSGPACK_FORMAT_BIN16:
        return Type::Bin16;
    case MSGPACK_FORMAT_BIN32:
        return Type::Bin32;
    case MSGPACK_FORMAT_EXT8:
        return Type::Ext8;
    case MSGPACK_FORMAT_EXT16:
        return Type::Ext16;
    case MSGPACK_FORMAT_EXT32:
        return Type::Ext32;
    case MSGPACK_FORMAT_FLOAT32:
        return Type::Float32;
    case MSGPACK_FORMAT_FLOAT64:
        return Type::Float64;
    case MSGPACK_FORMAT_UINT8:
        return Type::UInt8;
    case MSGPACK_FORMAT_UINT16:
        return Type::UInt16;
    case MSGPACK_FORMAT_UINT32:
        return Type::UInt32;
    case MSGPACK_FORMAT_UINT64:
        return Type::UInt64;
    case MSGPACK_FORMAT_INT8:
        return Type::Int8;
    case MSGPACK_FORMAT_INT16:
        return Type::Int16;
    case MSGPACK_FORMAT_INT32:
        return Type::Int32;
    case MSGPACK_FORMAT_INT64:
        return Type::Int64;
    case MSGPACK_FORMAT_FIXEXT1:
        return Type::FixExt1;
    case MSGPACK_FORMAT_FIXEXT2:
        return Type::FixExt2;
    case MSGPACK_FORMAT_FIXEXT4:
        return Type::FixExt4;
    case MSGPACK_FORMAT_FIXEXT8:
        return Type::FixExt8;
    case MSGPACK_FORMAT_FIXEXT16:
        return Type::FixExt16;
    case MSGPACK_FORMAT_STR8:
        return Type::Str8;
    case MSGPACK_FORMAT_STR16:
        return Type::Str16;
    case MSGPACK_FORMAT_STR32:
        return Type::Str32;
    case MSGPACK_FORMAT_ARRAY16:
        return Type::Array16;
    case MSGPACK_FORMAT_ARRAY32:
        return Type::Array32;
    case MSGPACK_FORMAT_MAP16:
        return Type::Map16;
    case MSGPACK_FORMAT_MAP32:
        return Type::Array32;
    }

    if (byte >= MSGPACK_FORMAT_POSITIVE_FIXINT_LOW && byte <= MSGPACK_FORMAT_POSITIVE_FIXINT_HIGH)
    {
        return Type::PositiveFixInt;
    }

    if (byte >= MSGPACK_FORMAT_NEGATIVE_FIXINT_LOW && byte <= MSGPACK_FORMAT_NEGATIVE_FIXINT_HIGH)
    {
        return Type::NegativeFixInt;
    }

    if (byte >= MSGPACK_FORMAT_FIXMAP_LOW && byte <= MSGPACK_FORMAT_FIXMAP_HIGH)
    {
        return Type::FixMap;
    }

    if (byte >= MSGPACK_FORMAT_FIXARRAY_LOW && byte <= MSGPACK_FORMAT_FIXARRAY_HIGH)
    {
        return Type::FixArray;
    }

    if (byte >= MSGPACK_FORMAT_FIXSTR_LOW && byte <= MSGPACK_FORMAT_FIXSTR_HIGH)
    {
        return Type::FixStr;
    }

    GoThrow(kERROR_FORMAT);
}

kSize GoMsgPackSerializer::GetLength(InputIterator& it)
{
    kSize length = 0;

    if (*it == MSGPACK_FORMAT_BIN8 || *it == MSGPACK_FORMAT_EXT8 || *it == MSGPACK_FORMAT_STR8)
    {
        ++it;
        length = it.DeserializeNumber<k8u>();
    }
    else if (*it == MSGPACK_FORMAT_BIN16 || *it == MSGPACK_FORMAT_EXT16 || *it == MSGPACK_FORMAT_STR16 || *it == MSGPACK_FORMAT_ARRAY16 || *it == MSGPACK_FORMAT_MAP16)
    {
        ++it;
        length = it.DeserializeNumber<k16u>();
    }
    else if (*it == MSGPACK_FORMAT_BIN32 || *it == MSGPACK_FORMAT_EXT32 || *it == MSGPACK_FORMAT_STR32 || *it == MSGPACK_FORMAT_ARRAY32 || *it == MSGPACK_FORMAT_MAP32)
    {
        ++it;
        length = it.DeserializeNumber<k32u>();
    }
    else if (*it >= MSGPACK_FORMAT_FIXSTR_LOW && *it <= MSGPACK_FORMAT_FIXSTR_HIGH)
    {
        length = *it & 0x1F;
        ++it;
    }
    else if ((*it >= MSGPACK_FORMAT_FIXARRAY_LOW && *it <= MSGPACK_FORMAT_FIXARRAY_HIGH) ||
             (*it >= MSGPACK_FORMAT_FIXMAP_LOW && *it <= MSGPACK_FORMAT_FIXMAP_HIGH))
    {
        length = *it & 0x0F;
        ++it;
    }
    else
    {
        GoThrow(kERROR_FORMAT);
    }

    return length;
}

void GoMsgPackSerializer::DeserializeMessagePack(InputIterator&& it, GoDataTree& tree)
{
    while (it)
    {
        Deserialize(it, tree);
    }
}

void GoMsgPackSerializer::Deserialize(InputIterator& it, GoDataTree& tree)
{
    switch (GetType(*it))
    {
        case Type::Nil:
        {
            tree.Set(nullptr);
            ++it;
            break;
        }
        case Type::True:
        {
            tree.Set(true);
            ++it;
            break;
        }
        case Type::False:
        {
            tree.Set(false);
            ++it;
            break;
        }
        case Type::Map:
        case Type::Map16:
        case Type::Map32:
        case Type::FixMap:
        {
            DeserializeMap(it, tree);
            break;
        }
        case Type::Array:
        case Type::Array16:
        case Type::Array32:
        case Type::FixArray:
        {
            DeserializeArray(it, tree);
            break;
        }
        case Type::Str:
        case Type::Str8:
        case Type::Str16:
        case Type::Str32:
        case Type::FixStr:
        {
            std::string str;

            DeserializeString(it, str);

            tree.Set(str);

            break;
        }
        case Type::Bin:
        case Type::Bin8:
        case Type::Bin16:
        case Type::Bin32:
        {
            DeserializeBin(it, tree);
            break;
        }
        case Type::Ext:
        case Type::Ext8:
        case Type::Ext16:
        case Type::Ext32:
        case Type::FixExt1:
        case Type::FixExt2:
        case Type::FixExt4:
        case Type::FixExt8:
        case Type::FixExt16:
        {
            DeserializeExt(it, tree);
            break;
        }
        case Type::Float32:
        {
            ++it;
            tree.Set(it.DeserializeNumber<k32f>());
            break;
        }
        case Type::Float64:
        {
            ++it;
            tree.Set(it.DeserializeNumber<k64f>());
            break;
        }
        case Type::UInt8:
        {
            ++it;
            tree.Set(it.DeserializeNumber<k8u>());
            break;
        }
        case Type::UInt16:
        {
            ++it;
            tree.Set(it.DeserializeNumber<k16u>());
            break;
        }
        case Type::UInt32:
        {
            ++it;
            tree.Set(it.DeserializeNumber<k32u>());
            break;
        }
        case Type::UInt64:
        {
            ++it;
            tree.Set(it.DeserializeNumber<k64u>());
            break;
        }
        case Type::Int8:
        {
            ++it;
            tree.Set(it.DeserializeNumber<k8s>());
            break;
        }
        case Type::Int16:
        {
            ++it;
            tree.Set(it.DeserializeNumber<k16s>());
            break;
        }
        case Type::Int32:
        {
            ++it;
            tree.Set(it.DeserializeNumber<k32s>());
            break;
        }
        case Type::Int64:
        {
            ++it;
            tree.Set(it.DeserializeNumber<k64s>());
            break;
        }
        case Type::PositiveFixInt:
        {
            tree.Set(DeserializePositiveFixInt(it));
            ++it;
            break;
        }
        case Type::NegativeFixInt:
        {
            tree.Set(DeserializeNegativeFixInt(it));
            ++it;
            break;
        }
        default:
        {
            GoThrow(kERROR_FORMAT);
        }
    }
}

void GoMsgPackSerializer::DeserializeMap(InputIterator& it, GoDataTree& tree)
{
    kSize length = GetLength(it);

    if (length == 0)
    {
        tree.MarkAsObject();

        return;
    }

    for (kSize i = 0; i < length; i++)
    {
        std::string key;
        DeserializeString(it, key);

        GoDataTree child = tree[key];

        Deserialize(it, child);
    }
}

void GoMsgPackSerializer::DeserializeArray(InputIterator& it, GoDataTree& tree)
{
    kSize length = GetLength(it);

    if (length == 0)
    {
        tree.MarkAsArray();

        return;
    }

    for (k32u i = 0; i < (k32u)length; i++)
    {
        GoDataTree element = tree[i];

        Deserialize(it, element);
    }
}

void GoMsgPackSerializer::DeserializeBin(InputIterator& it, GoDataTree& tree)
{
    kSize length = GetLength(it);

    Go::Object<kArray1> array;
    GoTest(kArray1_Construct(array.Ref(), kTypeOf(kByte), length, tree.Allocator()));
    GoTest(kArray1_Resize(array.Get(), length));

    it.ReadBytes((kByte*)kArray1_Data(array.Get()), length);

    tree.Set(array.Get());
}

void GoMsgPackSerializer::DeserializeExt(InputIterator& it, GoDataTree& tree)
{
    kSize length = GetLength(it);

    // Extension type. Currently not used.
    k8u extensionType = *it;

    // Skip extensionType byte.
    ++it;

    // TODO: Check if extensionType is correct type (kByte) if needed.
    Go::Object<kArray1> array;
    GoTest(kArray1_Construct(array.Ref(), kTypeOf(kByte), length, tree.Allocator()));
    GoTest(kArray1_Resize(array.Get(), length));

    it.ReadBytes((kByte*)kArray1_Data(array.Get()), length);

    tree.Set(array.Get());

    (void)extensionType;
}

k32s GoMsgPackSerializer::DeserializeNegativeFixInt(InputIterator& it)
{
    return (k32s)(*it & 0x1F) - 0x1F - 1;
}

k32u GoMsgPackSerializer::DeserializePositiveFixInt(InputIterator& it)
{
    return (k32u)(*it & 0x7F);
}

void GoMsgPackSerializer::DeserializeString(InputIterator& it, std::string& out)
{
    it.DeserializeObject<std::string>(out, (k32u)GetLength(it));
}

}