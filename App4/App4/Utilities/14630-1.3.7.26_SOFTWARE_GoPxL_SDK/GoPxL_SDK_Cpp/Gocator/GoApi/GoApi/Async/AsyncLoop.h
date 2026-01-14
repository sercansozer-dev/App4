/**\file    AsyncLoop.h
 * Declares the Go::AsyncLoop class.
 */
#ifndef GO_ASYNC_LOOP_H
#define GO_ASYNC_LOOP_H

#include <GoApi/GoApiDef.h>
#include <GoApi/Object.h>
#include <kApi/Threads/kThread.h>
#include <functional>
#include <queue>
#include <list>
#include <memory>

namespace Go
{
    
/**
 * A standard async callback function.
 */
using AsyncCallbackFx = std::function<void()>;

/**
 * Interface implemented by async schedulers.
 *
 * This is an internal interface used by async implementations.
 */
class IAsyncEventReceiver
{
public:
    virtual ~IAsyncEventReceiver() = default;

    /**
     * Notifies the async implementation to process an event.
     *
     * This method is guaranteed to be called from the event thread.
     *
     * For more information, see these methods:
     *
     * - @ref AsyncLoop::RegisterHandler
     * - @ref AsyncLoop::EnqueueImmediate
     * - @ref AsyncLoop::EnqueueScheduled
     */
    virtual void ProcessEvent() = 0;
};

/**
 * Represents an event loop.
 *
 * Each event loop encapsulates a worker thread, that allows events to be
 * posted from any thread, then dispatched to their targets for processing
 * from the worker thread. The effect is that handling of events are serialized,
 * even though they may arrive from different threads.
 *
 * For the most part, this class should not be used directly, but instead passed
 * into these helper classes:
 *
 * - @ref AsyncRunner
 * - @ref AsyncTimer
 * - @ref AsyncNotifier
 *
 * For convenience, this class defines a singleton "main loop", accessible via
 * these methods:
 *
 * - @ref MainLoop()
 * - @ref CreateMainLoop()
 * - @ref DestroyMainLoop()
 *
 * Unless specifically needed otherwise, the main loop should be the default choice.
 */
class GoApiClass AsyncLoop
{
public:

    /**
     * Creates the main loop if it doesn't already exist.
     *
     * If the loop already exists, this method does nothing.
     */
    static void CreateMainLoop();

    /**
     * Destroys the main loop if it exists.
     *
     * If the loop does not exist, this method does nothing.
     */
    static void DestroyMainLoop();

    /**
     * Returns the main loop.
     *
     * If the main loop does not exist, it is created.
     *
     * @return      Reference to the main loop.
     */
    static AsyncLoop& MainLoop();

    /**
     * Returns whether or not the caller is running on the main loop thread.
     * 
     * @return      True if on the main loop, otherwise false.
     */
    static bool OnMainLoop();

    /**
     * Creates an async loop.
     */
    AsyncLoop();

    /**
     * Destructs the async loop.
     *
     * Care must be taken to unregister all event handlers before destroying
     * a loop object (see @ref UnregisterHandler).
     */
    ~AsyncLoop();


    /**
     * Begins shutdown of the AsyncLoop by blocking new items from being queued,
     * deleting pending items from queues, and stopping the loop thread.
     * The only thing this function does not do is actually delete the singleton AsyncLoop object.
     */
    static void BeginShutdown();

    /**
     * Defines an async event handler.
     *
     * See @ref RegisterHandler.
     */
    using HandlerRef = void*;

    /**
     * Registers an async event handler.
     *
     * Before an entity can post events to the loop for processing, it must
     * register itself via this method. This function returns a handle to be
     * used for posting events.
     *
     * This method can be called from any thread, including from an event handler.
     *
     * @param   receiver    Interface to receive events for processing.
     * @return              Returns a handler reference.
     */
    HandlerRef RegisterHandler(IAsyncEventReceiver& receiver);

    /**
     * Unregisters an async event handler.
     *
     * It is acceptable to unregister an event handler even if there are
     * outstanding events in the loop for the handler. In this situation,
     * those outstanding events are cancelled and will not be posted to any
     * handler.
     *
     * This method can be called from any thread, including from an event handler.
     *
     * @param   handlerRef  Reference to the handler to unregister.
     */
    void UnregisterHandler(HandlerRef handlerRef);

    /**
     * Enqueue an event for immediate dispatch.
     *
     * An event is enqueued into the main loop and will be dispatched as soon
     * as possible to the @ref IAsyncEventReceiver::ProcessEvent() method
     * associated with the handler.
     *
     * Events are processed in a first-in-first-out order.
     *
     * This method can be called from any thread, including from an event handler.
     *
     * @param   handlerRef  The target handler of the event.
     *
     */
    void EnqueueImmediate(HandlerRef handlerRef);

    /**
     * Enqueue an event to be dispatched at a specific time.
     *
     * The event is put into a queue and processed when its scheduled time is
     * met. The current time is determined using kTimer_Now(). This scheduling
     * system is not high-precision in nature; the resolution and jitter are
     * similar to those of the underlying OS synchronization calls, e.g. from
     * kThread_Sleep() or the timeout in kSemaphore_Wait(). This is typically
     * on the order of several milliseconds.
     *
     * Note that this queue is asynchronous to the queue used by @ref EnqueueImmediate.
     * This queue is processed purely in  order of the target time, while the
     * queue in @ref EnqueueImmediate is FIFO ordered.
     *
     * This method can be called from any thread, including from an event handler.
     *
     * @param   handlerRef  The target handler of the event.
     * @param   targetTime  The target time of the event (us).
     */
    void EnqueueScheduled(HandlerRef handlerRef, k64u targetTime);

    AsyncLoop(const AsyncLoop&) = delete;
    AsyncLoop& operator=(const AsyncLoop&) = delete;

private:
    // Stores control information shared by the handler object, and queued
    // events for that handler. This is stored separately as a std::shared_ptr,
    // because it is possible for a handler to be destroyed while there are
    // still outstanding events referencing it. The control block cannot be
    // destroyed until the last reference is destroyed.
    struct HandlerControlBlock
    {
        IAsyncEventReceiver* receiver;
        volatile bool active;
        Go::Object<kLock> lock;
    };

    // Encapsulates a handler. Note the real information is in HandlerControlBlock.
    // This is just a convenient handle for registering/unregistering.
    struct Handler
    {
        std::shared_ptr<HandlerControlBlock> control;
        std::list<Handler*>::iterator listRef;
    };

    // Encapsulates a queued event.
    struct QueuedEvent
    {
        std::shared_ptr<HandlerControlBlock> control;
        k64u targetTime;

        QueuedEvent(std::shared_ptr<HandlerControlBlock> control, k64u targetTime = 0);

        bool operator>(const QueuedEvent& other) const;
    };

    volatile bool exitFlag;
    Go::Object<kThread> loopThread;
    Go::Object<kLock> lock;
    std::list<Handler*> handlers;
    Go::Object<kSemaphore> queueSem;
    std::priority_queue<QueuedEvent, std::vector<QueuedEvent>, std::greater<QueuedEvent>> scheduledQueue;
    std::queue<QueuedEvent> immediateQueue;

    static AsyncLoop* mainLoop;

    void RunLoop();
    void ProcessImmediateEvents();
    k64u ProcessScheduledEvents();
    void NotifyControl(HandlerControlBlock& control);

    static kStatus kCall LoopThreadFx(kPointer context);
};

} // namespace

#endif
