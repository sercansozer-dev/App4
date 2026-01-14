/**
* @file    Structure.h
* @brief   Declares the Structure class.
*
*/

#ifndef GOAPI_PROPERTIES_STRUCTURE_H
#define GOAPI_PROPERTIES_STRUCTURE_H

#include <map>
#include <string>
#include <stack>

#include <GoApi/Properties/Nodes/Node.h>
#include <GoApi/Properties/Nodes/NodeType.h>
#include <GoApi/Properties/Nodes/TypeInfo.h>

namespace Go
{
namespace Properties
{

/**
* @class   Structure
* @extends Node
*/
class GoApiClass Structure : public Node
{
private:
    Structure* propertiesSchema = nullptr;
protected:
    using container_t = std::map<std::string, Node&>;
    container_t children;
    Internal::NodeType type;
public:
    //This iterator is exposed by the underlying container iterator (default std::map)
    //as such it only iterates throught the immediate children of the Structure
    using iterator_t = container_t::iterator;
    using const_iterator_t = container_t::const_iterator;

    Structure(bool withSchema = true);

    virtual ~Structure();

    //Make this class non-copyable.
    Structure(const Structure&) = delete;
    Structure& operator=(const Structure&) = delete;
    //Make this class move constructable to remove the necessity for mandatory copy elision.
    Structure(Structure&& other) = default;

    // Node class overrides.
    void CheckValidationAndCollect(std::vector<std::string>& invalidParamsList, size_t invalidParamsMax = 0) override;
    void CopyValues(const Node& other) override;

    /**
    * Register a child node. If the child id already exists, an exception is thrown.
    *
    * @param    id      Unique Id of child
    * @param    node    Reference to the node to be registered
    *
    * @return   Reference to the child node with the given id.
    */
    Node& Register(const std::string& id, Node &node);

    /**
    * Register a child node. If throwIfExists is true and child id already exists, an exception is thrown.
    * If throwIfExists is false and child id already exists, this function changes nothing.
    *
    * @remarks This is useful if there's a valid chance a node will be registered 2+ times.
    *
    * @param    id              Unique Id of child
    * @param    node            Reference to the node to be registered
    * @param    throwIfExists   Whether to throw or silently pass if child id already exists.
    *
    * @return   Reference to the child node with the given id.
    */
    Node& Register(const std::string& id, Node &node, const bool throwIfExists);

    /** 
    * Checks if a child node is already registered
    *
    * @param    id      Unique Id of child
    * @return   True if registered false otherwise.
    */
    bool Registered(const std::string& id) const;

    /** 
    * Deregister a child node.
    *
    * @param    id      Unique Id of child
    */
    void Deregister(const std::string& id);

    /** 
    * Deregisters all child nodes.
    */
    void ClearRegistered();

    /**
    * Gets the number of children of this Node.
    *
    * @return               Number of children registered to the node
    */
    size_t Count() const;

    /**
    * Gets the child node at the specified index.
    *
    * @param    index       Index of the child to be fetched
    * @return               Child Node if found
    */
    Node& At(size_t index);

    /**
    * Gets the child node with the specified id.
    *
    * Throws std::runtime_error exception on child not found
    *
    * @param    id          Id of the child to be fetched
    * @return               Child Node if found
    */
    Node& At(const std::string &id);

    /**
    * Gets the child node with the specified id.
    * Similar to At() above but for "const" objects.
    *
    * Throws std::runtime_error exception on child not found
    *
    * @param    id          Id of the child to be fetched
    * @return               Child Node if found
    */
    const Node& At(const std::string &id) const;

    iterator_t begin();
    iterator_t end();

    const_iterator_t begin() const;
    const_iterator_t end() const;

    class GoApiClass DepthFirstIterator
    {
    private:
        std::stack<Node*> stack;
    public:
        DepthFirstIterator() = default;
        DepthFirstIterator(Node* root);
        DepthFirstIterator& operator++();
        bool operator!=(const DepthFirstIterator &other) const;
        Node*& operator*();
    };

    /**
    * @class   DFIteratorProvider
    * @brief   Provider for the DepthFirstIterator.
    */
    class GoApiClass DFIteratorProvider
    {
    private:
        Structure& root;
    public:
        DFIteratorProvider(Structure& c);
        virtual ~DFIteratorProvider() = default;
        DepthFirstIterator begin() const;
        DepthFirstIterator end() const;
    };

    /**
    * Perform depth first traversal on the Children of the structure.
    *
    * @return               BFIteratorProvider
    */
    DFIteratorProvider DepthFirstTraversal();

    void Accept(INodeVisitor& visitor) override;
    void Accept(INodeVisitorConst& visitor) const override;
};

}
} //Namespaces

#endif
