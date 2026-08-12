using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;

namespace App.BL
{
    // Provisions a fully operational tenant in under 60 seconds.
    // Each step is idempotent-safe: if provisioning fails mid-way the caller can retry
    // after fixing the root cause.
    public static class AppTenantProvisioningBL
    {
        private static readonly string MasterConnStr =
            AppCompanyBL.AppMasterDBConnectionString;

       
        public static AppTenantProvisionResultDto ProvisionNewTenant(AppTenantProvisionRequestDto request)
        {
            var result = new AppTenantProvisionResultDto();

            try
            {
                // ── Step 1: Validate ────────────────────────────────────────────────
                if (string.IsNullOrWhiteSpace(request?.CompanyName)
                    || string.IsNullOrWhiteSpace(request.DomainToken)
                    || string.IsNullOrWhiteSpace(request.AdminEmail)
                    || string.IsNullOrWhiteSpace(request.AdminLoginName)
                    || string.IsNullOrWhiteSpace(request.AdminPassword))
                {
                    result.ErrorMessage = "CompanyName, DomainToken, AdminEmail, AdminLoginName and AdminPassword are required.";
                    return result;
                }

                if (!Regex.IsMatch(request.DomainToken, @"^[a-z0-9\-]{2,50}$"))
                {
                    result.ErrorMessage = "DomainToken must be 2–50 lowercase alphanumeric characters or hyphens.";
                    return result;
                }

                if (IsDomainTokenTaken(request.DomainToken))
                {
                    result.ErrorMessage = $"DomainToken '{request.DomainToken}' is already in use.";
                    return result;
                }

                // ── Step 2: Create AppCompany in master DB ──────────────────────────
                int companyId = CreateCompanyInMaster(request.CompanyName, request.DomainToken);
                result.CompanyId = companyId;

                // ── Step 3: Derive DB name  (TenantDB_{DOMAIN} naming convention) ──
                string companyCode = request.DomainToken.ToUpperInvariant();
                string dbName = $"TenantDB_{companyCode}";
                result.DatabaseName = dbName;

                // ── Step 4: CREATE DATABASE ─────────────────────────────────────────
                CreateTenantDatabase(dbName);

                // ── Step 5: Build tenant connection string ──────────────────────────
                string tenantConnStr = BuildTenantConnectionString(dbName);

                // ── Step 6: Run schema migrations ───────────────────────────────────
                result.MigrationsApplied = AppTenantMigrationRunnerBL.RunPendingMigrations(tenantConnStr);

                // ── Step 6b: Seed from template DB (if selected) ────────────────────
                int templateDataSourceId = 0;
                if (!string.IsNullOrWhiteSpace(request.TemplateId))
                {
                    int.TryParse(request.TemplateId, out templateDataSourceId);
                    SeedFromTemplateDb(request.TemplateId, dbName);
                    result.SeededFromTemplate = ResolveTemplateDatabaseName(request.TemplateId);
                }

                // ── Step 7: Create admin user in master DB ──────────────────────────
                CreateAdminUser(companyId, request.AdminLoginName, request.AdminPassword, request.AdminEmail);

                // ── Step 8: Register encrypted connection string in AppDataSourceRegister ──
                int newDataSourceId = RegisterTenantDataSource(companyId, dbName, tenantConnStr, request.DomainToken);
                result.DataSourceId = newDataSourceId;

                // ── Step 9: Fill / repoint DataSource* FK columns → this tenant's DataSourceId ──
                // V007 seeds Entity/DataSet/Transaction/etc. with DataSourceFrom = NULL; fill those
                // with the new register id. Template seed may still copy a template DataSourceId —
                // remap that separately. FK on DataSourceFrom dropped in V004.
                if (newDataSourceId > 0)
                {
                    FillNullDataSourceFromReferences(tenantConnStr, newDataSourceId);
                    if (templateDataSourceId > 0)
                        UpdateDataSourceIdReferences(tenantConnStr, templateDataSourceId, newDataSourceId);
                }

                result.Success = true;
                result.LoginUrl = $"{request.DomainToken}.appai.com";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        // ── Private helpers ────────────────────────────────────────────────────────

        private static bool IsDomainTokenTaken(string domainToken)
        {
            const string sql = @"
                SELECT COUNT(1) FROM AppDataSourceRegister
                WHERE LOWER(DomainToken) = LOWER(@token)
                UNION ALL
                SELECT COUNT(1) FROM AppCompany
                WHERE LOWER(CompanyDomainIdentityToken) = LOWER(@token)";

            using (var conn = new SqlConnection(MasterConnStr))
            {
                conn.Open();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@token", domainToken);
                    // Any row with count > 0 means taken
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            if (reader.GetInt32(0) > 0) return true;
                    }
                }
            }
            return false;
        }

