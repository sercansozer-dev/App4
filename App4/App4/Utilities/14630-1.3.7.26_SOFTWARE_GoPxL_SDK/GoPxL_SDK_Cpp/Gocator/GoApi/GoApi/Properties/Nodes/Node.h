/**
* @file     Properties/Nodes/Node.h
* @brief    Declares the Node class. Node's are the building blocks of the "Model Tree", the
*           internal representation of the Model Data
*/

#ifndef GOAPI_PROPERTIES_NODE_H
#define GOAPI_PROPERTIES_NODE_H

#include <string>
#include <GoApi/GoApiDef.h>
#include <GoApi/Event.h>

namespace Go
{
namespace Properties
{

class INodeVisitor
{
public:
    ~INodeVisitor() {}

    virtual void Visit(Structure& node) = 0;
    virtual void Visit(Internal::ArrayBase& node) = 0;
    virtual void Visit(Internal::ReferenceArray& node) = 0;
    virtual void Visit(Internal::ValueBase& node) = 0;
};

class INodeVisitorConst
{
public:
    ~INodeVisitorConst() {}

    virtual void Visit(const Structure& node) = 0;
    virtual void Visit(const Internal::ArrayBase& node) = 0;
    virtual void Visit(const Internal::ReferenceArray& node) = 0;
    virtual void Visit(const Internal::ValueBase& node) = 0;
};

/**
* @class   Node
*/
class GoApiClass Node
{
private:
    Structure* schema = nullptr;
protected:
    bool writeOnly = false; //Is this node write only? Or does it need to be read back into the system as well?
    bool isRead = false; //Has the node been deserialized 

    ///Copy schema from the passed node.
    void CopySchema(const Node& other);

    Event<Node&> onChange; //Event that fires whenever the node is changed
public:
    //Constructors
    //========================================

    /**
    * Default constructor for the Node Class.
    *
    */
    Node() = default;

    virtual ~Node();
    //Make this class non-copyable.
    Node(const Node&) = delete;
    Node& operator=(const Node&) = delete;
    //Make this class move constructable to remove the necessity for mandatory copy elision.
    Node(Node&& other) = default;

    /**
     * Traverse through node and check validation on all params, adding
     * failed validation exceptions to invalidParamsList.
     * 
     * @param invalidParamsList     List of translated messages generated for invalid parameters.
     * @param invalidParamsMax      Max amount of problems to find, 0 finds all.
     */
    virtual void CheckValidationAndCollect(std::vector<std::string>& invalidParamsList, size_t invalidParamsMax = 0) {};

    ///Copy corresponding node values from the passed node.
    // This function is better suited as a protected function but won't work because
    // protected member functions can only called from inside base class/derived class
    // as discussed in https://stackoverflow.com/questions/477829/cannot-call-base-class-protected-functions.
    virtual void CopyValues(const Node& other);

    ///If the node is write only the serializer should not attempt to read it.
    virtual void SetWriteOnly(bool isWriteOnly);

    ///If the node is write only the serializer should not attempt to read it.
    bool WriteOnly() const;

    void SetIsRead(bool isRead);
    bool IsRead() const;

    /**
     * Register a value to the existing schema, or create a new one if no schema exists.
     * Throws an exception if the value is already registered.
     *
     * @param id         string id of the value to register
     * @param schemaNode node to register
     * 
     * @return           A Node object
     */
    Node& RegisterSchema(const std::string& id, Node& schemaNode);

    /**
     * Register a value to the existing schema, or create a new one if no schema exists.
     * Allows for specifying whether an exception should be thrown if the value is already registered.
     * 
     * @param id            string id of the value to register
     * @param schemaNode    node to register
     * @param throwIfExists if true, throw an exception if the value is already registered
     * 
     * @return           A Node object
     */
    Node& RegisterSchema(const std::string& id, Node& schemaNode, const bool throwIfExists);

    /**
     * Deregister a value from the schema.
     *
     * @param id         string id of the value to remove
     */
    void DeregisterSchema(const std::string& id);
    Structure* Schema() const;

    virtual void Accept(INodeVisitor& visitor) = 0;
    virtual void Accept(INodeVisitorConst& visitor) const = 0;
};

}
} //Namespaces

#endif