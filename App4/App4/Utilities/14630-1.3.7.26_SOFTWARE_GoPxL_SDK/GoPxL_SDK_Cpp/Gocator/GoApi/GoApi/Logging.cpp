#include <GoApi/Logging.h>
#include <kApi/Utils/kUtils.h>
#include <kApi/Utils/kDateTime.h>

#if defined(K_WINDOWS)
#include <windows.h>    // For DebugApi.h::IsDebuggerPresent() anq DebugApi.h::OutputDebugStringA()
                        // TBD: This is normally picked up automatically by kUtils.h -> kApiDef.h -> kApiCfg.h,
                        // but for some reason its not automatically being picked up.
#endif

#define GOAPILOG_USEC_PER_MSEC 1000L

namespace
{
// We need these modified levels to evaluate trace and debug levels because platform needs the log levels to be 0x8 and 0x4
constexpr k32s GO_LOG_LEVEL_TRACE = -2;
constexpr k32s GO_LOG_LEVEL_DEBUG = -1;

/**
    * Helper function to determine whether the option is greater than or equal to the min level.
    * GOS-10430: Accomodate newly added trace and debug logs being 0x4 and 0x8 respectively
    *
    * @param option    The provided level option.
    * @param minLevel  The minimum level for option to be greater than or equal to.
    */
bool GoLogIsLevelGE(kLogOption option, k32s minLevel)
{
    k32s logLevel = 0;
    if (GoLogIsDebug(option))
    {
        logLevel = GO_LOG_LEVEL_DEBUG;
    }
    else if (GoLogIsTrace(option))
    {
        logLevel = GO_LOG_LEVEL_TRACE;
    }
    else
    {
        logLevel = (k32s)((option)&GO_LOG_OPTION_LEVEL_MASK);
    }

    return logLevel >= minLevel;
}

} // namespace

