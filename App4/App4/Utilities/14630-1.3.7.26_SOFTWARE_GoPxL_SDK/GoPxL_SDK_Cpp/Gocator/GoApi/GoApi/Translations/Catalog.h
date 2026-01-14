/**@file    GoGetText.h
 * Defines the GoGetText class.
 */

#ifndef GOAPI_TRANSLATIONS_CATALOG_H
#define GOAPI_TRANSLATIONS_CATALOG_H

#include <string>
#include <kApi/Io/kSerializer.h>
#include <unordered_map>
#include <vector>

namespace Go
{
/**
* This class loads, parses and stores MO file data for translations. Generally, each
* Message Catalog will hold the translated strings for a single language.
* 
* Consuming code should usually use GoGetText, and not the Catalog directly.
* 
* @remarks  MO file format document: https://www.gnu.org/software/gettext/manual/html_node/MO-Files.html
*/
class Catalog
{
public:
    Catalog();
    ~Catalog();

    /**
    * Loads message catalog data from a kStream.
    * 
    * @param stream         The stream containing the catalog data. This is expected to be in MO file format.
    */
    void LoadFromStream(kStream stream);

    /**
    * Get a translated string. Returns nullptr if no match for the input string is found.
    * 
    * @param originalString     The raw string id to lookup the translation for.
    * @returns                  The translated string, or nullptr if no match is found.
    */
    const std::string* GetTranslatedString(const std::string& originalString);

private:
    void ReadHeader(kSerializer serializer);
    void ReadTable(kSerializer serializer, int num, k32u offsetOriginal, k32u offsetTranslated);
    void ReadLengthsAndOffsets(kSerializer serializer, int num, k32u offset,
        std::vector<k32u>& lengths, std::vector<k32u>& offsets);

    k32u m_magicNumber;
    int m_fileFormatRevision;
    int m_numberofStrings;
    k32u m_originalTableOffset;
    k32u m_translatedTableOffset;
    int m_hashingTableSize;
    k32u m_hashingTableOffset;

    bool m_hasLoadedData;

    std::unordered_map<std::string, std::string> m_map;
};
} // Namespace

#endif
