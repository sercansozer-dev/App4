#ifndef GOAPI_POINTER_WRAPPER_X_H
#define GOAPI_POINTER_WRAPPER_X_H

#include <kApi/kObject.h>

kDeclareClassEx(GoApi, PointerWrapper, kObject)

typedef struct PointerWrapperClass
{
    kObjectClass base; 

    kPointer pointer;
    kSize size;
} PointerWrapperClass; 

GoApiFx(kStatus) PointerWrapper_Init(PointerWrapper pointerWrapper, kType type, kPointer pointer, kSize size, kAlloc alloc);
GoApiFx(kSize) PointerWrapper_VSize(PointerWrapper pointerWrapper);
GoApiFx(kStatus) PointerWrapper_VRelease(PointerWrapper pointerWrapper);

#endif
