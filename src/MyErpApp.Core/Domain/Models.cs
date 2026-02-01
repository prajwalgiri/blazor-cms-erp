using System;
using System.Collections.Generic;

namespace MyErpApp.Core.Domain
{
    public class UiPage
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public List<UiComponent> Components { get; set; } = new();
    }

    public class UiComponent
    {
        public Guid Id { get; set; }
        public Guid UiPageId { get; set; }
        public int Order { get; set; }
        public string Type { get; set; } = string.Empty;
        public string TailwindHtml { get; set; } = string.Empty;
        public string ConfigJson { get; set; } = "{}";
    }

    public class EntitySnapshot
    {
        public Guid Id { get; set; }
        public string EntityName { get; set; } = string.Empty;
        public Guid EntityId { get; set; }
        public string JsonData { get; set; } = "{}";
        public DateTime SnapshotDate { get; set; } = DateTime.UtcNow;
    }
}
