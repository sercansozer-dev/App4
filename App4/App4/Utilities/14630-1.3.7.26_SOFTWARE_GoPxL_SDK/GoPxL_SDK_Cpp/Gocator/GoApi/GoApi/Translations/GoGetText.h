/**@file    GoGetText.h
 * Defines the GoGetText class.
 */

#ifndef GOAPI_TRANSLATIONS_GO_GET_TEXT_H
#define GOAPI_TRANSLATIONS_GO_GET_TEXT_H

#include <map>
#include <string>
#include <algorithm>
#include "Catalog.h"

// Duplicate of GoApiDef, as GoApiDef uses this class
#if defined (GOAPI_EXPORT)
#   define GoGetExportClass       kExportClass
#elif defined (GOAPI_STATIC)
#   define GoGetExportClass
#else
#   define GoGetExportClass       kImportClass
#endif

#if defined (K_MSVC)
    //This disables warnings for templated std containers (std::vector, etc) not being dll exported.
    //Note: If you have a templated std container member variable used outside its owner class where the std library isnt available
    //      you will have *serious issues*. This shouldn't be a problem for us, as the GoApi libraries are
    //      internal use only, but still, be aware. Google "Visual Studio Warning C4251" for more info.
    #pragma warning( disable : 4251 )
    #pragma warning( disable : 4275 )
#endif

namespace Go
{
struct LocaleInfo
{
    std::string localeCode;
    std::string displayName;
};


/**
 * Custom implementation of a libintl like interface (gettext) for accessing translations.
 * 
 * This class supports loading multiple message catalogs and accessing their translated strings.
 * This class is a Singleton, in order to be used by a gettext macro drop-in replacement 
 * with libintl gettext usage.
 */
class GoGetExportClass GoGetText
{
private:
    GoGetText() {};
public:
    /**
    * Get the GoGetText singleton instance.
    */
    static GoGetText& GetInstance();
    static void DestroyInstance();

    // Similar to gettext bindTextDomain macro
    /**
    * Loads a message catalog. In simpler terms, this loads a translation file 
    * and binds it to a domain key/id.
    * 
    * @param domain             The key (id) to bind to.
    * @param stream             The data to load. This should be from or in MO file format.
    */
    void LoadMessageCatalog(const std::string& domain, kStream stream);

    /**
    * Loads information for an available locale. Used for querying what locales the running system has.
    *
    * @param localeCode         The locale code (ie. 'en-US') of the locale.
    * @param displayName        The display name (ie. 'English (United States)') of the locale.
    */
    void LoadAvailableLocale(const std::string& localeCode, const std::string& displayName);

    /**
    * Gets the list of the available locales on the running system.
    *
    * @returns                  The list of the locale info of all the available locales.
    */
    const std::vector<LocaleInfo>& AvailableLocales();

    /**
    * Gets the currently set locale.
    *
    * @returns                  The currently set domain, or a blank string if none has been set.
    */
    const char* Locale();

    /**
    * Sets the current locale.
    *
    * @param locale             The locale code (ie. 'en-US') of the locale to set to.
    * @returns                  The new locale code.
    */
    const char* SetLocale(const char* locale);

    /**
    * Gets the currently set default domain, or nullptr if none has been set.
    * 
    * @remarks                  The default domain is the domain used for translation functions that don't provide a domain.
    * @returns                  The current default domain, or nullptr if none has been set.
    */
    const char* DefaultDomain();

    /**
    * Sets the default domain. This is the domain used for translation functions that don't provide a domain.
    *
    * @remarks                  The default domain is the domain used for translation functions that don't provide a domain.
    * @returns                  The current default domain after changing it.
    */
    const char* SetDefaultDomain(const char* domain);

    /**
    * Gets the translated string for the given input string, using the default domain.
    *
    * @param originalString     The message id (original untranslated string).
    * @returns                  The translated string if found. Otherwise, the originalString.
    */
    const char* GetText(const char* originalString);

    /**
    * Gets the translated string for the given input string and context, using the default domain.
    *
    * @param context            The message context.
    * @param originalString     The message id (original untranslated string).
    * @returns                  The translated string if found. Otherwise, the originalString.
    */
    const char* PGetText(const char* context, const char* originalString);

    /**
    * Gets the translated string for the given input string in the requested domain.
    *
    * @param domain             The domain to look in.
    * @param originalString     The message id (original untranslated string).
    * @returns                  The translated string if found. Otherwise, the originalString.
    */
    const char* DGetText(const char* domain, const char* originalString);

    /**
    * Gets the translated string for the given input string and context, in the requested domain.
    *
    * @param domain             The domain to look in.
    * @param context            The message context.
    * @param originalString     The message id (original untranslated string).
    * @returns                  The translated string if found. Otherwise, the originalString.
    */
    const char* DPGetText(const char* domain, const char* context, const char* originalString);

    GoGetText(GoGetText const&) = delete;
    void operator=(GoGetText const&) = delete;

private:
    // Case insensitive comparison implementation for std::map
    struct CaseInsensitiveCompare
    {
        struct nocase_compare
        {
            bool operator() (const unsigned char& c1, const unsigned char& c2) const 
            {
                return toupper(c1) < toupper(c2);
            }
        };

        bool operator() (const std::string& s1, const std::string& s2) const
        {
            return std::lexicographical_compare(
                s1.begin(), s1.end(),   // source range
                s2.begin(), s2.end(),   // dest range
                nocase_compare()        // comparison
            );  
        }
    };

    const char* GetTextImpl(const char* domain, const char* fullContextString, const char* originalString);

    std::map<std::string, Catalog, CaseInsensitiveCompare> m_catalogs;
    std::string m_defaultDomain;
    std::vector<LocaleInfo> m_availableLocales;
    std::string m_currentLocale = "";
};
}; // Namespace

#if defined (K_MSVC)
    #pragma warning( default : 4251 )
    #pragma warning( default : 4275 )
#endif

#endif
