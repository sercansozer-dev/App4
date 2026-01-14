/**
 * @file    GoDataSet.cpp
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#include <GoPxLSdk/GoDataSet.h>

namespace GoPxLSdk
{
    void GoDataSet::Clear()
    {
        content.clear();
    }

    void GoDataSet::Add(std::shared_ptr<GoGdpMsg> pGoGdpMsg)
    {
        content.push_back(pGoGdpMsg);
    }

    const GoGdpMsg& GoDataSet::GdpMsgAt(size_t index) const
    {
        GoThrowIf((index >= content.size()), kERROR_PARAMETER);

        return *(content.at(index));
    }

    const size_t GoDataSet::Count() const
    {
        return content.size();
    }
}
