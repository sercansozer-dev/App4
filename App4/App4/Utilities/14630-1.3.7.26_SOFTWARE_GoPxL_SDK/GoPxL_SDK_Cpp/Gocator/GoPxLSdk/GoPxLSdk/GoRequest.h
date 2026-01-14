/**
 * @file    GoRequest.h
 * @brief   Declares the GoPxLSdk.GoRequest class.
 * 
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#ifndef GO_PXL_SDK_GOREQUEST_H
#define GO_PXL_SDK_GOREQUEST_H

#include <GoPxLSdk/Def.h>
#include <GoPxLSdk/GoJson.h>
#include <GoPxLSdk/GoRequestMethod.h>

class GoRequestTests;

namespace GoPxLSdk
{

class GoPxLSdkClass GoRequest
{
public:
    /**
     * Default constructor.
     * 
     * @public                @memberof GoRequest
     * @version               Introduced in 0.2.1.53.
     */
    GoRequest() = default;

    /**
     * Constructs GoRequest.
     * 
     * @public                @memberof GoRequest
     * @version               Introduced in 0.2.1.53.
     * @param method          GoRequestMethod.
     * @param uri             Request path.
     */
    GoRequest(GoRequestMethod method, const std::string& uri);

    /**
     * Constructs GoRequest.
     * 
     * @public                @memberof GoRequest
     * @version               Introduced in 0.2.1.53.
     * @param method          GoRequestMethod.
     * @param uri             Request path.
     * @param content         Request payload.
     */
    GoRequest(GoRequestMethod method, const std::string& uri, const GoJson& content);

    /**
     * Constructs GoRequest.
     * 
     * @public                @memberof GoRequest
     * @version               Introduced in 0.2.1.53.
     * @param method          GoRequestMethod.
     * @param uri             Request path.
     * @param content         Request payload.
     * @param args            Request arguments.
     */
    GoRequest(GoRequestMethod method, const std::string& uri, const GoJson& content, const GoJson& args);

    /**
     * Gets the request id.
     *
     * @public                @memberof GoRequest
     * @version               Introduced in 1.1.9.56
     */
    k64u Id() const;

    /**
     * Gets the request method.
     * 
     * @public                @memberof GoRequest
     * @version               Introduced in 0.2.1.53.
     */
    GoRequestMethod Method() const;

    /**
     * Gets the relative URI of the request.
     * For example:
     ** /scanners/A/sensors/0
     ** /tool/someTool-0/inputs
     * 
     * @public                @memberof GoRequest
     * @version               Introduced in 0.2.1.53.
     */
    const std::string& Uri() const;

    /**
     * Gets the request arguments.
     * 
     * @public                @memberof GoRequest
     * @version               Introduced in 0.2.1.53.
     */
    const GoJson& Arguments() const;

    /**
     * Gets the request content.
     * Content are typically only used for Update, or less commonly, Create.
     * 
     * @public                @memberof GoRequest
     * @version               Introduced in 0.2.1.53.
     */
    const GoJson& Content() const;


    /**
     * Gets the request in form of ByteArray.
     * 
     * @public                @memberof GoRequest
     * @version               Introduced in 0.2.1.53.
    */
    const ByteArray ToByteArray() const;

private:
    k64u id;

    GoRequestMethod method = GoRequestMethod::Read;
    std::string uri;
    GoJson content = GoJson();
    GoJson args = GoJson();

    static k64u nextId;
    
    friend class ::GoRequestTests;
};

}

#endif