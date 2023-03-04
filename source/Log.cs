#nullable enable
using HBS.Logging;
using NullableLogging;

namespace CustomSalvage;

internal static class Log
{
    private const string Name = nameof(CustomSalvage);
    internal static readonly NullableLogger Main = NullableLogger.GetLogger(Name, LogLevel.Debug);
}