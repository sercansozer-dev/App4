#ifndef GOAPI_LOGGING_H
#define GOAPI_LOGGING_H

#include <GoApi/GoApiDef.h>
#include <string>

namespace Go
{
    class Logging 
    {
    public:
        Logging() {};

        static GoApiCppFx(kStatus) Go_Log(int line, 
                                             const kChar* fileName, 
                                             kLogOption options, 
                                             const kChar* source, 
                                             const kChar* format, ...);

        static GoApiCppFx(kStatus) FormatLogBuffer(kDateTime dateTime, kLogOption options, const kChar* source, 
                                                const kChar* message, kChar* buffer, kSize capacity);

        /**
         * Validates a given string with format specifiers (positional or non-positional), and inserts the arguments into the string if there are no issues. 
         * @param    dest                Output buffer.
         * @param    capacity            Output buffer size.
         * @param    format              Format string to insert arguments into.
         * @param    argList             List of arguments to insert. 
         *
         *      If validation fails, writes the original, non-formatted string to dest, and returns kERROR_FORMAT.
         *      If the formatted string + arguments is too large for dest, writes the truncated string and returns kERROR_INCOMPLETE.
         *
         * @returns  kOK on success, kERROR_FORMAT or kERROR_INCOMPLETE on an invalid string or too-long string respectively.
         */
        static GoApiCppFx(kStatus) ValidateAndSPrintf(kChar* dest, kSize capacity, const kChar* format, kVarArgList argList);

        /**
         * Handles format string validation and formatting with integrated error logging.
         * This function wraps ValidateAndSPrintf and automatically logs appropriate error messages
         * for different failure cases without returning error status.
         * 
         * @param    dest                Output buffer.
         * @param    capacity            Output buffer size.
         * @param    format              Format string to insert arguments into.
         * @param    argList             List of arguments to insert.
         */
        static GoApiCppFx(void) HandleValidateAndSprintf(kChar* dest, kSize capacity, const kChar* format, kVarArgList argList);

        /**
         * Strips trailing characters from a given string.
         *
         * @param    str                 String to remove trailing characters from.
         * @param    stripSet            The set of trailing characters to remove.
         *
         *  @code {.cpp}
         *  std::string trimMe = "Trim me...,,,";
         *  Go::Logging::StripTrailing(trimeMe, ".,");
         *  ASSERT_STREQ(trimMe.c_str(), "Trim me");
         *  @endcode
         */
        static GoApiCppFx(std::string) StripTrailing(std::string str, std::string stripSet);
        
        /**
         * Sets the platform minimum log level.
         * 
         * @param level     The minimum log level.
         */
        static GoApiCppFx(kStatus) SetPlatformMinLogLevel(k32s level);

        /**
         * Returns the platform minimum log level.
         *
         * @returns The platform minimum log level.
         */
        static GoApiCppFx(k32s) PlatformMinLogLevel();

        /**
         * Sets the app minimum log level.
         *
         * @param level     The minimum log level.
         */
        static GoApiCppFx(kStatus) SetAppMinLogLevel(k32s level);

        /**
         * Returns the app minimum log level.
         *
         * @returns The app minimum log level.
         */
        static GoApiCppFx(k32s) AppMinLogLevel();

        /**
         * Sets the user minimum log level.
         *
         * @param level     The minimum log level.
         */
        static GoApiCppFx(kStatus) SetUserMinLogLevel(k32s level);

        /**
         * Returns the user minimum log level.
         *
         * @returns The user minimum log level.
         */
        static GoApiCppFx(k32s) UserMinLogLevel();

        /**
         * Sets the log format.
         *
         * @param format  The log format.
         */
        static GoApiCppFx(kStatus) SetLogFormat(k32s format);

        /**
         * Returns the log format.
         *
         * @returns The log format.
         */
        static GoApiCppFx(k32s) LogFormat();

