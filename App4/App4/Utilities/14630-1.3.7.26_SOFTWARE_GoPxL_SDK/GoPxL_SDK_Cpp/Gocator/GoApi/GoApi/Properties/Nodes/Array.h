#ifndef GOAPI_PROPERTIES_ARRAY_H
#define GOAPI_PROPERTIES_ARRAY_H

#include <functional>
#include <GoApi/Properties/Nodes/ArrayBase.h>

namespace Go
{
namespace Properties
{

//Any class used as a template parameter for an Array must have a default constructor
//ie. a constructor with no parameters "T(void)"

template<typename T>
class Array : public Internal::ArrayBase
{
private:
    // This is where the pointers to each array element's schema are kept.
    Internal::ReferenceArray itemsSchema;
protected:
    static_assert(std::is_base_of<Structure, T>::value, "T Must be a descendant type of Structure");
    using pointer_t = std::unique_ptr<T>;
public:
    Array()
    {
        RegisterSchema("items", itemsSchema);
    }

    ~Array() = default;

    T& At(size_t index)
    {
        if (index >= children.size())
        {
            throw Exception(kERROR_NOT_FOUND);
        }
        return *dynamic_cast<T*>(children.at(index).get());
    }

    T& operator[](size_t index)
    {
        return At(index);
    }

    const T& At(size_t index) const
    {
        if (index >= children.size())
        {
            throw Exception(kERROR_NOT_FOUND);
        }
        return *dynamic_cast<T*>(children.at(index).get());
    }

    const T& operator[](size_t index) const
    {
        return At(index);
    }

    void CopyValues(const Node& other) override
    {
        auto& otherArray = dynamic_cast<const Array&>(other);

        this->Resize(otherArray.Count());

        for (size_t i = 0; i < this->Count(); i++)
        {
            this->At(i).CopyValues(otherArray.At(i));
        }
            
        this->CopySchema(other);
    }

    //Invalidates references/pointers to children and iterators
    template <typename... Args>
    T& Emplace(Args&&... args)
    {
        auto child = std::make_unique<T>(std::forward<Args>(args)...);
        itemsSchema.Add(child->Schema());
        children.push_back(std::move(child));
        onChange.Notify(*this);
        return *dynamic_cast<T*>(children.back().get());
    }

    //Invalidates references/pointers to children and iterators
    template <typename... Args>
    void Reserve(size_t count, Args&&... args)
    {
        onChange.Notify(*this);
        for (size_t i = 0; i < count; i++)
        {
            Emplace(std::forward<Args>(args)...);
        }
    }

    //Invalidates references/pointers to children and iterators
    virtual void Resize(size_t count) override
    {
        if (count == children.size())
        {
            return;
        }
        else if (count > children.size())
        {
            onChange.Notify(*this);
            for (size_t i = children.size(); i < count; i++)
            {
                Emplace();
            }
        }
        else if (count < children.size())
        {
            onChange.Notify(*this);
            children.resize(count);
            while (children.size() < itemsSchema.Count())
            {
                itemsSchema.Pop();
            }
        }
    }

    virtual void Clear() override
    {
        onChange.Notify(*this);
        children.clear();
        itemsSchema.Clear();
    }

    virtual void Remove(size_t index) override
    {
        if (index >= children.size())
        {
            throw Exception(kERROR_NOT_FOUND);
        }
        onChange.Notify(*this);
        auto it = children.begin() + index;
        children.erase(it);
        itemsSchema.Remove(index);
    }

    //Invalidates references/pointers to children and iterators
    T& Add(pointer_t newChild)
    {
        itemsSchema.Add(newChild->Schema());
        children.push_back(std::move(newChild));
        onChange.Notify(*this);
        return *dynamic_cast<T*>(children.back().get());
    }

    //Invalidates references/pointers to children and iterators
    T& Insert(pointer_t newChild, size_t index)
    {
        if (index > children.size())
        {
            throw Exception(kERROR_NOT_FOUND);
        }
        onChange.Notify(*this);
        itemsSchema.Insert(newChild->Schema(), index);
        auto it = children.begin() + index;
        auto ret = children.insert(it, std::move(newChild));
        return *dynamic_cast<T*>(ret->get());
    }
private:
    // The following factory-api methods are experimental, and are marked as private to prevent use
    // The purpose of this API is to support arrays of polymorphic types, which as of May 23rd 2019
    // is not required in by the design of the studio protocol.

    //Unanswered questions:
    //  -If we have a parent Structure A, and two subclasses of it B, and C, and an Array<A> of [B,C,C]
    //   say we deserialize an array of [B,B,C], how do we detect the type mismatch at index 1, and
    //   how do we handle it? Handling is easy, remove the improperly typed object and replace it with
    //   a new structure of the correct type. Detecting the problem is difficult, the user would probably
    //   have to provide some sort of comparison function, which is ugly.
    //      Perhaps for every index we could call the factory method to create a new instance, then compare the types
    //      if the same type of object was created, all good, if not we have detected improper typing and replace the
    //      existing object. This could be expensive if the factory method is expensive.
    //      Also, not sure if typeid() operator is well supported for us?

    //protected:
    using factoryMethod_t = std::function<std::unique_ptr<T>()>;

    factoryMethod_t DefaultFactory()
    {
        auto func = []() { return std::move(std::make_unique<T>()); };
        return func;
    }

    factoryMethod_t factory = DefaultFactory();

    //public:
    void SetFactory(factoryMethod_t fact)
    {
        if (!fact)
        {
            factory = DefaultFactory();
        }
        else
        {
            factory = fact;
        }
    }

    T& Create()
    {
        auto child = factory();
        return Add(std::move(child));
    }
};

}
}

#endif