#include <GoApi/Properties/Nodes/ArrayBase.h>

#include <GoApi/Exception.h>

namespace Go
{
namespace Properties
{
namespace Internal
{

ArrayBase::ArrayBase() :
    type("array")
{
    RegisterSchema("type", type);
}

ArrayBase::~ArrayBase()
{
}

ArrayBase::iterator_t ArrayBase::begin()
{
    return children.begin();
}
ArrayBase::iterator_t ArrayBase::end()
{
    return children.end();
}

Structure* ArrayBase::At(size_t index)
{
    if (index >= children.size())
    {
        throw Exception(kERROR_NOT_FOUND);
    }
    return children[index].get();
}

const Structure* ArrayBase::At(size_t index) const
{
    if (index >= children.size())
    {
        throw Exception(kERROR_NOT_FOUND);
    }
    return children[index].get();
}

size_t ArrayBase::Count() const
{
    return children.size();
}

void ArrayBase::Accept(INodeVisitor& visitor)
{
    visitor.Visit(*this);
}

void ArrayBase::Accept(INodeVisitorConst& visitor) const
{
    visitor.Visit(*this);
}

//Reference Array
///////////////////////////////////////////
Structure* ReferenceArray::At(size_t index)
{
    if (index >= children.size())
    {
        throw Exception(kERROR_NOT_FOUND);
    }
    return children[index];
}

const Structure* ReferenceArray::At(size_t index) const
{
    if (index >= children.size())
    {
        throw Exception(kERROR_NOT_FOUND);
    }
    return children[index];
}

size_t ReferenceArray::Count() const
{
    return children.size();
}

void ReferenceArray::Remove(size_t index)
{
    if (index >= children.size())
    {
        throw Exception(kERROR_NOT_FOUND);
    }
    onChange.Notify(*this);
    auto it = children.begin() + index;
    children.erase(it);
}

void ReferenceArray::Clear()
{
    children.clear();
}

void ReferenceArray::Pop()
{
    children.pop_back();
}

void ReferenceArray::Add(Structure* str)
{
    children.push_back(str);
    onChange.Notify(*this);
}
void ReferenceArray::Insert(Structure* str, size_t index)
{
    if (index >= children.size())
    {
        throw Exception(kERROR_NOT_FOUND);
    }
    auto it = children.begin() + index;
    children.insert(it, str);
    onChange.Notify(*this);
}

void ReferenceArray::Accept(INodeVisitor& visitor)
{
    visitor.Visit(*this);
}

void ReferenceArray::Accept(INodeVisitorConst& visitor) const
{
    visitor.Visit(*this);
}

void ReferenceArray::CopyValues(const Node& other)
{
    // This class stores an array of pointers which is a special case
    // and doesn't need to be copied when copying values of Node-based objects.
    // But it needs to be declared to prevent calling of the base class's virtual
    // function which would trigger an exception.
}

}}}

