/**
 * @file    GoGdpMsg.h
 * @brief   Declares the GoPxLSdk.GoGdpMsg class.
 * 
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#ifndef GO_PXL_SDK_GOGDPMSG_H
#define GO_PXL_SDK_GOGDPMSG_H

#include <GoApi/GoApi.h>
#include <kApi/Io/kSerializer.h>
#include <GoPxLSdk/GoGdpMsg/GoGdpMsgDef.h>
#include <GoPxLSdk/GoGdpMsg/GoGdpBoundingBox.h>
#include <GoPxLSdk/GoGdpMsg/GoGdpTransform.h>

namespace GoPxLSdk
{

    struct CommonAttributes
    {
        k8u spaceType;
        bool hasTransform;
        GoGdpTransform transform;
        bool hasBoundingBox;
        GoGdpBoundingBox boundingBox;
        k32u arrayedCount;
        k32u arrayedIndex;
        std::string dataSourceId;
        std::string stampSourceId;
        k64u dataSetId;
        bool isLastMsg;
        k16u gdpId;
    };

    class GoPxLSdkClass GoGdpMsg
    {
    public:
        /**
        * Constructs GoGdpMsg.
        *
        * @public                @memberof GoGdpMsg
        * @version               Introduced in 0.2.1.53.
        * @param type            The message type.
        */
        GoGdpMsg(MessageType type = MessageType::NULL_TYPE);

        virtual ~GoGdpMsg() = default;

        /**
        * Deserialize common attributes.
        *
        * @public                @memberof GoGdpMsg
        * @version               Introduced in 0.2.1.53.
        * @param serializer      The serializer to read.
        * @throws Go::Exception  If failed to deserialize common attributes.
        */
        virtual void Deserialize(kSerializer serializer);

        /**
        * Get concrete message type.
        *
        * @public                @memberof GoGdpMsg
        * @version               Introduced in 0.2.1.53.
        * @return                The gdp message type.
        */
        const MessageType Type() const;

        /**
        * Get the data source Id.
        * DataSource Id is used to identify the source of the message
        * and distinguish multiple arrayed messages.
        *
        * @public                @memberof GoGdpMsg
        * @version               Introduced in 0.2.1.53.
        * @return                The const string reference for the data source Id.
        */
        const std::string& DataSourceId() const;

        /**
        * Get the stamp source Id.
        * Unique Id string used to identify the set that this message belongs to.
        * As of writing, this is the "ScannerId",
        * however in the future this will change to either the dataSourceId for the stamp output
        * associated with this data or perhaps a more opaque uniuqe ID.
        * Regardless, clients should treat this as an opaque ID,
        * used solely for collecting many messages into a collection that describes a set.
        *
        * @public                @memberof GoGdpMsg
        * @version               Introduced in 0.2.1.53.
        * @return                The const string reference for the stamp source Id.
        */
        const std::string& StampSourceId() const;

        /**
        * Get space type value.
        * can be one of
        *   0 : none (Transform doesn't apply)
        *   1 : data
        *   2 : image
        *
        * @public                @memberof GoGdpMsg
        * @version               Introduced in 0.2.1.53.
        * @return                The space type value.
        */
        const k8u SpaceType() const;

        /**
        * Get indicator if a transform (returned by Transform() API) exists for ths message.
        * If not, then the transform returned by Transform() API is an identity transform which
        * contains an identity matrix for the transform.
        *
        * @public                @memberof GoGdpMsg
        * @version               Introduced in 0.2.1.53.
        * @return                True if non-identify tranform is present, otherwise transform is an identity transform.
        */
        const bool HasTransform() const;

        /**
        * Get pointer to the transformation matrix values.
        *   Optional: only sent if HasTransform() returns true.
        *   Applying this transform to the associated data results in the data in common frame of reference.
        *   This is typically true when "spaceType" is set as 1 : data.
        *   If "spaceType" is on of the other values, (0:none, 2 : image, 3 : custom),
        *   the interpretation of how the transform is applied can be different.This is to be determined.
        *
        * @public                @memberof GoGdpMsg
        * @version               Introduced in 0.2.1.53.
        * @return                A reference to the transformation matrix values.
        */
        const GoGdpTransform& Transform() const;

        /**
        * Get the count of messages in the array.
        * Any message could be a member of an arrayed output.
        * If this is the case, this indicates the count of messages in the array.
        * If the message is not in an array (ie it is a non-arrayed output) the value of this field will be 0.
        *
        * @public                @memberof GoGdpMsg
        * @version               Introduced in 0.2.1.53.
        * @return                The count of messages in the array.
        */
        const k32u ArrayedCount() const;

        /**
        * Get the index of this message in the array.
        * This field only applies if arrayCount is >= 1.(if it is in an array).
        *
        * @public                @memberof GoGdpMsg
        * @version               Introduced in 0.2.1.53.
        * @return                The index of this message in the array.
        */
        const k32u ArrayedIndex() const;

       /**
        * Get indicator if a bounding box exists or not.
        *
        * @public                @memberof GoGdpMsg
        * @version               Introduced in 0.2.1.53.
        * @return                True if bounding box exists, false if not.
        */
        const bool HasBoundingBox() const;

        /**
        * Get bounding box.
        * The bounding box is meaningful only if HasBoundingBox() returns true.
        * Otherwise the bounding box values are undefined.
        *
        * @public                @memberof GoGdpMsg
        * @version               Introduced in 0.2.1.53.
        * @return                A reference to the bounding box.
        */
        const GoGdpBoundingBox& BoundingBox() const;

        /**
        * Get the data set identifier.
        * The data set identifier is used to uniquely identify which set the current message belongs to.
        * All messages within a set will always be received together.
        * No messages between different sets will be interleaved.
        *
        * @public                @memberof GoGdpMsg
        * @version               Introduced in 0.3.5.24.
        * @return                The message set identifier this message belongs to.
        */
        const k64u DataSetId() const;

       /**
        * Get whether this messages is the last one within its current set.
        *
        * @public                @memberof GoGdpMsg
        * @version               Introduced in 0.2.1.53.
        * @return                True if this is the last message within current set, false otherwise.
        */
        const bool IsLastMsg() const;

       /**
        * Get the gdpId.
        * The gdpId is used to identify which data output source it is produced from.
        *
        * @public                @memberof GoGdpMsg
        * @version               Introduced in 0.3.5.24.
        * @return                The gdpId.
        */
        const k16u GdpId() const;

    protected:
        void ReadText(kSerializer serializer, k32u numCharsToRead, std::string& destString);

    protected:
        CommonAttributes attributes;
        MessageType msgType;
    };



}
#endif