        typedef struct
        {
            //------------------------------
            k64u platformErrorLogs;
            k64u platformWarnLogs;
            k64u platformInfoLogs;
            k64u platformLogsSkipped;
            //------------------------------
            k64u appErrorLogs;
            k64u appWarnLogs;
            k64u appInfoLogs;
            k64u appDebugLogs;
            k64u appTraceLogs;
            k64u appLogsSkipped;
            //------------------------------
            k64u userErrorLogs;
            k64u userWarnLogs;
            k64u userInfoLogs;
            k64u userLogsSkipped;
            //------------------------------
            k64u unknownLogsSkipped;
            //------------------------------
        } LogCounters;

        static GoApiCppFx(LogCounters) GetLogCounters();

    private:
        static GoApiCppFx(kStatus) FormatLogOption(kLogOption options, kChar* buffer, kSize bufferSize);

        static kStatus SkipLog(kLogOption options);
        static void CountLog(kLogOption options);

        static k32s platformMinLogLevel;
        static k32s appMinLogLevel;
        static k32s userMinLogLevel;
        static k32s logFormat;

        static LogCounters logCounters;
    };
}

// See (FSS-1301) and kApiDef.h:
// The lower 32 bits of an option value are reserved for platform-defined 
//  purposes; applications can use the upper 32 bits as desired. 

// Individual Log option bit masks.
#define GO_LOG_OPTION_PLAT_MASK                     (0x0000000000000010)

#define GO_LOG_OPTION_GOCAPP_MASK                   (0x8000000000000000)
#define GO_LOG_OPTION_GOCAPP_USER_MASK              (0x0000000100000000)
#define GO_LOG_OPTION_PLAT_APP_ANY_MASK             (0xFFFFFFFFFFFFFFFC) // Checks if any platform or app option is defined.

#define GO_LOG_OPTION_LEVEL_MASK                    (0x000000000000000F)
#define GO_LOG_OPTION_LEVEL_INFO                    (0x0)
#define GO_LOG_OPTION_LEVEL_WARN                    (0x1)
#define GO_LOG_OPTION_LEVEL_ERROR                   (0x2)
#define GO_LOG_OPTION_LEVEL_TRACE                   (0x4)
#define GO_LOG_OPTION_LEVEL_DEBUG                   (0x8)

// Commonly used options (we hard code them instead of bit shifting them each time.
#define GO_LOG_OPTION_PLAT_INFO                     ((kLogOption) 0x8000000000000010)  // (GO_LOG_OPTION_PLAT_MASK    | GO_LOG_OPTION_LEVEL_INFO)
#define GO_LOG_OPTION_PLAT_WARN                     ((kLogOption) 0x8000000000000011)  // (GO_LOG_OPTION_PLAT_MASK    | GO_LOG_OPTION_LEVEL_WARN)
#define GO_LOG_OPTION_PLAT_ERROR                    ((kLogOption) 0x8000000000000012)  // (GO_LOG_OPTION_PLAT_MASK    | GO_LOG_OPTION_LEVEL_ERROR)
#define GO_LOG_OPTION_GOCAPP_INFO                   ((kLogOption) 0x8000000000000000)  // (GO_LOG_OPTION_GOC_APP_MASK | GO_LOG_OPTION_LEVEL_INFO)
#define GO_LOG_OPTION_GOCAPP_WARN                   ((kLogOption) 0x8000000000000001)  // (GO_LOG_OPTION_GOC_APP_MASK | GO_LOG_OPTION_LEVEL_WARN)
#define GO_LOG_OPTION_GOCAPP_ERROR                  ((kLogOption) 0x8000000000000002)  // (GO_LOG_OPTION_GOC_APP_MASK | GO_LOG_OPTION_LEVEL_ERROR)
#define GO_LOG_OPTION_GOCAPP_TRACE                  ((kLogOption) 0x8000000000000004)  // (GO_LOG_OPTION_GOC_APP_MASK | GO_LOG_OPTION_LEVEL_TRACE)
#define GO_LOG_OPTION_GOCAPP_DEBUG                  ((kLogOption) 0x8000000000000008)  // (GO_LOG_OPTION_GOC_APP_MASK | GO_LOG_OPTION_LEVEL_DEBUG)
#define GO_LOG_OPTION_GOCAPP_USER_INFO              ((kLogOption) 0x8000000100000000)  // (GO_LOG_OPTION_GOCAPP_INFO  | GO_LOG_OPTION_GOCAPP_USER_MASK | GO_LOG_OPTION_GOCAPP_TOOL_MASK)
#define GO_LOG_OPTION_GOCAPP_USER_WARN              ((kLogOption) 0x8000000100000001)  // (GO_LOG_OPTION_GOCAPP_WARN  | GO_LOG_OPTION_GOCAPP_USER_MASK | GO_LOG_OPTION_GOCAPP_TOOL_MASK)
#define GO_LOG_OPTION_GOCAPP_USER_ERROR             ((kLogOption) 0x8000000100000002)  // (GO_LOG_OPTION_GOCAPP_ERROR | GO_LOG_OPTION_GOCAPP_USER_MASK | GO_LOG_OPTION_GOCAPP_TOOL_MASK)

