using System.Runtime.CompilerServices;

namespace Imova.IntegrationTests.TestSupport;

// Every test class starts its own API host, and each host watches its appsettings files. With the
// default inotify watchers that runs out of Linux's per-user limit (128) part-way through the suite
// ("The configured user limit (128) on the number of inotify instances has been reached"), so the
// configuration's file watchers poll instead — set once, when the test assembly loads, before any host
// is built (dotnet test, vstest and IDEs alike). Production is untouched.
internal static class PollingFileWatcher
{
    [ModuleInitializer]
    internal static void UsePolling() => Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "1");
}
