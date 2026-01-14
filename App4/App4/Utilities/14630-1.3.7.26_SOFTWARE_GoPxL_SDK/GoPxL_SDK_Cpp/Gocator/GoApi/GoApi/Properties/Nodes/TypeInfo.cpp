#include <GoApi/Properties/Nodes/TypeInfo.h>

namespace Go
{
namespace Properties
{

ByteVector::ByteVector()
{
    GoTest(kArray1_Construct(data.Ref(), kTypeOf(kByte), 0, kAlloc_App()));
}

ByteVector::ByteVector(const std::vector<kByte>& other)
{
    GoTest(kArray1_Construct(data.Ref(), kTypeOf(kByte), other.size(), kAlloc_App()));

    memcpy(kArray1_Data(data.Get()), other.data(), other.size());
}

ByteVector::ByteVector(void* ptr, kSize size)
{
    GoTest(kArray1_Construct(data.Ref(), kTypeOf(kByte), size, kAlloc_App()));

    memcpy(kArray1_Data(data.Get()), ptr, size);
}

ByteVector::ByteVector(kObject bytes) : ByteVector()
{
    if (kType_Is(kObject_Type(bytes), kTypeOf(kArray1)))
    {
        data.Reset(bytes, true);
    }
    else if (kType_Is(kObject_Type(bytes), kTypeOf(kArrayList)))
    {
        GoTest(kArray1_Resize(data, kArrayList_Count(bytes)));

        memcpy(kArray1_Data(data), kArrayList_Data(bytes), kArrayList_Count(bytes));
    }
    else
    {
        GoThrowMsg(kERROR_PARAMETER, "Expected kArray1 or kArrayList, got %s.", kType_Name(kObject_Type(bytes)));
    }
}

ByteVector::ByteVector(const Go::Object<kObject>& bytes) : ByteVector(bytes.Get())
{ }

ByteVector::~ByteVector()
{
    try
    {
        data.Reset();
    }
    catch (const std::exception&)
    { }
}

void ByteVector::Resize(kSize length)
{
    GoTest(kArray1_Resize(data.Get(), length));
}

kSize ByteVector::Count() const
{
    return kArray1_Count(data.Get());
}

void* ByteVector::Data()
{
    return kArray1_Data(data.Get());
}

const void* ByteVector::Data() const
{
    return (const void*)kArray1_Data(data.Get());
}

ByteVector::operator kArray1() const
{
    return data.Get();
}

ByteVector& ByteVector::operator=(const Go::Object<kObject>& other)
{
    if (kType_Is(kObject_Type(other.Get()), kTypeOf(kArray1)))
    {
        data = other;
    }
    else if (kType_Is(kObject_Type(other.Get()), kTypeOf(kArrayList)))
    {
        GoTest(kArray1_Resize(data.Get(), kArrayList_Count(other.Get())));

        memcpy(kArray1_Data(data.Get()), kArrayList_Data(other.Get()), kArrayList_Count(other.Get()));
    }
    else
    {
        GoThrowMsg(kERROR_PARAMETER, "Expected kArray1 or kArrayList, got %s.", kType_Name(kObject_Type(other.Get())));
    }

    return *this;
}

bool ByteVector::operator==(const ByteVector& other) const
{
    if (kArray1_Count(data) != kArray1_Count(other.data))
    {
        return false;
    }

    for (kSize i = 0; i < kArray1_Count(data); i++)
    {
        if (kArray1_AsT(data, i, kByte) != kArray1_AsT(other.data, i, kByte))
        {
            return false;
        }
    }

    return true;
}

bool ByteVector::operator!=(const ByteVector& other) const
{
    return !(*this == other);
}

bool ByteVector::operator<(const ByteVector& other) const
{
    kSize min = kMin(kArray1_Count(data), kArray1_Count(other.data));

    for (kSize i = 0; i < min; i++)
    {
        if (kArray1_AsT(data, i, kByte) < kArray1_AsT(other.data, i, kByte))
        {
            return true;
        }

        if (kArray1_AsT(other.data, i, kByte) < kArray1_AsT(data, i, kByte))
        {
            return false;
        }
    }

    return (kArray1_Count(data) == min && kArray1_Count(other.data) != min);
}

bool ByteVector::operator>(const ByteVector& other) const
{
    return !(*this < other);
}

bool ByteVector::operator<=(const ByteVector& other) const
{
    kSize min = kMin(kArray1_Count(data), kArray1_Count(other.data));

    for (kSize i = 0; i < min; i++)
    {
        if (kArray1_AsT(data, i, kByte) <= kArray1_AsT(other.data, i, kByte))
        {
            return true;
        }

        if (kArray1_AsT(other.data, i, kByte) <= kArray1_AsT(data, i, kByte))
        {
            return false;
        }
    }

    return (kArray1_Count(data) == min && kArray1_Count(other.data) != min);
}

bool ByteVector::operator>=(const ByteVector& other) const
{
    return !(*this <= other);
}

ByteVector::operator Go::Object<kArray1>() const
{
    return data;
}

ByteVector::operator std::vector<kByte>() const
{
    std::vector<kByte> bytes(kArray1_Count(data.Get()));

    memcpy(bytes.data(), kArray1_Data(data.Get()), kArray1_Count(data.Get()));

    return bytes;
}

}}