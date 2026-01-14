/**
 * @file    GoGdpMsg.cpp
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#include <GoPxLSdk/GoGdpMsg/GoGdpMsg.h>

namespace GoPxLSdk
{
    GoGdpMsg::GoGdpMsg(MessageType type)
        : msgType(type)
    {
        this->attributes.spaceType = 0;

        this->attributes.hasTransform = false;

        this->attributes.hasBoundingBox = false;
        // BoundingBox object self initializes to all 0.0.

        this->attributes.arrayedCount = 0;
        this->attributes.arrayedIndex = 0;

        this->attributes.dataSetId = 0;
        this->attributes.isLastMsg = false;
        this->attributes.gdpId = k16U_NULL;
    }

    void GoGdpMsg::Deserialize(kSerializer serializer)
    {
        k16u length;
        k8u byteValue;
        bool needEndRead = false;

        //CommonAttrs
        try
        {
            GoTest(kSerializer_BeginRead(serializer, kTypeOf(k32u), kTRUE));
            needEndRead = true;

            GoTest(kSerializer_Read8u(serializer, &attributes.spaceType));
            GoTest(kSerializer_Read8u(serializer, &byteValue));
            attributes.hasTransform = (byteValue > (k8u) 0);

            if (attributes.hasTransform)
            {
                attributes.transform.Deserialize(serializer);
            }

            GoTest(kSerializer_Read8u(serializer, &byteValue));
            attributes.hasBoundingBox = (byteValue > (k8u) 0);
            if (this->attributes.hasBoundingBox)
            {
                attributes.boundingBox.Deserialize(serializer);
            }

            GoTest(kSerializer_Read32u(serializer, &this->attributes.arrayedCount));
            GoTest(kSerializer_Read32u(serializer, &this->attributes.arrayedIndex));

            GoTest(kSerializer_Read16u(serializer, &length));
            ReadText(serializer, length, attributes.dataSourceId);

            GoTest(kSerializer_Read16u(serializer, &length));
            ReadText(serializer, length, attributes.stampSourceId);

            GoTest(kSerializer_Read64u(serializer, &this->attributes.dataSetId));
            GoTest(kSerializer_Read8u(serializer, &byteValue));
            this->attributes.isLastMsg = (byteValue > 0);

            GoTest(kSerializer_Read16u(serializer, &this->attributes.gdpId));

            GoTest(kSerializer_EndRead(serializer));
            needEndRead = false;
        }
        catch (const Go::Exception&)
        {
            if (needEndRead)
            {
                kSerializer_EndRead(serializer);
            }
            GoRethrow("Failed to deserialize GDP message.");
        }
    }

    // This function will add a null terminator to the character array received
    // from the sender.
    void GoGdpMsg::ReadText(kSerializer serializer, k32u numCharsToRead, std::string& destString)
    {
        kText128 text;
        kChar* longText = nullptr;

        if (numCharsToRead > 0)
        {
            if (numCharsToRead < (k32u) kCountOf(text))
            {
                GoTest(kSerializer_ReadCharArray(serializer, text, numCharsToRead));
                text[numCharsToRead] = '\0';  // Add null terminator.
                destString = text;
            }
            else
            {
                kStatus status;

                // Add one byte for null terminator. Buffer is initialized with zero so
                // string will always be null terminated.
                GoTest(kMemAllocZero(numCharsToRead + 1, &longText));
                status = kSerializer_ReadCharArray(serializer, longText, numCharsToRead);
                if (kSuccess(status))
                {
                    destString = longText;
                }
                // Must free memory before continuing. Ignore return status for the free call.
                kMemFree(longText);
                GoTest(status);
            }
        }
    }

    const MessageType GoGdpMsg::Type() const
    {
        return msgType;
    }

    const std::string& GoGdpMsg::DataSourceId() const
    {
        return attributes.dataSourceId;
    }

    const std::string& GoGdpMsg::StampSourceId() const
    {
        return attributes.stampSourceId;
    }

    const k8u GoGdpMsg::SpaceType() const
    {
        return attributes.spaceType;
    }

    const bool GoGdpMsg::HasTransform() const
    {
        return attributes.hasTransform;
    }

    const GoGdpTransform& GoGdpMsg::Transform() const
    {
        return attributes.transform;
    }

    const k32u GoGdpMsg::ArrayedCount() const
    {
        return attributes.arrayedCount;
    }

    const k32u GoGdpMsg::ArrayedIndex() const
    {
        return attributes.arrayedIndex;
    }

    const bool GoGdpMsg::HasBoundingBox() const
    {
        return attributes.hasBoundingBox;
    }

    const GoGdpBoundingBox& GoGdpMsg::BoundingBox() const
    {
        return attributes.boundingBox;
    }

    // GOS-5176: rename FrameId to DataSetId.
    const k64u GoGdpMsg::DataSetId() const
    {
        return attributes.dataSetId;
    }

    const bool GoGdpMsg::IsLastMsg() const
    {
        return attributes.isLastMsg;
    }

    const k16u GoGdpMsg::GdpId() const
    {
        return attributes.gdpId;
    }
}
