using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace MyErpApp.Core.Plugins
{
    public interface IPluginHealthMonitor
    {
        void RecordLoadResult(PluginLoadResult result);
        void RecordFailure(string pluginName, Exception ex);
        IEnumerable<PluginLoadResult> GetStatus();
    }

    public class PluginHealthMonitor : IPluginHealthMonitor
    {
        private readonly ConcurrentDictionary<string, PluginLoadResult> _status = new();

        public void RecordLoadResult(PluginLoadResult result)
        {
            _status[result.PluginName] = result;
        }

        public void RecordFailure(string pluginName, Exception ex)
        {
            if (_status.TryGetValue(pluginName, out var result))
            {
                result.Status = PluginStatus.Failed;
                result.ErrorMessage = ex.Message;
            }
            else
            {
                _status[pluginName] = new PluginLoadResult
                {
                    PluginName = pluginName,
                    Status = PluginStatus.Failed,
                    ErrorMessage = ex.Message
                };
            }
        }

        public IEnumerable<PluginLoadResult> GetStatus()
        {
            return _status.Values;
        }
    }
}
