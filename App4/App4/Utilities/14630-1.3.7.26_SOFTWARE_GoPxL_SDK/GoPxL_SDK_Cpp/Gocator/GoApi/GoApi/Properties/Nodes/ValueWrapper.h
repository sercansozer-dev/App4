/**
* @file    ValueWrapper.h
* @brief   Declares the ValueWrapper Class.
*
*/
#ifndef GOAPI_PROPERTIES_VALUE_WRAPPER_H
#define GOAPI_PROPERTIES_VALUE_WRAPPER_H

namespace Go
{
namespace Properties
{
namespace Internal
{

/**
 * The ValueWrapper class is a simple wrapper around a value.
 *
 * By wrapping around a value, this class provides features such as change
 * detection. A value can either primitive values like int, or objects like
 * std::vector.
 *
 * *Non-Copyable*          
 */
template <typename T>
class ValueWrapper
{
private:
    T value{};
public:
    virtual ValueWrapper& Set(const T& v)
    {
        value = v;
        return *this;
    }

    virtual ValueWrapper& operator=(const T& other)
    {
        Set(other);
        return *this;
    }

    /**
     * Returns a const reference to the value for typical use.
     *
     * It is acceptable for a derived class to override this method, to implement
     * customized behavior as needed, e.g. to redirect the value from elsewhere.
     * If the actually stored value is needed, use @ref GetStored().
     *
     * @return              Reference to the value.
     *
     * @remarks
     * Do NOT create a getter that returns a non-const reference, as this would
     * bypass the change detection mechanism.
     */
    virtual const T& Get() const
    {
        // Return the stored value by default.
        return GetStored();
    }

    /**
     * Returns a const reference to the stored value.
     *
     * This method always returns the value stored in this object, even if
     * @ref Get() has been overridden to return another value for regular use.
     * This method should be used for implementing serialization logic.
     *
     * @return              Reference to the stored value.
     */
    virtual const T& GetStored() const
    {
        return value;
    }

    virtual T& Modify()
    {
        return value;
    }

    virtual operator const T&() const
    {
        return Get();
    }

    ///Default constructor
    ValueWrapper() = default;

    /**
    * Initialized constructor for the Property class.
    *
    * @param    initialValue    Initial value.
    */
    ValueWrapper(const T& initialValue)
        : value(initialValue) {}

    virtual ~ValueWrapper() = default;

    //Make this class non-copyable.
    ValueWrapper(const ValueWrapper&) = delete;
    ValueWrapper& operator=(const ValueWrapper&) = delete;

    //Make this class move constructable to remove the necessity for mandatory copy elision.
    ValueWrapper(ValueWrapper&& other) = default;
    
    virtual bool operator==(const T& other) const
    {
        return Get() == other;
    }

    virtual bool operator==(ValueWrapper& other) const
    {
        return Get() == static_cast<T>(other);
    }

    friend bool operator==(const T& left, ValueWrapper& right)
    {
        return left == static_cast<T>(right);
    }

    virtual bool operator!=(const T& other) const
    {
        return Get() != other;
    }

    virtual bool operator!=(ValueWrapper& other) const
    {
        return Get() != static_cast<T>(other);
    }

};

}}} //Namespaces

#endif
