using System.Collections.Generic;
using System.Net;
using App.BL;
using APP.Components.EntityDto;
using AppAI.Web.Controllers.Base;
using Microsoft.AspNetCore.Mvc;

namespace AppAI.Web.Controllers;

// SysAdmin-only endpoint — extends SecureBaseController so the session is validated.
[Route("webapi/[controller]/[action]")]
public class TenantProvisioningController : SecureBaseController
{
    // POST /webapi/TenantProvisioning/Provision
    // Creates a new fully operational tenant in one API call.
    [HttpPost]
    public AppTenantProvisionResultDto Provision([FromBody] AppTenantProvisionRequestDto request)
    {
        RequireSysAdmin();
        return AppTenantProvisioningBL.ProvisionNewTenant(request);
    }

    // POST /webapi/TenantProvisioning/RunMigrations
    // Runs any pending schema migrations against every registered tenant DB.
    // Safe to call on every deployment — idempotent.
    [HttpPost]
    public Dictionary<string, int> RunMigrations()
    {
        RequireSysAdmin();
        return AppTenantMigrationRunnerBL.RunMigrationsOnAllTenants();
    }

    // POST /webapi/TenantProvisioning/RepairAdminUsers
    // Back-fills IsRegisterCompleted and MyOwnCompnanyId for tenant admin accounts
    // provisioned before those fields were set. Returns the count of rows fixed.
    [HttpPost]
    public IActionResult RepairAdminUsers()
    {
        RequireSysAdmin();
        int fixed_ = AppTenantProvisioningBL.RepairTenantAdminUsers();
        return Ok(new { RowsFixed = fixed_ });
    }

    // POST /webapi/TenantProvisioning/RepairDataSourceIds?templateDataSourceId=1070
    // Fills NULL DataSourceFrom in every company-master tenant DB with that tenant's
    // AppDataSourceRegister.DataSourceId. Optionally also remaps templateDataSourceId.
    [HttpPost]
    public Dictionary<string, string> RepairDataSourceIds([FromQuery] int? templateDataSourceId = null)
    {
        RequireSysAdmin();
        return AppTenantProvisioningBL.RepairDataSourceIdReferencesOnAllTenants(templateDataSourceId);
    }

    // POST /webapi/TenantProvisioning/PushAgentTemplates
    // Copies platform agent config (domains, libraries, tools, skill sets) from the designated
    // template tenant DB into every other registered tenant DB.
    // Uses IF NOT EXISTS — existing tenant customisations are never overwritten.
    [HttpPost]
    public Dictionary<string, string> PushAgentTemplates([FromBody] PushAgentTemplatesRequest request)
    {
        RequireSysAdmin();
        return AppTenantProvisioningBL.PushAgentTemplatesToAllTenants(request.TemplateDataSourceId);
    }

    private void RequireSysAdmin()
    {
        if (!AppSecurityUserBL.IsAdminUser())
            throw new Microsoft.AspNetCore.Http.BadHttpRequestException("Forbidden", (int)HttpStatusCode.Forbidden);
    }
}
