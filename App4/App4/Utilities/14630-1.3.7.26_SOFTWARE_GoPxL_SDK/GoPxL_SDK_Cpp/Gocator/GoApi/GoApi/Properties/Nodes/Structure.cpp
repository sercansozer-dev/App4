#include <GoApi/Properties/Nodes/Structure.h>
#include <GoApi/Exception.h>

namespace Go
{
namespace Properties
{

Structure::Structure(bool withSchema) :
    type("object")
{
    if (withSchema)
    {
        RegisterSchema("type", type);
        propertiesSchema = new Structure(false);
        RegisterSchema("properties", *propertiesSchema);
    }
}

Structure::~Structure()
{
    if (propertiesSchema)
    {
        delete propertiesSchema;
    }
}

void Structure::CheckValidationAndCollect(std::vector<std::string>& invalidParamsList, size_t invalidParamsMax)
{
    for (iterator_t it = this->begin(); it != this->end(); it++)
    {
        // Leave validation early if the requested amount of problems were retrieved.
        if (invalidParamsMax > 0 && invalidParamsList.size() >= invalidParamsMax)
        {
            break;
        }

        it->second.CheckValidationAndCollect(invalidParamsList);
    }
}

void Structure::CopyValues(const Node& other)
{
    auto& otherStruct = dynamic_cast<const Structure&>(other);
    std::vector<std::string> toBeRemoved;

    // GOS-4753: Made changes to ensure a child is deregistered if 'other' does not contain it.
    // This fixes an exception sometimes triggered when copying GoParams Nodes as their schemas may have different attributes enabled.
    for (iterator_t it = this->begin(); it != this->end(); it++)
    {
        // For items in this but not in other, deregister/remove them.
        if (!otherStruct.Registered(it->first))
        {
            toBeRemoved.push_back(it->first);
            GoLogWarn("CopyValues: Removed child '%s' from destination as source doesn't contain it.", it->first.c_str());
        }
        // If the item exists in both, copy the values of other. Assume the nodes are compatible.
        else
        {
            it->second.CopyValues(otherStruct.At(it->first));
        }
    }

    for (std::string& id : toBeRemoved)
    {
        // Deregister children after the loop to avoid invalidating the iterator.
        this->Deregister(id);
    }

    // For items in other but not in this, simply skip.
    // There's no good way to copy the child without a compatible Node to copy into.

    this->CopySchema(other);
}

Node& Structure::Register(const std::string& id, Node &node)
{
    return Register(id, node, true);
}

Node& Structure::Register(const std::string& id, Node &node, const bool throwIfExists)
{
    if (!children.count(id))
    {
        onChange.Notify(*this);
        children.emplace(id, node);
        if (propertiesSchema && node.Schema())
        {
            propertiesSchema->Register(id, *node.Schema());
        }
    }
    else
    {
        if (throwIfExists)
        {
            GoThrowMsg(kERROR_ALREADY_EXISTS, "'%s' already registered.", id.c_str());
        }
        return children.at(id);
    }

    return node;
}

bool Structure::Registered(const std::string& id) const
{
    return (children.count(id) > 0);
}

void Structure::Deregister(const std::string& id)
{
    if (children.count(id))
    {
        onChange.Notify(*this);
        if (propertiesSchema && children.at(id).Schema())
        {
            propertiesSchema->Deregister(id);
        }
        children.erase(id);
    }
}

void Structure::ClearRegistered()
{
    if (children.size())
    {
        for (auto mapIter = children.begin(); mapIter != children.end();)
        {
            onChange.Notify(*this);
            if (propertiesSchema && mapIter->second.Schema())
            {
                propertiesSchema->Deregister(mapIter->first);
            }
            mapIter = children.erase(mapIter);        
        }
    }
}

size_t Structure::Count() const
{
    return children.size();
}

Node& Structure::At(size_t index)
{
    if (index >= this->Count())
    {
        GoRethrowStatus(kERROR_PARAMETER, "Index %lu > %lu.", (k32u)index, (k32u)this->Count());
    }

    auto it = children.begin();
    std::advance(it, index);

    return it->second;
}

Node& Structure::At(const std::string &id)
{
    try
    {
        return children.at(id);
    }
    catch (const std::exception&)
    {
        GoRethrowStatus(kERROR_NOT_FOUND, "Child %s not found.", id.c_str());
    }
}

const Node& Structure::At(const std::string &id) const
{
    try
    {
        return children.at(id);
    }
    catch (const std::exception&)
    {
        GoRethrowStatus(kERROR_NOT_FOUND, "Child %s not found.", id.c_str());
    }
}

Structure::iterator_t Structure::begin()
{
    return children.begin();
}

Structure::iterator_t Structure::end()
{
    return children.end();
}

Structure::const_iterator_t Structure::begin() const
{
    return children.begin();
}

Structure::const_iterator_t Structure::end() const
{
    return children.end();
}

Structure::DepthFirstIterator::DepthFirstIterator(Node* root)
{
    stack.push(root);
}

Structure::DepthFirstIterator& Structure::DepthFirstIterator::operator++()
{
    if (stack.size() > 0)
    {
        auto currentNode = stack.top();
        stack.pop();

        auto listNode = dynamic_cast<Structure*>(currentNode);
        if (listNode)
        {
            for (int i = (int)listNode->Count() - 1; i >= 0; --i)
            {
                stack.push(&listNode->At(i));
            }
        }
    }
    else
    {
        GoThrowMsg(kERROR_STATE, "Invalid iterator increment");
    }
    return *this;
}

bool Structure::DepthFirstIterator::operator!=(const Structure::DepthFirstIterator &other) const
{
    return stack.size() != other.stack.size();
}

Node*& Structure::DepthFirstIterator::operator*()
{
    return stack.top();
}

Structure::DFIteratorProvider::DFIteratorProvider(Structure& c)
    : root(c)
{
}

Structure::DepthFirstIterator Structure::DFIteratorProvider::begin() const
{
    return Structure::DepthFirstIterator(&root);
}

Structure::DepthFirstIterator Structure::DFIteratorProvider::end() const
{
    return DepthFirstIterator();
}

Structure::DFIteratorProvider Structure::DepthFirstTraversal()
{
    return DFIteratorProvider(*this);
}

void Structure::Accept(INodeVisitor& visitor)
{
    visitor.Visit(*this);
}

void Structure::Accept(INodeVisitorConst& visitor) const
{
    visitor.Visit(*this);
}

}
} // namespaces
