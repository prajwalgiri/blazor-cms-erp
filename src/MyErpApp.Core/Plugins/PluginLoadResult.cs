using System;

namespace MyErpApp.Core.Plugins
{
    public enum PluginStatus
    {
        Loaded,
        Failed,
        Disabled
    }

    public class PluginLoadResult
    {
        public string PluginName { get; set; } = string.Empty;
        public string AssemblyPath { get; set; } = string.Empty;
        public PluginStatus Status { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime LoadTime { get; set; } = DateTime.UtcNow;
    }
}
