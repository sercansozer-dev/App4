/**
* @file     ValueIo.h
* @brief    Declares the value reader and writer interfaces.
*/

#ifndef GOAPI_PROPERTIES_VALUEIO_H
#define GOAPI_PROPERTIES_VALUEIO_H

#include <GoApi/Properties/Nodes/TypeInfo.h>
#include <string>

namespace Go
{
namespace Properties
{

class IValueWriter
{
public:
    virtual ~IValueWriter() {};

    virtual void WriteChar(k8s value) = 0;
    virtual void WriteUChar(k8u value) = 0;

    virtual void WriteShort(k16s value) = 0;
    virtual void WriteUShort(k16u value) = 0;

    virtual void WriteInt(k32s value) = 0;
    virtual void WriteUInt(k32u value) = 0;

    virtual void WriteLong(k64s value) = 0;
    virtual void WriteULong(k64u value) = 0;

    virtual void WriteFloat(k32f value) = 0;
    virtual void WriteDouble(k64f value) = 0;

    virtual void WriteBool(bool value) = 0;

    virtual void WriteString(const kChar* str) = 0;

    virtual void WriteBinary(const ByteVector bin) = 0;

    virtual void BeginWriteArray() = 0;
    virtual void EndWriteArray() = 0;
};

class IValueReader
{
public:
    virtual ~IValueReader() {};

    virtual k8u ReadUChar() = 0;
    virtual k8s ReadChar() = 0;

    virtual k16u ReadUShort() = 0;
    virtual k16s ReadShort() = 0;

    virtual k32u ReadUInt() = 0;
    virtual k32s ReadInt() = 0;

    virtual k64u ReadULong() = 0;
    virtual k64s ReadLong() = 0;

    virtual k32f ReadFloat() = 0;
    virtual k64f ReadDouble() = 0;

    virtual bool ReadBool() = 0;

    virtual std::string ReadString() = 0;

    virtual ByteVector ReadBinary() = 0;

    virtual void BeginReadArray() = 0;
    virtual bool NextArrayItem() = 0;
    virtual void EndReadArray() = 0;
};

class IValueIo
{
public:
    virtual ~IValueIo() {};

    virtual void ReadValue(IValueReader& reader) = 0;
    virtual void WriteValue(IValueWriter& writer) const = 0;
    virtual void ValidateValue(IValueReader& reader) const = 0;
};

}
} //Namespaces

#endif
