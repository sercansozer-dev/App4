#include "AsyncNotifier.h"
#include <GoApi/Logging.h>

namespace Go
{

AsyncNotifier::AsyncNotifier(AsyncLoop& loop, AsyncCallbackFx callback) :
    loop(loop),
    handlerRef(nullptr),
    callback(callback)
{
    handlerRef = loop.RegisterHandler(*this);
    kAtomic32s_Init(&notifyPending, 0);
}

AsyncNotifier::AsyncNotifier(AsyncCallbackFx callback) :
    AsyncNotifier(Go::AsyncLoop::MainLoop(), callback)
{
}

AsyncNotifier::~AsyncNotifier()
{
    if (handlerRef != nullptr)
    {
        loop.UnregisterHandler(handlerRef);
    }
}

void AsyncNotifier::Notify()
{
    // Trigger a callback only if there is no other pending.
    if (kAtomic32s_CompareExchange(&notifyPending, 0, 1))
    {
        loop.EnqueueImmediate(handlerRef);
    }
}

void AsyncNotifier::ProcessEvent()
{
    // Note that the call is no longer considered to be pending, even before
    // it actually runs. This is OK because this window is very brief, and
    // the effect of hitting this window means sometimes the call will be
    // made when it's not strictly necessary.
    kAtomic32s_Exchange(&notifyPending, 0);

    try
    {
        callback();
    }
    catch (const std::exception& e)
    {
        GoLogExceptionMsg(e, "AsyncNotifier callback exception: ");
    }
}

} // namespace
