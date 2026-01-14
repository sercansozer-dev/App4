/**
 * @file    GoGdpClient.cpp
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#include <GoPxLSdk/GoGdpClient.h>
#include <GoPxLSdk/GoGdpMsg/GoGdpMsg.h>
#include <GoPxLSdk/GoGdpMsg/GoGdpMeasurement.h>
#include <GoPxLSdk/GoGdpMsg/GoGdpMesh.h>
#include <GoPxLSdk/GoGdpMsg/GoGdpNull.h>
#include <GoPxLSdk/GoGdpMsg/GoGdpProfilePointCloud.h>
#include <GoPxLSdk/GoGdpMsg/GoGdpProfileUniform.h>
#include <GoPxLSdk/GoGdpMsg/GoGdpRendering.h>
#include <GoPxLSdk/GoGdpMsg/GoGdpSpots.h>
#include <GoPxLSdk/GoGdpMsg/GoGdpStamp.h>
#include <GoPxLSdk/GoGdpMsg/GoGdpString.h>
#include <GoPxLSdk/GoGdpMsg/GoGdpSurfacePointCloud.h>
#include <GoPxLSdk/GoGdpMsg/GoGdpSurfaceUniform.h>
#include <GoPxLSdk/GoGdpMsg/GoGdpImage.h>
#include <GoPxLSdk/GoGdpMsg/GoGdpFeaturePoint.h>
#include <GoPxLSdk/GoGdpMsg/GoGdpFeatureLine.h>
#include <GoPxLSdk/GoGdpMsg/GoGdpFeaturePlane.h>
#include <GoPxLSdk/GoGdpMsg/GoGdpFeatureCircle.h>
#include <GoPxLSdk/GoGdpMsg/GoGdpSignal.h>

namespace GoPxLSdk
{
    GoGdpClient::GoGdpClient() :
        isCompleteSet(false),
        ipAddress(),
        port(k16U_NULL)
    {
        InitThreadAndTimer();
    }

    GoGdpClient::~GoGdpClient()
    {
        try
        {
            Close();
            serializer.Reset();
            socket.Reset();
            receiveThread.Reset();
            dataThread.Reset();
            timer.Reset();
        }
        catch (const std::exception& e)
        {
            GoLogExceptionMsg(e, "Exception thrown in destructor");
        }
    }

    void GoGdpClient::InitThreadAndTimer()
    {
        try
        {
            GoTest(kThread_Construct(receiveThread.Ref(), kAlloc_App()));
            GoTest(kThread_Construct(dataThread.Ref(), kAlloc_App()));
            GoTest(kTimer_Construct(timer.Ref(), kAlloc_App()));
        }
        catch (const Go::Exception&)
        {
            receiveThread.Reset();
            dataThread.Reset();
            timer.Reset();

            GoRethrow("Failed to initialize timer and threads.");
        }
    }

    void GoGdpClient::InitTcpClient()
    {
        try
        {
            GoTest(kTcpClient_Construct(socket.Ref(), kIP_VERSION_4, kAlloc_App()));
            GoTest(kTcpClient_SetReadBuffers(socket, -1, 4096));
            GoTest(kSerializer_Construct(serializer.Ref(), socket, kNULL, kAlloc_App()));
        }
        catch (const Go::Exception&)
        {
            serializer.Reset();
            socket.Reset();
            GoRethrow("Failed to initialize TCP client.");
        }
    }

    void GoGdpClient::Connect(kIpAddress ipAddr, k16u port)
    {
        if (isConnected)
        {
            TryClose();

            isConnected = false;
        }

        if (!isConnected)
        {
            InitTcpClient();
            GoTest(kTcpClient_Connect(socket, ipAddr, port, CONNECT_TIMEOUT));
            isConnected = true;

            this->ipAddress = ipAddr;
            this->port = port;
        }
    }

    void GoGdpClient::Close()
    {
        if (isConnected)
        {
            isConnected = false;

            if (async)
            {
                GoTest(kThread_Join(receiveThread, kINFINITE, kNULL));
                GoTest(kThread_Join(dataThread, kINFINITE, kNULL));
            }

            socket.Reset();
            serializer.Reset();
            dataQueue.Clear();
        }
    }

    bool GoGdpClient::IsConnected()
    {
        return isConnected;
    }

    const GoDataSet& GoGdpClient::DataSet() const
    {
        return dataSet;
    }

    void GoGdpClient::ClearData()
    {
        bool shouldReconnect = IsConnected();

        if (IsConnected())
        {
            TryClose();
        }

        dataSet.Clear();
        dataQueue.Clear();

        if (shouldReconnect)
        {
            try
            {
                Connect(ipAddress, port);

                if (async)
                {
                    ReceiveDataAsync(func);
                }
            }
            catch (const Go::Exception&)
            {
                GoRethrow("Failed to re-open connection.");
            }
        }
    }

    void GoGdpClient::GetGdpMsgType(MessageType& type)
    {
        k16u messageType;
        bool needEndRead = false;

        try
        {
            //Header
            GoTest(kSerializer_BeginRead(serializer, kTypeOf(k32u), kTRUE));
            needEndRead = true;
            GoTest(kSerializer_Read16u(serializer, &messageType));
            type = (MessageType)messageType;
        }
        catch (const Go::Exception&)
        {
            if (needEndRead)
            {
                kSerializer_EndRead(serializer);
            }
            GoRethrow("Failed to get message type.");
        }

        // The message type section read remains open (ie. corresponding EndRead() is not called.
    }

    std::shared_ptr<GoGdpMsg> GoGdpClient::MakeGoGdpMsg(MessageType type)
    {
        std::shared_ptr<GoGdpMsg> msg = nullptr;

        switch (type)
        {
        case GoPxLSdk::MessageType::STAMP:
            msg = std::make_shared<GoGdpStamp>();
            break;
        case GoPxLSdk::MessageType::IMAGE:
            msg = std::make_shared<GoGdpImage>();
            break;
        case GoPxLSdk::MessageType::SIGNAL:
            msg = std::make_shared<GoGdpSignal>();
            break;
        case GoPxLSdk::MessageType::PROFILE_POINT_CLOUD:
            msg = std::make_shared<GoGdpProfilePointCloud>();
            break;
        case GoPxLSdk::MessageType::UNIFORM_PROFILE:
            msg = std::make_shared<GoGdpProfileUniform>();
            break;
        case GoPxLSdk::MessageType::SURFACE_POINT_CLOUD:
            msg = std::make_shared<GoGdpSurfacePointCloud>();
            break;
        case GoPxLSdk::MessageType::UNIFORM_SURFACE:
            msg = std::make_shared<GoGdpSurfaceUniform>();
            break;
        case GoPxLSdk::MessageType::MEASUREMENT:
            msg = std::make_shared<GoGdpMeasurement>();
            break;
        case GoPxLSdk::MessageType::STRING:
            msg = std::make_shared<GoGdpString>();
            break;
        case GoPxLSdk::MessageType::NULL_TYPE:
            msg = std::make_shared<GoGdpNull>();
            break;
        case GoPxLSdk::MessageType::SPOTS:
            msg = std::make_shared<GoGdpSpots>();
            break;
        case GoPxLSdk::MessageType::MESH:
            msg = std::make_shared<GoGdpMesh>();
            break;
        case GoPxLSdk::MessageType::RENDERING:
            msg = std::make_shared<GoGdpRendering>();
            break;
        case GoPxLSdk::MessageType::POINT_FEATURE:
            msg = std::make_shared<GoGdpFeaturePoint>();
            break;
        case GoPxLSdk::MessageType::LINE_FEATURE:
            msg = std::make_shared<GoGdpFeatureLine>();
            break;
        case GoPxLSdk::MessageType::PLANE_FEATURE:
            msg = std::make_shared<GoGdpFeaturePlane>();
            break;
        case GoPxLSdk::MessageType::CIRCLE_FEATURE:
            msg = std::make_shared<GoGdpFeatureCircle>();
            break;
        default:
            break;
        }

        return msg;
    }

    void GoGdpClient::ReceiveDataSync(k64u receiveTimeoutInMilliseconds)
    {
        kStatus result = kOK;
        MessageType type;
        std::shared_ptr<GoGdpMsg> goGdpMsg = nullptr;
        bool needEndRead = false;

        // GOS-5176: temporarily disable data set identifier sanity check until the issue of the surface message
        // kStamp.frame value being different from the stamp/profile messages is resolved.
        // Uncomment this code when the issue is finally resolved.
        // k64u firstDataSetId = 0;

        if (isCompleteSet)
        {
            dataSet.Clear();
            isCompleteSet = false;
        }

        GoTest(IsConnected());
        GoTest(kTimer_Start(timer, receiveTimeoutInMilliseconds * GO_PXL_SDK_MILLISECONDS_TO_MICROSECONDS_CONVERSION));
        while (IsConnected())
        {
            try
            {
                if (kTimer_IsExpired(timer))
                {
                    // If the timer has expired due to the client not having data to read, return
                    // a timeout error. Otherwise, if there is data to read but not enough time to
                    // form the complete data set, return an incomplete error.
                    GoThrow((result == kERROR_TIMEOUT) ? kERROR_TIMEOUT : kERROR_INCOMPLETE);
                }

                if (kSuccess(result = kTcpClient_Wait(socket, CANCEL_QUERY_INTERVAL)))
                {
                    GetGdpMsgType(type);
                    needEndRead = true;

                    // GOS-12872 - The previous shared_ptr object ownership will be properly released,
                    // if nullptr is returned due to unsupported GDP message type.
                    goGdpMsg.reset();
                    goGdpMsg = MakeGoGdpMsg(type);

                    if (goGdpMsg != nullptr)
                    {
                        goGdpMsg->Deserialize(serializer);

                        // Get the message set id of the first message in the data set.
                        //
                        // GOS-5176: temporarily disable data set identifier sanity check until the issue of the surface message
                        // kStamp.frame value being different from the stamp/profile messages is resolved.
                        // Uncomment this code when the issue is finally resolved.
                        //if (dataSet.Count() == 0)
                        //{
                        //    firstDataSetId = pGoGdpMsg->DataSetId();
                        //}
                        //else
                        //{
                        //    firstDataSetId = dataSet->GdpMsgAt(0)->DataSetId();
                        //}

                        // If the data set identifier has changed before the last message of the current set could
                        // be reached, discard the incomplete set. Start a new data set with the new
                        // data set identifier value.
                        // GOS-3905: A message drop count will be added and keep track of the discarded
                        // messages.
                        //
                        // GOS-5176: temporarily disable data set identifier sanity check until the issue of the surface message
                        // kStamp.frame value being different from the stamp/profile messages is resolved.
                        // Uncomment this code when the issue is finally resolved.
                        //if (pGoGdpMsg->DataSetId() != firstDataSetId)
                        //{
                        //    dataSet.Clear();
                        //    firstDataSetId = pGoGdpMsg->DataSetId();
                        //}

                        dataSet.Add(goGdpMsg);

                        // If this is the last message in the data set, then exit function. The variable "dataSet"
                        // will contain the complete data set.
                        if (goGdpMsg->IsLastMsg())
                        {
                            // This closes the message type read section.
                            GoTest(kSerializer_EndRead(serializer));
                            needEndRead = false;
                            isCompleteSet = true;
                            break;  // Leave while() loop.
                        }
                    }

                    // This closes the message type read section.
                    GoTest(kSerializer_EndRead(serializer));
                    needEndRead = false;
                }
                else if (result == kERROR_TIMEOUT)
                {
                    continue;
                }
                else
                {
                    GoThrow(result);
                }
            }
            catch (const Go::Exception&)
            {
                if (needEndRead)
                {
                    // This closes the message type read section.
                    kSerializer_EndRead(serializer);
                }

                GoRethrow("Failed to receive data synchronously.");
            }
        }
    }


    kStatus kCall GoGdpClient::DataThreadEntry(void* context)
    {
        auto* obj = (GoGdpClient*)context;

        if (obj->func)
        {
            while (obj->IsConnected())
            {
                kStatus status;
                std::unique_ptr<GoDataSet> data;
                if (kSuccess(status = obj->dataQueue.Remove(data, obj->CANCEL_QUERY_INTERVAL)))
                {
                    try
                    {
                        obj->func(*data);
                    }
                    catch (const Go::Exception& e)
                    {
                        GoLogError("Callback function failed to process data.", e.Status());
                    }
                }
                else if (status != kERROR_TIMEOUT)
                {
                    GoLogError("Failed to remove data from the data queue.", status);
                }
            }
        }

        return kOK;
    }

    void GoGdpClient::TryClose()
    {
        try
        {
            Close();
        }
        catch (const Go::Exception&)
        {
            GoRethrow("Failed to close existing connection.");
        }
    }

    void GoGdpClient::ReceiveDataAsync(std::function<void (const GoDataSet&)>& func)
    {
        this->func = func;

        GoTest(kThread_Start(receiveThread, OnDataReceive, this, "Sdk.GdpClnt.Rv"));
        GoTest(kThread_Start(dataThread, DataThreadEntry, this, "Sdk.GdpClnt.Dt"));

        async = true;
    }

    kStatus kCall GoGdpClient::OnDataReceive(void* context)
    {
        auto* obj = (GoGdpClient*)context;
        kStatus result;
        MessageType type;
        std::unique_ptr<GoDataSet> data = nullptr;
        std::shared_ptr<GoGdpMsg> goGdpMsg = nullptr;
        bool needEndRead = false;
        // GOS-5176: temporarily disable data set identifier sanity check until the issue of the surface message
        // kStamp.frame value being different from the stamp/profile messages is resolved.
        // Uncomment this code when the issue is finally resolved.
        // k64u firstDataSetId = 0;
        try
        {
            while (obj->IsConnected())
            {
                if (kSuccess(result = kTcpClient_Wait(obj->socket, obj->CANCEL_QUERY_INTERVAL)))
                {
                    obj->GetGdpMsgType(type);

                    needEndRead = true;

                    // GOS-12872 - The previous shared_ptr object ownership will be properly released,
                    // if nullptr is returned due to unsupported GDP message type.
                    goGdpMsg = MakeGoGdpMsg(type);

                    if (goGdpMsg != nullptr)
                    {
                        goGdpMsg->Deserialize(obj->serializer);

                        // Construct a new GoDataSet object if data is null.
                        if (data == nullptr)
                        {
                            data = std::make_unique<GoDataSet>();
                            // GOS-5176: temporarily disable data set identifier sanity check until the issue of the surface message
                            // kStamp.frame value being different from the stamp/profile messages is resolved.
                            // Uncomment this code when the issue is finally resolved.
                            // firstDataSetId = pGoGdpMsg->DataSetId();
                        }

                        // If the data set identifier has changed before the last message of the current set could
                        // be reached, discard the incomplete set. Start a new data set with the new
                        // data set identifier value.
                        // GOS-3905: A message drop count will be added and keep track of the discarded
                        // messages.
                        //
                        // GOS-5176: temporarily disable data set identifier sanity check until the issue of the surface message
                        // kStamp.frame value being different from the stamp/profile messages is resolved.
                        // Uncomment this code when the issue is finally resolved.
                        //if (pGoGdpMsg->DataSetId() != firstDataSetId)
                        //{
                        //    data->Clear();
                        //    firstDataSetId = pGoGdpMsg->DataSetId();
                        //}

                        data->Add(goGdpMsg);

                        // If this is the last message in the set, add the data set to dataQueue. After
                        // std::move is called, data will be a nullptr and the next message to be received
                        // will be part of a new data set.
                        if (goGdpMsg->IsLastMsg())
                        {
                            obj->dataQueue.Add(std::move(data));
                        }
                    }

                    GoTest(kSerializer_EndRead(obj->serializer));
                    needEndRead = false;
                }
                else if (result != kERROR_TIMEOUT)
                {
                    GoLogError("Failed to add data to the data queue with error code (%d).", result);
                    obj->isConnected = false;
                }
            }
        }
        catch (const Go::Exception& e)
        {
            if (needEndRead)
            {
                kSerializer_EndRead(obj->serializer);
            }

            obj->isConnected = false;

            GoLogError("Failed to add data to the data queue with error code (%d)", e.Status());
        }

        return kOK;
    }
}

