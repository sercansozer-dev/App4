/**
 * @file    GoGdpSignal.cpp
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#include <GoPxLSdk/GoGdpMsg/GoGdpSignal.h>


namespace GoPxLSdk
{
    GoGdpSignal::GoGdpSignal()
        : GoGdpMsg(MessageType::SIGNAL) {}

    void GoGdpSignal::Deserialize(kSerializer serializer)
    {
        bool needEndRead = false;

        GoGdpMsg::Deserialize(serializer);

        try
        {
            GoTest(kSerializer_BeginRead(serializer, kTypeOf(k16u), kTRUE));
            needEndRead = true;

            GoTest(kSerializer_EndRead(serializer));
            needEndRead = false;
        }
        catch (const Go::Exception&)
        {
            if (needEndRead)
            {
                kSerializer_EndRead(serializer);
            }
            GoRethrow("Failed to deserialize Signal GDP message.");
        }

    }

}
