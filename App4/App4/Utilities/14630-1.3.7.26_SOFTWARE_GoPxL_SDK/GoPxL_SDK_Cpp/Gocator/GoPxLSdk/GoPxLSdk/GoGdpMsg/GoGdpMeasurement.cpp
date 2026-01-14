/**
 * @file    GoGdpMeasurement.cpp
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#include <GoPxLSdk/GoGdpMsg/GoGdpMeasurement.h>

namespace GoPxLSdk
{
    GoGdpMeasurement::GoGdpMeasurement()
        : GoGdpMsg(MessageType::MEASUREMENT) {}

    void GoGdpMeasurement::Deserialize(kSerializer serializer)
    {
        GoGdpMsg::Deserialize(serializer);

        try
        {
            GoTest(kSerializer_Read64f(serializer, &value));
            GoTest(kSerializer_Read8u(serializer, &decision));
        }
        catch (const Go::Exception&)
        {
            GoRethrow("Failed to deserialize Measurement GDP message.");
        }
    }

    const k64f GoGdpMeasurement::Value() const
    {
        return value;
    }

    const k8u GoGdpMeasurement::Decision() const
    {
        return decision;
    }
}
