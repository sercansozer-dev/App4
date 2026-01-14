#include "PathExpressions.h"
#include <GoApi/Resource/ResourceDef.h>

namespace GoApi
{
//-----------------------------------------------------------------------------
// SubscriptionExpression
//-----------------------------------------------------------------------------
SubscriptionExpression::SubscriptionExpression(const std::string& expr) :
    subExpr(expr)
{
    // Default is std::regex::constants::ECMAScript behavior.
    subRegex = std::regex(WildcardToRegex(expr).c_str());
    subRegexEmbeddedUpdated = std::regex(WildcardToRegexEmbedded(expr).c_str());
}

const std::string& SubscriptionExpression::Text() const
{
    return subExpr;
}

bool SubscriptionExpression::IsMatch(const std::string& path) const
{
    bool result = false;
    std::smatch strMatch;

    // this only matches if the entire expression matches
    result = std::regex_match(path, strMatch, subRegex);

    return result;
}

bool SubscriptionExpression::IsEmbeddedMatch(const std::string& path) const
{
    bool result = false;
    std::smatch strMatch;

    result = std::regex_match(path, strMatch, subRegexEmbeddedUpdated);

    return result;
}

std::string SubscriptionExpression::WildcardToRegex(std::string expr)
{
    // Some references for this.  We may want to refactor this
    // to be more efficient in the future.

    // Currently the wild card expression is converted into a Regex only initially.

    //https://www.codeproject.com/Articles/11556/Converting-Wildcards-to-Regexes
    // Also see: https://www.geeksforgeeks.org/wildcard-pattern-matching/
    // Also see: https://www.prodevelopertutorial.com/wildcard-matching-in-c/
    // Also see: https://www.daniweb.com/programming/software-development/threads/288358/string-matching-with-wildcard
    // Also see: https://www.daniweb.com/programming/software-development/code/288506/matching-strings-with-wildcards#post1241451

    std::string regexStr = expr;

    EscapeRegexHelper(regexStr);
    ConvertRegexWildCards(regexStr);
    FindAndReplaceAll(regexStr, "?", ".");

    // If ends with /.*, we need to change it to  (/.*)?
    // eg. this is so that /tools/* also matches /tools
    FindAndReplaceEnd(regexStr, "/.*", "(/.*)?");

    return regexStr;
}

std::string SubscriptionExpression::WildcardToRegexEmbedded(std::string expr)
{
    // Creates the necessary regex that accepts the parent and sub-resources of any given resource.

    std::string regexStr = expr;
    std::string::difference_type numSlashes;

    // If resource is a path, modify otherwise treat as any other regex.
    if (!expr.empty() && expr.front() == '/')
    {
        // Count the number of slashes in the path to calculate number of resources in the resource tree.
        numSlashes = std::count(regexStr.begin(), regexStr.end(), '/');

        // Separate the resources into a tree of grouped expressions with opening parentheses.
        FindAndReplaceAll(regexStr, "/", "(/");

        for (int i = 0; i < numSlashes - 1; i++)
        {
            // For each slash, append a respective closing parenthese for each opening parentheses.
            // We also want to specify that each resource should either appear once or not at all.
            regexStr.append(")?");
        }

        // Close off the last one but don't append '?' to specify that the top-most resource must appear.
        regexStr.append(")");

    }

    EscapeRegexHelper(regexStr);
    ConvertRegexWildCards(regexStr);

    return regexStr;
}

// TBD: Could be replaced with C-based kApi functions if available.
void SubscriptionExpression::FindAndReplaceEnd(std::string& fullStr, const std::string& findStr, const std::string& replaceStr)
{
    // findStr doesn't fit within fullStr.
    if (findStr.length() > fullStr.length())
    {
        return;
    }
    else
    {
        // findStr found at end of the fullStr.
        size_t endPos = fullStr.length() - findStr.length();
        if (0 == fullStr.compare(endPos, findStr.length(), findStr))
        {
            // Replace this occurence of the string.
            fullStr.replace(endPos, findStr.size(), replaceStr);
        }
    }
}

// TBD: Could be replaced with C-based kApi functions if available.
void SubscriptionExpression::FindAndReplaceAll(std::string& fullStr, const std::string& findStr, const std::string& replaceStr)
{
    // Get the first occurrence
    size_t pos = fullStr.find(findStr);

    // Repeat till end is reached
    while( pos != std::string::npos)
    {
        // Replace this occurrence of Sub String
        fullStr.replace(pos, findStr.size(), replaceStr);
        // Get the next occurrence from the current position
        pos = fullStr.find(findStr, pos + replaceStr.size());
    }
}

// Escape all relevant regex characters
void SubscriptionExpression::EscapeRegexHelper(std::string& regex)
{
    FindAndReplaceAll(regex, ".", "\\.");
    FindAndReplaceAll(regex, ":", "\\:");
    FindAndReplaceAll(regex, "+", "\\+");
    FindAndReplaceAll(regex, "$", "\\$");
}

// Convert wildcards to regex '.*' and '.'
void SubscriptionExpression::ConvertRegexWildCards(std::string& regex)
{
    FindAndReplaceAll(regex, "**", "*");
    FindAndReplaceAll(regex, "*", ".*");
}

//-----------------------------------------------------------------------------
// ParameterExpression
//-----------------------------------------------------------------------------
ParameterExpression::ParameterExpression(const std::string& expr) :
    parameterExpr(expr)
{
    // We only support single character separators.
    kAssert(strlen(GOAPI_RESOURCE_PARAM_SEPARATOR) == 1);
    ParseExpression(parameterExpr);
}

size_t ParameterExpression::ParameterCount() const
{
    return parameters.size();
}

const std::pair<std::string, std::string> ParameterExpression::ParameterAt(size_t index) const
{
    return parameters.at(index);
}

void ParameterExpression::ParseExpression(const std::string& expr)
{
    size_t start = 0;
    std::string field = "";
    std::string value = "";

    while (start < expr.size())
    {
        size_t valueEnd = expr.find_first_of(GOAPI_RESOURCE_PARAM_SEPARATOR, start);
        size_t fieldEnd = expr.find_first_of(GOAPI_RESOURCE_PARAM_FIELD_VALUE_SEPARATOR, start);

        // No more separators.
        if (fieldEnd == expr.npos && valueEnd == expr.npos)
        {
            valueEnd = expr.size();
            if (valueEnd - start > 0)
            {
                value = expr.substr(start, valueEnd - start);
                parameters.emplace_back(std::make_pair(field, value));
            }
            start = valueEnd + 1;
        }
        // Value.
        else if (valueEnd < fieldEnd)
        {
            if (valueEnd - start > 0)
            {
                value = expr.substr(start, valueEnd - start);
                parameters.emplace_back(std::make_pair(field, value));
            }
            start = valueEnd + 1;
        }
        // Field.
        else // (fieldEnd < valueEnd)
        {
            field = expr.substr(start, fieldEnd - start);
            start = fieldEnd + 1;
        }
    }
}

} // Namespaces
