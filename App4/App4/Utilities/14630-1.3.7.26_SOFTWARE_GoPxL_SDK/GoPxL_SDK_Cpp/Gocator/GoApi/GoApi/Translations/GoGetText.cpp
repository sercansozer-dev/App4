#include "GoGetText.h"
#include "GoApi/Exception.h"

namespace Go
{
constexpr const char* GOAPI_GO_GET_TEXT_CONTEXT_GLUE = "\004";

static GoGetText* instance;

GoGetText& GoGetText::GetInstance()
{
    if (instance == nullptr)
    {
        instance = new GoGetText();
    }

    return *instance;
}

void GoGetText::DestroyInstance()
{
    if (instance != nullptr)
    {
        delete instance;
        instance = nullptr;
    }
}

void GoGetText::LoadMessageCatalog(const std::string& domain, kStream stream)
{
    if (m_catalogs.find(domain) != m_catalogs.end())
    {
        GoThrowMsg(kERROR_PARAMETER, "Catalog for domain %s is already loaded.", domain.c_str());
    }

    Catalog catalog;
    catalog.LoadFromStream(stream);
    m_catalogs[domain] = catalog;
}

void GoGetText::LoadAvailableLocale(const std::string& localeCode, const std::string& displayName)
{
    // Could use a map...but we shouldn't have too many locales so this is fine at O(n). 
    // Consider refactoring if we increase our number of locales past some point.
    if (std::find_if(m_availableLocales.begin(), m_availableLocales.end(),
        [&localeCode](const LocaleInfo& v) { return v.localeCode == localeCode; })
        != m_availableLocales.end())
    {
        GoLogError("Duplicate locale %s registered. Check if locale descriptor files are correct.", 
            localeCode.c_str());
    }

    m_availableLocales.push_back({ localeCode, displayName });
}

const std::vector<LocaleInfo>& GoGetText::AvailableLocales()
{
    return m_availableLocales;
}

const char* GoGetText::Locale()
{
    return m_currentLocale.c_str();
}

const char* GoGetText::SetLocale(const char* locale)
{
    m_currentLocale = std::string(locale);
    return m_currentLocale.c_str();
}

const char* GoGetText::DefaultDomain()
{
    if (m_defaultDomain == "")
    {
        return nullptr;
    }

    return m_defaultDomain.c_str();
}

const char* GoGetText::SetDefaultDomain(const char* domain)
{
    m_defaultDomain = std::string(domain);
    return m_defaultDomain.c_str();
}

const char* GoGetText::GetText(const char* originalString)
{
    return GetTextImpl(nullptr, originalString, originalString);
}

const char* GoGetText::PGetText(const char* context, const char* originalString)
{
    std::string fullContextString(context);
    fullContextString += GOAPI_GO_GET_TEXT_CONTEXT_GLUE;
    fullContextString += originalString;

    return GetTextImpl(nullptr, fullContextString.c_str(), originalString);
}

const char* GoGetText::DGetText(const char* domain, const char* originalString)
{
    return GetTextImpl(domain, originalString, originalString);
}

const char* GoGetText::DPGetText(const char* domain, const char* context, const char* originalString)
{
    std::string fullContextString(context);
    fullContextString += GOAPI_GO_GET_TEXT_CONTEXT_GLUE;
    fullContextString += originalString;

    return GetTextImpl(domain, fullContextString.c_str(), originalString);
}

const char* GoGetText::GetTextImpl(const char* domain, const char* fullContextString, const char* originalString)
{
    // resolve domain
    if (!domain)
    {
        if (!m_defaultDomain.empty())
        {
            domain = m_defaultDomain.c_str();
        }
        else 
        {
            // If default domain hasn't been set and no domain provided, return.
            return originalString;
        }
    }

    const auto& itr = m_catalogs.find(domain);
    if (itr == m_catalogs.end())
    {
        // If we don't have a catalog for this domain, return.
        return originalString;
    }

    const std::string* translatedString = itr->second.GetTranslatedString(fullContextString);
    if (!translatedString)
    {
        // If we don't have a translated match in the catalog, return.
        return originalString;
    }

    return translatedString->c_str();
}
}
