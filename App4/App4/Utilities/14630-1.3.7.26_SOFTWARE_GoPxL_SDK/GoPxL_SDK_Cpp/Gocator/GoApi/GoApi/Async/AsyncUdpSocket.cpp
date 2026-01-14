#include "AsyncUdpSocket.h"
#include <GoApi/Exception.h>
#include <kApi/Io/kSocket.h>

#define WORKER_PERIOD       (100 * 1000)
#define MAX_DATAGRAM_SIZE   (1600)
#define ASYNC_UDP_SOCKET_READ_BUFFER_SIZE   (4096)

namespace Go {

AsyncUdpSocket::AsyncUdpSocket(AsyncLoop& loop) :
    readNotifier(loop, std::bind(&AsyncUdpSocket::ReadAll, this)),
    writeNotifier(loop, std::bind(&AsyncUdpSocket::WriteAll, this))
{
    GoTest(kUdpClient_Construct(udp.Ref(), kIP_VERSION_4, kAlloc_App()));
    readBuffer.resize(MAX_DATAGRAM_SIZE);
    // Must be enabled before using kUdpClient_ReceiveEx.
    GoTest(kUdpClient_EnablePacketInfo(udp, kTRUE));
    // kUdpClient_ReceiveEx function is using the buffer alocated as part of kUdpClient
    GoTest(kUdpClient_SetReadBuffers(udp, ASYNC_UDP_SOCKET_READ_BUFFER_SIZE, MAX_DATAGRAM_SIZE));
}

AsyncUdpSocket::AsyncUdpSocket() :
    AsyncUdpSocket(AsyncLoop::MainLoop())
{
}

AsyncUdpSocket::~AsyncUdpSocket()
{
    try
    {
        Stop();
    }
    catch (const std::exception& e)
    {
        GoLogExceptionMsg(e, "Exception thrown in destructor");
    }
}

void AsyncUdpSocket::SetDataHandler(OnDataFx dataFx)
{
    this->dataFx = dataFx;
}

void AsyncUdpSocket::SetDataHandlerEx(OnDataExFx dataExFx)
{
    this->dataExFx = dataExFx;
}

void AsyncUdpSocket::SendTo(const kIpEndPoint& dest, const Datagram& data)
{
    GoThrowIf(!started, kERROR_STATE);

    WriteItem item;

    item.dest = dest;
    item.data = data;

    sendQueue.emplace(std::move(item));

    WriteAll();
}

void AsyncUdpSocket::Bind(const kIpEndPoint& endPoint)
{
    GoThrowIf(started, kERROR_STATE);

    Bind(endPoint.address, endPoint.port);
}

void AsyncUdpSocket::Bind(kIpAddress address, k32u port)
{
    GoThrowIf(started, kERROR_STATE);

    GoTest(kUdpClient_Bind(udp, address, port));
}

void AsyncUdpSocket::EnableReuseAddress(bool enable)
{
    GoThrowIf(started, kERROR_STATE);

    GoTest(kUdpClient_EnableReuseAddress(udp, enable));
}

void AsyncUdpSocket::EnableBroadcast(bool enable)
{
    GoThrowIf(started, kERROR_STATE);

    GoTest(kUdpClient_EnableBroadcast(udp, enable));
}

void AsyncUdpSocket::Start()
{
    GoThrowIf(started, kERROR_STATE);

    exitFlag = false;

    GoTest(kThread_Construct(thread.Ref(), kAlloc_App()));
    GoTest(kThread_Start(thread, ThreadFx, this, "AsyncUdpSocket"));

    started = true;
}

void AsyncUdpSocket::Stop()
{
    if (!started)
    {
        return;
    }

    exitFlag = true;
    (void)kThread_Join(thread, kINFINITE, kNULL);

    thread.Reset();
    started = false;
}

bool AsyncUdpSocket::IsRunning() const
{
    return started;
}

k32u AsyncUdpSocket::Port() const
{
    kIpEndPoint endPoint;

    GoTest(kUdpClient_LocalEndPoint(udp, &endPoint));

    return endPoint.port;
}

void AsyncUdpSocket::ReadAll()
{
    kIpEndPoint endPoint;
    kSize received;
    kSize adapterId = 0;

    // Read all data that can be read without blocking.
    // Added '!exitFlag' as part of  GOS-2196.
    while (!exitFlag && kSuccess(kUdpClient_ReceiveEx(udp, &endPoint, &received, 0, &adapterId))) 
    {
        GoTest(kStream_Read(udp, (void*)&readBuffer[0], received));

        Datagram datagram(readBuffer.begin(), readBuffer.begin() + received);

        if (dataFx)
        {
            dataFx(endPoint, datagram);
        }

        if(dataExFx)
        {
            dataExFx(endPoint, datagram, adapterId);
        }
    }
}

void AsyncUdpSocket::WriteAll()
{
    // Added '!exitFlag' as part of  GOS-2196.
    while (!exitFlag && !sendQueue.empty())
    {
        auto& item = sendQueue.front();

        kStatus status = kUdpClient_WriteTo(
            udp,
            item.data.data(),
            item.data.size(),
            item.dest.address,
            item.dest.port,
            0);

        if (status == kOK)
        {
            // Send successful. Deque item.
            sendQueue.pop();
        }
        else if (status == kERROR_TIMEOUT || status == kERROR_BUSY)
        {
            // Need to wait.
            // Note this can have a higher than optimal latency, similar to
            // issue outlined in GOS-925. However this is less of an issue
            // because the extra latency occurs only when the socket is already
            // blocked. Potential solutions include using separate threads for
            // polling read and write. Or use some kind of cancellable wait
            // mechanism if available.
            pendingSend = true;
        }
        else
        {
            // Error. Deque item.
            writeErrorCount++;
            sendQueue.pop();
        }
    }
}

void AsyncUdpSocket::RunWorker()
{
    kSocket socket = kUdpClient_Socket(udp);

    while (!exitFlag)
    {
        kSocketEvent events = kSOCKET_EVENT_READ;

        // If there's a pending send, we need to wait for WRITE also.
        if (pendingSend)
        {
            events |= kSOCKET_EVENT_WRITE;
        }

        GoTest(kSocket_SetEvents(socket, events));

        kStatus status = kSocket_Wait(socket, WORKER_PERIOD);

        if (status == kOK)
        {
            auto events = kSocket_Events(socket);

            if (events & kSOCKET_EVENT_READ)
            {
                readNotifier.Notify();
            }
            
            if (events & kSOCKET_EVENT_WRITE)
            {
                // Need to set pending to false; otherwise the WRITE event will
                // keep triggering at a high rate (it's a "level-based" trigger).
                // It must be unset *before* calling Notify(), which calls WriteAll().
                // WriteAll() has the potential to set the flag again, so this
                // function must not unset it without processing it.
                // Note that the callback is called at least once after Notify().
                pendingSend = false;
                writeNotifier.Notify();
            }
        }
    }
}

kStatus kCall AsyncUdpSocket::ThreadFx(kPointer context)
{
    AsyncUdpSocket* obj = static_cast<AsyncUdpSocket*>(context);

    GoBeginExceptionHandler();

    obj->RunWorker();

    GoEndExceptionHandler();

    return kOK;
}

} // namespace
