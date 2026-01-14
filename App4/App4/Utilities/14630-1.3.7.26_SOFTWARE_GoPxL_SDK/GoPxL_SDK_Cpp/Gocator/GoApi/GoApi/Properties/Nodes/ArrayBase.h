#ifndef GOAPI_PROPERTIES_ARRAY_BASE_H
#define GOAPI_PROPERTIES_ARRAY_BASE_H

#include <vector>
#include <memory>

#include <GoApi/GoApiDef.h>
#include <GoApi/Properties/Nodes/Node.h>
#include <GoApi/Properties/Nodes/Structure.h>

namespace Go
{
namespace Properties
{
namespace Internal
{

class GoApiClass ArrayBase : public Node
{
private:
    Internal::NodeType type;
protected:
    using value_t = std::unique_ptr<Structure>;
    using container_t = std::vector<value_t>;
    container_t children;
public:
    ArrayBase();

    virtual ~ArrayBase();

    //Make this class non-copyable.
    ArrayBase(const ArrayBase&) = delete;
    ArrayBase& operator=(const ArrayBase&) = delete;
    //Make this class move constructable to remove the necessity for mandatory copy elision.
    ArrayBase(ArrayBase&& other) = default;

    using iterator_t = container_t::iterator;
    iterator_t begin();
    iterator_t end();

    Structure* At(size_t index);
    const Structure* At(size_t index) const;

    size_t Count() const;

    //Invalidates references/pointers to children and iterators
    virtual void Resize(size_t count) = 0;
    virtual void Remove(size_t index) = 0;

    virtual void Clear() = 0;

    void Accept(INodeVisitor& visitor) override;
    void Accept(INodeVisitorConst& visitor) const override;
};

class GoApiClass ReferenceArray final : public Node
{
private:
    std::vector<Structure*> children;
public:
    Structure* At(size_t index);
    const Structure* At(size_t index) const;

    size_t Count() const;

    void Remove(size_t index);
    void Clear();
    void Pop();

    void Add(Structure* str);
    void Insert(Structure* str, size_t index);

    void Accept(INodeVisitor& visitor) override;
    void Accept(INodeVisitorConst& visitor) const override;

    void CopyValues(const Node& other) override;
};
}}} //Namespaces

#endif