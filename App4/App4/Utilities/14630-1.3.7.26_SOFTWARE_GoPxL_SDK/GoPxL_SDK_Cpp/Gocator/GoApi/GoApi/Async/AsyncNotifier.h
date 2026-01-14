/**\file    AsyncNotifier.h
 * Declares the Go::AsyncNotifier class.
 */
#ifndef GO_ASYNC_NOTIFIER_H
#define GO_ASYNC_NOTIFIER_H

#include <GoApi/GoApiDef.h>
#include <GoApi/Async/AsyncLoop.h>

namespace Go
{

/**
 * Runs a callback from an AsyncLoop when notified.
 *
 * This class represents the minimal action that can be invoked from the async
 * loop: to trigger a callback asynchronously. It should be used in situations
 * where the caller already implements or has thread-safe queuing behavior, and
 * just needs a signal to process the queue from the main loop.
 *
 * Examples include: 
 *   - An object that already implements a thread-safe queue (e.g. MsgQueue).
 *   - A non-blocking socket, which has OS-level buffering.
 *
 * Note that signals are "coalesced". See @ref Notify for more information.
 *
 * Unless otherwise noted, all methods are thread-safe, including the constructor
 * and deconstructor.
 */
class GoApiClass AsyncNotifier : public IAsyncEventReceiver
{
public:
    /**
     * Constructs an async notifier.
     *
     * @param   loop        Reference to the loop to use.
     * @param   callback    The callback to run when notified.
     */
    AsyncNotifier(AsyncLoop& loop, AsyncCallbackFx callback);

    /**
     * Constructs an async notifier using the main loop.
     *
     * @param   callback    The callback to run when notified.
     */
    AsyncNotifier(AsyncCallbackFx callback);

    ~AsyncNotifier();

    /**
     * Notifies the loop to run the callback.
     *
     * Notify calls are coalesced. If there are multiple calls before the
     * callback is run, the callback will run only once. A Notify call will
     * only trigger the callback when there is no pending callback.
     * Note that a callback that is currently running is not considered to
     * be "pending". If Notify is called in this situation, the callback will
     * be run again after it completes.
     */
    void Notify();

    // IAsyncEventReceiver overrides
    void ProcessEvent() override;

    AsyncNotifier(const AsyncNotifier&) = delete;
    AsyncNotifier& operator=(const AsyncNotifier&) = delete;

private:
    AsyncLoop& loop;
    AsyncLoop::HandlerRef handlerRef;
    AsyncCallbackFx callback;
    kAtomic32s notifyPending;
};

} // namespace

#endif
