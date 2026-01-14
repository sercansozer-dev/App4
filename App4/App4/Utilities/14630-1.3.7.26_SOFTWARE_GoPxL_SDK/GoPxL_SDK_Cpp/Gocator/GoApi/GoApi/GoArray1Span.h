#ifndef GOAPI_GO_ARRAY1_SPAN_H
#define GOAPI_GO_ARRAY1_SPAN_H

#include <GoApi/Object.h>
#include <kApi/kApi.h>

namespace GoApi
{
    /**
    * An object allows read access to a portion of the entire kArray1 data buffer.
    */
    struct GoArray1Span
    {
    public:
        using const_iterator = const kByte*;

        GoArray1Span(const Go::Object<kArray1> buffer) :
            buffer(buffer),
            offset(0)
        {
            if (kIsNull(buffer))
            {
                dataSize = 0;
            }
            else
            {
                dataSize = kArray1_DataSize(buffer);
            }
        }

        GoArray1Span() :
            buffer(nullptr),
            offset(0),
            dataSize(0)
        {
        }

        GoArray1Span(Go::Object<kArray1> buffer, kSize offset, kSize size) :
            buffer(buffer),
            offset(offset),
            dataSize(size)
        {
        }

        GoArray1Span(const GoArray1Span& object, kSize offset, kSize size) :
            buffer(object.buffer),
            offset(object.offset + offset),
            dataSize(size)
        {
        }

        /**
         * Returns the underlying kArray1 object wrapped as a smart object.
         * 
         * @return                      The data buffer.
         */
        Go::Object<kArray1> Buffer() const
        {
            return buffer;
        }

        /**
         * Returns the ptr to the start of the data buffer if buffer is not empty.
         * If data buffer is empty, returns nullptr.
         * 
         * @return                      The ptr to the data buffer.
         */
        const kByte* Data() const
        {
            if (!kIsNull(buffer.Get()))
            {
                return (kByte*)kArray1_Data(buffer) + offset;
            }
            else
            {
                return nullptr;
            }
        }

        /**
         * Returns the ptr to the start of the data buffer if buffer is not empty.
         * If data buffer is empty, returns nullptr.
         * Note this camelCase API is provided in order to match the similar std lib style.
         * 
         * @return                      The ptr to the data buffer.
         */
        const kByte* data() const
        {
            return Data();
        }

        /**
         * Returns the size of the data.
         * 
         * @return                      The size fo the data.
         */
        kSize Size() const
        {
            return dataSize;
        }

        /**
         * Returns the size of the data.
         * Note this camelCase API is provided in order to match the similar std lib style.
         * 
         * @return                      The size fo the data.
         */
        kSize size() const
        {
            return Size();
        }

        /**
         * Returns the const iterator to the beginning of the data.
         * 
         * @return                      The const iterator to the beginning of the data.
         */
        const_iterator CBegin() const
        {

            return Data();
        }

        /**
         * Returns the const iterator to the end of the data.
         * 
         * @return                      The const iterator to the end of the data.
         */
        const_iterator CEnd() const
        {
            return Data() + Size();
        }

        /**
         * Returns the iterator to the beginning of the data.
         * 
         * @return                      The iterator to the beginning of the data.
         */
        const_iterator Begin() const
        {
            return CBegin();
        }

        /**
         * Returns the iterator to the end of the data.
         * 
         * @return                      The iterator to the end of the data.
         */
        const_iterator End() const
        {
            return CEnd();
        }

        /**
         * Returns the const iterator to the beginning of the data.
         * Note this camelCase API is provided in order to match the similar std lib style.
         * 
         * @return                      The const iterator to the beginning of the data.
         */
        const_iterator cbegin() const
        {
            return CBegin();
        }

        /**
         * Returns the const iterator to the end of the data.
         * Note this camelCase API is provided in order to match the similar std lib style.
         * 
         * @return                      The const iterator to the end of the data.
         */
        const_iterator cend() const
        {
            return CEnd();
        }

        /**
         * Returns the iterator to the beginning of the data.
         * Note this camelCase API is provided in order to match the similar std lib style.
         * 
         * @return                      The iterator to the beginning of the data.
         */
        const_iterator begin() const
        {
            return Begin();
        }

        /**
         * Returns the iterator to the end of the data.
         * Note this camelCase API is provided in order to match the similar std lib style.
         * 
         * @return                      The iterator to the end of the data.
         */
        const_iterator end() const
        {
            return End();
        }

    private:

        // The original full data buffer.
        Go::Object<kArray1> buffer;

        // The offset in byte from where the data is relevant to the current object.
        kSize offset;

        // The number of bytes of the relevant section of data.
        kSize dataSize;
    };
}

#endif //GOAPI_GO_ARRAY1_SPAN_H
