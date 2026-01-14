#include "AsyncRunner.h"
#include <GoApi/Threads/Locker.h>
#include <GoApi/Logging.h>
#include <kApi/Threads/kTimer.h>

namespace Go
{

AsyncRunner::AsyncRunner(AsyncLoop& loop) :
    loop(loop),
    handlerRef(nullptr)
{
    handlerRef = loop.RegisterHandler(*this);
    kAtomic32s_Init(&notifyPending, 0);
    kAtomic32s_Init(&waitPending, 0);
    GoTest(kLock_Construct(callbackQueueLock.Ref(), kAlloc_App()));
}

AsyncRunner::AsyncRunner() :
    AsyncRunner(Go::AsyncLoop::MainLoop())
{
}

AsyncRunner::~AsyncRunner()
{
    loop.UnregisterHandler(handlerRef);
    handlerRef = nullptr;
}

void AsyncRunner::Run(AsyncCallbackFx callback)
{
    GoWithLock(Go::Locker(callbackQueueLock))
    {
        callbackQueue.push(callback);
    }

    // Coalesce unprocessed callbacks.
    if (kAtomic32s_CompareExchange(&notifyPending, 0, 1))
    {
        loop.EnqueueImmediate(handlerRef);
    }
}

void AsyncRunner::RunAndWait(AsyncCallbackFx callback, k64u timeout)
{
    std::string exceptionMsg;
    bool exceptionOccurred = false;

    // Construct the semaphore only if user calls this function.
    if (!waitSem)
    {
        GoTest(kSemaphore_Construct(waitSem.Ref(), 0, kAlloc_App()));
    }

    // Run a thunk that triggers the callback and then a semaphore.
    Run([callback, this, &exceptionMsg, &exceptionOccurred]() {

        try
        {
            callback();
        }
        catch (const std::exception& e)
        {
            // With vanilla C++ it's not possible to simply clone the exception
            // object polymorphically to preserve its type. So we have to coerce
            // it into a static type, a string in this case.
            exceptionMsg = e.what();
            exceptionOccurred = true;

            // TODO: Perhaps move to using Go::Exception::PrintException()
            //       in order to obtain the "full" exception trace.
            //       e.what() only contains the "current" exception.
            //       -- OR -- attempt to log the entire exception here.

            // For now, we have to log the exception otherwise we lose the
            // context nesting and backtrace.
            GoLogExceptionMsg(e, "RunAndWait failed.");
        }

        // This counter is needed to handle timeouts.
        // Make sure to set it before signalling the semaphore.
        kAtomic32s_Increment(&this->waitPending);
        GoTest(kSemaphore_Post(this->waitSem));
    });

    // Wait for the semaphore. Note that previous waits may have failed, so
    // the semaphore may be signalled by a previous call. The loop needs to
    // account for this.
    do
    {
        k64u startTime = kTimer_Now();

        GoTest(kSemaphore_Wait(waitSem, timeout));

        k64u elapsedTime = kTimer_Now() - startTime;

        // Adjust the timeout to ensure the overall wait does not exceed
        // the parameter.
        if (timeout != kINFINITE)
        {
            if (elapsedTime < timeout)
            {
                timeout -= elapsedTime;
            }
            else
            {
                timeout = 0;
            }
        }

    } while (kAtomic32s_Decrement(&waitPending) > 0);

    if (exceptionOccurred)
    {
        GoThrowMsg(kERROR_ABORT, "Async function failed (%s)", exceptionMsg.c_str());
    }
}

size_t AsyncRunner::EnqueuedCount()
{
    size_t queueSize = 0;

    GoWithLock(Go::Locker(callbackQueueLock))
    {
        queueSize = callbackQueue.size();
    }

    return queueSize;
}

void AsyncRunner::ProcessEvent()
{
    kAtomic32s_Exchange(&notifyPending, 0);

    bool empty = false;

    // Run all queued callbacks.
    do
    {
        AsyncCallbackFx callback;

        GoWithLock(Go::Locker(callbackQueueLock))
        {
            if (!callbackQueue.empty())
            {
                callback = callbackQueue.front();
                callbackQueue.pop();
            }

            empty = callbackQueue.empty();
        }

        // Run the callback outside of the lock to avoid holding up the threads
        // queuing the calls.
        if (callback)
        {
            try
            {
                callback();
            }
            catch (const std::exception& e)
            {
                GoLogError("AsyncRunner callback exception: %s", e.what());
            }
        }

    } while (!empty);
}

} // namespace
