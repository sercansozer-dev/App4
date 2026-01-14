/**
 * @file    GoGdpString.h
 * @brief   Declares the GoPxLSdk.GoGdpString class.
 * 
 * @internal
 * Copyright (C) 2024-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#pragma once

#include <GoPxLSdk/GoGdpMsg/GoGdpMsg.h>

class GoGdpMsgTests;

namespace GoPxLSdk
{

    class GoPxLSdkClass GoGdpString : public GoGdpMsg
    {
    public:
        /**
         * Constructs GoGdpString.
         * 
         * @public                @memberof GoGdpString
         */
        GoGdpString();
        ~GoGdpString() = default;

        /**
         * Deserializes string message.
         * 
         * @public                @memberof GoGdpString
         * @param serializer      The serializer to read.
         * @throws Go::Exception  If failed to deserialize string message.
         */
        void Deserialize(kSerializer serializer) override;

        /**
         * Gets the string contained in the message.
         *
         * @public                @memberof GoGdpString
         * @return                The string.
         */
        const std::string& String() const;

    private:
        std::string str;
        friend class ::GoGdpMsgTests;
    };
}
