/**@file    IGoDataTreeSerializerInputIterator.h
 * Defines the IGoDataTreeSerializerInputIterator interface.
 */

#ifndef GOAPI_GODATATREE_SERIALIZER_IGODATATREEINPUTITERATOR_H
#define GOAPI_GODATATREE_SERIALIZER_IGODATATREEINPUTITERATOR_H

#include <GoApi/GoApi.h>
#include <GoApi/GoApiDef.h>
#include <functional>

namespace GoApi
{

class GoApiClass IGoDataTreeSerializerInputIterator
{
protected:
    IGoDataTreeSerializerInputIterator(kSize size);

public:
    /**
     * Constructs ObjectT with given length from held bytes. Input data is shifted by length.
     * Limitations: ObjectT has to be support .resize() and .data() APIs. 
     * API currently used only with std::vector and std::string.
     *
     * @param object                            Object output.
     * @param length                            New object length.
     */
    template <typename ObjectT>
    void DeserializeObject(ObjectT& object, k32u length);

    /**
     * Deserializes number from held bytes. Input data is shifted by sizeof(NumberT).
     *
     * @param bigEndian                         Whether to deserialize number as big-endian or small-endian.
     * @return                                  NumberT value.
     */
    template <typename NumberT>
    NumberT DeserializeNumber(bool bigEndian = true);

    /**
     * Returns the size of input data.
     * 
     * @return                                  Input size.
     */
    kSize Size();

    /**
     * Finds a byte within the input data.
     * 
     * @param byte                              kByte to search for.
     * @return                                  Position of found byte relative to current byte, kSIZE_NULL if not found.
     */
    virtual kSize Find(kByte byte) = 0;

    /**
     * Finds a byte within the input data.
     *
     * @param toFind                            Condition function to execute.
     * @return                                  Position of found byte relative to current byte, kSIZE_NULL if not found.
     */
    virtual kSize FindIf(const std::function<bool(const kByte&)>& toFind) = 0;

    /**
     * Returns current byte.
     *
     * @return                                  kByte value.
     */
    virtual kByte operator*() = 0;

    /**
     * Checks if held data is valid.
     *
     * @return                                  True if valid, false otherwise.
     */
    virtual operator bool() = 0;

    /**
     * Moves the iterator forward by one position.
     *
     * @return                                  GoDataTreeSerializerInputIterator reference.
     */
    virtual IGoDataTreeSerializerInputIterator& operator++() = 0;

    /**
     * Moves the iterator by the specified offset.
     *
     * @param offset                            Iterator will be moved by this value.
     * @return                                  GoDataTreeSerializerInputIterator reference.
     */
    virtual IGoDataTreeSerializerInputIterator& operator+=(k32s offset) = 0;

    /**
     * Reads specified number of bytes into output buffer.
     * 
     * @param output                            Output buffer.
     * @param length                            Output buffer length.
     */
    virtual void ReadBytes(kByte* output, kSize length) = 0;

private:
    kSize size;
};

inline IGoDataTreeSerializerInputIterator::IGoDataTreeSerializerInputIterator(kSize size) : size(size)
{ }

inline kSize IGoDataTreeSerializerInputIterator::Size()
{
    return size;
}

template<typename ObjectT>
inline void IGoDataTreeSerializerInputIterator::DeserializeObject(ObjectT& object, k32u length)
{
    object.resize(length);

    if (length > 0)
    {
        ReadBytes((kByte*)object.data(), length);
    }
}

template<typename NumberT>
inline NumberT IGoDataTreeSerializerInputIterator::DeserializeNumber(bool bigEndian)
{
    NumberT value = 0;
    kByte* valueBytes = reinterpret_cast<kByte*>(&value);

    ReadBytes(valueBytes, sizeof(NumberT));

    if (bigEndian)
    {
        GoTest(kMemReverse(valueBytes, sizeof(NumberT)));
    }

    return value;
}

} // namespace

#endif // GOAPI_GODATATREE_SERIALIZER_IGODATATREEINPUTITERATOR_H
