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
        public static CompositionHost LoadPlugins(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            var assemblies = Directory.GetFiles(path, "*.dll")
                .Select(AssemblyLoadContext.Default.LoadFromAssemblyPath)
                .ToList();

            // Also include current assembly if it has any exports (optional)
            // assemblies.Add(Assembly.GetExecutingAssembly());

            var configuration = new ContainerConfiguration()
                .WithAssemblies(assemblies);

            return configuration.CreateContainer();
        }
    }
}
