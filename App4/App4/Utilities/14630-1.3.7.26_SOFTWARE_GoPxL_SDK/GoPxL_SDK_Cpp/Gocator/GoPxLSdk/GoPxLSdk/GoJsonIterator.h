/**
 * @file    GoJsonIterator.h
 * @brief   Declares the GoPxLSdk.GoJsonIterator class.
 * 
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#ifndef GO_PXL_SDK_JSON_ITERATOR_H
#define GO_PXL_SDK_JSON_ITERATOR_H

#include <GoPxLSdk/Def.h>
#include <GoPxLSdk/GoJsonError.h>

namespace GoPxLSdk
{

/* GoJson forward declaration - needed to avoid circular dependency. */
class GoJson;

/**
 * Represents json iterator.
 */
class GoPxLSdkClass GoJsonIterator
{
public:
    using RawIterator = nlohmann::detail::iter_impl<const nlohmann::json>;

    /**
     * Creates Iterator object.
     *
     * @public                @memberof GoJsonIterator
     * @version               Introduced in 0.2.1.53
     * @param iterator Json::iterator.
     */
    explicit GoJsonIterator(const RawIterator iterator);

    /**
     * Gets Key of an object iterator if object is not null.
     *
     * @public                @memberof GoJsonIterator
     * @version               Introduced in 0.2.1.53
     * @throw GoJsonError.
     * @return string key .
     */
    std::string Key();

    /**
     * Gets Key of an object iterator if object is not null.
     *
     * @public                @memberof GoJsonIterator
     * @version               Introduced in 0.2.1.53
     * @throw GoJsonError.
     * @return string key .
     */
    const std::string Key() const;

    /**
     * Gets value of an object iterator if object is not null.
     *
     * @public                @memberof GoJsonIterator
     * @version               Introduced in 0.2.1.53
     * Throw GoJsonError.
     * @return GoJson object.
     */
    GoJson Value();

    /**
     * Gets value of an object iterator if object is not null.
     *
     * @public                @memberof GoJsonIterator
     * @version               Introduced in 0.2.1.53
     * Throw GoJsonError.
     * @return GoJson object.
     */
    const GoJson Value() const;

    /**
     * Sets value of an object.
     *
     * @public                @memberof GoJsonIterator
     * @version               Introduced in 0.2.1.53
     * @param value GoJson
     * Throw GoJsonError.
     */
    void SetValue(const GoJson& value);

    /**
     * Gets iterator handle.
     *
     * @public                @memberof GoJsonIterator
     * @version               Introduced in 0.2.1.53
     */
    std::shared_ptr<RawIterator>& GetHandle();
    const std::shared_ptr<RawIterator>& GetHandle() const;

    GoJsonIterator& operator=(const GoJsonIterator& other);

    bool operator==(const GoJsonIterator other) const;
    bool operator!=(const GoJsonIterator other) const;

    bool operator<(const GoJsonIterator other) const;
    bool operator<=(const GoJsonIterator other) const;

    bool operator>(const GoJsonIterator other) const;
    bool operator>=(const GoJsonIterator other) const;

    GoJson operator*() const;

    GoJsonIterator& operator+(int val);
    GoJsonIterator& operator+=(int val);
    GoJsonIterator& operator++(int);

    GoJsonIterator& operator-(int val);
    GoJsonIterator& operator-=(int val);
    GoJsonIterator& operator--(int);

private:
    std::shared_ptr<RawIterator> iterator;
};

}

#endif