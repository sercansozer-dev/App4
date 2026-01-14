#ifndef GOAPI_OBJECT_H
#define GOAPI_OBJECT_H

#include <GoApi/GoApiDef.h>
#include <GoApi/Exception.h>
#include <kApi/kObject.h>

#define GoObjectNull (Go::Object<>(kNULL))

namespace Go
{
    /**
    * Smart pointer for kObject handles.
    */
    template <typename T = kObject>
    class Object
    {
    public:
        /**
        * Constructs a pointer with handle set to kNULL.
        */
        Object()
        {
            object = kNULL;
        }

        /**
        * Constructs a pointer with handle passed in.
        *
        * The handle is assumed to not have a previous owner, therefore the reference count is not incremented.
        *
        * @param handle     Handle to store in pointer.
        */
        explicit Object(T handle) : Object()
        {
            object = handle;
        }

        /**
        * Copy constructs the pointer.
        *
        * The reference count for the contained handle is incremented since there is now an additional owner.
        *
        * @param other      Reference to copied pointer.
        */
        Object(const Object& other) : Object()
        {
            Reset(other.object);

            if (other.object)
            {
                GoTest(kObject_Share(other.object));
            }            
        }

        /**
        * Move constructs the pointer.
        *
        * @param other      R-value reference to the pointer to move from.
        */
        Object(Object&& other)
        {
            object = other.object;
            other.object = kNULL;
        }

        ~Object()
        {
            // Put calls inside try/catch block in case the FSS API is changed
            // to throw exceptions.
            try
            {
                kDisposeRef(&object);
            }
            catch (const std::exception&)
            {
                // Swallow exception in destructor and exit.
                // Would logger use this object? If it does, then should not
                // try to log the exception.
                // Otherwise can log an exception message.
            }
        }

        /**
        * Resets the contained handle to a new one.
        *
        * @param handle     New handle to set. Can be kNULL.
        * @param addRef     Set to true to increment reference count.
        */
        void Reset(T handle = kNULL, bool addRef = false)
        {
            GoTest(kDestroyRef(&object));
            object = handle;

            if (addRef && handle != kNULL)
            {
                GoTest(kObject_Share(handle));
            }
        }

        /**
        * Sets the handle to kNULL without freeing it.
        */
        T Detach()
        {
            T temp = object;
            object = kNULL;
            return temp;
        }

        /**
        * Returns the contained handle.
        *
        * @return Contained handle.
        */
        T Get() const
        {
            return object;
        }

        /**
        * Returns the contained handle.
        *
        * @return Contained handle.
        */
        operator T() const
        {
            return object;
        }

        Object& operator=(T other) = delete;

        /**
        * Returns a pointer to the contained handle.
        *
        * The returned pointer can be used with kApi-style constructors.
        *
        * @return Pointer to contained handle.
        */
        T* Ref()
        {
            return &object;
        }

        /**
        * Returns a pointer to the contained handle. See @ref Ref
        */
        T* operator&()
        {
            return &object;
        }

        /**
        * Returns a pointer to the smart pointer itself.
        *
        * This method is necessary due to the overridden address operator.
        *
        * @return Pointer to the smart pointer.
        */
        Object* SelfAddress()
        {
            return this;
        }

        /**
        * Assigns from another pointer.
        *
        * @param other      Reference to other pointer.
        */
        Object& operator=(const Object& other)
        {
            T oldHandle = object;

            object = other.object;
            if (object != kNULL)
            {
                GoTest(kObject_Share(object));
            }

            GoTest(kDisposeRef(&oldHandle));

            return *this;
        }

        /**
        * Move assigns from another pointer.
        *
        * @param other      Reference to other pointer.
        */
        Object& operator=(Object&& other)
        {
            GoTest(kDisposeRef(&object));

            object = other.object;
            other.object = kNULL;

            return *this;
        }

        /**
        * Convenient method to cast the handle to another type.
        */
        template <class T2>
        T2 As()
        {
            return (T2)(T)*this;
        }

    private:
        T object;
    };
}

#endif
