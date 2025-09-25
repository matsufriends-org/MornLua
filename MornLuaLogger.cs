namespace MornLua
{
    internal static class MornLuaLogger
    {
        internal static void Log(string message)
        {
            MornLuaGlobal.I.LogInternal(message);
        }

        internal static void LogWarning(string message)
        {
            MornLuaGlobal.I.LogWarningInternal(message);
        }

        internal static void LogError(string message)
        {
            MornLuaGlobal.I.LogErrorInternal(message);
        }
    }
}