#include "Catalog.h"
#include "kApi/Io/kFile.h"
#include "kApi/Io/kSerializer.h"
#include "kApi/Io/kMemory.h"
#include "GoApi/Exception.h"

namespace Go
{
// MO spec magic numbers for verifying the byte data is the expected mo byte format.
constexpr k32u MAGIC_NUMBER_A = 0x950412de;
constexpr k32u MAGIC_NUMBER_B = 0xde120495;

// Maximum supported string, original or translated. Defined arbitrarily by us, not by the mo spec.
constexpr size_t MAX_STRING_LENGTH = 4096; 

Catalog::Catalog()
    :m_magicNumber(0),
    m_fileFormatRevision(0),
    m_numberofStrings(0),
    m_originalTableOffset(0),
    m_translatedTableOffset(0),
    m_hashingTableSize(0),
    m_hashingTableOffset(0),
    m_hasLoadedData(false)
{ }

Catalog::~Catalog()
{ }

const std::string* Catalog::GetTranslatedString(const std::string& originalString)
{
    const auto& itr = m_map.find(originalString);
    if (itr == m_map.end())
    {
        return nullptr;
    }
    
    return &itr->second;
}

void Catalog::LoadFromStream(kStream stream)
{
    // Local guard class. Can't use Go::Object easily due to circular ref with GoApiDef.
    class GuardClass {
    public:
        GuardClass(kObject obj) : m_obj(obj) {};
        ~GuardClass() { kObject_Dispose(m_obj); }
    private:
        kObject m_obj;
    };

    // Don't allow reloading. If we've already loaded data, this instance is locked in.
    GoThrowMsgIf(m_hasLoadedData, kERROR_ALREADY_EXISTS, "Catalog has already loaded data and cannot be reused.");
    m_hasLoadedData = true;

    GoTest(kStream_Seek(stream, 0, kSEEK_ORIGIN_BEGIN));

    kSerializer serializer = kNULL;
    GuardClass guard(serializer);
    GoTest(kSerializer_Construct(&serializer, stream, kNULL, kObject_Alloc(stream)));

    // Read header values.
    ReadHeader(serializer);

    // Check that we're reading a proper .mo file. Magic numbers are from the MO spec.
    if (m_magicNumber != MAGIC_NUMBER_A && m_magicNumber != MAGIC_NUMBER_B)
    {
        // The magic number we read MUST match. Byte ordering means either magic number is valid.
        GoThrowMsg(kERROR_FORMAT, "Unable to read message catalog due to invalid MO file verification number.");
    }

    if (m_fileFormatRevision != 0) 
    {
        // We only support version == 0.
        GoThrowMsg(kERROR_VERSION, 
            "Unable to read message catalog due to invalid MO file format version (%d).", m_fileFormatRevision);
    }

    // Read strings (original and translated).
    ReadTable(serializer, m_numberofStrings, m_originalTableOffset, m_translatedTableOffset);
}

void Catalog::ReadHeader(kSerializer serializer)
{
    GoTest(kSerializer_Read32u(serializer, &m_magicNumber));
    GoTest(kSerializer_Read32s(serializer, &m_fileFormatRevision));
    GoTest(kSerializer_Read32s(serializer, &m_numberofStrings));
    GoTest(kSerializer_Read32u(serializer, &m_originalTableOffset));
    GoTest(kSerializer_Read32u(serializer, &m_translatedTableOffset));
    GoTest(kSerializer_Read32s(serializer, &m_hashingTableSize));
    GoTest(kSerializer_Read32u(serializer, &m_hashingTableOffset));
}

void Catalog::ReadLengthsAndOffsets(kSerializer serializer, int num, k32u offset,
    std::vector<k32u>& lengths, std::vector<k32u>& offsets)
{
    // Read original lengths and offsets
    GoTest(kStream_Seek(kSerializer_Stream(serializer), offset, kSEEK_ORIGIN_BEGIN));

    for (int i = 0; i < num; i++)
    {
        GoTest(kSerializer_Read32u(serializer, &lengths[i]));
        GoTest(kSerializer_Read32u(serializer, &offsets[i]));
    }
}

void Catalog::ReadTable(kSerializer serializer, int num, k32u offsetOriginal, k32u offsetTranslated)
{
    std::vector<k32u> oLengths(num);
    std::vector<k32u> oOffsets(num);
    ReadLengthsAndOffsets(serializer, num, offsetOriginal, oLengths, oOffsets);

    std::vector<k32u> tLengths(num);
    std::vector<k32u> tOffsets(num);
    ReadLengthsAndOffsets(serializer, num, offsetTranslated, tLengths, tOffsets);

    kChar tBuffer[MAX_STRING_LENGTH];
    kChar oBuffer[MAX_STRING_LENGTH];

    // Read strings
    for (int i = 0; i < num; i++)
    {
        if (oLengths[i] + 1 >= (k32u)MAX_STRING_LENGTH)
        {
            GoLogError("Unable to load string id # %d due to expected length %d exceeding maximum length %d", 
                i, oLengths[i], MAX_STRING_LENGTH);
            continue;
        }

        if (tLengths[i] + 1 >= (k32u)MAX_STRING_LENGTH)
        {
            GoLogError("Unable to load string translation # %d due to expected length %d exceeding maximum length %d",
                i, tLengths[i], MAX_STRING_LENGTH);
            continue;
        }

        // Read original string
        GoTest(kStream_Seek(kSerializer_Stream(serializer), oOffsets[i], kSEEK_ORIGIN_BEGIN));
        GoTest(kSerializer_ReadCharArray(serializer, oBuffer, oLengths[i] + 1));

        // Read translated string
        GoTest(kStream_Seek(kSerializer_Stream(serializer), tOffsets[i], kSEEK_ORIGIN_BEGIN));
        GoTest(kSerializer_ReadCharArray(serializer, tBuffer, tLengths[i] + 1));

        m_map.emplace(oBuffer, tBuffer);
    }
}
}