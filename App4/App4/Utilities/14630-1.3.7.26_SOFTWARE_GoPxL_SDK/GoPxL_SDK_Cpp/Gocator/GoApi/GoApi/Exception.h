#ifndef GOAPI_EXCEPTION_H
#define GOAPI_EXCEPTION_H

#include <GoApi/Logging.h>
#include "ITerminateHandler.h"
#include <stdexcept>
#include <exception>
#include <vector>
#include <sstream>
#include <string>

namespace Go
{

/**
 * Custom exception class that keeps additional information about an exception.
 */
class GoApiClass Exception : public std::exception
{
public:
    /**
     * Called by the C++ runtime when the program cannot continue
     * due to uncaught exceptions, exceptions during destructors, etc.
     * See https://en.cppreference.com/w/cpp/error/terminate.
     */
    static void TerminateHandler() noexcept;

    /**
     * Resolves an exception to a kStatus.
     * 
     * @param   e       The exception to resolve (and unwrap).
     * @return          The resolved kStatus error code.
     */
    static kStatus ExceptionToStatus(const std::exception& e);

    /**
     * Print exception to a stream, unwrapping nested execptions along the way.
     * 
     * @param   ss      Output string stream.
     * @param   e       The exception to print (and unwrap).
     * @param   depth   Depth of nested exceptions (used for indentations).
     */
    static void PrintException(std::stringstream& ss,
        const std::exception& e, size_t depth = 0);

    /**
     * Print exception backtrace to a stream.
     * 
     * @param   ss      Output string stream.
     * @param   e       The exception to print backtrace for.
     */
    static void PrintBackTrace(std::stringstream& ss, const std::exception& e);

    /**
     * Log exception using GoApi logging, unwrapping nested execptions along the way.
     * 
     * @param   e       The exception to log (and unwrap).
     * @param   depth   Depth of nested exceptions (used for indentations).
     */
    static void LogException(const std::exception& e, std::size_t depth = 0);

    /**
     * Log exception backtrace using GoApi logging.
     * 
     * @param   e       The exception to log backtrace for.
     */
    static void LogBackTrace(const std::exception& e);

    /**
     * Exception class constructor.
     */
    Exception();

    /**
     * Exception class constructor.
     *
     * @param   status      kStatus error code.
     */
    Exception(kStatus status);

    /**
     * Exception class constructor.
     *
     * @param   message     Contextual message.
     */
    Exception(const char* message);

    /**
     * Exception class constructor.
     *
     * @param   message     Contextual message.
     */
    Exception(const std::string& message);

    /**
     * Exception class constructor.
     *
     * @param   status      kStatus error code.
     * @param   message     Contextual message.
     */
    Exception(kStatus status, const std::string& message);

    /**
     * Exception class constructor.
     *
     * @param   status      kStatus error code.
     * @param   source      Source of exception (ie. function/method).
     * @param   format      Formatted contextual message with args.
     */
    Exception(kStatus status, const char* source, const char* format, ...);

    /**
     * Exception class constructor.
     *
     * @param   line        Line number (usually via __LINE__ macro).
     * @param   fileName    Source code file (usually via __FILE__ macro).
     * @param   status      kStatus error code.
     * @param   source      Source of exception (ie. function/method).
     * @param   format      Formatted contextual message with args.
     */
    Exception(int line, const kChar* fileName, kStatus status, const char* source, const char* format, ...);

    /**
     * Retrieves the kStatus value associated with the exception.
     *
     * @return              kStatus error code.
     */
    kStatus Status() const;

    /**
     * Retrieves the source associated with the exception.
     *
     * @return              Source of exception (ie. function/method).
     */
    const char* Source() const noexcept;

    /**
     * Retrieves explanation/reason associated with the exception.
     *
     * @return              Explanation/reason associated with the exception.
     */
    const char* what() const noexcept override;

    /**
     * Retrieves exception as a pretty-printed multi-line string.
     * 
     * @param   depth       Depth of nested exceptions (used for indentations).
     */
    std::string to_string(size_t depth = 0) const;

    /**
     * Retrieves exception backtrace.
     * 
     * @return              Exception backtrace as a multi-line string.
     */
    std::string BackTrace() const;

    /**
     * Log exception using GoApi logging, unwrapping nested exceptions along the way.
     * 
     * @param   depth       Depth of nested exceptions (used for indentations).
     */
    void LogException(size_t depth = 0) const;

    /**
     * Log exception backtrace using GoApi logging, unwrapping nested exceptions along the way.
     */
    void LogBackTrace() const;

