#ifndef GOAPI_EVENT_H
#define GOAPI_EVENT_H

#include <GoApi/Object.h>
#include <GoApi/Threads/Locker.h>
#include <functional>
#include <list>

namespace Go
{
    template <typename... Args>
    class Event
    {
    public:
        typedef std::function<void(Args...)> CallbackType;

        class CallbackId
        {
        public:
            CallbackId() : valid(false) {};

            // GOS-11409: This function marks the CallbackId as invalid, meaning it
            // cannot be used to remove an event handler.
            // Typical use case for this API is to invalidate a CallbackId
            // for an event that is destructed (either directly or indirectly
            // as part of the destruction of a containing object) without
            // the owner of the event handler being notified.
            void Invalidate()
            {
                valid = false;
            }

            // GOS-12347: provide a way to determine if a callback id variable is valid or
            // not. If not valid, the callback id must not be used.
            bool Valid()
            {
                return valid;
            }

        private:
            friend class Event<Args...>;
            CallbackId(typename std::list<CallbackType>::iterator i)
                : iter(i), valid(true)
            {}

            typename std::list<CallbackType>::iterator iter;
            bool valid;
        };

        CallbackId AddListener(CallbackType callback)
        {
            if (callback)
            {
                callbackList.push_back(callback);
                return CallbackId(--callbackList.end());
            }

            return CallbackId();
        }

        void RemoveListener(CallbackId& id)
        {
            if (id.valid)
            {
                // GOS-11409: The CallbackId's iterator is no longer valid after the pointed-to entry
                // in the callbackList has been removed. Without a valid callbackList iterator
                // value, the CallbackId is not valid anymore.
                id.Invalidate();

                callbackList.erase(id.iter);
            }
        }

        void Notify(Args... args)
        {
            for (auto& callback : callbackList)
            {
                callback(args...);
            }
        }

        size_t Count()
        {
            return callbackList.size();
        }

        void ClearListeners()
        {
            callbackList.clear();
        }

    private:
        std::list<CallbackType> callbackList;
    };

    // This version of Event adds locking to certain critical sections.
    // This may impact speed, but it avoids nasty race conditions when listeners are removed and notified at the same time.
    template <typename... Args>
    class LockedEvent : public Event<Args...>
    {
    public:
        LockedEvent()
        {
            GoTest(kLock_Construct(&eventLock, kAlloc_App()));
        }

        typename Event<Args...>::CallbackId AddListener(typename Event<Args...>::CallbackType callback)
        {
            typename Event<Args...>::CallbackId cbId;

            GoWithLock(Go::Locker(eventLock))
            {
                cbId = Event<Args...>::AddListener(callback);
            }

            return cbId;
        }

        void RemoveListener(typename Event<Args...>::CallbackId& id)
        {
            GoWithLock(Go::Locker(eventLock))
            {
                Event<Args...>::RemoveListener(id);
            }
        }

        void Notify(Args... args)
        {
            GoWithLock(Go::Locker(eventLock))
            {
                Event<Args...>::Notify(args...);
            }
        }

        size_t Count()
        {
            size_t count = 0;

            GoWithLock(Go::Locker(eventLock))
            {
                count = Event<Args...>::Count();
            }

            return count;
        }

        void ClearListeners()
        {
            GoWithLock(Go::Locker(eventLock))
            {
                Event<Args...>::ClearListeners();
            }
        }

    private:
        Go::Object<kLock> eventLock;
    };

}

#endif
