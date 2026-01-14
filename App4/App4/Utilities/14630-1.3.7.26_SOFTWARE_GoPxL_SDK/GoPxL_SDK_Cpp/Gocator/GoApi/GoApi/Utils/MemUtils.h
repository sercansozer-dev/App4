/**@file    MemUtils.h
 * Memory related utilities and classes.
 */

#ifndef GOAPI_UTILS_MEMORY_H
#define GOAPI_UTILS_MEMORY_H

#include <GoApi/GoApiDef.h>
#include <kApi/kApiDef.h>

 //! namespace used for all GoApi related classes.
namespace Go
{
    class GoApiClass MemUtils
    {
    public:
        /**
         * Returns the last amount of contiguous memory successfully allocated in bytes.
         * 
         * @param   initialMemSize      Initial memory size to start with.
         * @param   stepMultiplier      Step multiplier to multiply memory size by every iteration.
         *                              Defaults to 2, if set to 0 then stepSize will be used instead
         * @param   stepSize            Step size to add to on every iteration
         *                              If set to 0, then it will not be used.
         * 
         * @remark                      Note that according to firesync, the call to allocate memory can succeed
         *                              but if CMA (Contiguos memory) is not enough, it will fall back into paged memory.
         *                              Any write to a new unused page may kill the process.
         */
        static void EstContigMem(k64u initialMemSize, k64f stepMultiplier = 2, k32u stepSize = 0);
    };

} // Namespace

#endif
