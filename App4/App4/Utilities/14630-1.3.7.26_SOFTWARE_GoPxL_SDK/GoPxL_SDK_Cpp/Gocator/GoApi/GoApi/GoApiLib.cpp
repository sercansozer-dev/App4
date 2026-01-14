#include <GoApi/GoApiLib.h>
#include <GoApi/PointerWrapper/PointerWrapper.h>
#include <GoApi/GoDataTree/GoDataTree.h>

#define GOAPI_VERSION  kVersion_Stringify_(1, 0, 0, 0)

kBeginAssemblyEx(GoApi, GoApiLib, GOAPI_VERSION, GOAPI_VERSION)
    kAddDependency(kApiLib)

    kAddType(PointerWrapper)

    kAddType(GoDataTreeArray)
    kAddType(GoDataTreeObject)

kEndAssemblyEx()
