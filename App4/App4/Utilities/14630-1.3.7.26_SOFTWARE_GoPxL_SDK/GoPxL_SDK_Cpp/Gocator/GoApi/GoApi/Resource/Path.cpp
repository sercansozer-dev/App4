#include "Path.h"
#include <GoApi/Exception.h>
#include <GoApi/Resource/ResourceDef.h>

namespace GoApi
{

std::string Path::Format(const std::string& pathTemplate, std::initializer_list<std::string> keys)
{
    std::string result;

    size_t curPos = 0;

    for (auto& curKey : keys)
    {
        auto keyPos = pathTemplate.find(GOAPI_RESOURCE_PATH_KEY_PREFIX, curPos);

        // No location to insert key into.
        if (keyPos == std::string::npos)
        {
            GoThrow(kERROR_INCOMPLETE);
        }

        auto separatorPos = pathTemplate.find(GOAPI_RESOURCE_PATH_SEPARATOR, keyPos + 1);

        // Normally the separator marks the end of the key.
        // In this case, use the end of the string, which is AFTER the last char.
        if (separatorPos == std::string::npos)
        {
            separatorPos = pathTemplate.size();
        }

        result += pathTemplate.substr(curPos, keyPos - curPos) + curKey;
        curPos = separatorPos;
    }

    if (curPos < pathTemplate.size())
    {
        result += pathTemplate.substr(curPos);
    }

    return result;
}

} // namespace
