/**
* @file     Profiling.h
* @brief    This header provides utility functions to make profiling easier, as well
*           as declaring the statically stored probes.
*            
*/
#ifndef GOAPI_PROFILING_H
#define GOAPI_PROFILING_H

#include <GoApi/GoApi.h>

//#define GO_API_PROFILE
#ifdef GO_API_PROFILE

#define K_PROFILE
#include <kFireSync/Health/kProfileProbe.h>

#else
typedef void* kProfileProbe;
#endif

namespace Go
{
namespace Profile
{
extern GoApiDx(bool) ProbesInitialized;

extern GoApiDx(kProfileProbe) xmlSerializer_deserialize;
extern GoApiDx(kProfileProbe) xmlSerializer_create;
extern GoApiDx(kProfileProbe) xmlSerializer_readChildren;
extern GoApiDx(kProfileProbe) DynamicContainerBase_Register;
extern GoApiDx(kProfileProbe) DynamicContainerBase_Register_FindId;
extern GoApiDx(kProfileProbe) DynamicContainerBase_Register_FindId_Any;
extern GoApiDx(kProfileProbe) DynamicContainerBase_Register_FindId_Specific;
extern GoApiDx(kProfileProbe) DynamicContainer_Create_FindFactory;
extern GoApiDx(kProfileProbe) DynamicContainer_Create_UseFactory;
extern GoApiDx(kProfileProbe) DynamicContainer_Create_Add;

GoApiCppFx(void) InitProbes(kAlloc alloc);
GoApiCppFx(void) FreeProbes();
GoApiCppFx(void) ResetProbes(kAlloc alloc);
GoApiCppFx(void) Start(kProfileProbe probe);
GoApiCppFx(void) Stop(kProfileProbe probe);

}

}

#endif