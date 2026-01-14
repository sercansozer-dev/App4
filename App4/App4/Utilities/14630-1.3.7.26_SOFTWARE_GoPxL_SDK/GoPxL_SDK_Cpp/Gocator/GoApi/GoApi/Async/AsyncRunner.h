/**\file    AsyncRunner.h
 * Declares the Go::AsyncRunner class.
 */
#ifndef GO_ASYNC_RUNNER_H
#define GO_ASYNC_RUNNER_H

#include <GoApi/GoApiDef.h>
#include <GoApi/Async/AsyncLoop.h>
#include <kApi/Threads/kSemaphore.h>

namespace Go
{

/**
 * Queues arbitrary functions to run from an AsyncLoop.
 *
 * This class allows arbitrary functions to be run from an async loop in a
 * FIFO fashion.
 *
 * While the execution order is FIFO within the same runner object, it may
 * not be in order with other runner or async objects.
 *
 * Unless otherwise noted, all methods are thread-safe, including the constructor
 * and deconstructor.
 */
class GoApiClass AsyncRunner : public IAsyncEventReceiver
{
public:
    /**
     * Constructs an async runner.
     *
     * @param   loop        Reference to the loop to use.
     */
    AsyncRunner(AsyncLoop& loop);

    /**
     * Constructs an async runner using the main loop.
     */
    AsyncRunner();

    ~AsyncRunner();

    /**
     * Queues an arbitrary function to run from the loop.
     *
     * The specified callback will be run as early as possible. When multiple
     * functions are queued, they are run in a FIFO order.
     *
     * While the execution order is FIFO within the runner object, it is not in
     * order with respect to other runners or async objects. Any strict ordering
     * should be enforced via chaining, as is typical in event-driven programming.
     *
     * If an exception occurs in the callback, it is logged but does not affect
     * loop operation. The caller should implement its own system for obtaining
     * exceptions if needed.
     *
     * @param   callback    The callback function to run.
     */
    void Run(AsyncCallbackFx callback);

    /**
     * Run an operation from the loop and wait for it to complete.
     *
     * This method works similarly to @ref Run() for the most part, but it will
     * wait for the function to complete (or timeout) before returning.
     *
     * If a timeout is specified, the clock starts as soon as this method is
     * invoked. If the callback does not complete in time, a timeout exception
     * is thrown. This can occur if the callback takes too long, or if the async
     * loop is busy with another operation.
     *
     * In the event of a timeout, the callback will still run and complete. A
     * subsequent call to this method will wait for both old callbacks and the
     * new callback.
     *
     * Care should be taken with this method to avoid deadlocks. In general, avoid
     * using this method unless absolutely needed. Furthermore, avoid using a timeout
     * unless you are aware of the implications.
     *
     * If an exception occurs in the callback, this method will throw Go::Exception(kERROR_ABORT),
     * containing the message of the original exception.
     *
     * @param   callback    The callback function to run.
     * @param   timeout     The time (in microseconds) to wait until aborting.
     * @throw   Go::Exception(kERROR_TIMEOUT)   If the timeout elapses before the operation completes.
     *
     */
    void RunAndWait(AsyncCallbackFx callback, k64u timeout = kINFINITE);

    /**
     * Returns the number of enqueued callbacks.
     * 
     * @return              The number of enqueued callbacks.
     */
    size_t EnqueuedCount();

    // IAsyncEventReceiver overrides
    void ProcessEvent() override;

    AsyncRunner(const AsyncRunner&) = delete;
    AsyncRunner& operator=(const AsyncRunner&) = delete;

private:
    AsyncLoop& loop;
    AsyncLoop::HandlerRef handlerRef;
    Go::Object<kLock> callbackQueueLock;
    std::queue<AsyncCallbackFx> callbackQueue;
    kAtomic32s notifyPending;
    Go::Object<kSemaphore> waitSem;
    kAtomic32s waitPending;
};

} // namespace

#endif
