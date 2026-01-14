#include <GoApi/GoDataTree/Serializer/GoDataTreeSerializerInputIterator.h>
#include <kApi/Io/kStream.h>

namespace GoApi
{

GoDataTreeSerializerInputStreamIterator::GoDataTreeSerializerInputStreamIterator(kStream stream, kSize size) :
    IGoDataTreeSerializerInputIterator(size),
    index(0),
    input( { kNULL, kOK, stream })
{
    ReadStreamByte();
}

kSize GoDataTreeSerializerInputStreamIterator::Find(kByte byte)
{
    GoThrow(kERROR_UNIMPLEMENTED);
}

kSize GoDataTreeSerializerInputStreamIterator::FindIf(const std::function<bool(const kByte&)>& toFind)
{
    GoThrow(kERROR_UNIMPLEMENTED);
}

kByte GoDataTreeSerializerInputStreamIterator::operator*()
{
    return input.currentByte;
}

GoDataTreeSerializerInputStreamIterator::operator bool()
{
    return index < Size() && input.operationStatus == kOK;
}

IGoDataTreeSerializerInputIterator& GoDataTreeSerializerInputStreamIterator::operator++()
{
    ReadStreamByte();

    return *this;
}

IGoDataTreeSerializerInputIterator& GoDataTreeSerializerInputStreamIterator::operator+=(k32s offset)
{
    offset -= 1;

    GoTest(kStream_Seek(input.stream, offset, kSEEK_ORIGIN_CURRENT));
    index += offset;

    ReadStreamByte();

    return *this;
}

void GoDataTreeSerializerInputStreamIterator::ReadBytes(kByte* output, kSize length)
{
    // If length == 0, do nothing.
    if (length > 0)
    {
        // First byte is already read and stored as input.currentByte, therefore decrement length by 1 and set output[0].
        length -= 1;
        output[0] = input.currentByte;

        // Read specified number of bytes (-1) and increment index.
        if ((input.operationStatus = kStream_Read(input.stream, &output[1], length)) == kOK)
        {
            index += length;
        }

        // Read next available byte.
        ReadStreamByte();
    }
}

void GoDataTreeSerializerInputStreamIterator::ReadStreamByte()
{    
    // If index is out of bounds (stream data length), do nothing.
    if (index >= Size())
    {
        return;
    }

    // Read single byte from stream and increment index.
    if ((input.operationStatus = kStream_Read(input.stream, &input.currentByte, 1)) == kOK)
    {
        index++;
    }
}

}