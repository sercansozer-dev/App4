/**@file    IGoDataTreeSerializerOutputWriter.h
 * Defines the IGoDataTreeSerializerOutputWriter interface.
 */

#ifndef GOAPI_GODATATREE_SERIALIZER_IGODATATREESERIALIZEROUTPUTWRITER_H
#define GOAPI_GODATATREE_SERIALIZER_IGODATATREESERIALIZEROUTPUTWRITER_H

#include <GoApi/GoApi.h>
#include <GoApi/GoApiDef.h>
#include <algorithm>

namespace GoApi
{

class GoApiClass IGoDataTreeSerializerOutputWriter
{
public:
    /**
     * Writes a byte into underlying buffer.
     *
     * @param byte                              kByte to write.
     */
    virtual void WriteByte(kByte byte) = 0;

    /**
     * Writes bytes into underlying buffer.
     *
     * @param bytes                             Pointer to kByte array which is the source of data to be written.
     * @param size                              Size of kByte array.
     */
    virtual void WriteByteArray(kByte* bytes, kSize size) = 0;

    /**
     * Writes text into underlying buffer. This is similar to WriteByteArray() except it assumes
     * the text array is small enough to not need to be sent in chunks like what is needed by WriteByteArray().
     * So this API should be able to write an array of text much faster than using WriteByteArray() because
     * WriteByteArray() has to check if the byte array is big enough to need to be written in chunks.
     *
     * @param text                              Pointer to kByte array which is the source of text to be written.
     * @param size                              Size of kByte array.
     */
    virtual void WriteTextArray(kByte* text, kSize size) = 0;

    /**
     * Writes a number as bytes into underlying buffer.
     *
     * @param number                            Pointer to kByte array which contains bytes of the number to be writtem.
     * @param size                              Size of the number in bytes.
     * @param bigEndian                         Whether to write number as big endian or small endian.
     */
    virtual void WriteNumberBytes(kByte* number, kSize size, bool bigEndian = true) = 0;
};

} // namespace

#endif // GOAPI_GODATATREE_SERIALIZER_IGODATATREESERIALIZEROUTPUTWRITER_H
