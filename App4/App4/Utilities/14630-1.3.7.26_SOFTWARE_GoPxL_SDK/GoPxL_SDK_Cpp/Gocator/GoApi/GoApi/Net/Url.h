/**@file    Url.h
 * Url utilities and classes. 
 */

#ifndef GOAPI_NET_URL_H
#define GOAPI_NET_URL_H

#include <GoApi/GoApiDef.h>
#include <string>

//! namespace used for all GoApi related classes.
namespace Go
{
namespace Net 
{
class GoApiClass Url
{
public:
    static std::string Encode(const std::string& src, bool spaceAsPlus = false);
    static std::string Decode(const std::string& src, bool spaceAsPlus = false);

private:
    static std::string CharToHex(char c);
    static char HexToChar(char first, char second);
};

}} // Namespace

#endif
