/**@file    GoDataTreeSerializerOutputWriter.h
 * Defines the GoDataTreeSerializerOutputContainerWriter and GoDataTreeSerializerOutputStreamWriter classes.
 */

#ifndef GOAPI_GODATATREE_SERIALIZER_GODATATREESERIALIZEROUTPUTWRITER_H
#define GOAPI_GODATATREE_SERIALIZER_GODATATREESERIALIZEROUTPUTWRITER_H

#include <GoApi/GoDataTree/Serializer/IGoDataTreeSerializerOutputWriter.h>

namespace GoApi
{

/*! @class GoDataTreeSerializerOutputContainerWriter GoDataTreeSerializerOutputWriter.h

@brief A class used to write data into std::string or std::vector.
@brief ContainerT type must support .push_back(), .insert() and .end() APIs.
@ingroup Go
*/
template <typename ContainerT>
class GoApiClass GoDataTreeSerializerOutputContainerWriter : public IGoDataTreeSerializerOutputWriter
{
public:
    /**
     * Constructs the GoDataTreeSerializerOutputContainerWriter object.
     *
     * @param output                            ContainerT pointer.
     */
    explicit GoDataTreeSerializerOutputContainerWriter(ContainerT* output);

    /* IGoDataTreeSerializerOutputWriter overrides */
    void WriteByte(kByte byte) override;
    void WriteByteArray(kByte* bytes, kSize size) override;
    void WriteTextArray(kByte* text, kSize size) override;
    void WriteNumberBytes(kByte* number, kSize size, bool bigEndian = true) override;

private:
    ContainerT* output;
};

/*! @class GoDataTreeSerializerOutputCounter GoDataTreeSerializerOutputWriter.h

@brief A class used to count bytes that serialized GoDataTree would take.
@ingroup Go
*/
class GoApiClass GoDataTreeSerializerOutputCounter : public IGoDataTreeSerializerOutputWriter
{
public:
    /**
     * Constructs the GoDataTreeSerializerOutputCounter object.
     */
    explicit GoDataTreeSerializerOutputCounter();

    /* IGoDataTreeSerializerOutputWriter overrides */
    void WriteByte(kByte) override;
    void WriteByteArray(kByte*, kSize size) override;
    void WriteTextArray(kByte* text, kSize size) override;
    void WriteNumberBytes(kByte*, kSize size, bool bigEndian = true) override;

    /**
     * Returns size of serialized GoDataTree object.
     */
    kSize Count();

private:
    kSize count;
};

/*! @class GoDataTreeSerializerOutputStreamWriter GoDataTreeSerializerOutputWriter.h

@brief A class used to write bytes into kStream.
@ingroup Go
*/
class GoApiClass GoDataTreeSerializerOutputStreamWriter : public IGoDataTreeSerializerOutputWriter
{
public:
    /**
     * Constructs the GoDataTreeSerializerOutputStreamWriter object.
     *
     * @param output                            kStream output.
     */
    explicit GoDataTreeSerializerOutputStreamWriter(kStream output);

    /* IGoDataTreeSerializerOutputWriter overrides */
    void WriteByte(kByte byte) override;
    void WriteByteArray(kByte* bytes, kSize size) override;
    void WriteTextArray(kByte* text, kSize size) override;
    void WriteNumberBytes(kByte* bytes, kSize size, bool bigEndian = true) override;

private:
    kStream output;
};

/*! @class GoDataTreeSerializerOutputWriter GoDataTreeSerializerOutputWriter.h

@brief A class used to write bytes into kSerializer.
@ingroup Go
*/
class GoApiClass GoDataTreeSerializerOutputWriter : public IGoDataTreeSerializerOutputWriter
{
public:
    /**
     * Constructs the GoDataTreeSerializerOutputWriter object.
     *
     * @param output                            kSerializer output.
     */
    explicit GoDataTreeSerializerOutputWriter(kSerializer output);

    /* IGoDataTreeSerializerOutputWriter overrides */
    void WriteByte(kByte byte) override;
    void WriteByteArray(kByte* bytes, kSize size) override;
    void WriteTextArray(kByte* text, kSize size) override;
    void WriteNumberBytes(kByte* bytes, kSize size, bool bigEndian = true) override;

// Constants
private:
    // GOS-10675: Define chunk size to use for chunked writes.
    // Refer to the investigation report GOS-10614 for the range of possible values to use.
    // This value can be changed to tweak how much time the kSerializer write call takes.
    static constexpr k64u GO_DATA_TREE_TCP_SERIALIZER_BYTE_ARRAY_WRITE_CHUNK_SIZE_BYTES = (k64u)100 * 1024 * 1024;  // 100 MB

// Member functions
private:
    /**
     * This private function writes a byte array into the serializer one chunk at a time.
     *
     * @param serializer            Serializer object to write into.
     * @param byteArray             Pointer to an array of bytes.
     * @param byteArraySize         Number of bytes in the byte array.
     * @param chunkSize             Number of bytes to write into the serializer with each
     *                              call to the serializer write API.
     */
    void SerializeByteArrayChunked(kSerializer serializer, kByte* byteArray, kSize byteArraySize, kSize chunkSize);

// Member variables
private:
    kSerializer output;
};

template<typename ContainerT>
inline GoDataTreeSerializerOutputContainerWriter<ContainerT>::GoDataTreeSerializerOutputContainerWriter(ContainerT* output) :
    output(output)
{ }

template<typename ContainerT>
inline void GoDataTreeSerializerOutputContainerWriter<ContainerT>::WriteByte(kByte byte)
{
    output->push_back(byte);
}

template<typename ContainerT>
inline void GoDataTreeSerializerOutputContainerWriter<ContainerT>::WriteByteArray(kByte* bytes, kSize size)
{
    output->insert(output->end(), &bytes[0], &bytes[size]);
}

template<typename ContainerT>
inline void GoDataTreeSerializerOutputContainerWriter<ContainerT>::WriteTextArray(kByte* text, kSize size)
{
    output->insert(output->end(), &text[0], &text[size]);
}

template<typename ContainerT>
inline void GoDataTreeSerializerOutputContainerWriter<ContainerT>::WriteNumberBytes(kByte* bytes, kSize size, bool bigEndian)
{
    if (bigEndian)
    {
        std::reverse_copy(&bytes[0], &bytes[size], std::back_inserter(*output));
    }
    else
    {
        std::copy(&bytes[0], &bytes[size], std::back_inserter(*output));
    }
}

} // namespace

#endif // GOAPI_GODATATREE_SERIALIZER_GODATATREESERIALIZEROUTPUTWRITER_H
