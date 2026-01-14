#ifndef MEMORYLEAKDETECTION_H
#define MEMORYLEAKDETECTION_H

#if defined(_DEBUG) && defined(_MSC_VER)
#define _CRTDBG_MAP_ALLOC
// TODO: How can we better obtain traceback of detected leaks?
// https://docs.microsoft.com/en-us/cpp/c-runtime-library/crtdbg-map-alloc?view=msvc-170
// The _CRTDBG_MAP_ALLOC should do it already.. but it doesn't seem to cover the following:
//    #define DEBUG_NEW new(_NORMAL_BLOCK, __FILE__, __LINE__)
//    #define new DEBUG_NEW
#include <crtdbg.h>
#endif
#include <GoApi/GoApiDef.h>


namespace Go {

enum
{
    NO_MEMORY_LEAKS = 0,
    MSVC_MEMORY_LEAKS = -2000,
    KAPI_MEMORY_LEAKS = -2001
};

#if defined(_DEBUG) && defined(_MSC_VER)
#   define GoMemState _CrtMemState
#else
#   define GoMemState size_t
#endif

/*
 * Encapsulates functionality memory leak detection in GoPxLService (GOS-4862).
 */
class GoApiClass MemoryLeakDetection
{
public:
    struct GoMemoryState
    {
        GoMemState memCheckpoint;
    };

    MemoryLeakDetection();

    ~MemoryLeakDetection();

    /**
     * Creates a (initial) system leak checkpoint.
     */
    void CreateSystemLeakCheckpoint();

    /**
     * Checks, if the system has memory leaks by comparing current state to last checkpoint.
     *
     * @return  true if memory leaks exist.
     */
    bool HasSystemLeaks() const;

    /**
     * Checks, if the kApi reports memory leaks.
     *
     * @return  true if memory leaks exist.
     */
    static bool HasKApiLeaks();

    /**
     * Print existing memory leaks (to console).
     * Cannot be called before system has been shutdown due to internal call of kApiLib_LeaksDetected.
     *
     * @return  Error code. Non-zero if memory leaks exist.
     */
    int PrintSystemLeaks() const;


private:
    GoMemoryState serviceMemState;
};

}; // Namespace

#endif
