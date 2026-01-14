#pragma once

#include <GoApi/GoApiDef.h>
#include <GoApi/Updating/ScopedUpdater.h>
#include <functional>

namespace Go
{

/*! @class BeginEndUpdater BeginEndUpdater.h "GoApi/GoApi/Updating/BeginEndUpdater.h"
 *  @brief Helper class for keeping track of events between a BeginUpdate()
 *         and EndUpdate() cycle.
 *
 *  @remarks  Example usage see below:
 *  @code {.cpp}
 *  class FooClass
 *  {
 *  public:
 *      FooClass()
 *      {
 *      }
 *
 *      void UpdateSomePart()
 *      {
 *          OnUpdated();
 *      }
 *
 *      void UpdateSomeOtherPart()
 *      {
 *          OnUpdated();
 *      }
 *
 *      Go::Event<> UpdatedEvent()
 *      {
 *           return updatedEvent;
 *      }
 *
 *      void OnUpdated()
 *      {
 *          updater.SoftUpdate([this]() {
 *             updatedEvent.Notify();
 *          });
 *      }
 *
 *      void BeginUpdate()
 *      {
 *          updater.BeginUpdate();
 *      }
 *
 *      void EndUpdate()
 *      {
 *          updater.EndUpdate([this]() {
 *             updatedEvent.Notify();
 *          });
 *
 *      }
 *
 *  private:
 *      Go::Event<> updatedEvent;
 *      BeginEndUpdater updater;
 *  };
 *  @endcode
 */
class GoApiClass BeginEndUpdater
{
public:
    /**
     * Constructor.
     */
    BeginEndUpdater();

    ~BeginEndUpdater() = default;

    /**
     * Begins an update cycle.
     */
    void BeginUpdate();

    /**
     * Similar to BeginUpdate, but returns an object that will call EndUpdate upon deletion.
     * 
     * @return              ScopedUpdater that calls this->EndUpdate(updateFunc) upon deletion.
     */
    ScopedUpdater BeginScopedUpdate(std::function<void()> updateFunc = [](){});

    /**
     * Returns whether we are currengly within an update cycle.
     *
     * @return true if within an update cycle, false otherwise.
     */
    bool IsUpdating();

    /**
     * Performs an update if not during an update cycle, otherwise increments an internal counter
     * checked when @ref EndUpdate() is called.
     * 
     * This can be used in setter functions to immediately apply an update if not in an update cycle
     * but wait until EndUpdate() to apply multiple changes at once if it is in an update cycle.
     *
     * @param updateFunc    Callback funciton to perform if not during an update cycle.
     */
    void SoftUpdate(std::function<void()> updateFunc);

    /**
     * Ends an update cycle without performing any update action.
     */
    void EndUpdate();

    /**
     * Performs an update when ending an update cycle, if internal counter was incremented
     * with @ref SoftUpdate().
     * 
     * This allows multiple updates to be applied at once if performing a bulk update.
     *
     * @param updateFunc Callback function to perform upon ending an update cycle.
     */
    void EndUpdate(std::function<void()> updateFunc);

private:
    bool updating;
    size_t updatedCount;
};

} // namespace
