#include "EventType.h"

namespace GoApi
{
GoApiCppFx(std::string) EventType_Name(EventType type)
{
    std::string typeName;

    switch (type)
    {
    case EventType::CREATED:
        typeName = "created";
        break;
    case EventType::UPDATED:
        typeName = "updated";
        break;
    case EventType::DELETED:
        typeName = "deleted";
        break;
    case EventType::EMBEDDED_UPDATED:
        typeName = "embeddedUpdated";
        break;
    default:
        typeName = "Unknown";
    }

    return typeName;
}

} // Namespaces