        private static int CreateCompanyInMaster(string companyName, string domainToken)
        {
            using (var adapter = new DataAccessAdapter(AppCompanyBL.AppMasterDBConnectionString))
            {
                var entity = new AppCompanyEntity
                {
                    ShortName = companyName,
                    FullName = companyName,
                    Code = domainToken.ToUpperInvariant(),
                    CompanyDomainIdentityToken = domainToken.ToLowerInvariant(),
                    Status = "A",
                    AppCreatedDate = DateTime.UtcNow,
                };
                adapter.SaveEntity(entity);
                return entity.AppCompanyId;
            }
        }

        private static void CreateTenantDatabase(string dbName)
        {
            // Must run against master (not a specific catalog) to issue CREATE DATABASE.
            var builder = new SqlConnectionStringBuilder(MasterConnStr)
            {
                InitialCatalog = "master"
            };

            using (var conn = new SqlConnection(builder.ToString()))
            {
                conn.Open();
                // Validate dbName to prevent SQL injection (alphanumeric + underscore only)
                if (!Regex.IsMatch(dbName, @"^[A-Za-z][A-Za-z0-9_]{1,127}$"))
                    throw new ArgumentException($"Invalid database name: {dbName}");

                string sql = $"IF DB_ID(N'{dbName}') IS NULL CREATE DATABASE [{dbName}] COLLATE SQL_Latin1_General_CP1_CI_AS;";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.CommandTimeout = 120;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static string BuildTenantConnectionString(string dbName)
        {
            var builder = new SqlConnectionStringBuilder(MasterConnStr)
            {
                InitialCatalog = dbName
            };
            return builder.ToString();
        }

        private static void CreateAdminUser(int companyId, string loginName, string password, string email)
        {
            string hashedPassword = AppSecurityPasswordHashBL.HashPassword(password);

            const string sql = @"
                INSERT INTO AppSecurityUser
                    (LoginName, UserName, [Password], Email, DomainId,
                     AppCreatedByCompanyId, AppCreatedDate, IsActive, IsDeleted,
                     IsRegisterCompleted, MyOwnCompnanyId)
                VALUES
                    (@login, @login, @pwd, @email, @domainId,
                     @companyId, GETUTCDATE(), 1, 0,
                     1, @companyId);";

            using (var conn = new SqlConnection(MasterConnStr))
            {
                conn.Open();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@login", loginName);
                    cmd.Parameters.AddWithValue("@pwd", hashedPassword);
                    cmd.Parameters.AddWithValue("@email", email);
                    cmd.Parameters.AddWithValue("@domainId", (int)EmAppUserType.SaasCompanyAdmin);
                    cmd.Parameters.AddWithValue("@companyId", companyId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Fixes tenant admin users that were provisioned before IsRegisterCompleted/MyOwnCompnanyId
        // were included in CreateAdminUser. Safe to call repeatedly — only touches null/false rows.
        public static int RepairTenantAdminUsers()
        {
            const string sql = @"
                UPDATE AppSecurityUser
                SET    IsRegisterCompleted = 1,
                       MyOwnCompnanyId    = AppCreatedByCompanyId
                WHERE  DomainId                = @domainId
                  AND  AppCreatedByCompanyId IS NOT NULL
                  AND  IsDeleted              = 0
                  AND  (IsRegisterCompleted IS NULL OR IsRegisterCompleted = 0
                        OR MyOwnCompnanyId IS NULL);
                SELECT @@ROWCOUNT;";

            using (var conn = new SqlConnection(MasterConnStr))
            {
                conn.Open();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@domainId", (int)EmAppUserType.SaasCompanyAdmin);
                    return (int)cmd.ExecuteScalar();
                }
            }
        }

        private static int RegisterTenantDataSource(int companyId, string dbName, string plainConnStr, string domainToken)
        {
            string encryptedConnStr = AppConnectionStringEncryptionBL.Encrypt(plainConnStr);

            int newDataSourceId;
            using (var adapter = new DataAccessAdapter(AppCompanyBL.AppMasterDBConnectionString))
            {
                var entity = new AppDataSourceRegisterEntity
                {
                    DataSourceOwnerCompanyId = companyId,
                    DataSourceName = dbName,
                    ConnectionString = encryptedConnStr,
                    DatabaseName = dbName,
                    DataSourceType = (int)EmAppDataServerType.SqlServer,
                    IsCompanyMasterDb = true,
                };
                adapter.SaveEntity(entity);
                newDataSourceId = entity.DataSourceId;
            }

            // Write DomainToken via raw SQL (new column, not in LLBLGen entity yet).
            const string updateSql = @"
                UPDATE AppDataSourceRegister
                SET    DomainToken = @token
                WHERE  DataSourceOwnerCompanyId = @companyId
                  AND  DatabaseName = @dbName;";

            using (var conn = new SqlConnection(MasterConnStr))
            {
                conn.Open();
                using (var cmd = new SqlCommand(updateSql, conn))
                {
                    cmd.Parameters.AddWithValue("@token", domainToken.ToLowerInvariant());
                    cmd.Parameters.AddWithValue("@companyId", companyId);
                    cmd.Parameters.AddWithValue("@dbName", dbName);
                    cmd.ExecuteNonQuery();
                }
            }

            return newDataSourceId;
        }

        // V007 seeds DataSourceFrom as NULL; after register, fill NULL → tenant DataSourceId.
        private static void FillNullDataSourceFromReferences(string tenantConnStr, int newDataSourceId)
        {
            if (newDataSourceId <= 0) return;

            const string findTablesSql = @"
                SELECT TABLE_NAME
                FROM   INFORMATION_SCHEMA.COLUMNS
                WHERE  TABLE_SCHEMA = 'dbo'
                  AND  COLUMN_NAME  = 'DataSourceFrom'
                  AND  DATA_TYPE IN ('int', 'bigint', 'smallint')";

            using (var conn = new SqlConnection(tenantConnStr))
            {
                conn.Open();
                var tables = new List<string>();
                using (var cmd = new SqlCommand(findTablesSql, conn))
                using (var rdr = cmd.ExecuteReader())
                    while (rdr.Read()) tables.Add(rdr.GetString(0));

                foreach (var table in tables)
                {
                    if (!Regex.IsMatch(table, @"^[A-Za-z][A-Za-z0-9_]{0,127}$")) continue;
                    string updateSql =
                        $"UPDATE [dbo].[{table}] SET [DataSourceFrom] = @newId WHERE [DataSourceFrom] IS NULL";
                    using (var cmd = new SqlCommand(updateSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@newId", newDataSourceId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        // Finds every dbo table/column that stores an AppDataSourceRegister id and
        // rewrites oldId → newId (e.g. template DataSourceId copied during seed).
        private static void UpdateDataSourceIdReferences(string tenantConnStr, int oldDataSourceId, int newDataSourceId)
        {
            if (oldDataSourceId <= 0 || newDataSourceId <= 0 || oldDataSourceId == newDataSourceId)
                return;

            // Build a safe IN-list from the allowlist (names are code constants, not user input).
            string inList = string.Join(", ", Array.ConvertAll(DataSourceIdColumnNames, c => $"N'{c}'"));
            string findColumnsSql = $@"
                SELECT TABLE_NAME, COLUMN_NAME
                FROM   INFORMATION_SCHEMA.COLUMNS
                WHERE  TABLE_SCHEMA = 'dbo'
                  AND  DATA_TYPE IN ('int', 'bigint', 'smallint')
                  AND  COLUMN_NAME IN ({inList})";

            using (var conn = new SqlConnection(tenantConnStr))
            {
                conn.Open();
                var targets = new List<(string Table, string Column)>();
                using (var cmd = new SqlCommand(findColumnsSql, conn))
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                        targets.Add((rdr.GetString(0), rdr.GetString(1)));
                }

                foreach (var (table, column) in targets)
                {
                    if (!Regex.IsMatch(table, @"^[A-Za-z][A-Za-z0-9_]{0,127}$")) continue;
                    if (!Regex.IsMatch(column, @"^[A-Za-z][A-Za-z0-9_]{0,127}$")) continue;

                    string updateSql =
                        $"UPDATE [dbo].[{table}] SET [{column}] = @newId WHERE [{column}] = @oldId";
                    using (var cmd = new SqlCommand(updateSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@oldId", oldDataSourceId);
                        cmd.Parameters.AddWithValue("@newId", newDataSourceId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        // Copies the tenant's own AppDataSourceRegister row from AppMasterDB into the tenant DB
        // so that FK_*_AppDataSourceRegister constraints are satisfied when we repoint DataSourceFrom.
        private static void RegisterTenantDataSourceInTenantDb(string tenantConnStr, int newDataSourceId)
        {
            const string sql = @"
                IF NOT EXISTS (SELECT 1 FROM [dbo].[AppDataSourceRegister] WHERE DataSourceId = @id)
                BEGIN
                    SET IDENTITY_INSERT [dbo].[AppDataSourceRegister] ON;
                    INSERT INTO [dbo].[AppDataSourceRegister]
                        (DataSourceId, DataSourceName, DataSourceOwnerCompanyId,
                         ConnectionString, DatabaseName, DataSourceType, IsCompanyMasterDb)
                    SELECT
                         DataSourceId, DataSourceName, DataSourceOwnerCompanyId,
                         ConnectionString, DatabaseName, DataSourceType, IsCompanyMasterDb
                    FROM [AppMasterDB].[dbo].[AppDataSourceRegister]
                    WHERE DataSourceId = @id;
                    SET IDENTITY_INSERT [dbo].[AppDataSourceRegister] OFF;
                END";

            using (var conn = new SqlConnection(tenantConnStr))
            {
                conn.Open();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", newDataSourceId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Repairs existing tenants: fill NULL DataSourceFrom, and optionally remap a
        // leftover template/legacy DataSourceId. Safe to call repeatedly.
        public static int RepairDataSourceIdReferences(
            string tenantConnStr,
            int tenantDataSourceId,
            int? templateDataSourceId = null)
        {
            if (string.IsNullOrWhiteSpace(tenantConnStr) || tenantDataSourceId <= 0)
                return 0;

            FillNullDataSourceFromReferences(tenantConnStr, tenantDataSourceId);
            if (templateDataSourceId.HasValue && templateDataSourceId.Value > 0)
                UpdateDataSourceIdReferences(tenantConnStr, templateDataSourceId.Value, tenantDataSourceId);

            return tenantDataSourceId;
        }

        // Backward-compatible wrapper (old name / signature).
        public static void RepairDataSourceFromReferences(string tenantConnStr, int templateDataSourceId, int tenantDataSourceId)
        {
            RepairDataSourceIdReferences(tenantConnStr, tenantDataSourceId, templateDataSourceId);
        }

        // Repairs every registered company-master tenant DB: fill NULL DataSourceFrom
        // (and optionally remap templateDataSourceId) to that tenant's DataSourceId.
        public static Dictionary<string, string> RepairDataSourceIdReferencesOnAllTenants(int? templateDataSourceId = null)
        {
            var results = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var tenants = AppDataSourceRegisterBL.RetrieveAllAppDataSourceRegisterEntity();

            foreach (var tenant in tenants)
            {
                string key = tenant.DatabaseName ?? tenant.DataSourceName ?? tenant.DataSourceId.ToString();
                if (tenant.IsCompanyMasterDb != true)
                {
                    results[key] = "skipped (not company master)";
                    continue;
                }
                if (string.IsNullOrEmpty(tenant.ConnectionString))
                {
                    results[key] = "skipped (no connection string)";
                    continue;
                }

                try
                {
                    string plain = AppConnectionStringEncryptionBL.Decrypt(tenant.ConnectionString);
                    RepairDataSourceIdReferences(plain, tenant.DataSourceId, templateDataSourceId);
                    results[key] = $"ok → DataSourceId={tenant.DataSourceId}";
                }
                catch (Exception ex)
                {
                    results[key] = $"error: {ex.Message}";
                }
            }

            return results;
        }

        // Copies app-definition rows from a registered template DB into the freshly migrated tenant DB.
        // Uses cross-database INSERT … SELECT which requires both DBs on the same SQL Server instance.
        private static void SeedFromTemplateDb(string templateId, string newDbName)
        {
            // Resolve the template DB name from the registry (connection string is encrypted — we only need DatabaseName).
            string templateDbName = ResolveTemplateDatabaseName(templateId);
            if (string.IsNullOrWhiteSpace(templateDbName))
                throw new InvalidOperationException($"Template DB with id '{templateId}' not found in AppDataSourceRegister.");

            // Validate both names to prevent SQL injection.
            if (!Regex.IsMatch(templateDbName, @"^[A-Za-z][A-Za-z0-9_]{1,127}$"))
                throw new ArgumentException($"Invalid template database name: {templateDbName}");
            if (!Regex.IsMatch(newDbName, @"^[A-Za-z][A-Za-z0-9_]{1,127}$"))
                throw new ArgumentException($"Invalid target database name: {newDbName}");

            // App-definition tables to seed (order respects FK dependencies).
            var tables = new[]
            {
                "AppForm", "AppSearch", "AppSearchView", "AppListMenu",
                "AppReport", "AppDesktop", "AppDesktopItem",
                "AppDataSet", "AppEntityInfo", "AppTransaction",
                "AppSecurityGroup", "AppSecurityGroupMember", "AppSecurityEntityAction",
                "AppTenantSetting",
            };

            // Connect via master catalog so we can issue cross-DB queries.
            var builder = new SqlConnectionStringBuilder(MasterConnStr) { InitialCatalog = "master" };
            using (var conn = new SqlConnection(builder.ToString()))
            {
                conn.Open();

                // Disable FK constraints on all target tables before seeding so insertion order
                // doesn't matter. Re-enabled (with re-validation) after all tables are copied.
                foreach (var table in tables)
                {
                    using (var cmd = new SqlCommand(
                        $"IF OBJECT_ID(N'[{newDbName}].[dbo].[{table}]') IS NOT NULL " +
                        $"ALTER TABLE [{newDbName}].[dbo].[{table}] NOCHECK CONSTRAINT ALL", conn))
                        cmd.ExecuteNonQuery();
                }

                foreach (var table in tables)
                {
                    // Skip if either DB doesn't have the table.
                    string existsSql = $@"
                        SELECT CASE WHEN OBJECT_ID(N'[{templateDbName}].[dbo].[{table}]') IS NOT NULL
                                     AND OBJECT_ID(N'[{newDbName}].[dbo].[{table}]')   IS NOT NULL
                                    THEN 1 ELSE 0 END";
                    int exists;
                    using (var cmd = new SqlCommand(existsSql, conn))
                        exists = (int)cmd.ExecuteScalar();
                    if (exists == 0) continue;

                    // Fetch ordered column list from the template DB.
                    // IDENTITY_INSERT ON requires an explicit column list in INSERT — SELECT * is not accepted.
                    string colSql = $@"
                        SELECT COLUMN_NAME
                        FROM [{templateDbName}].INFORMATION_SCHEMA.COLUMNS
                        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = '{table}'
                        ORDER BY ORDINAL_POSITION";
                    var cols = new List<string>();
                    using (var cmd = new SqlCommand(colSql, conn))
                    using (var rdr = cmd.ExecuteReader())
                        while (rdr.Read()) cols.Add($"[{rdr.GetString(0)}]");

                    if (cols.Count == 0) continue;
                    string colList = string.Join(", ", cols);

                    string insertSql = $@"
                        SET IDENTITY_INSERT [{newDbName}].[dbo].[{table}] ON;
                        INSERT INTO [{newDbName}].[dbo].[{table}] ({colList})
                        SELECT {colList} FROM [{templateDbName}].[dbo].[{table}];
                        SET IDENTITY_INSERT [{newDbName}].[dbo].[{table}] OFF;";
                    using (var cmd = new SqlCommand(insertSql, conn))
                    {
                        cmd.CommandTimeout = 120;
                        cmd.ExecuteNonQuery();
                    }
                }

                // Re-enable FK constraints for future inserts.
                // Do NOT use WITH CHECK — seeded rows may reference tables outside the seed list
                // (e.g. AppComOrganization) that are intentionally left empty in a fresh tenant DB.
                foreach (var table in tables)
                {
                    using (var cmd = new SqlCommand(
                        $"IF OBJECT_ID(N'[{newDbName}].[dbo].[{table}]') IS NOT NULL " +
                        $"ALTER TABLE [{newDbName}].[dbo].[{table}] CHECK CONSTRAINT ALL", conn))
                        cmd.ExecuteNonQuery();
                }
            }
        }

        private static string ResolveTemplateDatabaseName(string templateId)
        {
            const string sql = "SELECT TOP 1 DatabaseName FROM AppDataSourceRegister WHERE DataSourceId = @id";
            using (var conn = new SqlConnection(MasterConnStr))
            {
                conn.Open();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    if (int.TryParse(templateId, out int id))
                        cmd.Parameters.AddWithValue("@id", id);
                    else
                        cmd.Parameters.AddWithValue("@id", DBNull.Value);

                    var result = cmd.ExecuteScalar();
                    return result as string;
                }
            }
        }

         // Columns that store an AppDataSourceRegister.DataSourceId FK (not names/types/query text).
        private static readonly string[] DataSourceIdColumnNames =
        {
            "DataSourceFrom",
            "DataSourceFromId",
            "DataSourceRegistrationId",
            "DataSourceRegisterId",
            "DataSourceRegisterID",
            "DataSourceID",
        };

    }
}
