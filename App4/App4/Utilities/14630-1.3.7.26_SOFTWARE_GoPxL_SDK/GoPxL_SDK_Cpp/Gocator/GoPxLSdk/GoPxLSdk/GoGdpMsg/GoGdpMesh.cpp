/**
 * @file    GoGdpMesh.cpp
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#include <GoPxLSdk/GoGdpMsg/GoGdpMesh.h>
#include <kApi/Data/kArray1.h>

namespace GoPxLSdk
{
    GoGdpMesh::GoGdpMesh()
        : GoGdpMsg(MessageType::MESH) {}

    void GoGdpMesh::Deserialize(kSerializer serializer)
    {
        k32u numSystemChannel;
        k32u maxUserChannel;
        k32u numUserChannel;
        k32u numTotalChannel;
        k32u allocatedSize;
        k32u usedSize;
        k8u byteValue;
        bool needEndRead = false;


        GoGdpMsg::Deserialize(serializer);

        try
        {
            GoTest(kSerializer_BeginRead(serializer, kTypeOf(k16u), kTRUE));
            needEndRead = true;

            GoTest(kSerializer_Read8u(serializer, &byteValue));
            hasData = (byteValue > 0);
            GoTest(kSerializer_Read32u(serializer, &numSystemChannel));
            GoTest(kSerializer_Read32u(serializer, &maxUserChannel));
            GoTest(kSerializer_Read32u(serializer, &numUserChannel));
            GoTest(kSerializer_Read32u(serializer, &numTotalChannel));

            GoTest(kSerializer_Read64f(serializer, &offset.x));
            GoTest(kSerializer_Read64f(serializer, &offset.y));
            GoTest(kSerializer_Read64f(serializer, &offset.z));

            GoTest(kSerializer_Read64f(serializer, &range.x));
            GoTest(kSerializer_Read64f(serializer, &range.y));
            GoTest(kSerializer_Read64f(serializer, &range.z));

            GoTest(kSerializer_EndRead(serializer));
            needEndRead = false;

            systemChannelCount = (kSize)numSystemChannel;
            maxUserChannelCount = (kSize)maxUserChannel;
            userChannelCount = (kSize)numUserChannel;
            channelCount = (kSize)numTotalChannel;

            channels.clear();

            for (k32u i = 0; i < channelCount; i++)
            {
                MeshMsgChannel channel;

                GoTest(kSerializer_BeginRead(serializer, kTypeOf(k16u), kTRUE));
                needEndRead = true;
                GoTest(kSerializer_Read32u(serializer, &channel.id));
                GoTest(kSerializer_Read32u(serializer, &channel.type));
                GoTest(kSerializer_Read32s(serializer, &channel.state));
                GoTest(kSerializer_Read32u(serializer, &channel.flag));
                GoTest(kSerializer_Read32u(serializer, &allocatedSize));
                GoTest(kSerializer_Read32u(serializer, &usedSize));
                GoTest(kSerializer_EndRead(serializer));
                needEndRead = false;

                channel.allocatedCount = (kSize)allocatedSize;
                channel.usedCount = (kSize)usedSize;

                if (channel.allocatedCount > 0)
                {
                    switch (channel.id)
                    {
                    case ID_Vertex:
                    case ID_Facet_normal:
                    case ID_Vertex_normal:
                    {
                        for (k32u i = 0; i < channel.allocatedCount; i++)
                        {
                            auto item = std::make_shared<kPoint3d32f>();
                            GoTest(kSerializer_Read32f(serializer, &item->x));
                            GoTest(kSerializer_Read32f(serializer, &item->y));
                            GoTest(kSerializer_Read32f(serializer, &item->z));
                            channel.buffer.push_back(item);
                        }
                        break;
                    }
                    case ID_Facet:
                    {
                        for (k32u i = 0; i < channel.allocatedCount; i++)
                        {
                            auto item = std::make_shared<GoFacet32u>();
                            GoTest(kSerializer_Read32u(serializer, &item->vertex1));
                            GoTest(kSerializer_Read32u(serializer, &item->vertex2));
                            GoTest(kSerializer_Read32u(serializer, &item->vertex3));
                            channel.buffer.push_back(item);
                        }
                        break;
                    }
                    case ID_Vertex_texture:
                    {
                        for (k32u i = 0; i < channel.allocatedCount; i++)
                        {
                            auto item = std::make_shared<k8u>();
                            GoTest(kSerializer_Read8u(serializer, item.get()));
                            channel.buffer.push_back(item);
                        }
                        break;
                    }

                    case ID_Vertex_curvature:
                    {
                        for (k32u i = 0; i < channel.allocatedCount; i++)
                        {
                            auto item = std::make_shared<k32f>();
                            GoTest(kSerializer_Read32f(serializer, item.get()));
                            channel.buffer.push_back(item);
                        }
                        break;
                    }

                    default:
                        for (k32u i = 0; i < channel.allocatedCount; i++)
                        {
                            auto item = std::make_shared<k8u>();
                            GoTest(kSerializer_Read8u(serializer, item.get()));
                            channel.buffer.push_back(item);
                        }
                        break;
                    }
                }

                channels.push_back(channel);
            }

        }
        catch (const Go::Exception&)
        {
            if (needEndRead)
            {
                kSerializer_EndRead(serializer);
            }
            GoRethrow("Failed to deserialize Mesh GDP message.");
        }
    }

    const bool GoGdpMesh::HasData() const
    {
        return hasData;
    }

    const kSize GoGdpMesh::SystemChannelCount() const
    {
        return systemChannelCount;
    }

    const kSize GoGdpMesh::MaxUserChannelCount() const
    {
        return maxUserChannelCount;
    }

    const kSize GoGdpMesh::UserChannelCount() const
    {
        return userChannelCount;
    }

    const kSize GoGdpMesh::ChannelCount() const
    {
        return channelCount;
    }

    const kPoint3d64f GoGdpMesh::Offset() const
    {
        return offset;
    }

    const kPoint3d64f GoGdpMesh::Range() const
    {
        return range;
    }

    const Channel_Type GoGdpMesh::ChannelType(size_t id) const
    {
        return (Channel_Type)channels.at(id).type;
    }

    const Channel_State GoGdpMesh::ChannelState(size_t id) const
    {
        return (Channel_State)channels.at(id).state;
    }

    const k32u GoGdpMesh::ChannelFlag(size_t id) const
    {
        return channels.at(id).flag;
    }

    const kSize GoGdpMesh::AllocatedChannelDataCount(size_t id) const
    {
        return channels.at(id).allocatedCount;
    }

    const kSize GoGdpMesh::UsedChannelDataCount(size_t id) const
    {
        return channels.at(id).usedCount;
    }

    const std::vector<std::shared_ptr<void>>& GoGdpMesh::ChannelData(size_t id) const
    {
        return channels.at(id).buffer;
    }


}
