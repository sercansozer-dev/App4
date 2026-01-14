#include "Url.h"
#include <cctype>

namespace Go
{
namespace Net 
{
//-----------------------------------------------------------------------------
// General utility functions.
//-----------------------------------------------------------------------------

// Based on cgicc::form_urlencode, version cgicc-3.2.19, 2017-06-27.
// The default implmentation would always encode spaces as '+', which we would 
// prefer to restrict to only do within queries.
// 
// See https://stackoverflow.com/questions/2678551/when-to-encode-space-to-plus-or-20
// and https://stackoverflow.com/questions/1634271/url-encoding-the-space-character-or-20
//
// For original form_urlencode, see https://www.gnu.org/software/cgicc/doc/namespacecgicc.html#90356a1f522eeb502bb68e7d87a1f848.
std::string Url::Encode(const std::string& src, bool spaceAsPlus)
{
    std::string result;
    std::string::const_iterator iter;
  
    for(iter = src.begin(); iter != src.end(); ++iter) 
    {
        // Simplified from original cgicc implementation.
        // to use isalnum to cover (a-z, A-z, 0-9)
        // See http://www.cplusplus.com/reference/cctype/.
        if (isalnum(*iter))
        {
            result.append(1, *iter);
        }
        else
        {
            switch(*iter) 
            {
                case ' ':
                    if (spaceAsPlus)
                    {
                        result.append(1, '+');
                    }
                    else
                    {
                        result.append(1, '%');
                        result.append(CharToHex(*iter));
                    }
                    break;
                // mark
                case '-': case '_': case '.': case '!': case '~': case '*': case '\'': 
                case '(': case ')':
                    result.append(1, *iter);
                    break;
                // escape
                default:
                    result.append(1, '%');
                    result.append(CharToHex(*iter));
                    break;
            } // end switch
        }
    } // end for

  return result;
}

// Based on cgicc::form_urldecode, version cgicc-3.2.19, 2017-06-27.
// The default implmentation would always decode '+' as spaces, which we would 
// prefer to restrict to only do within queries see UrlDecode() comments above.
//
// For original form_urldecode, see https://www.gnu.org/software/cgicc/doc/namespacecgicc.html#6d606205b854b83dc93b0e180e8d5598.
std::string Url::Decode(const std::string& src, bool spaceAsPlus)
{
    std::string result;
    std::string::const_iterator iter;
    char c;

    for(iter = src.begin(); iter != src.end(); ++iter)
    {
        switch(*iter) 
        {
            case '+':
                if (spaceAsPlus)
                {
                    result.append(1, ' ');
                }
                else
                {
                    result.append(1, *iter);
                }
                break;
            case '%':
                // Don't assume well-formed input
                if(std::distance(iter, src.end()) >= 2
                   && std::isxdigit(*(iter + 1)) && std::isxdigit(*(iter + 2)))
                {
                    c = *++iter;
                    result.append(1, HexToChar(c, *++iter));
                }
                // Just pass the % through untouched
                else
                {
                    result.append(1, '%');
                }
                break;
    
            default:
                result.append(1, *iter);
                break;
        } // end switch
    } // end for
  
  return result;
}

std::string Url::CharToHex(char c)
{
    std::string result;
    char first, second;

    first = (c & 0xF0) / 16;
    first += first > 9 ? 'A' - 10 : '0';
    second = c & 0x0F;
    second += second > 9 ? 'A' - 10 : '0';

    result.append(1, first);
    result.append(1, second);
    return result;
}

char Url::HexToChar(char first, char second)
{
    int digit;
  
    digit = (first >= 'A' ? ((first & 0xDF) - 'A') + 10 : (first - '0'));
    digit *= 16;
    digit += (second >= 'A' ? ((second & 0xDF) - 'A') + 10 : (second - '0'));
    return static_cast<char>(digit);
}

}} // Namespaces
