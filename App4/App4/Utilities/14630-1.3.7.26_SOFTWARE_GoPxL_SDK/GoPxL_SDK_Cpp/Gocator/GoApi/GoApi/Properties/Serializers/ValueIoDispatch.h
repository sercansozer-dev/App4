/**
* @file     ValueIoDispatch.h
* @brief    Declares the value IO dispatch functions.
*/

#ifndef GOAPI_PROPERTIES_VALUEIO_DISPATCH_H
#define GOAPI_PROPERTIES_VALUEIO_DISPATCH_H

#include <GoApi/Properties/Serializers/ValueIo.h>

namespace Go
{
namespace Properties
{
    // Default
    template <typename T, typename = void>
    struct ValueIoDispatch
    {
        static void Read(IValueReader& reader, T& value)
        {
            throw Go::Exception("Type not handled");
        }

        static void Write(IValueWriter& writer, const T& value)
        {
            throw Go::Exception("Type not handled");
        }
    };

    // Char
    template <typename T>
    struct ValueIoDispatch<T, std::enable_if_t<std::is_same<T, k8s>::value>>
    {
        static void Read(IValueReader& reader, T& value)
        {
            value = reader.ReadChar();
        }

        static void Write(IValueWriter& writer, const T& value)
        {
            writer.WriteChar(value);
        }
    };

    // UChar
    template <typename T>
    struct ValueIoDispatch<T, std::enable_if_t<std::is_same<T, k8u>::value>>
    {
        static void Read(IValueReader& reader, T& value)
        {
            value = reader.ReadUChar();
        }

        static void Write(IValueWriter& writer, const T& value)
        {
            writer.WriteUChar(value);
        }
    };

    // Short
    template <typename T>
    struct ValueIoDispatch<T, std::enable_if_t<std::is_same<T, k16s>::value>>
    {
        static void Read(IValueReader& reader, T& value)
        {
            value = reader.ReadShort();
        }

        static void Write(IValueWriter& writer, const T& value)
        {
            writer.WriteShort(value);
        }
    };

    // UShort
    template <typename T>
    struct ValueIoDispatch<T, std::enable_if_t<std::is_same<T, k16u>::value>>
    {
        static void Read(IValueReader& reader, T& value)
        {
            value = reader.ReadUShort();
        }

        static void Write(IValueWriter& writer, const T& value)
        {
            writer.WriteUShort(value);
        }
    };

    // Int (default signed integer numeric)
    template <typename T>
    struct ValueIoDispatch<T, std::enable_if_t<
        std::is_integral<T>::value &&
        std::is_signed<T>::value &&
        !std::is_same<T, k64s>::value &&
        !std::is_same<T, k16s>::value &&
        !std::is_same<T, k8s>::value>>
    {
        static void Read(IValueReader& reader, T& value)
        {
            value = static_cast<T>(reader.ReadInt());
        }

        static void Write(IValueWriter& writer, const T& value)
        {
            writer.WriteInt(static_cast<k32s>(value));
        }
    };

    // UInt (default unsigned integer numeric)
    template <typename T>
    struct ValueIoDispatch<T, std::enable_if_t<
        std::is_integral<T>::value &&
        !std::is_signed<T>::value &&
        !std::is_same<T, k64u>::value &&
        !std::is_same<T, k16u>::value &&
        !std::is_same<T, k8u>::value>>
    {
        static void Read(IValueReader& reader, T& value)
        {
            value = static_cast<T>(reader.ReadUInt());
        }

        static void Write(IValueWriter& writer, const T& value)
        {
            writer.WriteUInt(static_cast<k32u>(value));
        }
    };

    // Long Int
    template <typename T>
    struct ValueIoDispatch<T, std::enable_if_t<std::is_same<T, k64s>::value>>
    {
        static void Read(IValueReader& reader, T& value)
        {
            value = reader.ReadLong();
        }

        static void Write(IValueWriter& writer, const T& value)
        {
            writer.WriteLong(value);
        }
    };

    // Long UInt
    template <typename T>
    struct ValueIoDispatch<T, std::enable_if_t<std::is_same<T, k64u>::value>>
    {
        static void Read(IValueReader& reader, T& value)
        {
            value = reader.ReadULong();
        }

        static void Write(IValueWriter& writer, const T& value)
        {
            writer.WriteULong(value);
        }
    };

