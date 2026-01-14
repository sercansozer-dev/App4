/**@file    ITerminateHandler.h
 * Declares the ITerminateHandler interface.
 */
#ifndef GOSTUDIO_ITERMINATEHANDLER_H
#define GOSTUDIO_ITERMINATEHANDLER_H

#include <string>

namespace GoStudio
{

/**
 * Defines the TerminateHandler interface.
 *
 * This interface enables handler for the post TerminateHandler call actions.
 */
class ITerminateHandler
{
public:
    virtual ~ITerminateHandler() {};

    /**
     * Reports the exception back to the main handler.
     *
     * @param exception     Exception reason.
     * @param callTrace     Call trace printout.
     */
    virtual void ReportTermination(const std::string& exception, const std::string& callTrace) = 0;
};

} // namespace

#endif
