/**\file    Locker.h
 * GoApi Locker class
 *
 * RAII implementation of locks to allow for exception safe locking
 * This class should be used in place of locks
 */

#ifndef GO_LOCKER_H
#define GO_LOCKER_H

#include <GoApi/GoApiDef.h>
#include <kApi/Threads/kLock.h>
#include <GoApi/Exception.h>

namespace Go
{

/**
 * Locker class used for exception safe lock handling.
 */
class Locker
{
public:
    /**
    * Constructor
    *
    * \param lock a kLock object that will be used for locking/unlocking
    * \param timeout optional timeout field that defaults to kINFINITE if not specified
    *                If specified will define the time to wait before timing out in microseconds
    */
    Locker(kLock lock, const k64u timeout = kINFINITE) : lock(lock)
    {
        if (timeout == kINFINITE)
        {
            GoTest(kLock_Enter(lock));
        }
        else
        {
            GoTest(kLock_EnterEx(lock, timeout));
        }
    }

    /**
    * Destructor the destructor will unlock the lock if there is one assigned.
    */
    ~Locker()
    {
        // Never let exceptions escape from dtor.
        try
        {
            if (lock != kNULL)
            {
                GoTest(kLock_Exit(lock));
            }
        }
        catch (const std::exception& e)
        {
            GoLogExceptionMsg(e, "Exception thrown in destructor");
        }
    }

    // Move Constructor and Assignment Operator
    /**
    * Move Constructor
    *
    * \param other rvalue reference to a Locker object
    */
    Locker (Locker&& other) noexcept : lock(other.lock)
    {
        other.lock = kNULL;
    }

    /**
    * Move assignment operator
    *
    * \param other rvalue ref to another Locker object
    */
    Locker& operator=(Locker&& other) noexcept
    {
        kLock temp = other.lock;
        other.lock = kNULL;
        this->lock = temp;

        return *this;
    }

    // Make class non-copyable
    Locker(const Locker&) = delete;
    Locker& operator=(const Locker&) = delete;

private:
    kLock lock;
};

/**
 * Helper macro to run code with lock.
 *
 * This macro can be used to write cleaner code when using locks.
 * Example:
 * @code
 * GoWithLock(FunctionReturnsLock())
 * {
 *     CodeRequiringLock();
 * }
 * @endcode
 *
 * @param   LOCK        Expression that returns a @ref Go::Locker object.
 */
#define GoWithLock(LOCK)                                            \
    for(std::pair<Go::Locker, bool> GoWithLockCtrl__(LOCK, true);   \
    GoWithLockCtrl__.second;                                        \
    GoWithLockCtrl__.second = false)

}; // namespace

#endif