    /**
     * Set callback for post TerminateHandler call actions.
     *
     * The terminate handler is thread specific. It must be set in every thread.
     * Use GoSetTerminateHandler() to set the handler.
     *
     * @param   callback    Post TerminateHandler call action handler.
     */
    static void SetTerminateCallback(GoStudio::ITerminateHandler* callback);

protected:
    int line;
    std::string fileName;
    kStatus status;
    std::string source;
    std::string message;
    std::vector<std::string> backTraceList;

    void GetBackTrace(size_t skip);
};

// Do not add this function; it's too tempting to use this in place
// of a return statement, causing warnings.
// Making this a macro is a hack that should be avoided (namespacing issues).
// Just throw the exception directly.
//inline void Throw(kStatus status)
//{
//    throw Exception(status);
//}
}

#define GoBeginExceptionHandler()       \
    try                                 \
    {

#define GoEndExceptionHandler()         \
    }                                   \
    catch (const Go::Exception& e)      \
    {                                   \
        GoLogException(e);              \
        return e.Status();              \
    }                                   \
    catch (const std::exception& e)     \
    {                                   \
        GoLogException(e);              \
        return kERROR;                  \
    }

//---------------------------------------------------------------------------------------------------------
// Use of the following macros will pass along the file and line number where the exception occurred,
// to be kLog'd.  Also if running in Visual Studio with a debugger attached, the file and line number will
// be formatted as a context-sensitive clickable line.
//---------------------------------------------------------------------------------------------------------

/**
 * Helper macro to rethrow an exception (wrapping the "current" exception within).
 *
 * @param   format      Contextual message.
 *
 * @remarks             Uses the "exception dispatcher" idiom to disambiguate the "current" exception
 *                      and assign a kStatus.  See https://isocpp.org/wiki/faq/exceptions#what-to-catch.
 *
 * @remarks             Propagates the kStatus from the inner exception, or kERROR if inner exception
 *                      is std::exception.
 *
 * @remarks             NOTE: This macro should only be used within a catch block.
 */
