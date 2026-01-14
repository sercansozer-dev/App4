#include <GoApi/PointerWrapper/PointerWrapper.h>

kBeginClassEx(GoApi, PointerWrapper)
    //virtual methods
    kAddVMethod(PointerWrapper, kObject, VRelease)
    kAddVMethod(PointerWrapper, kObject, VSize)
kEndClassEx()

GoApiFx(kStatus) PointerWrapper_Construct(PointerWrapper* pointerWrapper, kPointer pointer, kSize size, kObject allocator)
{
    kAlloc alloc = kAlloc_Fallback(allocator);
    kStatus status;

    kCheck(kAlloc_GetObject(alloc, kTypeOf(PointerWrapper), pointerWrapper));

    if (!kSuccess(status = PointerWrapper_Init(*pointerWrapper, kTypeOf(PointerWrapper), pointer, size, alloc)))
    {
        kAlloc_FreeRef(alloc, pointerWrapper);
    }

    return status;
}

GoApiFx(kStatus) PointerWrapper_Init(PointerWrapper pointerWrapper, kType type, kPointer pointer, kSize size, kAlloc alloc)
{
    kObjR(PointerWrapper, pointerWrapper);

    kCheck(kObject_Init(pointerWrapper, type, alloc));
    obj->pointer = pointer;
    obj->size = size;

    return kOK;
}

GoApiFx(kStatus) PointerWrapper_VRelease(PointerWrapper pointerWrapper)
{
    kCheck(kObject_VRelease(pointerWrapper));

    return kOK;
}

GoApiFx(kSize) PointerWrapper_VSize(PointerWrapper pointerWrapper)
{
    kObjR(PointerWrapper, pointerWrapper);

    return obj->size;
}

GoApiFx(kPointer) PointerWrapper_Pointer(PointerWrapper pointerWrapper)
{
    kObjR(PointerWrapper, pointerWrapper);

    kPointer tempPtr = obj->pointer;
    obj->pointer = nullptr;

    return tempPtr;
}