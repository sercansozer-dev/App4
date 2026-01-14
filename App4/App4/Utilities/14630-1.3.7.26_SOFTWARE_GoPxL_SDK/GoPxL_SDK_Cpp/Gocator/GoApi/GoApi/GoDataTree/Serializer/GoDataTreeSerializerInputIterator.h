/**@file    GoDataTreeSerializerInputIterator.h
 * Defines the GoDataTreeSerializerInputContainerIterator and GoDataTreeSerializerInputStreamIterator classes.
 */

#ifndef GOAPI_GODATATREE_SERIALIZER_GODATATREEINPUTITERATOR_H
#define GOAPI_GODATATREE_SERIALIZER_GODATATREEINPUTITERATOR_H

#include <GoApi/GoDataTree/Serializer/IGoDataTreeSerializerInputIterator.h>

namespace GoApi
{

constexpr kSize MIN_SIZE_PARALLEL_FIND = 1024 * 1024;

/*! @class GoDataTreeSerializerInputContainerIterator GoDataTreeSerializerInputIterator.h

@brief A class used to iterate through a std::string or std::vector.
@ingroup Go
*/
template <typename ContainerIteratorT>
class GoApiClass GoDataTreeSerializerInputContainerIterator : public IGoDataTreeSerializerInputIterator
{
protected:
    struct ContainerInputIterator
    {
        ContainerIteratorT current;
        ContainerIteratorT end;
    };

public:
    /**
     * Constructs the GoDataTreeSerializerInputContainerIterator object
     *
     * @param begin                             ContainerIteratorT begin iterator.
     * @param end                               ContainerIteratorT end iterator.
     */
    GoDataTreeSerializerInputContainerIterator(const ContainerIteratorT& begin, const ContainerIteratorT& end);

    /* IGoDataTreeSerializerInputIterator overrides */
    kSize Find(kByte byte) override;
    kSize FindIf(const std::function<bool(const kByte&)>& toFind) override;
    kByte operator*() override;
    operator bool() override;
    IGoDataTreeSerializerInputIterator& operator++() override;
    IGoDataTreeSerializerInputIterator& operator+=(k32s offset) override;
    void ReadBytes(kByte* output, kSize length) override;

private:
    ContainerInputIterator input;
};

/*! @class GoDataTreeSerializerInputStreamIterator GoDataTreeSerializerInputIterator.h

@brief A class used to iterate through kStream.
@ingroup Go
*/
class GoApiClass GoDataTreeSerializerInputStreamIterator : public IGoDataTreeSerializerInputIterator
{
protected:
    struct StreamInputIterator
    {
        kByte currentByte;
        kStatus operationStatus;

        kStream stream;
    };

public:
    /**
     * Constructs the GoDataTreeSerializerInputStreamIterator object
     *
     * @param stream                            kStream input.
     * @param size                              kStream input size.
     */
    GoDataTreeSerializerInputStreamIterator(kStream stream, kSize size);

    /* IGoDataTreeSerializerInputIterator overrides */
    kSize Find(kByte byte) override;
    kSize FindIf(const std::function<bool(const kByte&)>& toFind) override;
    kByte operator*() override;
    operator bool() override;
    IGoDataTreeSerializerInputIterator& operator++() override;
    IGoDataTreeSerializerInputIterator& operator+=(k32s offset) override;
    void ReadBytes(kByte* output, kSize length) override;

private:
    void ReadStreamByte();

private:
    kSize index;
    StreamInputIterator input;
};

template<typename ContainerIteratorT>
inline GoDataTreeSerializerInputContainerIterator<ContainerIteratorT>::GoDataTreeSerializerInputContainerIterator(const ContainerIteratorT& begin, const ContainerIteratorT& end) :
    IGoDataTreeSerializerInputIterator(std::distance(begin, end)),
    input({ begin, end })
{ }

template<typename ContainerIteratorT>
inline kSize GoDataTreeSerializerInputContainerIterator<ContainerIteratorT>::Find(kByte byte)
{
    ContainerIteratorT it = std::find(input.current, input.end, byte);

    if (it != input.end)
    {
        return std::distance(input.current, it);
    }

    return kSIZE_NULL;
}

template<typename ContainerIteratorT>
inline kSize GoDataTreeSerializerInputContainerIterator<ContainerIteratorT>::FindIf(const std::function<bool(const kByte&)>& toFind)
{
    ContainerIteratorT it = std::find_if(input.current, input.end, toFind);

    if (it != input.end)
    {
        return std::distance(input.current, it);
    }

    return kSIZE_NULL;
}

template<typename ContainerIteratorT>
inline kByte GoDataTreeSerializerInputContainerIterator<ContainerIteratorT>::operator*()
{
    return *input.current;
}

template<typename ContainerIteratorT>
inline GoDataTreeSerializerInputContainerIterator<ContainerIteratorT>::operator bool()
{
    return input.current != input.end;
}

template<typename ContainerIteratorT>
inline IGoDataTreeSerializerInputIterator& GoDataTreeSerializerInputContainerIterator<ContainerIteratorT>::operator++()
{
    input.current++;

    return *this;
}

template<typename ContainerIteratorT>
inline IGoDataTreeSerializerInputIterator& GoDataTreeSerializerInputContainerIterator<ContainerIteratorT>::operator+=(k32s offset)
{
    input.current += offset;

    return *this;
}

template<typename ContainerIteratorT>
inline void GoDataTreeSerializerInputContainerIterator<ContainerIteratorT>::ReadBytes(kByte* output, kSize length)
{
    if (length > 0)
    {
        memcpy(output, &(*input.current), length);
        input.current += length;
    }
}

} // namespace

#endif // GOAPI_GODATATREE_SERIALIZER_GODATATREEINPUTITERATOR_H