// Used to check what options are set.
#define GoLogIsPlatform(option)                     (((option) & GO_LOG_OPTION_PLAT_MASK) > 0)
#define GoLogIsGocApplication(option)               (((option) & GO_LOG_OPTION_GOCAPP_MASK) > 0)
#define GoLogIsGocUser(option)                      (((option) & GO_LOG_OPTION_GOCAPP_USER_MASK) > 0)       // User-facing system messages.
#define GoLogIsDirectkLog(option)                   (((option) & GO_LOG_OPTION_PLAT_APP_ANY_MASK) == 0)     // Direct kLog doesn't set any platform or app flags.

#define GoLogIsInfo(option)                         (((option) & GO_LOG_OPTION_LEVEL_MASK) == GO_LOG_OPTION_LEVEL_INFO)
#define GoLogIsWarn(option)                         (((option) & GO_LOG_OPTION_LEVEL_MASK) == GO_LOG_OPTION_LEVEL_WARN)
#define GoLogIsError(option)                        (((option) & GO_LOG_OPTION_LEVEL_MASK) == GO_LOG_OPTION_LEVEL_ERROR)
#define GoLogIsDebug(option)                        (((option) & GO_LOG_OPTION_LEVEL_MASK) == GO_LOG_OPTION_LEVEL_DEBUG)
#define GoLogIsTrace(option)                        (((option) & GO_LOG_OPTION_LEVEL_MASK) == GO_LOG_OPTION_LEVEL_TRACE)

// General purpose logging macros.  Use this to log items that can be hot-clickable
// when running within Visual Studio, 
// NOTE: Child Process Debugging must be enabled for Google tests.

/**
 * Helper macro to log an informational message.
 *
 * @param   format      Informational message to be logged.
 */
#define GoLogInfo(format, ...)                      Go::Logging::Go_Log(__LINE__, __FILE__, GO_LOG_OPTION_GOCAPP_INFO, __FUNCTION__, format, ## __VA_ARGS__)

 /**
 * Helper macro to log a warning message.
 *
 * @param   format      Warning message to be logged.
 */
#define GoLogWarn(format, ...)                      Go::Logging::Go_Log(__LINE__, __FILE__, GO_LOG_OPTION_GOCAPP_WARN, __FUNCTION__, format, ## __VA_ARGS__)

 /**
 * Helper macro to log an error message.
 *
 * @param   format      Error message to be logged.
 */
#define GoLogError(format, ...)                     Go::Logging::Go_Log(__LINE__, __FILE__, GO_LOG_OPTION_GOCAPP_ERROR, __FUNCTION__, format, ## __VA_ARGS__)

 /**
 * Helper macro to log an debug message.
 *
 * @param   format      Debug message to be logged.
 */
