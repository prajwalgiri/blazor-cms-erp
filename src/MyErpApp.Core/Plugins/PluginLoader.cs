using System;
using System.Collections.Generic;
using System.Composition;
using System.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace MyErpApp.Core.Plugins
{
    public static class PluginLoader
    {
        public static (CompositionHost Host, List<PluginLoadResult> Results) LoadPlugins(string path)
        {
            var results = new List<PluginLoadResult>();
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                return (new ContainerConfiguration().CreateContainer(), results);
            }

            var assemblies = new List<Assembly>();
            var files = Directory.GetFiles(path, "*.dll");

            foreach (var file in files)
            {
                var result = new PluginLoadResult
                {
                    PluginName = Path.GetFileNameWithoutExtension(file),
                    AssemblyPath = file
                };

                try
                {
                    var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(file);
                    assemblies.Add(assembly);
                    result.Status = PluginStatus.Loaded;
                }
                catch (Exception ex)
                {
                    result.Status = PluginStatus.Failed;
                    result.ErrorMessage = ex.Message;
                }

                results.Add(result);
            }

            var configuration = new ContainerConfiguration()
                .WithAssemblies(assemblies);

            return (configuration.CreateContainer(), results);
        }
    }
}
