/** 
 * @file    kAtomic.h
 * @brief   Declares atomic operation classes. 
 *
 * @internal
 * Copyright (C) 2012-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#include <kApi/kApiDef.h>   //--inclusion order controlled by kApiDef

#ifndef K_API_ATOMIC_H
#define K_API_ATOMIC_H

#include <kApi/Threads/kAtomic.x.h>

/**
* @struct  kAtomic32s
* @extends kValue
* @ingroup kApi-Threads
* @brief   Represents a 32-bit, atomically-accessed, signed integer.
* @see     kAtomic, k32s
*/
//typedef xkAtomic32s kAtomic32s;           // --forward-declared in kApiDef.x.h 

/** 
 * Initializes an atomic variable with a particular value. 
 * 
 * This method is not thread-safe. It can be used for first initialization of an 
 * atomic value, before subsequent concurrent access.  
 *
 * @public              @memberof kAtomic32s
 * @param   atomic      Pointer to  atomic variable.
 * @param   value       Initial value.
 */
kInlineFx(void) kAtomic32s_Init(kAtomic32s* atomic, k32s value)
{
    *atomic = value;
}

/** 
 * Increments an atomic variable. 
 * 
 * This method is thread-safe. Implements a full memory barrier. 
 *
 * @public              @memberof kAtomic32s
 * @param   atomic      Pointer to  atomic variable.
 * @return              New atomic value.
 */
kInlineFx(k32s) kAtomic32s_Increment(kAtomic32s* atomic)
{
   return xkAtomic32s_IncrementImpl(atomic); 
}

/** 
 * Decrements an atomic variable. 
 * 
 * This method is thread-safe. Implements a full memory barrier. 
 *
 * @public              @memberof kAtomic32s
 * @param   atomic      Pointer to atomic variable.
 * @return              New atomic value.
 */
kInlineFx(k32s) kAtomic32s_Decrement(kAtomic32s* atomic)
{
   return xkAtomic32s_DecrementImpl(atomic);
}

/** 
 * Exchanges the value of an atomic variable.
 * 
 * This method is thread-safe. Implements a full memory barrier. 
 *
 * @public              @memberof kAtomic32s
 * @param   atomic      Pointer to atomic variable.
 * @param   value       New atomic value.
 * @return              Previous atomic value.
 */
kInlineFx(k32s) kAtomic32s_Exchange(kAtomic32s* atomic, k32s value)
{
   return xkAtomic32s_ExchangeImpl(atomic, value); 
}

/** 
 * Conditionally exchanges the value of an atomic variable.
 * 
 * If the atomic value is equal to the oldValue argument, then it is replaced by the value argument. 
 *
 * This method is thread-safe. Implements a full memory barrier. 
 *
 * @public              @memberof kAtomic32s
 * @param   atomic      Pointer to atomic variable.
 * @param   oldValue    Previous atomic value.
 * @param   value       New atomic value.
 * @return              kTRUE if the exchange succeeded. 
 */
kInlineFx(kBool) kAtomic32s_CompareExchange(kAtomic32s* atomic, k32s oldValue, k32s value)
{
   return xkAtomic32s_CompareExchangeImpl(atomic, oldValue, value); 
}

/** 
 * Gets the current value of an atomic variable. 
 * 
 * This method is thread-safe. Implements a full memory barrier. 
 *
 * @public              @memberof kAtomic32s
 * @param   atomic      Pointer to atomic variable.
 * @return              Atomic value.
 */
kInlineFx(k32s) kAtomic32s_Get(kAtomic32s* atomic)
{
   return xkAtomic32s_GetImpl(atomic); 
}

/**
* @struct  kAtomic64s
* @extends kValue
* @ingroup kApi-Threads
* @brief   Represents a 64-bit, atomically-accessed, signed integer.
* @see     kAtomic, k64s
*/
//typedef xkAtomic64s kAtomic64s;           // --forward-declared in kApiDef.x.h 

/**
 * Initializes an atomic variable with a particular value.
 *
 * This method is not thread-safe. It can be used for first initialization of an
 * atomic value, before subsequent concurrent access.
 *
 * @public              @memberof kAtomic64s
 * @param   atomic      Pointer to  atomic variable.
 * @param   value       Initial value.
 */
kInlineFx(void) kAtomic64s_Init(kAtomic64s* atomic, k64s value)
{
    *atomic = value;
}

/**
 * Increments an atomic variable.
 *
 * This method is thread-safe. Implements a full memory barrier.
 *
 * @public              @memberof kAtomic64s
 * @param   atomic      Pointer to  atomic variable.
 * @return              New atomic value.
 */
