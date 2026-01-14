/**@file    EventType.h
 * Resource notification event types
 */

#ifndef GOAPI_EVENT_TYPE_H
#define GOAPI_EVENT_TYPE_H

#include <GoApi/GoApiDef.h>

namespace GoApi
{
enum struct EventType : k32s
{
    // Raised when a resource is created.
    CREATED = 1,

    // Raised when a resource has updated parameters.
    UPDATED = 2,

    // Raised when a resource is deleted.
    DELETED = 3,

    // Raised when the embedded items on a resource has been updated, signalling a client to refresh a resource.
    EMBEDDED_UPDATED = 4
};

/**
 * Provides an EventType name.
 *
 * @param type          The EventType to resolve.
 * @return              EventType name.
 */
GoApiCppFx(std::string) EventType_Name(EventType type);


} // Namespace

#endif  // GOAPI_EVENT_TYPE_H
