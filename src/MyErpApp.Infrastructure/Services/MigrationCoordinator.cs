using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyErpApp.Core.Domain;
using MyErpApp.Core.Plugins;
using MyErpApp.Infrastructure.Data;

namespace MyErpApp.Infrastructure.Services
{
    public interface IMigrationCoordinator
    {
        Task CoordinateMigrations(IEnumerable<IErpModule> modules);
    }

    public class MigrationCoordinator : IMigrationCoordinator
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MigrationCoordinator> _logger;
        private readonly AppDbContext _context;

        public MigrationCoordinator(IServiceProvider serviceProvider, ILogger<MigrationCoordinator> logger, AppDbContext context)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _context = context;
        }

        public async Task CoordinateMigrations(IEnumerable<IErpModule> modules)
        {
            _logger.LogInformation("Starting coordinated plugin migrations...");

            // Sort modules by priority and then by dependency (simplified dependency sorting)
            var sortedModules = modules
                .OrderBy(m => m.MigrationPriority)
                .ToList();

            // Note: In a production system, we'd use a proper topological sort for DependsOnModules.
            // For now, we use MigrationPriority as the primary control.

            foreach (var module in sortedModules)
            {
                try
                {
                    _logger.LogInformation("Checking migrations for module: {ModuleName}", module.Name);

                    // Here we can check if migration already applied in PluginMigrations table
                    // but most EF migrations handle their own history. 
                    // This coordinator is for high-level module-specific migration triggers.

                    await module.ApplyMigrations(_serviceProvider);

                    _logger.LogInformation("Successfully completed migrations for module: {ModuleName}", module.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to apply migrations for module: {ModuleName}", module.Name);
                    // Decide if we should continue or stop. In coordinated mode, we usually stop.
                    throw;
                }
            }

            _logger.LogInformation("All plugin migrations coordinated successfully.");
        }
    }
}
