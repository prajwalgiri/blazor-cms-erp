using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyErpApp.Core.Plugins;
using MyErpApp.Core.Abstractions;
using Accounting.Plugin.Data;
using Accounting.Plugin.Domain;
using System.Composition;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace Accounting.Plugin
{
    [Export(typeof(IErpModule))]
    public class AccountingModule : IErpModule
    {
        public string Name => "Accounting";

        public void RegisterServices(IServiceCollection services)
        {
            var configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
            var section = configuration.GetSection(GetConfigurationSection());
            var connectionString = section["ConnectionString"] ?? configuration.GetConnectionString("DefaultConnection");

            services.AddDbContext<AccountingDbContext>(options =>
                options.UseSqlServer(connectionString));
        }

        public void ValidateConfiguration(IConfiguration config)
        {
            var section = config.GetSection(GetConfigurationSection());
            if (string.IsNullOrEmpty(section["ConnectionString"]) && string.IsNullOrEmpty(config.GetConnectionString("DefaultConnection")))
            {
                throw new Exception($"Missing ConnectionString for {Name} module.");
            }
        }

        public async Task ApplyMigrations(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AccountingDbContext>();
            await context.Database.MigrateAsync();
        }

        public void MapEndpoints(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/accounting");

            group.MapGet("/ledger", async (AccountingDbContext db) =>
            {
                return await db.LedgerEntries.ToListAsync();
            });

            group.MapPost("/ledger", async (AccountingDbContext db, ISnapshotService snapshotService, LedgerEntry entry) =>
            {
                if (entry.Id == Guid.Empty) entry.Id = Guid.NewGuid();
                
                await db.LedgerEntries.AddAsync(entry);
                await db.SaveChangesAsync();

                // Create snapshot for auditing
                await snapshotService.CreateSnapshotAsync(entry.Id, entry);

                return Results.Created($"/accounting/ledger/{entry.Id}", entry);
            });

            group.MapGet("/ledger/{id}/history", async (ISnapshotService snapshotService, Guid id) =>
            {
                var history = await snapshotService.GetLatestSnapshotAsync<LedgerEntry>(id);
                return history != null ? Results.Ok(history) : Results.NotFound();
            });
        }
    }
}