#define GoRethrow(format, ...)                                                                          \
    do                                                                                                  \
    {                                                                                                   \
        try                                                                                             \
        {                                                                                               \
            throw;                                                                                      \
        }                                                                                               \
        catch (const Go::Exception& ge)                                                                 \
        {                                                                                               \
            /* Throws an exception that combines both the currently handled exception and e.  */        \
            std::throw_with_nested(Go::Exception(__LINE__, __FILE__,                                    \
                ge.Status(), __FUNCTION__, format, ## __VA_ARGS__));                                    \
        }                                                                                               \
        catch (const std::exception&)                                                                   \
        {                                                                                               \
            /* Throws an exception that combines both the currently handled exception and e.  */        \
            std::throw_with_nested(Go::Exception(__LINE__, __FILE__,                                    \
                kERROR, __FUNCTION__, format, ## __VA_ARGS__));                                         \
        }                                                                                               \
    } while (0)

/**
 * Helper macro to rethrow an exception (wrapping the "current" exception within).
 *
 * @param   status      kStatus error code to override.
 * @param   format      Contextual message.
 *
 * @remarks             Simliar to GoRethrow() macro except that the user can explicitly
 *                      specify the kStatus error code to use, overriding/wrapping the inner exception.
 *
 * @remarks             NOTE: This macro should only be used within a catch block.
 */
#define GoRethrowStatus(status, format, ...)                                                            \
    do                                                                                                  \
    {                                                                                                   \
        /* Throws an exception that combines both the currently handled exception and e.  */            \
        std::throw_with_nested(Go::Exception(__LINE__, __FILE__,                                        \
            status, __FUNCTION__, format, ## __VA_ARGS__));                                             \
    } while (0)

/**
 * Helper macro to throw an exception.
 *
 * @param   status      kStatus error code.
 */
#define GoThrow(status)                                                                                 \
    do                                                                                                  \
    {                                                                                                   \
        throw Go::Exception(__LINE__, __FILE__, status, __FUNCTION__, nullptr);                         \
    } while (0)

/**
 * Helper macro to rethrow an exception. Forwards the exception to its external level.
 * This can be used to make it clear we want to pass the exception along after logging.
 *
 */
#define GoResumeThrow()                                                                                 \
    do                                                                                                  \
    {                                                                                                   \
        throw;                                                                                          \
    } while (0)

/**
 * Helper macro to throw an exception with a contextual message.
 *
 * @param   status      kStatus error code.
 * @param   format      Contextual message.
 */
#define GoThrowMsg(status, format, ...)                                                                 \
    do                                                                                                  \
    {                                                                                                   \
        throw Go::Exception(__LINE__, __FILE__, status, __FUNCTION__, format, ## __VA_ARGS__);          \
    } while (0)

/**
 * Helper macro to conditionally throw an exception.
 *
 * @param   condition   Exception is thrown if condition is met.
 * @param   status      kStatus error code.
 */
#define GoThrowIf(condition, status)                                                                    \
    do                                                                                                  \
    {                                                                                                   \
        if (condition)                                                                                  \
        {                                                                                               \
            throw Go::Exception(__LINE__, __FILE__, status, __FUNCTION__, nullptr);                     \
        }                                                                                               \
    } while (0)

/**
 * Helper macro to conditionally throw an exception with a contextual message.
 *
 * @param   condition   Exception is thrown if condition is met.
 * @param   status      kStatus error code.
 * @param   format      Contextual message.
 */
#define GoThrowMsgIf(condition, status, format, ...)                                                    \
    do                                                                                                  \
    {                                                                                                   \
        if (condition)                                                                                  \
        {                                                                                               \
            throw Go::Exception(__LINE__, __FILE__, status, __FUNCTION__, format, ## __VA_ARGS__);      \
        }                                                                                               \
    } while (0)

/**
 * Helper macro to test kStatus and conditionally throw an exception if status is not kOK.
 *
 * @param   EXPRESSION  kStatus error code or expression returning such.
 *
 * @remarks  Example usage see below:
 * @code {.cpp}
 * GoTest(kApiLib_Construct(&kApiLib)); // Throws if kApiLib construction fails.
 * GoTest(kERROR);  // Always throws.
 * GoTest(kOK);  // Never throws.
 * @endcode                        
 */
#define GoTest(EXPRESSION)                                                                              \
    do                                                                                                  \
    {                                                                                                   \
        kStatus GoTest_temp = (kStatus)(EXPRESSION);                                                    \
        GoThrowIf(kIsError(GoTest_temp), GoTest_temp);                                                  \
    } while (0)

/**
 * Helper macro to test kStatus and conditionally throw an exception with message if status is not kOK.
 *
 * @param   EXPRESSION  kStatus error code or expression returning such.
 * @param   format      Contextual message.
 *
 * @remarks  Example usage see below:
 * @code {.cpp}
 * GoTestMsg(kApiLib_Construct(&kApiLib), "kApiLib construction failed.");
 * GoTest(kERROR, "This always throws.);
 * GoTest(kOK, "This should never throw.);
 * @endcode                        
 */
#define GoTestMsg(EXPRESSION, format, ...)                                                              \
    do                                                                                                  \
    {                                                                                                   \
        kStatus GoTest_temp = (kStatus)(EXPRESSION);                                                    \
        GoThrowMsgIf(kIsError(GoTest_temp), GoTest_temp, format, ## __VA_ARGS__);                       \
    } while (0)

/**
 * Helper function to print an exception (and backtrace if available).
 * GOS-3902: changed from macro to inline function because 
 * ss must be copied to avoid potential repeat function calls, but stringstream is non-copyable.
 *
 * @param   ss          Output string stream.
 * @param   e           The exception to print (and unwrap).
 */
inline void GoPrintException(std::stringstream& ss, const std::exception& e)
{
    Go::Exception::PrintException(ss, e);
    Go::Exception::PrintBackTrace(ss, e);
}

/**
 * Helper macro to log an exception (and backtrace if available).
 *
 * @param   e           The exception to log (and unwrap).
 */
#define GoLogException(e)                                                                               \
    do                                                                                                  \
    {                                                                                                   \
        const std::exception& GoLogException_temp = (const std::exception&)(e);                         \
        GoLogError("EXCEPTION THROWN: ");                                                               \
        Go::Exception::LogException(GoLogException_temp);                                               \
        Go::Exception::LogBackTrace(GoLogException_temp);                                               \
    } while (0)

/**
 * Helper macro to log an exception message (and backtrace if available).
 *
 * @param   e           The exception to log (and unwrap).
 * @param   format      Contextual message.
 */
#define GoLogExceptionMsg(e, format, ...)                                                               \
    do                                                                                                  \
    {                                                                                                   \
        const std::exception& GoLogException_temp = (const std::exception&)(e);                         \
        GoLogError(format, ## __VA_ARGS__);                                                             \
        Go::Exception::LogException(GoLogException_temp);                                               \
        Go::Exception::LogBackTrace(GoLogException_temp);                                               \
    } while (0)

/**
* Helper macro to set a terminate handler.
*
*/
#define GoSetTerminateHandler()                                                                         \
    do                                                                                                  \
    {                                                                                                   \
        std::set_terminate([]()                                                                         \
        {                                                                                               \
            Go::Exception::TerminateHandler();                                                          \
            std::abort();                                                                               \
        });                                                                                             \
    } while (0)

#endif
