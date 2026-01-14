/**\file    AsyncTimer.h
 * Declares the Go::AsyncTimer class.
 */
#ifndef GO_ASYNC_TIMER_H
#define GO_ASYNC_TIMER_H

#include <GoApi/GoApiDef.h>
#include <GoApi/Async/AsyncLoop.h>

namespace Go
{

/**
 * Schedules a timer to run from an AsyncLoop.
 *
 * The timer runs on a "best effort" basis. For example, if a timer is scheduled
 * to run every 1000 ms, but the async loop is blocked by another operation for
 * 2000 ms, the timer runs once immediately after being unblocked, and will run
 * again 1000 ms from that time. i.e. it "skips a beat".
 * 
 * This reflects the needs of most polling/update logic, where the number of times
 * the callback runs with respect to the time is not important.
 *
 * It should also be noted that this timer is not high precision, and has the precision
 * of the underlying OS synchronizatio calls, which is typically on the order of
 * at least several milliseconds, and can be as high as 20 milliseconds.
 *
 * Unless otherwise noted, all methods are thread-safe, including the constructor
 * and deconstructor.
 */
class GoApiClass AsyncTimer : public IAsyncEventReceiver
{
public:
    /**
     * Declares a callback specific for timers.
     *
     * @param   timer       The timer sending the callback.
     */
    using AsyncTimerCallbackFx = std::function<void(AsyncTimer& timer)>;

    /**
     * Constructs a timer using the default callback type.
     *
     * The default callback type contains no arguments.
     *
     * The timer is not running directly after construction.
     *
     * @param   loop        Reference to the loop to use.
     * @param   callback    The callback function.
     */
    AsyncTimer(AsyncLoop& loop, AsyncCallbackFx callback);

    /**
     * Constructs a timer using the timer callback type.
     *
     * The timer callback contains a reference to the sender timer object.
     *
     * The timer is not running directly after construction.
     *
     * @param   loop        Reference to the loop to use.
     * @param   callback    The callback function.
     */
    AsyncTimer(AsyncLoop& loop, AsyncTimerCallbackFx callback);

    /**
     * Constructs a timer using the main loop.
     *
     * The timer callback contains a reference to the sender timer object.
     *
     * The timer is not running directly after construction.
     *
     * @param   callback    The callback function.
     */
    AsyncTimer(AsyncTimerCallbackFx callback);

    ~AsyncTimer();

    /**
     * Starts the timer.
     *
     * When started, the callback will be run at the rate specified by period.
     * If immediateCall is set to false (default), the first call will be made
     * after the period elapses. Otherwise, the first call occurs right away.
     *
     * @param   period          The period to run the timer at (us).
     * @param   immediateCall   If true, make the first call right away; otherwise, wait for period to elapse.
     *
     */
    void Start(k64u period, bool immediateCall = false);

    /**
     * Stops the timer.
     *
     * Calling this will cancel all pending callbacks. It is safe to assume the
     * callback is no longer invoked after this call.
     *
     * It is safe to call this method from within the callback.
     */
    void Stop();

    // IAsyncEventReceiver overrides
    void ProcessEvent() override;

    AsyncTimer(const AsyncTimer&) = delete;
    AsyncTimer& operator=(const AsyncTimer&) = delete;

private:
    AsyncLoop& loop;
    AsyncLoop::HandlerRef handlerRef;
    AsyncCallbackFx callback;
    k64u period = 0;
};

} // namespace

#endif
