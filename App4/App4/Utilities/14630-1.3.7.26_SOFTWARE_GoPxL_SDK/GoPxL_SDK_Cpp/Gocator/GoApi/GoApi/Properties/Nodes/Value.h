/**
* @file    Properties/Nodes/Value.h
* @brief   Declares the Value class.
*
*/

#ifndef GOAPI_PROPERTIES_VALUE_H
#define GOAPI_PROPERTIES_VALUE_H

#include <string>
#include <GoApi/Properties/Nodes/ValueWrapper.h>
#include <GoApi/Properties/Nodes/ValueBase.h>
#include <GoApi/Properties/Nodes/NodeType.h>
#include <GoApi/Properties/Serializers/ValueIoDispatch.h>
#include <kApi/Data/kMath.h>

namespace Go 
{ 
namespace Properties
{

/**
* @class   Value
* @extends ValueBase
* @brief   Represents a Value in the Configuration Node.
*
* Supports the following primitive types: 
*   k8u-k64u, k8s-k64s, k32f, k64f, std::string, bool
* Supports the following std::container types, as long as the container value is a primitive type: 
*   Array Types: (Serialized Value Node will be a JSON array)
*      vector, set, multiset, unordered_set, unordered_multiset, list, forward_list, deque, array
*   Object Types: (Serialized Value Node will be a JSON object, the Key value for the map must be convertable to string)
*       map, multimap, unordered_map, unordered_multimap
*
*/

template<typename T, typename = void>
struct ValueEqFunc
{
    static constexpr const bool Eq(T val1, T val2)
    {
        return val1 == val2;
    }
};

template<typename T>
struct ValueEqFunc<T, std::enable_if_t<std::is_floating_point<T>::value>>
{
    static constexpr const bool Eq(T val1, T val2)
    {
        // Using float comparison because it should work for doubles, while the double epsilon may be too small for floats.
        // If desired, could possibly split up float and double using std::is_same<T, k64f>::value.
        return static_cast<bool>(kMath_NearlyEquals32f(static_cast<k32f>(val1), static_cast<k32f>(val2)));
    }
};

template<typename T>
class Value : public Internal::ValueBase, public Internal::ValueWrapper<T>
{
private:
    Structure* internalSchema = nullptr;
    void PerformSchemaRegistrations()
    {
        RegisterSchema("type", type);
        if (Internal::IsTypeArray<T>::value || Internal::IsTypeObject<T>::value)
        {
            internalSchema = new Structure(false);
            if (Internal::IsTypeArray<T>::value)
            {
                using ItemType = typename Internal::ArrayItemType<T>::type;

                RegisterSchema("items", *internalSchema);

                internalType.Set(Internal::TypeValueName<ItemType>::value);
                internalSchema->Register("type", internalType);
            }
        }
    }

    Value& ConditionalSet(const T& v)
    {
        if (v != Internal::ValueWrapper<T>::Get())
        {
            onChange.Notify(*this);
            Internal::ValueWrapper<T>::Set(v);
        }
        return *this;
    }

protected:
    Internal::NodeType type;
    Internal::NodeType internalType;

public:
    Value() :
        type(Internal::TypeValueName<T>::value),
        internalType("")
    {
        PerformSchemaRegistrations();
    }

    Value(const T& initialValue) :
        Internal::ValueWrapper<T>(initialValue),
        type(Internal::TypeValueName<T>::value),
        internalType("")
    {
        PerformSchemaRegistrations();
    }
   
    virtual ~Value()
    {
        if (internalSchema)
        {
            delete internalSchema;
        }
    }

    //Make this class non-copyable.
    Value(const Value&) = delete;
    Value& operator=(const Value&) = delete;
    //Make this class move constructable to remove the necessity for mandatory copy elision.
    Value(Value&& other) = default;

    void CopyValues(const Node& other) override
    {
        auto& otherValue = dynamic_cast<const Value<T>&>(other);

        this->Set(otherValue);

        this->CopySchema(other);
    }

    Value& operator=(const T& other) override
    {
        return ConditionalSet(other);
    }

    Value& Set(const T& v) override
    {
        return ConditionalSet(v);
    }

    const std::string& TypeName() const
    {
        return type.Get();
    }

    // Implementation of virtual method from IValueIo
    void ReadValue(IValueReader& reader) override
    {
        T val;
        ValueIoDispatch<T>::Read(reader, val);
        this->Set(val);
    }

    // Implementation of virtual method from IValueIo
    void WriteValue(IValueWriter& writer) const override
    {
        ValueIoDispatch<T>::Write(writer, this->GetStored());
    }

    // Implementation of virtual method from IValueIo
    void ValidateValue(IValueReader& reader) const override
    {
        T val;
        ValueIoDispatch<T>::Read(reader, val);
        // UpdateDocument calls may update unchanged values. This causes readOnly to fail validation.
        // So only validate if the new value is different.
        if (!ValueEqFunc<T>::Eq(val, Internal::ValueWrapper<T>::Get()))
        {
            CheckValidation(val);
        }
    }

   /**
    * Checks validation requirements based on the given data type, and throws warnings and exceptions
    * for any failed validation.
    * 
    * @param newValue           Generic value to be validated.
    * @param loggingEnabled     Shows log messages if true, nothing otherwise. Default to true.
    * 
    * @remarks                  Will return translated strings when returning an exception/warning.
    */
    virtual void CheckValidation(const T& newValue, const bool loggingEnabled = true) const
    {
        // GoProps::Value currently has no validation.
        return;
    }
};

}} //Namespaces

#endif
