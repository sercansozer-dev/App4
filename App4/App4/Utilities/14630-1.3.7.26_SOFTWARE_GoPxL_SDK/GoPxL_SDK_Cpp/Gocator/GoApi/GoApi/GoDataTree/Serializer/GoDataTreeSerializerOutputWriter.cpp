#include <GoApi/GoDataTree/Serializer/GoDataTreeSerializerOutputWriter.h>

#include <kApi/Io/kStream.h>
#include <kApi/Io/kSerializer.h>
#include <kApi/Io/kWebSocket.h>

namespace GoApi
{

// GoDataTreeSerializerOutputCounter
GoDataTreeSerializerOutputCounter::GoDataTreeSerializerOutputCounter() :
    count(0)
{
}

void GoDataTreeSerializerOutputCounter::WriteByte(kByte)
{
    count++;
}

void GoDataTreeSerializerOutputCounter::WriteByteArray(kByte*, kSize size)
{
    count += size;
}

void GoDataTreeSerializerOutputCounter::WriteTextArray(kByte*, kSize size)
{
    count += size;
}

void GoDataTreeSerializerOutputCounter::WriteNumberBytes(kByte*, kSize size, bool)
{
    count += size;
}

kSize GoDataTreeSerializerOutputCounter::Count()
{
    return count;
}
// GoDataTreeSerializerOutputCounter

// GoDataTreeSerializerOutputStreamWriter
GoDataTreeSerializerOutputStreamWriter::GoDataTreeSerializerOutputStreamWriter(kStream output) :
    output(output)
{
}

void GoDataTreeSerializerOutputStreamWriter::WriteByte(kByte byte)
{
    GoTest(kStream_Write(output, &byte, 1));
}

void GoDataTreeSerializerOutputStreamWriter::WriteByteArray(kByte* bytes, kSize size)
{
    GoTest(kStream_Write(output, bytes, size));
}

void GoDataTreeSerializerOutputStreamWriter::WriteTextArray(kByte* text, kSize size)
{
    GoTest(kStream_Write(output, text, size));
}

void GoDataTreeSerializerOutputStreamWriter::WriteNumberBytes(kByte* bytes, kSize size, bool bigEndian)
{
    if (bigEndian)
    {
        GoTest(kMemReverse(bytes, size));
    }

    GoTest(kStream_Write(output, bytes, size));
}
// GoDataTreeSerializerOutputStreamWriter

// GoDataTreeSerializerOutputWriter
GoDataTreeSerializerOutputWriter::GoDataTreeSerializerOutputWriter(kSerializer output) :
    output(output)
{
}

void GoDataTreeSerializerOutputWriter::SerializeByteArrayChunked(kSerializer serializer, kByte* byteArray, kSize byteArraySize, kSize chunkSize)
{
    kSize bytesWritten = 0;

    try
    {
        while (bytesWritten < byteArraySize)
        {
            kSize bytesRemaining = byteArraySize - bytesWritten;
            kSize bytesToWrite = kMin_(chunkSize, bytesRemaining);

            GoTest(kSerializer_WriteByteArray(serializer, (void*)&byteArray[bytesWritten], bytesToWrite));

            if (kObject_Is(kSerializer_Stream(output), kTypeOf(kWebSocket)))
            {
                // GOS-12647: For websocket connection, need to make sure the final frame is sent out for every single chunk.
                // Each chunk becomes a complete websocket message.
                // It is up to the client to reassemble individual chunked but completed websocket messages into a complete REST message.
                GoTest(kWebSocket_Send(kSerializer_Stream(output)));
            }

            bytesWritten += bytesToWrite;
        }
    }
    catch (const Go::Exception& e)
    {
        GoLogException(e);

        GoLogError("Failed to serialize byte array of size %u after writing %u bytes", byteArraySize, bytesWritten);
        
        if (Go::Exception::ExceptionToStatus(e) == kERROR_MEMORY)
        {
            const kChar* errorMsg = bepgettext("sensor", "Insufficient memory error encountered outputting data. Decrease data size if possible and try again.");
            GoLogUserError("sensor", bepgettext("sensor", errorMsg));

            // GOS-12336 - Convert kERROR_MEMORY into kERROR_NETWORK.
            GoThrowMsg(kERROR_NETWORK, errorMsg);
        }
        else
        {
            const kChar* errorMsg = bepgettext("sensor", "Failed to output data. Decrease data size if possible and try again.");
            GoLogUserError("sensor", bepgettext("sensor", errorMsg));

            GoResumeThrow();
        }
    }
}

void GoDataTreeSerializerOutputWriter::WriteByte(kByte byte)
{
    GoTest(kSerializer_WriteByte(output, byte));
}

void GoDataTreeSerializerOutputWriter::WriteByteArray(kByte* bytes, kSize size)
{
    // GOS-10675: Based on the results of the GOS-10614 investigation:
    // if the GoDataTreeSerializerOutputWriter's "output" member variable is associated
    // with a TCP stream output kSerializer, then use chunked writes to put as much data as possible
    // with each call to the kSerializer_WriteByteArray() api call.
    // To summarize the results of GOS-10614: the impact of specifiying a chunk size for:
    //  - Linux: no effect. Linux limits the chunks to 2 * send socket buffer size bytes.
    //  - Windows internal TCP connection: Loopback link layer protocol is used instead of Ethernet.
    //    While a bigger chunk reduces the time to write the byte array, this scenario is not
    //    typical of customer deployments, so this scenario is ignored.
    //  - Windows external TCP connection: chunk size from about 100 MB to 1 GB reduces the time
    //    to write the entire byte array into the kernel's socket send API.
    //
    // Since kSerializer class can be used for different stream output types. use chunk writes
    // only for TCP connections for now. For non-TCP streams, don't break up the byte array call
    // into chunks. This can be changed later if there is a reason to include non-TCP stream.
    // GOS-12647: Also enables chunked write for websocket as well.
    if ((size > GO_DATA_TREE_TCP_SERIALIZER_BYTE_ARRAY_WRITE_CHUNK_SIZE_BYTES) &&
        ((kObject_Is(kSerializer_Stream(output), kTypeOf(kTcpClient)) || 
         (kObject_Is(kSerializer_Stream(output), kTypeOf(kWebSocket))))))
    {
        // Use chunked writes.
        SerializeByteArrayChunked(output, bytes, size, GO_DATA_TREE_TCP_SERIALIZER_BYTE_ARRAY_WRITE_CHUNK_SIZE_BYTES);
    }
    else
    {
        // No need for chunked writes in this case.
        // Just write the entire thing into the serializer.
        GoTest(kSerializer_WriteByteArray(output, bytes, size));
    }
}

void GoDataTreeSerializerOutputWriter::WriteTextArray(kByte* text, kSize size)
{
    GoTest(kSerializer_WriteByteArray(output, text, size));
}

void GoDataTreeSerializerOutputWriter::WriteNumberBytes(kByte* bytes, kSize size, bool bigEndian)
{
    if (bigEndian)
    {
        GoTest(kMemReverse(bytes, size));
    }

    GoTest(kSerializer_WriteByteArray(output, bytes, size));
}
// GoDataTreeSerializerOutputWriter

}