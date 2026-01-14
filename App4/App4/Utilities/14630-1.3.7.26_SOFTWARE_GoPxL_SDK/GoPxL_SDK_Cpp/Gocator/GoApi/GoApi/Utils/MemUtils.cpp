#include "MemUtils.h"
#include <GoApi/Object.h>
#include <kApi/Io/kMemory.h>

namespace Go
{
    //-----------------------------------------------------------------------------
    // Memory related utility functions.
    //-----------------------------------------------------------------------------

    void MemUtils::EstContigMem(k64u initialMemSize, k64f stepMultiplier, k32u stepSize)
    {
        GoThrowMsgIf((stepMultiplier == 0 && stepSize == 0), kERROR_PARAMETER,
            "Either stepMultiplier or stepSize must be non-zero.");
        GoThrowMsgIf((stepMultiplier != 0 && stepSize != 0), kERROR_PARAMETER,
            "Only one of stepMultiplier or stepSize can be non-zero");
        GoThrowMsgIf((initialMemSize == 0), kERROR_PARAMETER,
            "Initial memory size can not be zero");

        Go::Object<kMemory> memory;
        bool memoryLimitReached = false;
        GoTest(kMemory_Construct(memory.Ref(), kNULL));

        do
        {
            try
            {
                GoLogInfo("Trying to allocate contiguous memory of size %llu bytes", initialMemSize);
                GoTest(kMemory_Allocate(memory, initialMemSize));
                if (stepSize == 0)
                {
                    initialMemSize = (k64u)(kMemory_Capacity(memory) * stepMultiplier);
                }
                else if (stepMultiplier == 0)
                {
                    initialMemSize = (k64u)(kMemory_Capacity(memory) + stepSize);
                }
            }
            catch (const Go::Exception& e)
            {
                memoryLimitReached = true;
                if (e.Status() == kERROR_MEMORY)
                {
                    GoLogInfo("Could not allocate %llu bytes of contiguous memory.", initialMemSize);
                }
                else
                {
                    GoRethrow("kMemory_Reserve failed for %llu bytes.", initialMemSize);
                }
            }
        } while (!memoryLimitReached);
    }

} // Namespaces
