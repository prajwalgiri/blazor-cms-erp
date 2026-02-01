using System;

namespace MyErpApp.Core.Domain
{
    public class PluginMigration
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string PluginName { get; set; } = string.Empty;
        public string MigrationName { get; set; } = string.Empty;
        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
        public bool Success { get; set; }
    }
}
