#include "AsyncTimer.h"
#include <kApi/Threads/kTimer.h>
#include <GoApi/Logging.h>

namespace Go
{

AsyncTimer::AsyncTimer(AsyncLoop& loop, AsyncCallbackFx callback) :
    loop(loop),
    handlerRef(nullptr),
    callback(callback)
{
}

AsyncTimer::AsyncTimer(AsyncLoop& loop, AsyncTimerCallbackFx callback) :
    loop(loop),
    handlerRef(nullptr)
{
    this->callback = [callback, this]() {
        callback(*this);
    };
}

AsyncTimer::AsyncTimer(AsyncTimerCallbackFx callback) :
    AsyncTimer(Go::AsyncLoop::MainLoop(), callback)
{
}

AsyncTimer::~AsyncTimer()
{
    if (handlerRef != nullptr)
    {
        loop.UnregisterHandler(handlerRef);
    }
}

void AsyncTimer::Start(k64u period, bool immediateCall)
{
    // May have been unregistered by a previous Stop call.
    if (!handlerRef)
    {
        handlerRef = loop.RegisterHandler(*this);
    }

    this->period = period;

    if (immediateCall)
    {
        loop.EnqueueImmediate(handlerRef);
    }
    else
    {
        k64u nextCallTime = kTimer_Now() + period;
        loop.EnqueueScheduled(handlerRef, nextCallTime);
    }
}

void AsyncTimer::Stop()
{
    // Unregistering the handler discards all outstanding events.
    if (handlerRef)
    {
        loop.UnregisterHandler(handlerRef);
        handlerRef = nullptr;
    }
}

void AsyncTimer::ProcessEvent()
{
    // GOS-6961: handlerRef can be nullptr if event processing is done after Stop().
    if (!handlerRef)
    {
        GoLogWarn("AsyncTimer event processing found handlerRef was nullptr");
    }
    else
    {
        // Calculate the next call time before running the callback.
        // Period should not be affected by the duration of the callback.
        k64u nextCallTime = kTimer_Now() + period;
        loop.EnqueueScheduled(handlerRef, nextCallTime);

        try
        {
            callback();
        }
        catch (const std::exception& e)
        {
            GoLogExceptionMsg(e, "AsyncTimer callback exception: ");
        }
    }
}

} // namespace
