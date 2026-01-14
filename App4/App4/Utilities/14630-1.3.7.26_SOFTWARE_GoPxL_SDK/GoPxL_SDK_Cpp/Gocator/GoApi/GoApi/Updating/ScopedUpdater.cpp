#include "ScopedUpdater.h"
#include <GoApi/Updating/BeginEndUpdater.h>

namespace Go
{

ScopedUpdater::ScopedUpdater(BeginEndUpdater* updater, std::function<void()> updateFunc) :
    updater(updater),
    endUpdateCallback(updateFunc)
{
    kAssert(updater != nullptr);

    updater->BeginUpdate();
}

ScopedUpdater::~ScopedUpdater()
{
    try
    {
        if (updater)
        {
            updater->EndUpdate(endUpdateCallback);
        }
    }
    catch (const std::exception& e)
    {
        GoLogExceptionMsg(e, "Exception thrown in destructor");
    }
}

ScopedUpdater::ScopedUpdater(ScopedUpdater&& other) noexcept :
    updater(other.updater),
    endUpdateCallback(other.endUpdateCallback)
{
    other.updater = nullptr;
}

ScopedUpdater& ScopedUpdater::operator=(ScopedUpdater&& other) noexcept
{
    this->updater = other.updater;
    this->endUpdateCallback = other.endUpdateCallback;

    other.updater = nullptr;

    return *this;
}

ScopedUpdater::operator bool() const
{
    return true;
}

}
