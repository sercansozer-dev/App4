/**\file    MsgQueue.h
 * GoApi MsgQeueue class
 *
 * A C++ implementation of kMsgQueue container in Firesync
 * This is to allow us to work with a thread safe queuing system with C++ objects
 */

#ifndef GO_MSGQUEUE_H
#define GO_MSGQUEUE_H

#include <memory>
#include <functional>
#include <GoApi/GoApiDef.h>
#include <GoApi/Exception.h>
#include <GoApi/Object.h>
#include <GoApi/PointerWrapper/PointerWrapper.h>
#include <kApi/Threads/kMsgQueue.h>

namespace Go
{

/**
 * A C++ wrapper of kMsgQueue to allow for use with C++ objects.
 * This implementation stores unique ptrs of objects templated with,
 * and will only allow for moving of objects and no copying whatsoever.
 */
template <typename T>
class MsgQueue
{
public:
    /**
     * Constructor
     */
    MsgQueue()
    {
        GoTest(kMsgQueue_Construct(msgQueue.Ref(), kTypeOf(PointerWrapper), kAlloc_App()));
        GoTest(kMsgQueue_SetDropHandler(msgQueue, OnQueueDrop, this));
    }

    ~MsgQueue()
    {
        // GOS-6893: Discovered a memory leak caused by not clearing the queue upon destruction.
        Clear();
    }

    /**
     * Adds an item into the queue.
     *
     * @param item              unique_ptr to the item added into the queue.
     * @param option            kMsgQueueItemOption to state whether item is critical or not,
     *                          Defaults to kMSG_QUEUE_ITEM_OPTION_NULL
     *                          Available options are :
     *                              kMSG_QUEUE_ITEM_OPTION_NULL
     *                              kMSG_QUEUE_ITEM_OPTION_CRITICAL
     * @param size              Estimated size of the object, defaults to 0.
     *                          Size should be provided if users want to leverage
     *                          the dropping mechanism of kMsgQueue.
     * 
     * @remarks                  If an item is added to the queue that causes its max item count
     *                           or max size to be exceeded, it may prune older item(s) in the 
     *                           queue to fit the new item possibly affecting the queue's item count.
     *                           See kMsgQueue class for pruning details.
     */
    void Add(std::unique_ptr<T> item, kMsgQueueItemOption option = kMSG_QUEUE_ITEM_OPTION_NULL, kSize size = 0)
    {
        // Construct a PointerWrapper object to hold the C++ object.
        // Release the pointer from the unique ptr
        // and give it to the pointer wrapper.
        kPointer itemPtr = (kPointer)item.release();
        Go::Object<PointerWrapper> ptr;
        GoTest(PointerWrapper_Construct(ptr.Ref(), itemPtr, size, kAlloc_App()));
        GoTest(kObject_Share(ptr));

        GoTest(kMsgQueue_AddEx(msgQueue, &ptr, option));
    }

    /**
     * Removes the first item from the queue.
     *
     * @param item                   reference to the smart pointer to the first item in the queue.
     * @param timeout                A timeout value to wait before returning.
     * @return                       status of the operation.
     */
    kStatus Remove(std::unique_ptr<T>& item, k64u timeout)
    {
        Go::Object<PointerWrapper> ptr;
        kStatus status = kMsgQueue_RemoveT(msgQueue, &ptr, timeout);

        if (status != kOK)
        {
            return status;
        }

        kPointer objPtr = PointerWrapper_Pointer(ptr);
        item.reset((T*)objPtr);

        return kOK;
    }

    /**
     * Clears the queue.
     */
    void Clear()
    {
        // Specifically clear the queue using the provided drop handler
        // to properly delete memory to the pointer.
        GoTest(kMsgQueue_PurgeEx(msgQueue, kMSG_QUEUE_PURGE_OPTION_USE_HANDLER));
    }

    /**
     * Gets the size of the queue (number of items).
     *
     * @return                      size of the queue.
     */
    kSize Count() const
    {
        return kMsgQueue_Count(msgQueue);
    }

    /**
     * Gets the maximum count of items the queue can handle.
     */
    kSize MaxCount() const
    {
        return kMsgQueue_MaxCount(msgQueue);
    }

    /**
     * Sets the maximum count of items the queue can handle.
     *
     * @param count                 max count for the queue to set.
     */
    void SetMaxCount(kSize count)
    {
        GoTest(kMsgQueue_SetMaxCount(msgQueue, count));
    }

    /**
     * Gets the maximum size the queue can handle.
     */
    kSize MaxSize() const
    {
        return kMsgQueue_MaxSize(msgQueue);
    }

    /**
     * Sets the maximum size the queue can handle.
     *
     * @param size                 max size for the queue to set.
     */
    void SetMaxSize(kSize size)
    {
        GoTest(kMsgQueue_SetMaxSize(msgQueue, size));
    }

    /**
     * Gets the number of dropped items from the queue
     */
    k64u DropCount() const
    {
        return kMsgQueue_DropCount(msgQueue);
    }

    /**
     * Sets the drop handler to be used instead of the default one.
     */
    void SetDropHandler(std::function<void(T*)> dropHandler)
    {
        m_dropHandler = dropHandler;
    }

private:
    Go::Object<kMsgQueue> msgQueue;
    std::function<void(T*)> m_dropHandler;

    static kStatus kCall OnQueueDrop(kPointer receiver, kMsgQueue queue, kMsgQueueDropArgs* args)
    {
        auto obj = static_cast<MsgQueue*>(receiver);

        // Deletion of pointer to an object must match original type.
        Go::Object<PointerWrapper> ptr(kPointer_ReadAs(args->item, PointerWrapper));
        T* objPtr = (T*)PointerWrapper_Pointer(ptr);

        // GOS-11507 - Before cleaning up the object, see if the caller
        // wants to do any other cleanup first.
        if (obj->m_dropHandler)
        {
            obj->m_dropHandler(objPtr);
        }

        delete objPtr;

        return kOK;
    }
};

} // namespace

#endif