kInlineFx(k64s) kAtomic64s_Increment(kAtomic64s* atomic)
{
    return xkAtomic64s_IncrementImpl(atomic);
}

/**
 * Decrements an atomic variable.
 *
 * This method is thread-safe. Implements a full memory barrier.
 *
 * @public              @memberof kAtomic64s
 * @param   atomic      Pointer to atomic variable.
 * @return              New atomic value.
 */
kInlineFx(k64s) kAtomic64s_Decrement(kAtomic64s* atomic)
{
    return xkAtomic64s_DecrementImpl(atomic);
}

/**
 * Exchanges the value of an atomic variable.
 *
 * This method is thread-safe. Implements a full memory barrier.
 *
 * @public              @memberof kAtomic64s
 * @param   atomic      Pointer to atomic variable.
 * @param   value       New atomic value.
 * @return              Previous atomic value.
 */
kInlineFx(k64s) kAtomic64s_Exchange(kAtomic64s* atomic, k64s value)
{
    return xkAtomic64s_ExchangeImpl(atomic, value);
}

/**
 * Conditionally exchanges the value of an atomic variable.
 *
 * If the atomic value is equal to the oldValue argument, then it is replaced by the value argument.
 *
 * This method is thread-safe. Implements a full memory barrier.
 *
 * @public              @memberof kAtomic64s
 * @param   atomic      Pointer to atomic variable.
 * @param   oldValue    Previous atomic value.
 * @param   value       New atomic value.
 * @return              kTRUE if the exchange succeeded.
 */
kInlineFx(kBool) kAtomic64s_CompareExchange(kAtomic64s* atomic, k64s oldValue, k64s value)
{
    return xkAtomic64s_CompareExchangeImpl(atomic, oldValue, value);
}

/**
 * Gets the current value of an atomic variable.
 *
 * This method is thread-safe. Implements a full memory barrier.
 *
 * @public              @memberof kAtomic64s
 * @param   atomic      Pointer to atomic variable.
 * @return              Atomic value.
 */
kInlineFx(k64s) kAtomic64s_Get(kAtomic64s* atomic)
{
    return xkAtomic64s_GetImpl(atomic);
}

/**
* @struct  kAtomicPointer
* @extends kValue
* @ingroup kApi-Threads
* @brief   Represents an atomically-accessed pointer.
* @see     kAtomic, kPointer
*/
//typedef xkAtomicPointer kAtomicPointer;       // --forward-declared in kApiDef.x.h 

/** 
 * Initializes an atomic variable with a particular value. 
 * 
 * This method is not thread-safe. It can be used for first initialization of an 
 * atomic value, before subsequent concurrent access.  
 *
 * @public              @memberof kAtomicPointer
 * @param   atomic      Pointer to  atomic variable.
 * @param   value       Initial value.
 */
kInlineFx(void) kAtomicPointer_Init(kAtomicPointer* atomic, kPointer value)
{
    *(kPointer*) atomic = value;
}

/** 
 * Exchanges the value of an atomic variable.
 * 
 * This method is thread-safe. Implements a full memory barrier. 
 *
 * @public              @memberof kAtomicPointer
 * @param   atomic      Pointer to atomic variable.
 * @param   value       New atomic value.
 * @return              Previous atomic value.
 */
kInlineFx(kPointer) kAtomicPointer_Exchange(kAtomicPointer* atomic, kPointer value)
{
   return xkAtomicPointer_ExchangeImpl(atomic, value); 
}

/** 
 * Conditionally exchanges the value of an atomic variable.
 * 
 * If the atomic value is equal to the oldValue argument, then it is replaced by the value argument. 
 *
 * This method is thread-safe. Implements a full memory barrier. 
 *
 * @public              @memberof kAtomicPointer
 * @param   atomic      Pointer to atomic variable.
 * @param   oldValue    Previous atomic value.
 * @param   value       New atomic value.
 * @return              Previous atomic value.
 */
kInlineFx(kBool) kAtomicPointer_CompareExchange(kAtomicPointer* atomic, kPointer oldValue, kPointer value)
{
   return xkAtomicPointer_CompareExchangeImpl(atomic, oldValue, value); 
}

/** 
 * Gets the current value of an atomic variable. 
 * 
 * This method is thread-safe. Implements a full memory barrier. 
 *
 * @public              @memberof kAtomicPointer
 * @param   atomic      Pointer to atomic variable.
 * @return              Atomic value.
 */
kInlineFx(kPointer) kAtomicPointer_Get(kAtomicPointer* atomic)
{
   return xkAtomicPointer_GetImpl(atomic); 
}

#endif