    // Float
    template <typename T>
    struct ValueIoDispatch<T, std::enable_if_t<std::is_same<T, k32f>::value>>
    {
        static void Read(IValueReader& reader, T& value)
        {
            value = reader.ReadFloat();
        }

        static void Write(IValueWriter& writer, const T& value)
        {
            writer.WriteFloat(value);
        }
    };

    // Double (default floating point numeric)
    template <typename T>
    struct ValueIoDispatch<T, std::enable_if_t<
        std::is_floating_point<T>::value &&
        !std::is_same<T, k32f>::value>>
    {
        static void Read(IValueReader& reader, T& value)
        {
            value = static_cast<T>(reader.ReadDouble());
        }

        static void Write(IValueWriter& writer, const T& value)
        {
            writer.WriteDouble(static_cast<k64f>(value));
        }
    };

    // Enum types
    template <typename T>
    struct ValueIoDispatch<T, std::enable_if_t<std::is_enum<T>::value>>
    {
        static void Read(IValueReader& reader, T& value)
        {
            using IntrinsictType = typename std::underlying_type<T>::type;
            IntrinsictType intrinsicVal;

            ValueIoDispatch<IntrinsictType>::Read(reader, intrinsicVal);
            value = static_cast<T>(intrinsicVal);
        }

        static void Write(IValueWriter& writer, const T& value)
        {
            using IntrinsictType = typename std::underlying_type<T>::type;
            ValueIoDispatch<IntrinsictType>::Write(writer, static_cast<IntrinsictType>(value));
        }
    };

    // Bool
    template <>
    struct ValueIoDispatch<bool>
    {
        static void Read(IValueReader& reader, bool& value)
        {
            value = reader.ReadBool();
        }

        static void Write(IValueWriter& writer, bool value)
        {
            writer.WriteBool(value);
        }
    };

    // String
    template <>
    struct ValueIoDispatch<std::string>
    {
        static void Read(IValueReader& reader, std::string& value)
        {
            value = reader.ReadString();
        }

        static void Write(IValueWriter& writer, const std::string& value)
        {
            writer.WriteString(value.c_str());
        }
    };

    // Binary
    template <>
    struct ValueIoDispatch<ByteVector>
    {
        static void Read(IValueReader& reader, ByteVector& value)
        {
            value = reader.ReadBinary();
        }

        static void Write(IValueWriter& writer, const ByteVector& value)
        {
            writer.WriteBinary(value);
        }
    };

    template <typename T>
    struct ArrayWriter
    {
        static void Write(IValueWriter& writer, const T& items)
        {
            using ItemType = typename Internal::ArrayItemType<T>::type;

            writer.BeginWriteArray();

            for (auto& item : items)
            {
                ValueIoDispatch<ItemType>::Write(writer, item);
            }

            writer.EndWriteArray();
        }
    };

    // Container
    template <typename T>
    struct ValueIoDispatch<T, std::enable_if_t<Internal::IsTypeArray<T>::value && !Internal::IsTypeCArray<T>::value>>
    {
        static void Read(IValueReader& reader, T& items)
        {
            using ItemType = typename Internal::ArrayItemType<T>::type;

            items.clear();

            reader.BeginReadArray();

            while (reader.NextArrayItem())
            {
                ItemType item;
                ValueIoDispatch<ItemType>::Read(reader, item);
                items.insert(items.end(), item);
            }

            reader.EndReadArray();
        }

        static void Write(IValueWriter& writer, const T& items)
        {
            ArrayWriter<T>::Write(writer, items);
        }
    };

    // C arrays
    template <typename T>
    struct ValueIoDispatch<T, std::enable_if_t<Internal::IsTypeCArray<T>::value>>
    {
        static void Read(IValueReader& reader, T& items)
        {
            using ItemType = typename Internal::ArrayItemType<T>::type;

            reader.BeginReadArray();
            auto it = items.begin();

            while (reader.NextArrayItem() && it != items.end())
            {
                ValueIoDispatch<ItemType>::Read(reader, *it);
                it++;
            }

            reader.EndReadArray();
        }

        static void Write(IValueWriter& writer, const T& items)
        {
            ArrayWriter<T>::Write(writer, items);
        }
    };
}
} //Namespaces

#endif
