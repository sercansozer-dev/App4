#ifndef GOAPI_RESOURCE_DEF_H
#define GOAPI_RESOURCE_DEF_H

namespace GoApi
{
#define GOAPI_RESOURCE_PATH_SEPARATOR       "/"
#define GOAPI_RESOURCE_PATH_KEY_PREFIX      ":"
#define GOAPI_RESOURCE_PATH_KEY_PREFIX_CHAR ':'
// GOS-10590: Same value as above, but renamed for the root resource path.
#define GOAPI_ROOT_RESOURCE_PATH            "/"

// See PathExpressions::ParameterExpression class
// which uses these for parsing URI templates of form {id*}.
// See (see https://tools.ietf.org/html/rfc6570) for more details about
// URI templates and the usage of relevant reserved characters
#define GOAPI_RESOURCE_PARAM_SEPARATOR                  ","
#define GOAPI_RESOURCE_PARAM_FIELD_VALUE_SEPARATOR      "="
} // namespace

#endif  // GOAPI_RESOURCE_DEF_H
