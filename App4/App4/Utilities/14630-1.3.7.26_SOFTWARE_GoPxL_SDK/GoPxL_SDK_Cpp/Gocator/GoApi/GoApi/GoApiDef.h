#ifndef GOAPI_DEF_H
#define GOAPI_DEF_H

#include <kApi/kApiDef.h>

// TODO: are all these includes needed?  Pollution like this can drastically increase compilation times!
#include <stdlib.h>
#include <stdio.h>
#include <errno.h>
#include <math.h>
#include <sstream>
#include <string>
#include <sstream>
#include <iomanip>
#include <stdlib.h>

#include "Translations/GoGetText.h"

#if defined (GOAPI_EXPORT)
#   define GoApiCppFx(TYPE)     kExportEx(TYPE)
#   define GoApiFx(TYPE)        kExportFx(TYPE)
#   define GoApiCx(TYPE)        kExportCx(TYPE)
#   define GoApiDx(TYPE)        kExportDx(TYPE)
#   define GoApiClass           kExportClass
#elif defined (GOAPI_STATIC)
#   define GoApiCppFx(TYPE)     TYPE
#   define GoApiFx(TYPE)        kInFx(TYPE)
#   define GoApiCx(TYPE)        kInCx(TYPE)
#   define GoApiDx(TYPE)        kInDx(TYPE)
#   define GoApiClass
#else
#   define GoApiCppFx(TYPE)     kImportEx(TYPE)
#   define GoApiFx(TYPE)        kImportFx(TYPE)
#   define GoApiCx(TYPE)        kImportCx(TYPE)
#   define GoApiDx(TYPE)        kImportDx(TYPE)
#   define GoApiClass           kImportClass
#endif

#if defined (K_MSVC)
    //This disables warnings for templated std containers (std::vector, etc) not being dll exported.
    //Note: If you have a templated std container member variable used outside its owner class where the std library isnt available
    //      you will have *serious issues*. This shouldn't be a problem for us, as the GoApi libraries are
    //      internal use only, but still, be aware. Google "Visual Studio Warning C4251" for more info.
    #pragma warning( disable : 4251 )
    #pragma warning( disable : 4275 )
#endif

// Define the formats for text to indicate if the text is associated with a
// context or not, and if the text is translated or not.
// The defines are used to mark the text that a parsing script extracts and organizes
// for post-processing, such as sending the text out of translation.
// - _be(): belongs to global dictionary context and not translated.
// - bepgettext(C, T): belongs to context C and is translated.
// The parameters are:
// - "C" is the context.
// - "T" is the log text.
//
// Text translation is needed for:
// - user visible logs (most obvious use)
// - read-only and initial display names, parameter titles etc.
// Therefore these definitions are not defined in the GcLogger, but instead
// is defined here so they be used in non-logging cases, such as tool names etc.
#ifndef bepgettext
#define bepgettext(C, T)          (Go::GoGetText::GetInstance().PGetText(C, T))
#endif
#ifndef _be
#define _be(T)                    (Go::GoGetText::GetInstance().GetText(T))
#endif

namespace Go
{
    namespace Properties
    {
        class Structure;

        namespace Internal
        {
            class ArrayBase;
            class ReferenceArray;
            class ValueBase;
        }
    }
}

#endif
