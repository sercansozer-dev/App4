//This file is only named PropertyNode.cpp until the Configuration/Node.cpp file can be deleted
//Two files with the same name results in compiler output collisions (two Node.o files, one gets overwritten)

#include <GoApi/Properties/Nodes/Node.h>
#include <GoApi/Properties/Nodes/Structure.h>
#include <GoApi/Properties/Nodes/ArrayBase.h>

namespace Go
{
namespace Properties
{

Node::~Node()
{
    if (schema)
    {
        delete schema;
    }
}

///If the node is write only the serializer should not attempt to read it.
void Node::SetWriteOnly(bool isWriteOnly)
{
    writeOnly = isWriteOnly;
}

///If the node is write only the serializer should not attempt to read it.
bool Node::WriteOnly() const
{
    return writeOnly;
}

void Node::SetIsRead(bool isRead)
{
    this->isRead = isRead;
}

bool Node::IsRead() const
{
    return isRead;
}

Node& Node::RegisterSchema(const std::string& id, Node& schemaNode)
{
    return RegisterSchema(id, schemaNode, true);
}

Node& Node::RegisterSchema(const std::string& id, Node& schemaNode, const bool throwIfExists)
{
    if (!schema)
    {
        schema = new Structure(false);
    }
    onChange.Notify(*this);
    return schema->Register(id, schemaNode, throwIfExists);
}

// GOS-912: Advanced parameter visibility control
// Added to support the ability to enable/disable a visibility property and remove it from the schema
void Node::DeregisterSchema(const std::string& id)
{
    if (schema)
    {
        onChange.Notify(*this);
        schema->Deregister(id);
    }
}

Structure* Node::Schema() const
{
    return schema;
}

void Node::CopyValues(const Node& other)
{
    throw Go::Exception(kERROR_UNIMPLEMENTED);
}

void Node::CopySchema(const Node& other)
{
    Structure* otherSchema = other.Schema();

    if (otherSchema)
    {
        this->schema->CopyValues(*otherSchema);
    }
}

}
} // namespaces
