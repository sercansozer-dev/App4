#include <GoApi/Exception.h>
#include <GoApi/Object.h>
#include <GoApi/Logging.h>
#include <kApi/Utils/kUtils.h>
#include <kApi/Utils/kBackTrace.h>
#include <kApi/Data/kArrayList.h>
#include <kApi/Data/kString.h>
#include <memory>

#define GOAPI_EXCEPTION_TAG         "*** EXCEPTION ***"
#define GOAPI_BACKTRACE_BEGIN_TAG   "*** BACKTRACE BEGIN ***"
#define GOAPI_BACKTRACE_END_TAG     "*** BACKTRACE END ***"

namespace Go
{
    //-------------------------------------------------------------------------------
    // GO NAMESPACE STATIC VARIABLES
    //-------------------------------------------------------------------------------

    // Only one handler for termination per application.
    static std::unique_ptr<GoStudio::ITerminateHandler*> terminateCallback = nullptr;

//-------------------------------------------------------------------------------
// GO NAMESPACE STATIC METHODS
//-------------------------------------------------------------------------------

// No Go namespace static methods currently defined.

//-------------------------------------------------------------------------------
// STATIC METHODS
//-------------------------------------------------------------------------------
void Exception::TerminateHandler() noexcept
{
    GoLogError("Termination called.");

    std::string exceptionString;
    std::string backTrace;

    if (auto exc = std::current_exception()) {
        // we have an exception
        try
        {
            rethrow_exception(exc); // throw to recognize the type
        }
        catch (const Go::Exception& e)
        {
            exceptionString = e.to_string();
            backTrace = e.BackTrace();
            std::stringstream ss;
            GoPrintException(ss, e);
        }
        catch (const std::exception & e)
        {
            exceptionString = e.what();
            std::stringstream ss;
            GoPrintException(ss, e);
        }
    }

    if (terminateCallback == nullptr)
    {
        exit(EXIT_FAILURE);
    }
    else
    {
        (*terminateCallback.get())->ReportTermination(exceptionString, backTrace);
    }
}

void Exception::SetTerminateCallback(GoStudio::ITerminateHandler* callback)
{
    if (callback == nullptr)
    {
        terminateCallback.reset();
    }
    else if (terminateCallback == nullptr)
    {
        terminateCallback = std::make_unique<GoStudio::ITerminateHandler*>(callback);
    }
    else
    {
        kAssert(0);
    }
}

kStatus Exception::ExceptionToStatus(const std::exception& e)
{
    // Printing is exception type dependent.
    if (const Go::Exception *ge = dynamic_cast<const Go::Exception*>(&e))
    {
        return ge->Status();
    }
    // GOS-7051 Check for std::bad_alloc exception.
    else if (kStrFindFirst(e.what(), "bad alloc") ||
             kStrFindFirst(e.what(), "bad_alloc")) // GOS-8683: Added due to failing unit tests on Linux X64.
    {
        return kERROR_MEMORY;
    }
    else
    {
        // TODO: We could actually attempt to resolve any of the other std::exception's here
        // if we wanted to.  ie. see all of them listed at https://en.cppreference.com/w/cpp/error/exception.
        return kERROR;
    }
}

void Exception::PrintException(std::stringstream& ss, const std::exception& e, std::size_t depth)
{
    // Printing is exception type dependent.
    if (const Go::Exception *ge = dynamic_cast<const Go::Exception*>(&e))
    {
        ss << ge->to_string(depth);
    }
    else
    {
        std::string indent = std::string(depth*2, ' ');
        ss << indent << GOAPI_EXCEPTION_TAG << std::endl;
        ss << indent << "Reason: " << e.what() << std::endl;
    }

    try {
        std::rethrow_if_nested(e);
    } catch (const std::exception& nested) {
        PrintException(ss, nested, depth + 1);
    }
}

void Exception::PrintBackTrace(std::stringstream& ss, const std::exception& e)
{
    // Backtrace availability is type dependent.
    if (const Go::Exception *ge = dynamic_cast<const Go::Exception*>(&e))
    {
        ss << ge->BackTrace();
    }
}

void Exception::LogException(const std::exception& e, std::size_t depth)
{
    // Logging is exception type dependent.
    if (const Go::Exception *ge = dynamic_cast<const Go::Exception*>(&e))
    {
        ge->LogException(depth);
    }
    else
    {
        std::string indent = std::string(depth*2, ' ') ;
        Logging::Go_Log(-1, "", GO_LOG_OPTION_GOCAPP_ERROR, "",
            "%s" GOAPI_EXCEPTION_TAG, indent.c_str());
        Logging::Go_Log(-1, "", GO_LOG_OPTION_GOCAPP_ERROR, "",
            "%sReason: %s", indent.c_str(), e.what());
    }

    try {
        std::rethrow_if_nested(e);
    } catch (const std::exception& nested) {
        LogException(nested, depth + 1);
    }
}

void Exception::LogBackTrace(const std::exception& e)
{
    // Backtrace availability is type dependent.
    if (const Go::Exception *ge = dynamic_cast<const Go::Exception*>(&e))
    {
        ge->LogBackTrace();
    }
}

//-------------------------------------------------------------------------------
// EXCEPTION CLASS
//-------------------------------------------------------------------------------
Exception::Exception() :
        Exception(-1, nullptr, kERROR, nullptr, nullptr)
{
}

Exception::Exception(kStatus status) :
        Exception(-1, nullptr, status, nullptr, nullptr)
{
}

Exception::Exception(const char* message) :
        Exception(-1, nullptr, kERROR, nullptr, message)
{
}
//
Exception::Exception(const std::string& message) :
        Exception(-1, nullptr, kERROR, nullptr, message.c_str())
{
}
//
Exception::Exception(kStatus status, const std::string& message) :
        Exception(-1, nullptr, status, nullptr, message.c_str())
{
}

//  NOTE: to populate the source but not the message, 
//  use a form of:
//    Exception(status, source, nullptr);
Exception::Exception(int line, const char* fileName, kStatus status,
                     const char* source, const char* format, ...)
{
    kText256 text; 
    kVarArgList argList;

    this->status = status;
    this->source = (source == nullptr ? "" : source);
    this->line = line;
    this->fileName = fileName == nullptr ? "" : std::string(fileName);

    if (format != nullptr)
    {
        kVarArgList_Start(argList, format);
        {
            // GOS-13157: Use positional parameters to ensure translation correctness and avoid crashes
            Logging::HandleValidateAndSprintf(text, sizeof(text), format, argList);
        }
        kVarArgList_End(argList);

        this->message = text;
    }
    else
    {
        this->message = kStatus_Name(status);
    }

    // Store the firesync backtrace for later.
    // Skip level = 2, this function and next
    GetBackTrace(2);
}

kStatus Exception::Status() const
{
    return status;
}

const char* Exception::Source() const noexcept
{
    return source.c_str();
}

const char* Exception::what() const noexcept
{
    return message.c_str();
}

void Exception::GetBackTrace(size_t skip)
{
// Backtrace depends on debug symbols that are only present
// in debug builds... don't attempt to spit out the backtrace unless needed.
#ifdef _DEBUG
    Go::Object<kBackTrace> trace;
    Go::Object<kArrayList> lines;
    kSize i; 

    GoTest(kBackTrace_Construct(trace.Ref(), kAlloc_App())); 

    GoTest(kBackTrace_Capture(trace, (kSize)skip)); 
    GoTest(kBackTrace_Describe(trace, &lines, kAlloc_App()));
        
    for (i = 0; i < kArrayList_Count(lines); ++i)
    {
        backTraceList.emplace_back(kString_Chars(kArrayList_AsT(lines, i, kString)));
    }
#endif
}

std::string Exception::to_string(size_t depth) const
{
    std::stringstream ss;

    auto indent = std::string(depth*2, ' ');

    ss << indent << GOAPI_EXCEPTION_TAG << std::endl;
    if (this->line >= 0 && this->fileName != "")
    { 
        // For macro'd exceptions thrown with file and line context.
        // Legacy exceptions are thrown without file and line number context (to be converted).
        ss << indent << "Location: " << this->fileName << "(" << this->line << ")" << std::endl;
    }
    ss << indent << "Source: " << this->source << std::endl;
    ss << indent << "Status: " << this->status << "(" << kStatus_Name(this->status) << ")" << std::endl;
    ss << indent << "Reason: " << this->message << std::endl;

    return ss.str();
}


std::string Exception::BackTrace() const
{
    std::stringstream ss;

// Backtrace depends on debug symbols that are only present
// in debug builds... don't attempt to spit out the backtrace unless needed.
#ifdef _DEBUG
    ss << GOAPI_BACKTRACE_BEGIN_TAG << std::endl;
    for (auto backTrace : this->backTraceList)
    {
        // Add some indent to the backtraces.
        ss << "    " << backTrace.c_str() << std::endl;
    }
    ss << GOAPI_BACKTRACE_END_TAG << std::endl;
#endif
    
    return ss.str();
}

void Exception::LogException(size_t depth) const
{
    auto indent = std::string(depth*2, ' ');

    // The below will kLog even in release mode -- with the file and line number.
    // It will also be logged to the VS Output window if running in debug mode and a debugger is attached.
    Logging::Go_Log(line, fileName.c_str(), GO_LOG_OPTION_GOCAPP_ERROR, source.c_str(),
        "%s" GOAPI_EXCEPTION_TAG , indent.c_str());
    if (this->line >= 0 && this->fileName != "")
    {
        // For legacy exceptions thrown without file and line number context (to be converted).
        Logging::Go_Log(line, fileName.c_str(), GO_LOG_OPTION_GOCAPP_ERROR, source.c_str(),
            "%sLocation: %s(%d)", indent.c_str(), fileName.c_str(), line);
    }
    Logging::Go_Log(line, fileName.c_str(), GO_LOG_OPTION_GOCAPP_ERROR, source.c_str(),
        "%sStatus: %d(%s)", indent.c_str(), this->status, kStatus_Name(this->status));
    Logging::Go_Log(line, fileName.c_str(), GO_LOG_OPTION_GOCAPP_ERROR, source.c_str(),
        "%sReason: %s", indent.c_str(), this->message.c_str());
}

void Exception::LogBackTrace() const
{
// Backtrace depends on debug symbols that are only present
// in debug builds... don't attempt to spit out the backtrace unless needed.
#ifdef _DEBUG
    Logging::Go_Log(line, fileName.c_str(), GO_LOG_OPTION_GOCAPP_ERROR, source.c_str(),
        GOAPI_BACKTRACE_BEGIN_TAG);

    for (auto backTrace : this->backTraceList)
    {
        // Add some indent to the backtraces.
        Logging::Go_Log(line, fileName.c_str(), GO_LOG_OPTION_GOCAPP_ERROR, source.c_str(),
            "    %s", backTrace.c_str());
    }

    Logging::Go_Log(line, fileName.c_str(), GO_LOG_OPTION_GOCAPP_ERROR, source.c_str(),
        GOAPI_BACKTRACE_END_TAG);
#endif
}

} // end namespace
