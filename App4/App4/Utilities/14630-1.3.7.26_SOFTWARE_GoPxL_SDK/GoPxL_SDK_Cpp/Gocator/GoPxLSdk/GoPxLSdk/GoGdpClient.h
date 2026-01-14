/**
 * @file    GoGdpClient.h
 * @brief   Declares the GoPxLSdk.GoGdpClient class.
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#ifndef GO_PXL_SDK_GOGDPCLIENT_H
#define GO_PXL_SDK_GOGDPCLIENT_H

#include <kApi/Io/kNetwork.h>
#include <kApi/Io/kTcpClient.h>

#include <kApi/Threads/kThread.h>
#include <kApi/Threads/kLock.h>
#include <kApi/Threads/kTimer.h>

#include <GoApi/GoApi.h>
#include <GoApi/Threads/MsgQueue.h>
#include <GoApi/Threads/Locker.h>

#include <GoPxLSdk/Def.h>
#include <GoPxLSdk/GoDataSet.h>
#include <GoPxLSdk/GoGdpMsg/GoGdpMsgDef.h>

class GoGdpClientTests;

namespace GoPxLSdk
{
    class GoPxLSdkClass GoGdpClient
    {
    public:
       /**
        * Constructs GoGdpClient.
        *
        * @public                                @memberof GoGdpClient
        * @version                               Introduced in 0.2.1.53.
        * @throws Go::Exception                  If unable to create client.
        */
        GoGdpClient();
        ~GoGdpClient();

        const int CONNECT_TIMEOUT = 5000000;
        const int CANCEL_QUERY_INTERVAL = 100000;

       /**
        * Connect to GdpServer.
        *
        * @public                                @memberof GoGdpClient
        * @version                               Introduced in 1.1.50.15
        * @param ipAddr                          Ip addresss.
        * @param port                            Default DEFAULT_GDP_PORT.
        * @throws Go::Exception                  If unable to connect.
        */
        void Connect(kIpAddress ipAddr, k16u port = GO_PXL_SDK_DEFAULT_GDP_SERVER_PORT);

       /**
        * Close the tcp connection.
        *
        * @public                                @memberof GoGdpClient
        * @version                               Introduced in 0.2.1.53.
        * @throws Go::Exception                  If unable to close connection.
        */
        void Close();

       /**
        * Checks if the client is connected or not.
        *
        * @public                                @memberof GoGdpClient
        * @version                               Introduced in 0.2.1.53.
        * @return True                           If connected, false otherwise.
        */
        bool IsConnected();

       /**
        * Sync Receive data from gdp server.
        *
        * @public                                @memberof GoGdpClient
        * @version                               Introduced in 0.2.1.53.
        * @param receiveTimeoutInMilliseconds    Set time out in milliseconds.
        * @throws Go::Exception                  If failed to receive data synchronously.
        */
        void ReceiveDataSync(k64u receiveTimeoutInMilliseconds);

       /**
        * Async Receive data from gdp server.
        *
        * @public                                @memberof GoGdpClient
        * @version                               Introduced in 0.2.1.53.
        * @param func                            Callback function to get data.
        * @throws Go::Exception                  If failed to receive data asynchronously. 
        */
        void ReceiveDataAsync(std::function<void(const GoDataSet& receivedDataSet)>& func);

       /**
        * Get the data set received from the sensor over the GoPxL Data Protocol.
        *
        * @public                                @memberof GoGdpClient
        * @version                               Introduced in 0.2.1.53.
        * @return                                The pointer of dataSet.
        */
        const GoDataSet& DataSet() const;

        /**
         * Clears any buffered data messages.
         *
         * When stopping and then restarting a system, it may be desirable to ensure that no messages from
         * the previous session remain in any buffers.
         *
         * @public                               @memberof GoGdpClient
         * @version                              Introduced in 1.2.30.46.
         */
        void ClearData();

    private:
       /**
        * Get the gdp msg type.
        *
        * @private                               @memberof GoGdpClient
        * @version                               Introduced in 0.2.1.53.
        * @param type                            Pass a reference to get the msg type.
        * @throws Go::Exception                  If failed to get message type.
        */
        void GetGdpMsgType(MessageType& type);

       /**
        * Initializes the TCP client. 
        *
        * @private                               @memberof GoGdpClient
        * @version                               Introduced in 0.2.1.53.
        * @throws Go::Exception                  If failed to initialize TCP client.
        */
        void InitTcpClient();

       /**
        * Initializes the recieveThread, dataThread and the timer.
        *
        * @private                               @memberof GoGdpClient
        * @version                               Introduced in 0.2.1.53.
        * @throws Go::Exception                  If failed to initialize timer and threads.
        */
        void InitThreadAndTimer();

       /**
        * Creates a new GoGdp message of the specified message type.
        *
        * @private                               @memberof GoGdpClient
        * @return                                The new object if successful, a nullptr if failed.
        */
        static std::shared_ptr<GoGdpMsg> MakeGoGdpMsg(MessageType type);

       /**
        * Add the received data to the data queue. 
        *
        * @private                               @memberof GoGdpClient
        * @version                               Introduced in 0.2.1.53.
        * @param context                         Context of the onDataReceive event. It can be casted to a GoGdpClient pointer. 
        * @throws Go::Exception                  If failed to add data to the data queue.
        * @return                                The status of adding data to the data queue.        
        */
        static kStatus kCall OnDataReceive(void* context);

       /**
        * Removes the received dataset from data queue and call the callback function to get data, if the callback function exists.
        *
        * @private                               @memberof GoGdpClient
        * @version                               Introduced in 0.2.1.53.
        * @param context                         Context of the onDataReceive event. It can be casted to a GoGdpClient pointer. 
        * @throws Go::Exception                  If failed to remove data from the data queue or callback function failed to process data.
        * @return                                The status of removing data from data queue and processing the data.    
        */
        static kStatus kCall DataThreadEntry(void* context);

    private:
        void TryClose();

        std::function<void(const GoDataSet& pDataSet)> func;

        GoDataSet dataSet;

        bool isConnected = false;
        bool async = false;
        bool isCompleteSet;

        Go::Object<kTcpClient> socket;
        Go::Object<kSerializer> serializer;
        Go::Object<kTimer> timer;
        Go::Object<kThread> receiveThread;
        Go::MsgQueue<GoDataSet> dataQueue;
        Go::Object<kThread> dataThread;

        kIpAddress ipAddress;
        k16u port;

        friend class ::GoGdpClientTests;
    };
}


#endif


