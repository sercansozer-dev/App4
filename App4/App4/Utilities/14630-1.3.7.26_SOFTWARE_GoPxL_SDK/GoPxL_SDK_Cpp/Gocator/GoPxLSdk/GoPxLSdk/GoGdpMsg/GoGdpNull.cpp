/**
 * @file    GoGdpNull.cpp
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#include <GoPxLSdk/GoGdpMsg/GoGdpNull.h>


namespace GoPxLSdk
{
    GoGdpNull::GoGdpNull()
        : GoGdpMsg(MessageType::NULL_TYPE) {}

    void GoGdpNull::Deserialize(kSerializer serializer)
    {
        bool needEndRead = false;

        GoGdpMsg::Deserialize(serializer);

        try
        {
             GoTest(kSerializer_BeginRead(serializer, kTypeOf(k16u), kTRUE));
             needEndRead = true;

             GoTest(kSerializer_Read32s(serializer, &errorStatus));
             GoTest(kSerializer_EndRead(serializer));
             needEndRead = false;
        }
        catch (const Go::Exception&)
        {
            if (needEndRead)
            {
                kSerializer_EndRead(serializer);
            }
            GoRethrow("Failed to deserialize Null GDP message.");
        }
    }

    const k32s GoGdpNull::ErrorStatus() const
    {
        return errorStatus;
    }

}
