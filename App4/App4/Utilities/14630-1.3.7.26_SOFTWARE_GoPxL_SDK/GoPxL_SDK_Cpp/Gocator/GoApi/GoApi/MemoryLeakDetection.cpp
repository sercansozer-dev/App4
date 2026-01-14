#include "MemoryLeakDetection.h"
#include <kApi/kApi.h>

#if defined(K_WINDOWS)
#include <windows.h>    // For DebugApi.h::IsDebuggerPresent()
#endif

namespace Go {


MemoryLeakDetection::MemoryLeakDetection(): serviceMemState()
{
}

MemoryLeakDetection::~MemoryLeakDetection()
{
}

#if defined(_DEBUG) && defined(_MSC_VER)

void MemoryLeakDetection::CreateSystemLeakCheckpoint()
{
    _CrtMemCheckpoint(&serviceMemState.memCheckpoint);
}

bool MemoryLeakDetection::HasSystemLeaks() const
{
    GoMemState now, diff;

    _CrtMemCheckpoint(&now);

    return _CrtMemDifference(&diff, &serviceMemState.memCheckpoint, &now);
}

bool MemoryLeakDetection::HasKApiLeaks()
{
    return kApiLib_LeaksDetected() > 0;
}

int MemoryLeakDetection::PrintSystemLeaks() const
{
    if (HasSystemLeaks())
    {
        int reportMode = _CRTDBG_MODE_FILE;

#if defined(K_WINDOWS)
        if (IsDebuggerPresent())
        {
            // this also outputs to the Visual Studio Debugger Output window if attached.
            reportMode |= _CRTDBG_MODE_DEBUG;
        }
#endif

        _CrtSetReportMode(_CRT_WARN, reportMode);
        _CrtSetReportFile(_CRT_WARN, _CRTDBG_FILE_STDOUT);
        _CrtSetReportMode(_CRT_ERROR, reportMode);
        _CrtSetReportFile(_CRT_ERROR, _CRTDBG_FILE_STDOUT);
        _CrtSetReportMode(_CRT_ASSERT, reportMode);
        _CrtSetReportFile(_CRT_ASSERT, _CRTDBG_FILE_STDOUT);

        _CrtMemDumpAllObjectsSince(&serviceMemState.memCheckpoint);
        return MSVC_MEMORY_LEAKS;
    }

    if (HasKApiLeaks())
    {
        return KAPI_MEMORY_LEAKS;
    }

    return NO_MEMORY_LEAKS;
}

#elif defined(_DEBUG) // For Linux debug builds

void MemoryLeakDetection::CreateSystemLeakCheckpoint()
{
}

bool MemoryLeakDetection::HasSystemLeaks() const
{
    return false;
}

bool MemoryLeakDetection::HasKApiLeaks()
{
    return kApiLib_LeaksDetected() > 0;
}

int MemoryLeakDetection::PrintSystemLeaks() const
{
    return HasKApiLeaks() ? KAPI_MEMORY_LEAKS: NO_MEMORY_LEAKS;
}

#else  // For Linux release builds

void MemoryLeakDetection::CreateSystemLeakCheckpoint()
{
}

bool MemoryLeakDetection::HasSystemLeaks() const
{
    return false;
}

bool MemoryLeakDetection::HasKApiLeaks()
{
    return false;
}

int MemoryLeakDetection::PrintSystemLeaks() const
{
    return NO_MEMORY_LEAKS;
}

#endif

} // namespace
