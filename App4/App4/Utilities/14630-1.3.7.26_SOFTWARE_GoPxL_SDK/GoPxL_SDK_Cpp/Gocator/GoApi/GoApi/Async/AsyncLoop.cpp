#include "AsyncLoop.h"
#include <GoApi/Threads/Locker.h>
#include <kApi/Threads/kLock.h>
#include <kApi/Threads/kSemaphore.h>
#include <kApi/Threads/kTimer.h>
#include <GoApi/Logging.h>

namespace Go
{

AsyncLoop* AsyncLoop::mainLoop = nullptr;

void AsyncLoop::CreateMainLoop()
{
    if (!mainLoop)
    {
        mainLoop = new AsyncLoop();
    }
}

void AsyncLoop::DestroyMainLoop()
{
    if (mainLoop)
    {
        delete mainLoop;
        mainLoop = nullptr;
    }
}

AsyncLoop& AsyncLoop::MainLoop()
{
    if (!mainLoop)
    {
        CreateMainLoop();
    }

    return *mainLoop;
}

bool AsyncLoop::OnMainLoop()
{
    // This call should not create the main loop if it doesn't exist.
    if (mainLoop)
    {
        return mainLoop->loopThread.Get() && kThread_IsSelf(mainLoop->loopThread.Get());
    }

    return false;
}

AsyncLoop::AsyncLoop() :
    scheduledQueue()
{
    exitFlag = false;

    GoTest(kSemaphore_Construct(queueSem.Ref(), 0, kAlloc_App()));
    GoTest(kLock_Construct(lock.Ref(), kAlloc_App()));
    GoTest(kThread_Construct(loopThread.Ref(), kAlloc_App()));
    GoTest(kThread_Start(loopThread, LoopThreadFx, this, "AsyncLoop.Loop"));
}

AsyncLoop::~AsyncLoop()
{
    // It's a problem if there are still handlers registered, because they
    // may call Unregister after this object is destroyed.
    // There's nothing we can do about this except raising an error to prompt
    // a code review.
    if (!handlers.empty())
    {   
        GoLogError("~AsyncLoop() callled but %lu handler(s) have not been unregistered.", handlers.size());
        kAssert(0);
    }

    exitFlag = true;
    kSemaphore_Post(queueSem);
    loopThread.Reset();
}

void AsyncLoop::BeginShutdown()
{
    if (!mainLoop) 
    {
        return;
    }
    GoLogUserInfoIfDev("AsyncLoop", "Beginning shutdown...", mainLoop->immediateQueue.size());

    GoWithLock(Go::Locker(mainLoop->lock))
    {
        mainLoop->exitFlag = true;

        GoLogUserInfoIfDev("AsyncLoop", "Clearing Immediate Queue, %zu outstanding items removed.", mainLoop->immediateQueue.size());
        while (!mainLoop->immediateQueue.empty()) 
        { 
            mainLoop->immediateQueue.pop(); 
        }

        GoLogUserInfoIfDev("AsyncLoop", "Clearing Scheduled Queue, %zu outstanding items removed.", mainLoop->scheduledQueue.size());
        while (!mainLoop->scheduledQueue.empty())
        { 
            mainLoop->scheduledQueue.pop(); 
        }
    }

    kSemaphore_Post(mainLoop->queueSem);
    kThread_Join(mainLoop->loopThread, kINFINITE, kNULL);
}

AsyncLoop::HandlerRef AsyncLoop::RegisterHandler(IAsyncEventReceiver& receiver)
{
    // Create the control block.
    auto control = std::make_shared<HandlerControlBlock>();
    control->active = true;
    control->receiver = &receiver;
    GoTest(kLock_Construct(control->lock.Ref(), kAlloc_App()));

    // Create the handler.
    auto handler = new Handler();
    handler->control = std::move(control);

    // Set up the bidirectional link.
    GoWithLock(Go::Locker(lock))
    {
        auto listIter = handlers.insert(handlers.end(), handler);
        handler->listRef = listIter;
    }

    // Return the pointer directly which can be represented as an opaque pointer,
    // unlike a list iterator.
    return handler;
}

void AsyncLoop::UnregisterHandler(AsyncLoop::HandlerRef handlerRef)
{
    auto handler = static_cast<Handler*>(handlerRef);

    // Ensures that any outstanding events in the queue won't be sent to the
    // receiver after this point. The assumption is that this function is
    // called from the event loop thread.
    GoWithLock(Go::Locker(handler->control->lock))
    {
        handler->control->active = false;
    }    

    // Erase the objects.
    handlers.erase(handler->listRef);
    delete handler;
}

void AsyncLoop::EnqueueImmediate(HandlerRef handlerRef)
{
    auto handler = static_cast<Handler*>(handlerRef);

    // Use a standard queue rather than the priority queue for scheduled
    // events. The priority queue is not necessary "stable" (preserving order
    // for items of equal priority). Order preservation is a good property,
    // even if it is not strictly necessary for now.

    GoWithLock(Go::Locker(lock))
    {
        if (exitFlag) { return; } // early exit if we're in process of shutting down the loop.

        immediateQueue.emplace(handler->control);
    }

    GoTest((kSemaphore_Post(queueSem)));
}

void AsyncLoop::EnqueueScheduled(HandlerRef handlerRef, k64u targetTime)
{
    auto handler = static_cast<Handler*>(handlerRef);

    // Because scheduled events are in a different queue, they are asynchronous
    // to immediate events. This is OK, because scheduled events are primarily
    // dependent on the target time, rather than when the event is enqueued.
    // While there are potentially some niche cases where events may be scheduled
    // at precisely the same time, this is not a case we are designing for.
    // Also, even in that case, comparing scheduled events to immediate events
    // is not in principle meaningful, since "immediate" does not translate into
    // a precise time target. It can always be shifted earlier or later without
    // compromising correctness.

    GoWithLock(Go::Locker(lock))
    {
        if (exitFlag) { return; } // early exit if we're in process of shutting down the loop.

        scheduledQueue.emplace(handler->control, targetTime);
    }

    GoTest((kSemaphore_Post(queueSem)));
}

void AsyncLoop::RunLoop()
{
    k64u waitTime = kINFINITE;

    while (!exitFlag)
    {
        kStatus status = kSemaphore_Wait(queueSem, waitTime);

        if (status != kOK && status != kERROR_TIMEOUT)
        {
            GoThrowMsg(status, "Async loop semaphore wait failed (%d).", status);
        }

        if (exitFlag)
        {
            GoLogInfo("Async loop exiting normally.");
            break;
        }

        ProcessImmediateEvents();

        waitTime = ProcessScheduledEvents();
    }
}

// This processes all events in the immediate queue at the time of call.
void AsyncLoop::ProcessImmediateEvents()
{
    // Record the current count and run only this many events.
    // Prevents this function from looping indefinitely if a continuous
    // stream of events comes in.
    size_t currentCount = 0;
    GoWithLock(Go::Locker(lock))
    {
        currentCount = immediateQueue.size();
    }

    while (currentCount > 0)
    {
        std::shared_ptr<HandlerControlBlock> control = nullptr;

        GoWithLock(Go::Locker(lock))
        {
            if (exitFlag)
            {
                return;
            }

            // Should never happen.
            kAssert(!immediateQueue.empty());

            control = immediateQueue.front().control;
            immediateQueue.pop();
            currentCount--;
        }

        if (control != nullptr)
        {
            NotifyControl(*control);
        }
    }
}

// This processes all qualifying events in the scheduled queue at the time of call.
// Qualified meaning their target time is at or before the current time.
k64u AsyncLoop::ProcessScheduledEvents()
{
    k64u currentTime = kTimer_Now();
    k64u waitTime = kINFINITE;
    bool needToWait = false;

    // Record the current count and run only up to this many events.
    // Prevents this function from looping indefinitely if a continuous
    // stream of events comes in.
    size_t currentCount = 0;
    GoWithLock(Go::Locker(lock))
    {
        currentCount = scheduledQueue.size();
    }

    while (currentCount > 0 && !needToWait)
    {
        std::shared_ptr<HandlerControlBlock> control = nullptr;

        GoWithLock(Go::Locker(lock))
        {
            if(exitFlag)
            {
                return 0;
            }

            // Should never happen.
            kAssert(!scheduledQueue.empty());

            auto& top = scheduledQueue.top();

            if (top.targetTime <= currentTime)
            {
                control = top.control;
                scheduledQueue.pop();
                currentCount--;
            }
            else
            {
                needToWait = true;
                waitTime = top.targetTime - currentTime;
            }
        }

        // Make sure to run the callback outside of the lock to avoid holding
        // up the schedulers.
        if (control != nullptr)
        {
            NotifyControl(*control);
        }
    }

    return waitTime;
}

void AsyncLoop::NotifyControl(HandlerControlBlock& control)
{
    // This lock ensures that the check for "active" and the processing
    // are atomic. Otherwise there could be a race condition where the
    // handler is deactivated (handler destroyed) but the call continues anyway.
    GoWithLock(Go::Locker(control.lock))
    {
        if (control.active)
        {
            control.receiver->ProcessEvent();
        }
    }
}

kStatus kCall AsyncLoop::LoopThreadFx(kPointer context)
{
    auto obj = static_cast<AsyncLoop*>(context);

    GoSetTerminateHandler();

    GoBeginExceptionHandler();

    obj->RunLoop();

    GoEndExceptionHandler();

    return kOK;
}

AsyncLoop::QueuedEvent::QueuedEvent(std::shared_ptr<HandlerControlBlock> control, k64u targetTime) :
    control(control), targetTime(targetTime)
{
}

bool AsyncLoop::QueuedEvent::operator>(const AsyncLoop::QueuedEvent& other) const
{
    // std::priority_queue::top() returns the last item, so to get the
    // smallest item, the list must be sorted in descending order.
    return this->targetTime > other.targetTime;
}

} // namespace
