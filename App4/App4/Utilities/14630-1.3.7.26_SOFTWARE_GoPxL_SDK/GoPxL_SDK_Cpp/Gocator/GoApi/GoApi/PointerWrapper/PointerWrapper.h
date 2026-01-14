#ifndef GOAPI_POINTER_WRAPPER_H
#define GOAPI_POINTER_WRAPPER_H

#include <GoApi/GoApiDef.h>

/**
 * @class       PointerWrapper
 * @extends     kObject
 * @brief       Represents a wrapper to a C++ pointer.
 */
typedef kObject PointerWrapper;

/**
 * Creates a pointer wrapper for use with Firesync data structures.
 *
 * @param   pointerWrapper      A @ref PointerWrapper object.
 * @param   pointer             A kPointer to the object to contain.
 * @param   size                The estimated size of the object being pointed to.
 * @param   allocator           Memory allocator (or kNULL for default).
 * @return                      Operation status.
 */
GoApiFx(kStatus) PointerWrapper_Construct(PointerWrapper* pointerWrapper, kPointer pointer, kSize size, kObject allocator);

/**
 * Gets the stored pointer from the object.
 *
 * @param   pointerWrapper      A @ref PointerWrapper object.
 * @return                      A Pointer to the internal object.
 *
 * @remark                      Ownership and memory management of the pointer
 *                              is transferred to the caller.
 *
 - @code
 * Go::Object<PointerWrapper> ptrWrapper;
 * SomeObject* obj = new SomeObject();
 * // Get some estimated size.
 * kSize size = sizeof(*obj);
 * kPointer ptr = (kPointer)obj
 * PointerWrapper_Construct(ptrWrapper, ptr, size, kNULL);
 * // ptrWrapper no longer owns the memory after this call.
 * // newPtr will be in charge of deleting the allocated memory.
 * kPointer newPtr = PointerWrapper_Pointer(ptrWrapper);
 * SomeObject* newObj = (SomeObject*)newPtr
 * delete newObj;
 * @endcode
 */
GoApiFx(kPointer) PointerWrapper_Pointer(PointerWrapper pointerWrapper);


#include <GoApi/PointerWrapper/PointerWrapper.x.h>

#endif