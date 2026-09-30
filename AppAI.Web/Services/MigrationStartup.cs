using App.BL;

namespace AppAI.Web.Services;

// After the host starts (in the background, so startup is not delayed):
//  - warns about migration scripts whose version number is used twice;
//  - Migrations:RunOnStartup = true  -> applies pending migrations to every registered tenant database;
//  - otherwise                        -> logs which tenants still have pending migrations, so a missing
//                                        "Invalid column name" deploy step shows up in the log instead of the UI.
public static class MigrationStartup
{
    public static void Register(WebApplication app)
    {
        app.Lifetime.ApplicationStarted.Register(() => Task.Run(() => Run(app.Configuration)));
    }

    private static void Run(IConfiguration configuration)
    {
        var log = NLog.LogManager.GetCurrentClassLogger();
        try
        {
            foreach (var duplicate in AppTenantMigrationRunnerBL.FindDuplicateVersionNumbers())
                log.Warn("Duplicate migration number — {0}. Use the next free number for new scripts.", duplicate);

            if (configuration.GetValue<bool>("Migrations:RunOnStartup"))
            {
                foreach (var (tenant, applied) in AppTenantMigrationRunnerBL.RunMigrationsOnAllTenants())
                {
                    if (applied < 0) log.Error("Startup migration FAILED for tenant {0} (see the error logged above)", tenant);
                    else if (applied > 0) log.Info("Startup migration: applied {0} script(s) to tenant {1}", applied, tenant);
                }
                return;
            }

            foreach (var (tenant, pending) in AppTenantMigrationRunnerBL.GetPendingCountsForAllTenants())
                if (pending > 0)
                    log.Warn("Tenant {0} has {1} pending migration(s). Run POST /webapi/TenantProvisioning/RunMigrations, or set Migrations:RunOnStartup=true.", tenant, pending);
        }
        catch (Exception ex)
        {
            log.Error(ex, "Migration startup check failed");
        }
    }
}
