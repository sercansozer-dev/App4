/**
 * @file    GoJsonPointer.h
 * @brief   Declares the GoPxLSdk.GoJsonPointer class.
 *
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#ifndef GO_PXL_SDK_JSON_POINTER_H
#define GO_PXL_SDK_JSON_POINTER_H

#include <GoPxLSdk/Def.h>
#include <GoPxLSdk/GoJsonError.h>

namespace GoPxLSdk
{
/**
 * Represents json pointer as described in [Section 3 of RFC6901](https://tools.ietf.org/html/rfc6901#section-3).
**/
class GoPxLSdkClass GoJsonPointer
{
public:
    /**
     * Creates GoJson::Pointer object
     *
     * @public                @memberof GoJsonPointer
     * @version               Introduced in 0.2.1.53
     */
    GoJsonPointer() = default;

    /**
     * Creates GoJson::Pointer object
     *
     * @public                @memberof GoJsonPointer
     * @version               Introduced in 0.2.1.53
     * @param data valid pointer
     * @throw GoJsonError if data is not valid json pointer.
     */
    explicit GoJsonPointer(const std::string& data);

    /**
     * Creates GoJson::Pointer object
     *
     * @public                @memberof GoJsonPointer
     * @version               Introduced in 0.2.1.53
     * @param data valid pointer
     * @throw GoJsonError if data is not valid json pointer.
     */
    GoJsonPointer(const GoJsonPointer& data);

    /**
     * Gets handle to Json::json_pointer object.
     *
     * @public                @memberof GoJsonPointer
     * @version               Introduced in 0.2.1.53
     * @return Json::json_pointer
     */
    std::shared_ptr<nlohmann::json_pointer<nlohmann::json>>& GetHandle();
    const std::shared_ptr<nlohmann::json_pointer<nlohmann::json>>& GetHandle() const;

    /**
     * Appends the key.
     *
     * @public                @memberof GoJsonPointer
     * @version               Introduced in 0.2.1.53
     * @param key key to add.
     */ 
    void PushBack(const std::string& key);

    /**
     * Appends the GoJsonPointer.
     *
     * @public                @memberof GoJsonPointer
     * @version               Introduced in 0.2.1.53
     * @param pointer pointer to add.
     */
    void PushBack(const GoJsonPointer& pointer);

    /**
     * Deletes last pointer key.
     *
     * @public                @memberof GoJsonPointer
     * @version               Introduced in 0.2.1.53
     */
    void PopBack();

    /**
     * Represents GoJsonPointer as string.
     *
     * @public                @memberof GoJsonPointer
     * @version               Introduced in 0.2.1.53
     * @return std::string value.
     */
    const std::string ToString() const;

    /**
     * Combines GoJsonPointer and key.
     *
     * @public                @memberof GoJsonPointer
     * @version               Introduced in 0.2.1.53
     * @param target base GoJsonPointer.
     * @param tail string value to add.
     * @return new GoJsonPointer.
     */
    static GoJsonPointer Combine(const GoJsonPointer& target, const std::string& tail);

    /**
     * Combines two GoJsonPointers.
     *
     * @public                @memberof GoJsonPointer
     * @version               Introduced in 0.2.1.53
     * @param target base GoJsonPointer.
     * @param tail GoJsonPointer value to add.
     * @return new GoJsonPointer.
     */
    static GoJsonPointer Combine(const GoJsonPointer& target, const GoJsonPointer& tail);

    /**
     * Parses json pointer string.
     *
     * @public                @memberof GoJsonPointer
     * @version               Introduced in 0.2.1.53
     * @param data valid pointer
     * @throw GoJsonError if data is not valid json pointer.
     * @return GoJson::Pointer object
     */
    static GoJsonPointer ParseString(const std::string& data);

private:
    mutable std::shared_ptr<nlohmann::json_pointer<nlohmann::json>> handle;

    /**
     * Split key into tokens.
     *
     * @public                @memberof GoJsonPointer
     * @version               Introduced in 0.2.1.53
     * @param key data to split.
     * @param delimiter character.
     * @return vector with tokens.
     */
    static std::vector<std::string> Tokenize(const std::string& key, char delimiter);
};

}

#endif