#include "BeginEndUpdater.h"

namespace Go
{

BeginEndUpdater::BeginEndUpdater()
{
    updating = false;
    updatedCount = 0;
}

void BeginEndUpdater::BeginUpdate()
{
    updating = true;
}

ScopedUpdater BeginEndUpdater::BeginScopedUpdate(std::function<void()> updateFunc)
{
    return ScopedUpdater(this, updateFunc);
}

bool BeginEndUpdater::IsUpdating()
{
    return updating;
}

void BeginEndUpdater::SoftUpdate(std::function<void()> updateFunc)
{
    if (updating)
    {
        // Within updating cycle, so remember the update.
        updatedCount++;
    }
    else
    {
        updateFunc();
    }
}

void BeginEndUpdater::EndUpdate()
{
    EndUpdate([](){});
}

void BeginEndUpdater::EndUpdate(std::function<void()> updateFunc)
{
    // Update is ending.
    if (updating)
    {
        updating = false;
        if (updatedCount > 0)
        {
            updatedCount = 0;
            updateFunc();
        }
    }
}

} // namespace
