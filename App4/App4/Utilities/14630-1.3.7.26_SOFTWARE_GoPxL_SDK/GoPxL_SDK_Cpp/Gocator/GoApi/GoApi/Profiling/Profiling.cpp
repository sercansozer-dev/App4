#include <GoApi/Profiling/Profiling.h>

namespace Go
{
namespace Profile
{
    GoApiDx(bool) ProbesInitialized = false;
    //Storage for statically declared probes
    GoApiDx(kProfileProbe) xmlSerializer_deserialize = nullptr;
    GoApiDx(kProfileProbe) xmlSerializer_create = nullptr;
    GoApiDx(kProfileProbe) xmlSerializer_readChildren = nullptr;
    GoApiDx(kProfileProbe) DynamicContainerBase_Register = nullptr;
    GoApiDx(kProfileProbe) DynamicContainerBase_Register_FindId = nullptr;
    GoApiDx(kProfileProbe) DynamicContainerBase_Register_FindId_Any = nullptr;
    GoApiDx(kProfileProbe) DynamicContainerBase_Register_FindId_Specific = nullptr;
    GoApiDx(kProfileProbe) DynamicContainer_Create_FindFactory = nullptr;
    GoApiDx(kProfileProbe) DynamicContainer_Create_UseFactory = nullptr;
    GoApiDx(kProfileProbe) DynamicContainer_Create_Add = nullptr;

    GoApiCppFx(void) InitProbes(kAlloc alloc)
    {
        #ifdef GO_API_PROFILE
        if (!ProbesInitialized)
        {
            ProbesInitialized = true;
            kProfileProbe_Construct(&xmlSerializer_deserialize, "XMLSerializer_Deserialize", alloc);
            kProfileProbe_Construct(&xmlSerializer_create, "XMLSerializer_Deserialize_Create", alloc);
            kProfileProbe_Construct(&xmlSerializer_readChildren, "XMLSerializer_Deserialize_ReadChildren", alloc);
            kProfileProbe_Construct(&DynamicContainerBase_Register, "DynContBase_Register", alloc);
            kProfileProbe_Construct(&DynamicContainerBase_Register_FindId, "DynContBase_Register_FindId", alloc);
            kProfileProbe_Construct(&DynamicContainerBase_Register_FindId_Any, "DynContBase_Register_FindId_Any", alloc);
            kProfileProbe_Construct(&DynamicContainerBase_Register_FindId_Specific, "DynContBase_Register_FindId_Specific", alloc);
            kProfileProbe_Construct(&DynamicContainer_Create_FindFactory, "DynCont_Create_FindFactory", alloc);
            kProfileProbe_Construct(&DynamicContainer_Create_UseFactory, "DynCont_Create_UseFactory", alloc);
            kProfileProbe_Construct(&DynamicContainer_Create_Add, "DynCont_Create_AddFactory", alloc);
        }
        #endif
    }

    GoApiCppFx(void) FreeProbes()
    {
        #ifdef GO_API_PROFILE
        kObject_Destroy(xmlSerializer_deserialize);
        kObject_Destroy(xmlSerializer_create);
        kObject_Destroy(xmlSerializer_readChildren);
        kObject_Destroy(DynamicContainerBase_Register);
        kObject_Destroy(DynamicContainerBase_Register_FindId);
        kObject_Destroy(DynamicContainerBase_Register_FindId_Any);
        kObject_Destroy(DynamicContainerBase_Register_FindId_Specific);
        kObject_Destroy(DynamicContainer_Create_FindFactory);
        kObject_Destroy(DynamicContainer_Create_UseFactory);
        kObject_Destroy(DynamicContainer_Create_Add);
        ProbesInitialized = false;
        #endif
    }

    GoApiCppFx(void) ResetProbes(kAlloc alloc)
    {
        FreeProbes();
        InitProbes(alloc);
    }

    GoApiCppFx(void) Start(kProfileProbe probe)
    {
        #ifdef GO_API_PROFILE
        kProfileProbe_Start(probe);
        #endif
    }
    GoApiCppFx(void) Stop(kProfileProbe probe)
    {
        #ifdef GO_API_PROFILE
        kProfileProbe_Stop(probe);
        #endif
    }
}
}


