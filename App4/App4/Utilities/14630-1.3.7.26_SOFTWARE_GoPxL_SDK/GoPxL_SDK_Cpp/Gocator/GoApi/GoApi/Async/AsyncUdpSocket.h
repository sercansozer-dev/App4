/**\file    AsyncUdpSocket.h
 * Declares the Go::AsyncUdpSocket class.
 */
#ifndef GO_ASYNC_UDP_SOCKET_H
#define GO_ASYNC_UDP_SOCKET_H

#include <GoApi/GoApiDef.h>
#include <GoApi/Async/AsyncLoop.h>
#include <GoApi/Async/AsyncNotifier.h>
#include <GoApi/Object.h>
#include <kApi/Io/kUdpClient.h>
#include <kApi/Threads/kThread.h>
#include <vector>
#include <queue>
#include <functional>

namespace Go
{

/**
 * An asynchronous UDP socket class that uses event loops.
 *
 * This class exposes a non-blocking API based on the supplied event loop.
 * Sending is non-blocking, while receiving occurs in the background, and
 * received datagrams are delivered to a callback from the event loop.
 *
 * All methods in this class should be called from the specified event loop.
 *
 * @remark
 * Due to platform limitations, under high load scenarios, sending can have
 * higher than normal latencies. This is due to the need to wait for any
 * pending read event wait to timeout, before being able to wait for write
 * events. This is not an issue for low load scenarios, because the send call
 * does not need to wait for write events. This issue is related to GOS-925,
 * and the same solution may be applicable here.
 */
class GoApiClass AsyncUdpSocket
{
public:
    /**
     * An UDP datagram.
     */
    using Datagram = std::vector<kByte>;

    /**
     * A data receive callback.
     *
     * - The first parameter is a reference to the endpoint (address and port) of the sender.
     * - The second parameter is a reference to the received datagram.
     */
    using OnDataFx = std::function<void(const kIpEndPoint& sender, const Datagram& data)>;

    /**
     * A data receive callback.
     *
     * - The first parameter is a reference to the endpoint (address and port) of the sender.
     * - The second parameter is a reference to the received datagram.
     */
    using OnDataExFx = std::function<void(const kIpEndPoint& sender, const Datagram& data, kSize& adapterId)>;

    /**
     * Constructs a socket using the specified event loop.
     *
     * Data receive callbacks are delivered from the specified loop.
     *
     * @param   loop        Reference to the event loop.
     */
    AsyncUdpSocket(AsyncLoop& loop);

    /**
     * Constructs a socket using the main loop.
     *
     * Data receive callbacks are delivered from the main loop.
     */
    AsyncUdpSocket();

    ~AsyncUdpSocket();

    /**
     * Sets a data handler for receiving data.
     *
     * The socket is listening for data in the background without blocking.
     * Whenever it has received a datagram, it calls the callback with it from
     * the specified event loop.
     *
     * @param   dataFx      The callback handler.
     *
     */
    void SetDataHandler(OnDataFx dataFx);

    /**
     * Sets a data handler for receiving data.
     *
     * The socket is listening for data in the background without blocking.
     * Whenever it has received a datagram, it calls the callback with it from
     * the specified event loop.
     *
     * @param   dataExFx      The callback handler.
     *
     */
    void SetDataHandlerEx(OnDataExFx  dataExFx);

    /**
     * Sends a datagram to the specified endpoint.
     *
     * This method is guaranteed to be non-blocking. If the socket is not yet
     * ready to send data, it is buffered to be sent at a later time.
     * This method must be called from the event loop specified during
     * construction. The behavior is undefined if called from another thread.
     *
     * @param   dest        Reference to the endpoint (address and port) of the destination.
     * @param   data        Reference to the datagram to send.
     */
    void SendTo(const kIpEndPoint& dest, const Datagram& data);

    /**
     * Bind the socket to the specified endpoint.
     *
     * This method must be called before calling @ref Start().
     *
     * @param   endPoint    Reference to the endpoint to listen on.
     */
    void Bind(const kIpEndPoint& endPoint);

    /**
     * Bind the socket to the specified address and port.
     *
     * This method must be called before calling @ref Start().
     *
     * @param   address     Address to listen on.
     * @param   port        Port to listen on.
     */
    void Bind(kIpAddress address, k32u port);

    /**
     * Enable or disable address reuse.
     *
     * This method must be called before calling @ref Start().
     *
     * @param   enable      Enable or disable address reuse.
     */
    void EnableReuseAddress(bool enable);

    /**
     * Enable or disable broadcast.
     *
     * This method must be called before calling @ref Start().
     *
     * @param   enable      Enable or disable broadcast.
     */
    void EnableBroadcast(bool enable);

    /**
     * Returns the local port of the socket.
     *
     * This method may not return a useful value before calling @ref Bind().
     *
     * @return              Local socket port.
     */
    k32u Port() const;

    /**
     * Starts the asynchronous IO process.
     *
     * The socket must have been started before data can be sent and for the
     * callback to be invoked with incoming data.
     *
     */
    void Start();

    /**
     * Stops the asynchronous IO process.
     *
     * See @ref Start() for more information.
     *
     */
    void Stop();

    /**
     * Returns the flag telling whether the socket has been started.
     *
     * @return              Is socket running.
     */
    bool IsRunning() const;

    AsyncUdpSocket(const AsyncUdpSocket&) = delete;
    AsyncUdpSocket& operator=(const AsyncUdpSocket&) = delete;

private:
    struct WriteItem
    {
        kIpEndPoint dest;
        Datagram data;
    };

    OnDataFx dataFx;
    OnDataExFx dataExFx;
    Go::Object<kUdpClient> udp;
    Go::Object<kThread> thread;
    bool started = false;
    volatile bool exitFlag = false;
    volatile bool pendingSend = false;
    std::queue<WriteItem> sendQueue;
    std::vector<kByte> readBuffer;
    Go::AsyncNotifier readNotifier;
    Go::AsyncNotifier writeNotifier;
    k64u writeErrorCount = 0;

    void ReadAll();
    void WriteAll();

    void RunWorker();    
    static kStatus kCall ThreadFx(kPointer context);
};

} // namespace

#endif
