/**
 * @file    GoGdpMesh.h
 * @brief   Declares the GoPxLSdk.GoGdpMesh class.
 * 
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#ifndef GO_PXL_SDK_GOGDPMESH_H
#define GO_PXL_SDK_GOGDPMESH_H

#include <GoPxLSdk/GoGdpMsg/GoGdpMsg.h>

class GoGdpMsgTests;

namespace GoPxLSdk
{
    enum Channel_ID
    {
        ID_Vertex = 0,
        ID_Facet = 1,
        ID_Facet_normal = 2,
        ID_Vertex_normal = 3,
        ID_Vertex_texture = 4,
        ID_Vertex_curvature = 5,
    };

    enum Channel_Type
    {
        Type_Invalid = 0,
        Type_Vertex = 1,
        Type_Facet = 2,
        Type_Facet_normal = 3,
        Type_Vertex_normal = 4,
        Type_Vertex_texture = 5,
        Type_Vertex_curvature = 6
    };

    enum Channel_State
    {
        Error = 0,
        Unallocated = 1,
        Allocated = 2,
        Empty = 3,
        Partial_used = 4,
        Full = 5
    };

    typedef struct GoFacet32u
    {
        k32u vertex1;
        k32u vertex2;
        k32u vertex3;
    } GoFacet32u;

    struct MeshMsgChannel
    {
        k32u id;
        k32u type;
        k32s state;
        k32u flag;
        kSize allocatedCount;
        kSize usedCount;
        std::vector<std::shared_ptr<void>> buffer;

        MeshMsgChannel() = default;
        MeshMsgChannel(k32s id, k32u type, k32s state, k32u flag, kSize allocatedCount, kSize usedCount, std::vector<std::shared_ptr<void>> buffer) :
            id(id), type(type), state(state), flag(flag), allocatedCount(allocatedCount), usedCount(usedCount), buffer(buffer)
        {

        }
    };

    class GoPxLSdkClass GoGdpMesh : public GoGdpMsg
    {
    public:
        /**
        * Constructs class GoGdpMesh.
        *
        * @public                @memberof GoGdpMesh
        * @version               Introduced in 0.2.1.53.
        */
        GoGdpMesh();
        ~GoGdpMesh() = default;

        /**
        * Deserialize mesh message.
        *
        * @public                @memberof GoGdpMesh
        * @version               Introduced in 0.2.1.53.
        * @param serializer      The serializer to read.
        * @throws Go::Exception  If failed to deserialize mesh message.
        */
        void Deserialize(kSerializer serializer) override;

        /**
        * Indicator whether any data are allocated.
        *
        * @public                @memberof GoGdpMesh
        * @version               Introduced in 0.2.1.53.
        * @return                True if allocated.
        */
        const bool HasData() const;

        /**
        * Number of system channels(currently 6).
        *
        * @public                @memberof GoGdpMesh
        * @version               Introduced in 0.2.1.53.
        * @return                True if allocated.
        */
        const kSize SystemChannelCount() const;

        /**
        * Maximum number of user channels supported (currently 5).
        *
        * @public                @memberof GoGdpMesh
        * @version               Introduced in 0.2.1.53.
        * @return                The maximum number of user channels supported.
        */
        const kSize MaxUserChannelCount() const;

        /**
        * Number of currently active user channels.
        *
        * @public                @memberof GoGdpMesh
        * @version               Introduced in 0.2.1.53.
        * @return                The number of currently active user channels.
        */
        const kSize UserChannelCount() const;

        /**
        * Number of all channels, including all system and active user channels.
        *
        * @public                @memberof GoGdpMesh
        * @version               Introduced in 0.2.1.53.
        * @return                The number of all channels.
        */
        const kSize ChannelCount() const;

        /**
        * Offset of mesh data.
        *
        * @public                @memberof GoGdpMesh
        * @version               Introduced in 0.2.1.53.
        * @return                The offset values of mesh data.
        */
        const kPoint3d64f Offset() const;

        /**
        * Range of mesh data.
        *
        * @public                @memberof GoGdpMesh
        * @version               Introduced in 0.2.1.53.
        * @return                The range values of mesh data.
        */
        const kPoint3d64f Range() const;

        /**
        * Get channel type.
        *
        * @public                @memberof GoGdpMesh
        * @version               Introduced in 0.2.1.53.
        * @param id              The channel id which to get type value.
        * @return                The channel type value.
        */
        const Channel_Type ChannelType(size_t id) const;

        /**
        * Get channel state.
        *
        * @public                @memberof GoGdpMesh
        * @version               Introduced in 0.2.1.53.
        * @param id              The channel id which get state value.
        * @return                The channel state value.
        */
        const Channel_State ChannelState(size_t id) const;

        /**
        * Channel flag is an user specified field.
        * It can be used to send any optional data to determine how to deserialize user channel data.
        *
        * @public                @memberof GoGdpMesh
        * @version               Introduced in 0.2.1.53.
        * @param id              The channel id which to get flag.
        * @return                The channel flag value.
        */
        const k32u ChannelFlag(size_t id) const;

        /**
        * Number of allocated channel data items.
        * System channels – Equal to number of items(ie, allocateCount for vertex channel is number of Point3d32f allocated).
        * User channels – Equal to number of bytes(8u) of entire buffer.
        *
        * @public                @memberof GoGdpMesh
        * @version               Introduced in 0.2.1.53.
        * @param id              The channel id which to get allocated channel data count.
        * @return                The size of channel count.
        */
        const kSize AllocatedChannelDataCount(size_t id) const;

        /**
        * Number of used channel data items.
        * 
        * @param id              The channel id which to get used channel data count.
        * @return                The number of used channel data items.
        */
        const kSize UsedChannelDataCount(size_t id) const;

        /**
        * Get data buffer of various types depending on channel ID.
        * 
        * @param id              The channel id which to get the buffer data.
        * @return                An array of data values.
        */
        const std::vector<std::shared_ptr<void>>& ChannelData(size_t id) const;

    private:
        bool hasData = false;
        kSize systemChannelCount = 0;
        kSize maxUserChannelCount = 0;
        kSize userChannelCount = 0;
        kSize channelCount = 0;
        kPoint3d64f offset = { 0.0 };
        kPoint3d64f range = { 0.0 };
        std::vector<MeshMsgChannel> channels;

        friend class ::GoGdpMsgTests;

    };

}

#endif

