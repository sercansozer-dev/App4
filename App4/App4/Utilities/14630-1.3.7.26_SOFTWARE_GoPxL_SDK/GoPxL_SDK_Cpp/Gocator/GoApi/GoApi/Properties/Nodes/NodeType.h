/**
* @file    NodeType.h
* @brief   Declares the NodeType class.
*
*   Everything in here is kind of hacky, and is done only so that Value<> nodes will autogenerate their own type string
*   for their schema. 
*
*/

#ifndef GOAPI_PROPERTIES_TYPE_H
#define GOAPI_PROPERTIES_TYPE_H

#include <string>

#include <GoApi/Exception.h>
#include <GoApi/Properties/Nodes/ValueBase.h>

namespace Go
{
namespace Properties
{
namespace Internal
{

/**
* @class   NodeType
* @brief   Represents a Node Type, which is used when generating the Schema for a Structure
*/
class NodeType : public ValueBase
{
protected:
    std::string type;
public:
    NodeType(const std::string& t)
        : type(t)
    {
        writeOnly = true;
    }
    virtual ~NodeType() = default;

    void CopyValues(const Node& other) override
    {
        auto& otherNodeType = dynamic_cast<const NodeType&>(other);

        this->Set(otherNodeType.Get());

        this->CopySchema(other);
    }

    void Set(const std::string& value)
    {
        type = value;
    }

    const std::string& Get() const
    {
        return type;
    }

    operator std::string() const
    {
        return type;
    }

    virtual void SetWriteOnly(bool isWriteOnly) override
    {
        if (!isWriteOnly)
        {
            throw Exception(kERROR_WRITE_ONLY);
        }
    }

    //Make this class non-copyable.
    NodeType(const NodeType&) = delete;
    NodeType& operator=(const NodeType&) = delete;
    //Make this class move constructable to remove the necessity for mandatory copy elision.
    NodeType(NodeType&& other) = default;

    void ReadValue(IValueReader& reader) override
    {
        throw Exception(kERROR_UNIMPLEMENTED);        
    }

    void WriteValue(IValueWriter& writer) const override
    {
        writer.WriteString(type.c_str());
    }

    void ValidateValue(IValueReader& reader) const override
    {
        return;
    }
};

}
}
} //Namespaces

#endif
