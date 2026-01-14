#pragma once

#include <GoApi/GoApiDef.h>
#include <GoApi/Exception.h>
#include <functional>

namespace Go
{

class BeginEndUpdater;

/*! @class ScopedUpdater ScopedUpdater.h "GoApi/GoApi/Updating/ScopedUpdater.h"
 *  @brief Helper class for keeping track of a @ref Go::BeginEndUpdater update cycle within a given scope.
 * 
 *  @remarks  Example usage see below. Note that even if an exception is thrown, the scopedUpdater will properly conclude the update cycle upon function exit.
 *  @code {.cpp}
 *  void Update()
 *  {
 *      ScopedUpdater scopedUpdater(&beginEndUpdater, &OnUpdateEnd);
 * 
 *      ModifyParams();
 *      CodeThatMayThrow();
 *      ModifyMoreParams();
 * 
 *  }
 *  @endcode
 */
class GoApiClass ScopedUpdater
{
public:
    /**
     * Constructor
     * 
     * @param   updater     The BeginEndUpdater being updated.
     * @param   updateFunc  Callback function to invoke when ending the update.
     */
    ScopedUpdater(BeginEndUpdater* updater, std::function<void()> updateFunc = [](){});

    ~ScopedUpdater();

    /**
     * Move Constructor
     *
     * @param   other       rvalue reference to another ScopedUpdater object
     */
    ScopedUpdater(ScopedUpdater&& other) noexcept;

    /**
     * Move assignment operator
     *
     * @param   other       rvalue reference to another ScopedUpdater object
     */
    ScopedUpdater& operator=(ScopedUpdater&& other) noexcept;

    /**
     * Boolean operator to support GoWithScopedLock. Always returns true.
     */
    operator bool() const;

    // Make class non-copyable.
    ScopedUpdater(const ScopedUpdater&) = delete;
    ScopedUpdater& operator=(const ScopedUpdater&) = delete;

private:
    BeginEndUpdater* updater;
    std::function<void()> endUpdateCallback;
};

}

/**
 * Helper macro to run code with a scoped Updater.
 *
 * This macro can be used to write cleaner code when updating a module.
 * Example:
 * @code
 * GoWithScopedUpdate(FunctionReturnsScopedUpdater())
 * {
 *     CodeUpdatingModule();
 * }
 * @endcode
 *
 * @param   SCOPED_UPDATER  Expression that returns a @ref Go::ScopedUpdater object.
 */
#define GoWithScopedUpdate(SCOPED_UPDATER)                      \
    if (Go::ScopedUpdater GoWithUpdaterCtrl__ = SCOPED_UPDATER)
