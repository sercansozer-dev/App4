/** 
 * @file    kImage.cpp
 *
 * @internal
 * Copyright (C) 2003-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#include <kApi/Data/kImage.h>
#include <kApi/Data/kArrayProvider.h>
#include <kApi/Io/kFile.h>
#include <kApi/Io/kSerializer.h>
#include <kApi/Io/kDat5Serializer.h>
#include <kApi/Utils/kImageUtils.x.h>

kBeginClassEx(k, kImage) 
    
    //serialization versions
    kAddPrivateVersionEx(kImage, "kdat5", "5.0.0.0", "31-3", WriteDat5V3, ReadDat5V3)
    kAddPrivateVersionEx(kImage, "kdat6", "5.7.1.0", "kImage-0", WriteDat6V0, ReadDat6V0)

    //special constructors
    kAddPrivateFrameworkConstructor(kImage, ConstructFramework)

    //virtual methods
    kAddPrivateVMethod(kImage, kObject, VRelease)
    kAddPrivateVMethod(kImage, kObject, VClone)
    kAddPrivateVMethod(kImage, kObject, VSize)
    kAddPrivateVMethod(kImage, kObject, VAllocTraits)

    //array provider interface 
    kAddInterface(kImage, kArrayProvider)
    kAddPrivateIVMethod(kImage, kArrayProvider, VConstructDefault, ConstructDefaultEx)
    kAddPrivateIVMethod(kImage, kArrayProvider, VImitate, Imitate)
    kAddPrivateIVMethod(kImage, kArrayProvider, VAssign, Assign)
    kAddIVMethod(kImage, kArrayProvider, VItemType, PixelType)
    kAddIVMethod(kImage, kArrayProvider, VCount, Area)
    kAddIVMethod(kImage, kArrayProvider, VData, Data)
    kAddIVMethod(kImage, kArrayProvider, VDataSize, DataSize)
    kAddIVMethod(kImage, kArrayProvider, VDataAlloc, DataAlloc)

kEndClassEx() 

kFx(kStatus) kImage_ConstructEx(kImage* image, kType pixelType, kPixelFormat format, kSize width, kSize height, kAlloc allocator, kAlloc valueAllocator, kMemoryAlignment valueAlignment)
{
    kAlloc alloc = kAlloc_Fallback(allocator);
    kAlloc valueAlloc = kAlloc_Fallback(valueAllocator);
    kStatus status; 

    kCheck(kAlloc_GetObject(alloc, kTypeOf(kImage), image)); 

    if (!kSuccess(status = xkImage_Init(*image, kTypeOf(kImage), pixelType, format, width, height, alloc, valueAlloc, valueAlignment)))
    {
        kAlloc_FreeRef(alloc, image); 
    }

    return status; 
} 

kFx(kStatus) kImage_Construct(kImage* image, kType pixelType, kSize width, kSize height, kAlloc allocator)
{
    return kImage_ConstructEx(image, pixelType, width, height, allocator, allocator);
}

kFx(kStatus) xkImage_ConstructFramework(kImage* image, kAlloc allocator)
{
    return kImage_ConstructEx(image, kNULL, 0, 0, allocator, allocator);
}

kFx(kStatus) xkImage_ConstructDefaultEx(kImage* image, kAlloc objectAlloc, kAlloc valueAlloc)
{
    return kImage_ConstructEx(image, kNULL, 0, 0, objectAlloc, valueAlloc);
}

kFx(kStatus) kImage_Import(kImage* image, const kChar* fileName, kAlloc allocator)
{
    return xkImageUtils_Load(image, fileName, allocator);
}

kFx(kStatus) kImage_Export(kImage image, const kChar* fileName)
{
    return xkImageUtils_Save(image, fileName, kNULL);
}

kFx(kStatus) xkImage_Init(kImage image, kType classType, kType pixelType, kPixelFormat format, kSize width, kSize height, kAlloc alloc, kAlloc valueAlloc, kMemoryAlignment alignment)
{
    kObjR(kImage, image);
    kStatus status = kOK; 

    kCheckArgs(kIsNull(pixelType) || kType_IsValue(pixelType)); 

    if (format >= kPIXEL_FORMAT_VOID_TYPE_START)
    {
        kCheckArgs(kIsNull(pixelType) || pixelType == kTypeOf(kVoid));
    }

    kCheck(kObject_Init(image, classType, alloc)); 
    
    obj->dataAlloc = valueAlloc;
    obj->pixelType = kNULL;
    obj->pixelSize = 0;
    obj->allocSize = 0;
    obj->pixels = kNULL;
    obj->stride = 0;
    obj->width = 0;
    obj->height = 0;
    obj->isAttached = kFALSE;
    obj->format = format;
    obj->cfa = 0;
    obj->alignment = alignment;

    kTry
    {
        kTest(kImage_AllocateEx(image, pixelType, format, width, height));
    }
    kCatch(&status)
    {
        xkImage_VRelease(image); 
        kEndCatch(status); 
    }

    return kOK; 
}

kFx(kStatus) xkImage_VClone(kImage image, kImage source, kAlloc valueAlloc, kObject context)
{
    kObj(kImage, image);

    obj->dataAlloc = valueAlloc;
    obj->format = kImage_PixelFormat(source);
    obj->cfa = kImage_Cfa(source);
 
    kCheck(xkImage_Imitate(image, source)); 

    kCheck(xkImage_CopyPixels(image, source, context)); 

    return kOK; 
}

kFx(kStatus) xkImage_VRelease(kImage image)
{
    kObj(kImage, image); 

    if (!obj->isAttached)
    {
        kCheck(kAlloc_Free(obj->dataAlloc, obj->pixels)); 
    }

    kCheck(kObject_VRelease(image)); 

    return kOK; 
}

kFx(kAllocTrait) xkImage_VAllocTraits(kImage image)
{
    kObj(kImage, image);

    return kAlloc_Traits(kObject_Alloc(image)) | kAlloc_Traits(kImage_DataAlloc(image));
}

kFx(kStatus) xkImage_WriteDat5V3(kImage image, kSerializer serializer)
{
    kObj(kImage, image); 
    kTypeVersion itemVersion; 
    kSize i; 

    kCheckState(!kAlloc_IsForeign(obj->dataAlloc));
    
    kCheck(kSerializer_WriteSize(serializer, obj->width)); 
    kCheck(kSerializer_WriteSize(serializer, obj->height)); 
    kCheck(kSerializer_Write32s(serializer, obj->cfa)); 

    //works around a bug/limitation in serialization version 5.3 -- pixel type version not written
    kCheck(kObject_Is(serializer, kTypeOf(kDat5Serializer))); 
    kCheck(xkDat5Serializer_WriteTypeWithoutVersion(serializer, obj->pixelType, &itemVersion));

    for (i = 0; i < obj->height; ++i)
    {
        void* row = kImage_RowAt(image, i); 

        kCheck(kSerializer_WriteSize(serializer, obj->width)); 
        kCheck(kSerializer_WriteType(serializer, obj->pixelType, &itemVersion));
        kCheck(kSerializer_WriteItems(serializer, obj->pixelType, itemVersion, row, obj->width)); 
    }

    return kOK; 
}

kFx(kStatus) xkImage_ReadDat5V3(kImage image, kSerializer serializer)
{
    kObj(kImage, image); 
    kSize width = 0, height = 0; 
    k32s cfa; 
    kTypeVersion itemVersion;
    kType pixelType = kNULL;            

    kCheck(kSerializer_ReadSize(serializer, &width)); 
    kCheck(kSerializer_ReadSize(serializer, &height)); 

    kCheck(kSerializer_Read32s(serializer, &cfa)); 
    obj->cfa = cfa; 

    //works around a bug/limitation in serialization version 5.3 -- pixel type version not written
    kCheck(kObject_Is(serializer, kTypeOf(kDat5Serializer))); 
    kCheck(xkDat5Serializer_ReadTypeExplicitVersion(serializer, 0, &pixelType, &itemVersion)); 

    kCheck(kImage_Allocate(image, pixelType, width, height)); 

    for (kSize i = 0; i < obj->height; ++i)
    {
        void* row = kImage_RowAt(image, i); 

        kCheck(kSerializer_ReadSize(serializer, &width)); 
        kCheck(kSerializer_ReadType(serializer, &pixelType, &itemVersion));
        kCheck(kSerializer_ReadItems(serializer, pixelType, itemVersion, row, obj->width)); 
    }
   
    return kOK; 
}

kFx(kStatus) xkImage_WriteDat6V0(kImage image, kSerializer serializer)
{
    kObj(kImage, image); 
    kTypeVersion itemVersion; 
    kBool isVoidType = xkPixelFormat_UseVoidPixel(obj->format);
    kType streamPixType = (isVoidType) ? kTypeOf(k8u) : obj->pixelType;
    kSize streamWidth = (isVoidType) ? obj->stride : obj->width;
    kSize streamPixelSize = (isVoidType) ? 1 : obj->pixelSize;
    kSize i; 

    kCheckState(!kAlloc_IsForeign(obj->dataAlloc));

    kCheck(kSerializer_WriteType(serializer, streamPixType, &itemVersion));
    kCheck(kSerializer_WriteSize(serializer, streamWidth)); 
    kCheck(kSerializer_WriteSize(serializer, obj->height));
    kCheck(kSerializer_Write32s(serializer, obj->cfa)); 
    kCheck(kSerializer_Write32s(serializer, obj->format)); 

    //reserve space for optional fields
    kCheck(kSerializer_Write16u(serializer, 0)); 

    if ((streamWidth*streamPixelSize) == obj->stride)
    {
        kCheck(kSerializer_WriteItems(serializer, streamPixType, itemVersion, obj->pixels, streamWidth*obj->height));       
    }
    else
    {
        for (i = 0; i < obj->height; ++i)
        {
            void* row = kImage_RowAt(image, i); 

            kCheck(kSerializer_WriteItems(serializer, streamPixType, itemVersion, row, streamWidth)); 
        }
    }

    return kOK; 
}

kFx(kStatus) xkImage_ReadDat6V0(kImage image, kSerializer serializer)
{
    kObj(kImage, image); 
    kType pixelType = kNULL;            
    kType allocPixelType = kNULL;            
    kTypeVersion itemVersion; 
    kSize width = 0, height = 0; 
    kSize allocWidth;
    k16u optional = 0; 
    k32s cfa, format;
    kBool isVoidType;

    kCheck(kSerializer_ReadType(serializer, &pixelType, &itemVersion));
    kCheck(kSerializer_ReadSize(serializer, &width)); 
    kCheck(kSerializer_ReadSize(serializer, &height)); 

    kCheck(kSerializer_Read32s(serializer, &cfa)); 
    kCheck(kSerializer_Read32s(serializer, &format)); 

    //consume optional fields
    kCheck(kSerializer_Read16u(serializer, &optional)); 
    kCheck(kSerializer_AdvanceRead(serializer, optional)); 

    isVoidType = xkPixelFormat_UseVoidPixel(format);
    allocPixelType = (isVoidType) ? kTypeOf(kVoid) : pixelType;
    allocWidth = (isVoidType) ? xkPixelFormat_Width(format, width) : width;

    kCheck(kImage_AllocateEx(image, allocPixelType, format, allocWidth, height)); 
    obj->cfa = cfa;

    if ((width*kType_Size(pixelType)) == obj->stride)
    {
        kCheck(kSerializer_ReadItems(serializer, pixelType, itemVersion, obj->pixels, width*obj->height));
    }
    else
    {
        for (kSize i = 0; i < obj->height; ++i)
        {
            void* row = kImage_RowAt(image, i);

            kCheck(kSerializer_ReadItems(serializer, pixelType, itemVersion, row, width));
        }
    }

    return kOK; 
}

kFx(kStatus) xkImage_CopyPixels(kImage image, kImage source, kObject context)
{
    kObj(kImage, image); 
    kObjN(kImage, sourceObj, source); 
    kSize i; 

    kCheckArgs((obj->width == sourceObj->width) && (obj->height == sourceObj->height) && (obj->pixelSize == sourceObj->pixelSize)); 

    if (obj->stride == sourceObj->stride)
    {
        kCheck(kAlloc_Copy(obj->dataAlloc, obj->pixels, sourceObj->dataAlloc, sourceObj->pixels, obj->stride*obj->height, context));
    }
    else if (obj->pixelSize != 0)
    {
        for (i = 0; i < obj->height; ++i)
        {
            void* row = kImage_RowAt(image, i);
            void* srcRow = kImage_RowAt(source, i);

            kCheck(kAlloc_Copy(obj->dataAlloc, row, sourceObj->dataAlloc, srcRow, obj->pixelSize*obj->width, context));
        }
    }
    else
    {
        return kERROR_UNIMPLEMENTED;
    }

    return kOK; 
}

kFx(kStatus) kImage_Allocate(kImage image, kType pixelType, kSize width, kSize height)
{
    return kImage_AllocateEx(image, pixelType, kPIXEL_FORMAT_NULL, width, height);
}

kFx(kStatus) kImage_AllocateEx(kImage image, kType pixelType, kPixelFormat format, kSize width, kSize height)
{
    kObj(kImage, image); 
    kSize newWidth = width; 
    kSize newHeight = height; 
    kType allocPixelType = kIsNull(pixelType) ? kTypeOf(kVoid) : pixelType; 
    kSize newPixelSize = kType_Size(allocPixelType);
    kSize newStrideRaw = (newPixelSize == 0) ? xkPixelFormat_Stride(format, newWidth) : newWidth * newPixelSize; 
    kSize newStride = kSize_Align(newStrideRaw, xkIMAGE_ALIGNMENT);
    kSize newSize = kMax_(obj->allocSize, newHeight*newStride); 

    if (obj->isAttached)
    {
        obj->pixels = kNULL; 
        obj->isAttached = kFALSE; 
    }

    if (newSize > obj->allocSize)
    {
        void* oldPixels = obj->pixels; 
        void* newPixels = kNULL; 

        kCheck(kAlloc_Get(obj->dataAlloc, newSize, &newPixels, obj->alignment));
        
        obj->pixels = newPixels; 
        obj->allocSize = newSize; 

        kCheck(kAlloc_Free(obj->dataAlloc, oldPixels));
    }
        
    obj->pixelType = allocPixelType; 
    obj->pixelSize = newPixelSize; 
    obj->width = newWidth; 
    obj->height = newHeight; 
    obj->stride = newStride; 
    obj->format = format;
    
    return kOK; 
}

kFx(kStatus) kImage_AttachEx(kImage image, void* pixels, kType pixelType, kPixelFormat format, kSize width, kSize height, kSize stride)
{
    kObj(kImage, image); 

    if (format >= kPIXEL_FORMAT_VOID_TYPE_START)
    {
        kCheckArgs(kIsNull(pixelType) || pixelType == kTypeOf(kVoid));
    }

    if (!obj->isAttached)
    {
        kCheck(kAlloc_FreeRef(obj->dataAlloc, &obj->pixels));
    }
    
    obj->pixelType = kIsNull(pixelType) ? kTypeOf(kVoid) : pixelType; 
    obj->pixelSize = kType_Size(obj->pixelType); 
    obj->allocSize = 0; 
    obj->pixels = pixels; 
    obj->stride = stride; 
    obj->width = width; 
    obj->height = height; 
    obj->isAttached = kTRUE; 
    obj->format = format;

    return kOK; 
}

kFx(kStatus) kImage_Attach(kImage image, void* pixels, kType pixelType, kSize width, kSize height, kSize stride)
{
    return kImage_AttachEx(image, pixels, pixelType, kPIXEL_FORMAT_NULL, width, height, stride);
}

kFx(kStatus) xkImage_Imitate(kImage image, kArray1 source)
{
    kObj(kImage, image); 

    kCheck(kImage_AllocateEx(image, kImage_PixelType(source), kImage_PixelFormat(source), kImage_Width(source), kImage_Height(source)));

    obj->cfa = kImage_Cfa(source); 

    return kOK;
}

kFx(kStatus) xkImage_Assign(kImage image, kImage source, kObject context)
{
    kObj(kImage, image); 

    kCheck(xkImage_Imitate(image, source));

    kCheck(xkImage_CopyPixels(image, source, context)); 

    return kOK;   
}

kFx(kStatus) kImage_Zero(kImage image)
{
    kObj(kImage, image); 

    kCheckArgs(!kAlloc_IsForeign(obj->dataAlloc));

    return kMemSet(obj->pixels, 0, kImage_DataSize(image)); 
}

kFx(kSize) kImage_Area(kImage image)
{
    kObj(kImage, image); 

    if (xkPixelFormat_UseVoidPixel(obj->format))
    {
        return 0;
    }

    return obj->width * obj->height; 
}

kFx(kStatus) kImage_SetPixel(kImage image, kSize x, kSize y, const void* pixel)
{
    kObj(kImage, image); 

    kAssert(!kAlloc_IsForeign(obj->dataAlloc));
    kCheckArgs((x < obj->width) && (y < obj->height)); 

    kValue_Import(obj->pixelType, kImage_DataAt(image, (kSSize)x, (kSSize)y), pixel); 

    return kOK; 
}

kFx(kStatus) kImage_Pixel(kImage image, kSize x, kSize y, void* pixel)
{
    kObj(kImage, image); 

    kAssert(!kAlloc_IsForeign(obj->dataAlloc));
    kCheckArgs((x < obj->width) && (y < obj->height)); 

    kItemCopy(pixel, kImage_DataAt(image, (kSSize)x, (kSSize)y), obj->pixelSize); 

    return kOK; 
}

kFx(kStatus) kImage_SetPixelFormat(kImage image, kPixelFormat format)
{
    kObj(kImage, image);

    obj->format = format; 

    return kOK; 
}
