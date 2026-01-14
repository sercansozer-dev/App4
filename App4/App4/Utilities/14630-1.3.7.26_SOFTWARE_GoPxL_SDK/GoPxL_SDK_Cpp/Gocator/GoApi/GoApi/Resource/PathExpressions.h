/**@file    PathExpressions.h
 * Path Expressions class
 */

#ifndef GOAPI_PATH_EXPRESSIONS_H
#define GOAPI_PATH_EXPRESSIONS_H

#include <GoApi/GoApiDef.h>
#include <string>
#include <vector>
#include <regex>

namespace GoApi
{
/**
 * @class SubscriptionExpression
 * @brief Provides Gocator scheme specific subscription matching functionality with support for wildcards.
 * @ingroup GoApi-Resources
 *
 * TBD: Can this be replaced/refactored to reuse anything from ResolvedResource,
 *       PathToken, or PathTokenTree?
 */
class GoApiClass SubscriptionExpression
{
public:
    /**
     * Constructs the subscription expression utility class.
     *
     * @param expr           Reference to a subscription matching expression.
     */
    SubscriptionExpression(const std::string& expr);

    /**
     * Returns the original subscription matching expression.
     *
     * @return              The original subscription matching expression as a string.
     */
    const std::string& Text() const;

    /**
     * Returns whether the path expression "matches" a given path.
     *
     * @param path          The path to check for a match against the path expression.
     * @return              True if the path expression matches the given path; false otherwise.
     *
     * @remarks             This function will return false for a parent with a specific subscription underneath said parent.
     */
    bool IsMatch(const std::string& path) const;

    /**
     * Returns whether the path expression "matches" a given path when paired with an embeddedUpdated event.
     * This means that all clients subscribed to a subresource should
     * receive parent resource embeddedUpdated event notifications.
     *
     * @param path          The path to check for a match against the path expression under embeddedUpdated event specifications.
     * @return              True if the path expression matches the given path; false otherwise.
     *
     * @remarks             This function will return true for a parent with a specific subscription underneath said parent.
     */
    bool IsEmbeddedMatch(const std::string& path) const;

private:
    std::string subExpr;
    std::string subExprEmbeddedUpdated;
    std::regex subRegex;
    std::regex subRegexEmbeddedUpdated;

    std::string WildcardToRegex(std::string expr);
    std::string WildcardToRegexEmbedded(std::string expr);
    void FindAndReplaceAll(std::string& fullStr, const std::string& findStr, const std::string& replaceStr);
    void FindAndReplaceEnd(std::string& fullStr, const std::string& findStr, const std::string& replaceStr);
    void EscapeRegexHelper(std::string& regex);
    void ConvertRegexWildCards(std::string& regex);
};


/**
 * @class ParameterExpression
 * @brief Provides Gocator scheme specific URI parameter functionality,
 *        usually used for path parameters.
 * @ingroup GoApi-Resources
 *
 * @remarks
 * See URI Template https://tools.ietf.org/html/rfc6570,
 * and Uniform Resource Identifier (URI): Generic Syntax sec 3.3 Path
 * http://tools.ietf.org/html/rfc3986#section-3.3
 * and https://swagger.io/docs/specification/serialization/#path
 * This is to support the "simple, exploded=yes" URI template:
 * @code
 *   tools/{id*}
 * @endcode

 * which for example expands to:
 * @code
 *   tools/TestForward-0
 *   tools/extId=My%20Tool
 *   tools/extId=My%20Tool,otherExtId=Something%20else
 * @endcode
 */
class GoApiClass ParameterExpression
{
public:
    /**
     * Constructs the parameter expression.
     */
    ParameterExpression(const std::string& expr);

    /**
     * Returns the number of 'field=value' parameters parsed from the expression.
     *
     * @return           Count of parameters.
     * @remarks          A single 'value' with no field is also counted as 1.
     */
    size_t ParameterCount() const;

    /**
     * Returns the 'field=value' parameter at the given index.
     *
     * @param index      Index of parameter.
     * @return           A 'field=value' parameter as a tuple, ie. (field=first, value=second).
     */
    const std::pair<std::string, std::string> ParameterAt(size_t index) const;

private:
    std::string parameterExpr;
    std::vector<std::pair<std::string, std::string>> parameters;

    void ParseExpression(const std::string& expr);
};

} // Namespace

#endif