#define GoLogDebug(format, ...)                     Go::Logging::Go_Log(__LINE__, __FILE__, GO_LOG_OPTION_GOCAPP_DEBUG, __FUNCTION__, format, ## __VA_ARGS__)

 /**
 * Helper macro to log an trace message.
 *
 * @param   format      Trace message to be logged.
 */
#define GoLogTrace(format, ...)                     Go::Logging::Go_Log(__LINE__, __FILE__, GO_LOG_OPTION_GOCAPP_TRACE, __FUNCTION__, format, ## __VA_ARGS__)

// NOTE: User level logs do not have debug and trace levels.
/**
 * Helper macro to log a user informational message.
 * NOTE: The message should be marked for localization with bepgettext() macro.
 *
 * @param   source      Source of the message.
 * @param   format      User informational message to be logged.
 */
#define GoLogUserInfo(source, format, ...)          Go::Logging::Go_Log(__LINE__, __FILE__, GO_LOG_OPTION_GOCAPP_USER_INFO, source, format, ## __VA_ARGS__)

/**
 * Helper macro to log a user warning message.
 * NOTE: The message should be marked for localization with bepgettext() macro.
 *
 * @param   source      Source of the message.
 * @param   format      User warning message to be logged.
 */
#define GoLogUserWarn(source, format, ...)          Go::Logging::Go_Log(__LINE__, __FILE__, GO_LOG_OPTION_GOCAPP_USER_WARN, source, format, ## __VA_ARGS__)

/**
 * Helper macro to log a user error message.
 * NOTE: The message should be marked for localization with bepgettext() macro.
 *
 * @param   source      Source of the message.
 * @param   format      User error message to be logged.
 */
#define GoLogUserError(source, format, ...)         Go::Logging::Go_Log(__LINE__, __FILE__, GO_LOG_OPTION_GOCAPP_USER_ERROR, source, format, ## __VA_ARGS__)

// GOS-5108: Added GoLogDebug macros which act as GoLogUser in Debug builds and GoLog in Release builds.
// First set of macros are used in Debug builds.
// GOS-10430: Renamed the following user debug functions for clarity. Log at user level only if this is a debug (dev) build.
// Similar to user level logs, there is no support for debug and trace levels
#ifdef _DEBUG

/**
 * Helper macro to log a user info message in debug builds and a normal info message in release builds.
 *
 * @param   source      Source of the message.
 * @param   format      User info message to be logged.
 */
#define GoLogUserInfoIfDev(source, format, ...)         GoLogUserInfo(source, format, ## __VA_ARGS__)

/**
 * Helper macro to log a user warning message in debug builds and a normal warning message in release builds.
 *
 * @param   source      Source of the message.
 * @param   format      User warning message to be logged.
 */
#define GoLogUserWarnIfDev(source, format, ...)         GoLogUserWarn(source, format, ## __VA_ARGS__)

/**
 * Helper macro to log a user error message in debug builds and a normal error message in release builds.
 *
 * @param   source      Source of the message.
 * @param   format      User error message to be logged.
 */
#define GoLogUserErrorIfDev(source, format, ...)        GoLogUserError(source, format, ## __VA_ARGS__)

// GOS-5108: Release builds use this second set of macros.
#else

/**
 * Helper macro to log a user info message in debug builds and a normal info message in release builds.
 *
 * @param   source      Source of the message.
 * @param   format      User info message to be logged.
 */
#define GoLogUserInfoIfDev(source, format, ...)         GoLogInfo(format, ## __VA_ARGS__)

/**
 * Helper macro to log a user warning message in debug builds and a normal warning message in release builds.
 *
 * @param   source      Source of the message.
 * @param   format      User warning message to be logged.
 */
#define GoLogUserWarnIfDev(source, format, ...)         GoLogWarn(format, ## __VA_ARGS__)

/**
 * Helper macro to log a user error message in debug builds and a normal error message in release builds.
 *
 * @param   source      Source of the message.
 * @param   format      User error message to be logged.
 */
#define GoLogUserErrorIfDev(source, format, ...)        GoLogError(format, ## __VA_ARGS__)

#endif


#endif
