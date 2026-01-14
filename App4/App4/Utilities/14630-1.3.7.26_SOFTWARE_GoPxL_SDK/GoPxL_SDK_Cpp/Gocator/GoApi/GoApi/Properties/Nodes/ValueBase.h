/**
* @file    Properties/Nodes/ValueBase.h
* @brief   Declares the ValueBase class.
*
*/

#ifndef GOAPI_PROPERTIES_VALUE_BASE_H
#define GOAPI_PROPERTIES_VALUE_BASE_H

#include <string>

#include <GoApi/GoApiDef.h>
#include <GoApi/Properties/Nodes/Node.h>
#include <GoApi/Properties/Serializers/ValueIo.h>

namespace Go 
{ 
namespace Properties
{
namespace Internal
{

/**
* @class   ValueBase
* @extends Node
* @brief   Represents a Base Value, exposing the Format/Parse functions for serialization
*/
class GoApiClass ValueBase : public Node, public IValueIo
{
protected:
public:
    ValueBase() = default;
    virtual ~ValueBase() = default;

    //Make this class non-copyable.
    ValueBase(const ValueBase&) = delete;
    ValueBase& operator=(const ValueBase&) = delete;
    //Make this class move constructable to remove the necessity for mandatory copy elision.
    ValueBase(ValueBase&& other) = default;

    void Accept(INodeVisitor& visitor) override
    {
        visitor.Visit(*this);
    }

    void Accept(INodeVisitorConst& visitor) const override
    {
        visitor.Visit(*this);
    }
};

}}} //Namespaces

#endif