namespace Go
{

//We don't set the defaults here, as they are application specific and are set in the higher level GoPxL application.
k32s Logging::platformMinLogLevel = GO_LOG_OPTION_LEVEL_INFO;
k32s Logging::appMinLogLevel = GO_LOG_OPTION_LEVEL_INFO;
k32s Logging::userMinLogLevel = GO_LOG_OPTION_LEVEL_INFO;
k32s Logging::logFormat = kDATE_TIME_FORMAT_SIMPLE;

Logging::LogCounters Logging::logCounters = { 0 };

GoApiCppFx(kStatus) Logging::Go_Log(int line, 
                                          const kChar* fileName, 
                                          kLogOption options, 
                                          const kChar* source, 
                                          const kChar* format, ...)
{
    // Skip logging as early as possible.
    if (SkipLog(options))
    {
        return kOK;
    }
    kLogArgs args;
    // GOS-2563 - Send in epoch time in ms for FE display.
    args.dateTime = kDateTime_Now();
    args.upTime = (k64u)((args.dateTime - kDATE_TIME_EPOCH_UNIX) / GOAPILOG_USEC_PER_MSEC); 
    args.options = options;
    args.source = source;
    // Won't be exactly the same -- but at least pretty close for the VS Output.
    // Some json can be quite long.
    kChar logMsgBuffer[512];

    if (format != nullptr)
    {
        // For now, we process the kVarArgList in here, but if we
        // need to in the future, we may need to alter the signature and pass along
        // kVarArgList to kLog().
        kVarArgList argList;
        kVarArgList_Start(argList, format);
        {
            if (GoLogIsGocUser(options))
            {
                // GOS-3678 - Use the final log message with the arguments already parsed in because the
                //            ticket revealed potential problems when passing arguments to the FE client.
                //      TODO - Retest GOS-3678 as part of GOS-13258.

                // GOS-13157 - As we're already using the final log message with the arguments parsed in,
                // use positional parameters instead of stripping them. 
                Logging::HandleValidateAndSprintf(logMsgBuffer, kCountOf(logMsgBuffer), format, argList);
            }
            else
            {
                // TODO: GOS-13258: profile the performance of Logging::HandleValidateAndSprintf() and adopt it for all logging...
                // If non-user system message, assume that positional parameters are not being used.
                kCheck(kStrPrintvf(logMsgBuffer, kCountOf(logMsgBuffer), format, argList));
            }
        }
        kVarArgList_End(argList);
    }
    else
    {
        kCheck(kStrPrintf(logMsgBuffer, kCountOf(logMsgBuffer), ""));
    }

    //----------------------------------------------------------------------
    // Populate the message to be logged by kLog and callbacks.
    //----------------------------------------------------------------------
    args.message = logMsgBuffer;

    // GOS-4608: This is a bit of a workaround, but log to firesync per new 
    //           logging options and with source to trigger kApiLib_InvokeLogListeners(),
    //           (of which GcLogger is a listener for public/private log handling).
    //           This is to ensure that kApiLogStatic::lock is always acquired 
    //           BEFORE GcLoggerClass::lock, and so that logging works for both
    //           Go_Log() based macros, and for direct calls to kLogf().
    kCheck(kLog(args.options, args.source, args.message));

    // GOS-7745: Increment log counters once we reach here and actually log the message.
    CountLog(args.options);

    //----------------------------------------------------------------------
    // Perform logging if VS debugger is attached.
    //----------------------------------------------------------------------
#if defined(K_WINDOWS) && defined(_DEBUG)
    kChar presBuffer[1024];
    kCheck(FormatLogBuffer(args.dateTime, args.options, args.source, args.message,
        presBuffer, kCountOf(presBuffer)));

    char dBuffer[1024];
    // This logs to the Visual Studio Output window using a format that is hot-clickable 
    // for ease in debugging.
    if (IsDebuggerPresent())
    {
        kCheck(kStrPrintf(dBuffer, kCountOf(dBuffer), "%s(%d) : %s\n", fileName, line, presBuffer));
        OutputDebugStringA(dBuffer);
    }
#endif

    return kOK;
}

// This is based off of xkFormatLogOption().  We may want FireSync to provide access to  this for us.
GoApiCppFx(kStatus) Logging::FormatLogOption(kLogOption options, kChar* buffer, kSize capacity)
{
    const kChar* value = "";

    if (options & kLOG_OPTION_PLATFORM)
    {
        if (options & kLOG_OPTION_TRACE)            value = "Trc Plt";
        else if (options & kLOG_OPTION_DEBUG)       value = "Dbg Plt";
        else if (options & kLOG_OPTION_WARNING)     value = "Wrn Plt";
        else if (options & kLOG_OPTION_ERROR)       value = "Err Plt";
        else                                        value = "Inf Plt";
    }
    else if (options > 0) // application
    {
        if (options & kLOG_OPTION_TRACE)            value = "Trc App";
        else if (options & kLOG_OPTION_DEBUG)       value = "Dbg App";
        else if (options & kLOG_OPTION_WARNING)     value = "Wrn App";
        else if (options & kLOG_OPTION_ERROR)       value = "Err App";
        else                                        value = "Inf App";
    }

    kCheck(kStrCopy(buffer, capacity, value)); 

    return kOK;
}

// This is based off of xkFormatLogMessage().  We may want FireSync to provide access to  this for us.
//
// From FSS-1301 Sprint Notes:
//
//    [2000-01-01 00:00:00 UTC+8:00] Err App:0x03100040.kHxCamera: This is the log message.
//
//Where
//  Inf     Information
//  Err     Error
//  Wrn     Warning
//  Plt     Platform-specific
//  App     Application-specific
//  0xXXXX  Upper 32-bits of application options
//  Source  Source
//
GoApiCppFx(kStatus) Logging::FormatLogBuffer(kDateTime dateTime, kLogOption options, const kChar* source, const kChar* message,
                                          kChar* buffer, kSize capacity)
{
    kText64 logOptions = { 0 };
    kText64 dateTimeText = { 0 };
    kBool isSenderEmpty = (source == kNULL || kStrEquals("", source));
    k32u applicationOptions = (options & 0xFFFFFFFF00000000) >> 32;    // only upper 32-bits are preserved
    
    kCheck(FormatLogOption(options, logOptions, kCountOf(logOptions)));
    kCheck(kDateTime_Format(dateTime, logFormat, dateTimeText, kCountOf(dateTimeText)));

    if (!kStrEquals("", logOptions))
    {
        if (!isSenderEmpty)
        {
            if (applicationOptions != 0)
            {
                kStrPrintf(buffer, capacity, "[%s] %s.%s (0x%X): %s", dateTimeText, logOptions, source, applicationOptions, message);
            }
            else
            {
                kStrPrintf(buffer, capacity, "[%s] %s.%s: %s", dateTimeText, logOptions, source, message);
            }
        }
        else
        {
            if (applicationOptions != 0)
            {
                kStrPrintf(buffer, capacity, "[%s] %s (0x%X): %s", dateTimeText, logOptions, applicationOptions, message);
            }
            else
            {
                kStrPrintf(buffer, capacity, "[%s] %s: %s", dateTimeText, logOptions, message);
            }
        }
    }
    else
    {
        if (!isSenderEmpty)
        {
            kStrPrintf(buffer, capacity, "[%s] %s: %s", dateTimeText, source, message);
        }
        else
        {
            kStrPrintf(buffer, capacity, "[%s] %s", dateTimeText, message);
        }
    }

    return kOK;
}

GoApiCppFx(kStatus) Logging::ValidateAndSPrintf(kChar* dest, kSize capacity, const kChar* format, kVarArgList argList)
{
    kCheckArgs(capacity > 0); 
    if (format == nullptr)
    {
        return kERROR_PARAMETER;
    }

    // This function fails optional flags/width/precision format specifiers, or when there are 10+ format specifiers in a string, even though they're "correct".
    // Accounting for them would make the function even more complex then it already is, and could open it up to vulnerabilities. 
    // Given that we don't use either of those cases in our code, they're omitted from this function. 
    const kChar* validSpecifiers = "diuoxXfFeEgGaAcspn";

    // Use this to catch strings with both positional and non-positional args
    int positional = -1;
    bool validationFailed = false;

    for (const kChar* ptr = format; *ptr != '\0'; ptr++)
    {
        if (*ptr == '%')
        {
            ptr++;
            if (*ptr == '\0')
            {
                validationFailed = true;
                break;
            }
            if (*ptr == '%')
            {
                // Two %% next to eachother is OK.
                continue;
            }
            if (strchr("lhz", *ptr))
            {
                // Non-positional length specifier
                if (positional == 1)
                {
                    // Mixing positional and non-positional specifiers causes a crash. 
                    validationFailed = true;
                    break;
                }
                positional = 0;
                if (*ptr == 'l')
                {
                    ptr++;
                    if (*ptr == 'l')
                        ptr++; // ll
                }
                else if (*ptr == 'h')
                {
                    ptr++;
                    if (*ptr == 'h')
                        ptr++; // hh
                }
                else if (*ptr == 'z')
                {
                    ptr++; // z
                }
            }
            if (*ptr != '\0' && strchr(validSpecifiers, *ptr))
            {
                // Non - positional format specifier
                if (positional == 1)
                {
                    // Mixing positional and non-positional specifiers causes a crash. 
                    validationFailed = true;
                    break;
                }
                positional = 0;
                continue;
            }
            if (isdigit(*ptr))
            {
                // Positional specifier - check it's well formed.
                if (positional == 0)
                {
                    // Mixing positional and non-positional specifiers causes a crash. 
                    validationFailed = true;
                    break;
                }
                positional = 1;

                ptr++;
                if (*ptr != '\0' && *ptr == '$')
                {
                    ptr++;
                }
                if (strchr("lhz", *ptr))
                {
                    // Positional specifier with length modifiers.
                    if (*ptr == 'l')
                    {
                        ptr++;
                        if (*ptr == 'l')
                            ptr++; // ll
                    }
                    else if (*ptr == 'h')
                    {
                        ptr++;
                        if (*ptr == 'h')
                            ptr++; // hh
                    }
                    else if (*ptr == 'z')
                    {
                        ptr++; // z
                    }
                }
                if (*ptr != '\0' && strchr(validSpecifiers, *ptr))
                {
                    continue;
                }
            }
            // Either a singleton % sign or a malformed format specifier has been found, so return an error.
            validationFailed = true;
            break;
        }
    }

    kSSize written; 
    #if defined (K_MSVC)
    if (validationFailed)
    {
        // Validation failed - write the unformatted string to the buffer and return an error.
        // If the unformatted message is too long and failed validation, 
        // it will truncate the string and error on the parameter.
        snprintf(dest, capacity, "%s", format);
        return kERROR_FORMAT;
    }
    else 
    {
        written = _vsprintf_p(dest, capacity, format, argList); 
    }
    if (written < 0 || written >= (kSSize) capacity)
    {
        dest[capacity-1] = 0; 
    }
    return (written < 0 || written >= (kSSize) capacity) ? kERROR_INCOMPLETE : kOK; 
    #else
    if (validationFailed)
    {
        // Validation failed - write the unformatted string to the buffer and return an error.
        // If the unformatted message is too long and failed validation, 
        // it will truncate the string and error on the parameter.
        snprintf(dest, capacity, "%s", format);
        return kERROR_FORMAT;
    }
    else 
    {
        written = vsnprintf(dest, capacity, format, argList); 
    }
    if (written >= (kSSize) capacity)
    {
        dest[capacity-1] = 0; 
    }

    return (written >= (kSSize)capacity) ? kERROR_INCOMPLETE : kOK; 
    #endif
}

GoApiCppFx(void) Logging::HandleValidateAndSprintf(kChar* dest, kSize capacity, const kChar* format, kVarArgList argList)
{
    kStatus formatArgsStatus = ValidateAndSPrintf(dest, capacity, format, argList);
    if (formatArgsStatus == kERROR_INCOMPLETE)
    {
        GoLogError("Message is too long: %s", format);
    }
    else if (formatArgsStatus == kERROR_FORMAT)
    {
        GoLogError("Message format has malformed specifier(s): %s", format);
    }
    else if (!kSuccess(formatArgsStatus))
    {
        GoLogError("Message or format has other error (%u): %s", formatArgsStatus, format);
    }
}


GoApiCppFx(std::string) Logging::StripTrailing(std::string str, std::string stripSet)
{
  std::size_t found = str.find_last_not_of(stripSet);
  if (found != std::string::npos)
  {
    // str consists entirely of chars in stripSet.
    return str.erase(found+1);
    
  }
  else
  {
    // str consists entirely of chars in stripSet.
    str.clear();
    return str;
  }
}

GoApiCppFx(kStatus) Logging::SetPlatformMinLogLevel(k32s level)
{
    Go::Logging::platformMinLogLevel = level;

    return kOK;
}

GoApiCppFx(k32s) Logging::PlatformMinLogLevel()
{
    return Go::Logging::platformMinLogLevel;
}

GoApiCppFx(kStatus) Logging::SetAppMinLogLevel(k32s level)
{
    if (level == GO_LOG_OPTION_LEVEL_DEBUG)
    {
        Go::Logging::appMinLogLevel = GO_LOG_LEVEL_DEBUG;
    }
    else if (level == GO_LOG_OPTION_LEVEL_TRACE)
    {
        Go::Logging::appMinLogLevel = GO_LOG_LEVEL_TRACE;
    }
    else
    {
        Go::Logging::appMinLogLevel = level;
    }

    GoLogInfo("===== Application Log level changed to %d =====", Go::Logging::appMinLogLevel);
    return kOK;
}

GoApiCppFx(k32s) Logging::AppMinLogLevel()
{
    return Go::Logging::appMinLogLevel;
}

GoApiCppFx(kStatus) Logging::SetUserMinLogLevel(k32s level)
{
    Go::Logging::userMinLogLevel = level;

    return kOK;
}

GoApiCppFx(k32s) Logging::UserMinLogLevel()
{
    return Go::Logging::userMinLogLevel;
}

GoApiCppFx(kStatus) Logging::SetLogFormat(k32s format)
{
    Go::Logging::logFormat = format;

    return kOK;
}

GoApiCppFx(k32s) Logging::LogFormat()
{
    return Go::Logging::logFormat;
}

GoApiCppFx(Logging::LogCounters) Logging::GetLogCounters()
{
    return logCounters;
}

kBool Logging::SkipLog(kLogOption options)
{
    kBool skip = kFALSE;

    if (GoLogIsPlatform(options))
    {
        if (!GoLogIsLevelGE(options, platformMinLogLevel))
        {
            skip = kTRUE;
            Logging::logCounters.platformLogsSkipped++;
        }
    }
    else if (GoLogIsGocApplication(options))
    {
        // NOTE: User logs have BOTH:
        // - GO_LOG_OPTION_GOCAPP_MASK and
        // - GO_LOG_OPTION_GOCAPP_USER_MASK
        if (GoLogIsGocUser(options))
        {
            if (!GoLogIsLevelGE(options, userMinLogLevel))
            {
                skip = kTRUE;
                Logging::logCounters.userLogsSkipped++;
            }
        }
        else if (!GoLogIsLevelGE(options, appMinLogLevel))
        {
            skip = kTRUE;
            Logging::logCounters.appLogsSkipped++;
        }
    }
    else
    {
        // catch-all -- we want to know if we are skipping some logs that
        // we didn't recognize for some reason.
        Logging::logCounters.unknownLogsSkipped++;
        skip = kTRUE;
    }

    return skip;
}

void Logging::CountLog(kLogOption options)
{
    switch (options)
    {
    case GO_LOG_OPTION_PLAT_ERROR:
        Logging::logCounters.platformErrorLogs++; break;
    case GO_LOG_OPTION_PLAT_WARN:
        Logging::logCounters.platformWarnLogs++; break;
    case GO_LOG_OPTION_PLAT_INFO:
        Logging::logCounters.platformInfoLogs++; break;

    case GO_LOG_OPTION_GOCAPP_TRACE:
        Logging::logCounters.appTraceLogs++; break;
    case GO_LOG_OPTION_GOCAPP_DEBUG:
        Logging::logCounters.appDebugLogs++; break;
    case GO_LOG_OPTION_GOCAPP_ERROR:
        Logging::logCounters.appErrorLogs++; break;
    case GO_LOG_OPTION_GOCAPP_WARN:
        Logging::logCounters.appWarnLogs++; break;
    case GO_LOG_OPTION_GOCAPP_INFO:
        Logging::logCounters.appInfoLogs++; break;

    case GO_LOG_OPTION_GOCAPP_USER_ERROR:
        Logging::logCounters.userErrorLogs++; break;
    case GO_LOG_OPTION_GOCAPP_USER_WARN:
        Logging::logCounters.userWarnLogs++; break;
    case GO_LOG_OPTION_GOCAPP_USER_INFO:
        Logging::logCounters.userInfoLogs++; break;

    default:
        Logging::logCounters.unknownLogsSkipped++;
    }
}
} // end namespace
