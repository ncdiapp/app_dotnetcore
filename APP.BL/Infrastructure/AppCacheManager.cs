using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
using DatabaseSchemaMrg;
using DatabaseSchemaMrg.DataSchema;
using APP.Components.EntityDto;
using System;
using APP.Framework;
using APP.LBL.DatabaseSpecific;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.LBL;
using System.Threading.Tasks;
using APP.Components.Dto;
using System.Threading;

namespace App.BL
{
    /// <summary>
    /// Thread-safe cache manager with TTL support and memory management.
    /// Provides caching for database schema, transactions, users, and business entities.
    /// </summary>
    public class AppCacheManagerBL
    {
        #region Cache Entry Wrapper with TTL

        /// <summary>
        /// Wrapper class for cached items with expiration support.
        /// </summary>
        private class CacheEntry<T>
        {
            public T Value { get; }
            public DateTime CreatedAt { get; }
            public DateTime? ExpiresAt { get; }

            public CacheEntry(T value, TimeSpan? ttl = null)
            {
                Value = value;
                CreatedAt = DateTime.UtcNow;
                ExpiresAt = ttl.HasValue ? DateTime.UtcNow.Add(ttl.Value) : (DateTime?)null;
            }

            public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;
        }

        #endregion

        #region Cache Configuration

        /// <summary>
        /// Default TTL for cached items (30 minutes).
        /// </summary>
        private static readonly TimeSpan DefaultCacheTtl = TimeSpan.FromMinutes(30);

        /// <summary>
        /// TTL for user-related cache (15 minutes - shorter for security).
        /// </summary>
        private static readonly TimeSpan UserCacheTtl = TimeSpan.FromMinutes(15);

        /// <summary>
        /// TTL for database schema cache (1 hour - schema changes rarely).
        /// </summary>
        private static readonly TimeSpan SchemaCacheTtl = TimeSpan.FromHours(1);

        /// <summary>
        /// Maximum number of form data entries to cache.
        /// </summary>
        private const int MaxFormDataCacheSize = 1000;

        /// <summary>
        /// Maximum number of transaction cache entries.
        /// </summary>
        private const int MaxTransactionCacheSize = 500;

        #endregion

        #region Thread-Safe Cache Dictionaries

        // Database schema caches (long TTL)
        private static readonly ConcurrentDictionary<int, CacheEntry<Dictionary<string, DatabaseTable>>> _dictRegisterIdTableBaseTable
            = new ConcurrentDictionary<int, CacheEntry<Dictionary<string, DatabaseTable>>>();

        private static readonly ConcurrentDictionary<int, CacheEntry<DatabaseFixture>> _dictRegisterIdFixtureInstance
            = new ConcurrentDictionary<int, CacheEntry<DatabaseFixture>>();

        private static readonly ConcurrentDictionary<string, CacheEntry<DatabaseFixture>> _dictConnstringDatabaseFixture
            = new ConcurrentDictionary<string, CacheEntry<DatabaseFixture>>();

        // Company/business caches
        private static readonly ConcurrentDictionary<int, CacheEntry<AppDataSourceRegisterEntity>> _dictCompanyIdUserMasterDbDataSource
            = new ConcurrentDictionary<int, CacheEntry<AppDataSourceRegisterEntity>>();

        private static readonly ConcurrentDictionary<int, CacheEntry<string>> _dictCompanyIdAnonymousToken
            = new ConcurrentDictionary<int, CacheEntry<string>>();

        private static readonly ConcurrentDictionary<string, CacheEntry<bool>> _hashsetCompanyAnonymousToken
            = new ConcurrentDictionary<string, CacheEntry<bool>>();

        private static readonly ConcurrentDictionary<string, CacheEntry<AppBusinessPartnerEntity>> _dictCompanyIdInviteUserIdAppBusinessPartnerEntity
            = new ConcurrentDictionary<string, CacheEntry<AppBusinessPartnerEntity>>();

        private static readonly ConcurrentDictionary<string, CacheEntry<int?>> _dictInviteUserType
            = new ConcurrentDictionary<string, CacheEntry<int?>>();

        private static readonly ConcurrentDictionary<int, CacheEntry<Dictionary<int, int>>> _dictCompanyIdUserIdPartnerId
            = new ConcurrentDictionary<int, CacheEntry<Dictionary<int, int>>>();

        // User caches (shorter TTL for security)
        private static readonly ConcurrentDictionary<int, CacheEntry<AppSecurityUserEntity>> _dictUserIdMasterDbUserEntity
            = new ConcurrentDictionary<int, CacheEntry<AppSecurityUserEntity>>();

        private static readonly ConcurrentDictionary<int, CacheEntry<AppSecurityUserEntity>> _dictLoginUserCache
            = new ConcurrentDictionary<int, CacheEntry<AppSecurityUserEntity>>();

        // Transaction caches
        private static readonly ConcurrentDictionary<string, CacheEntry<AppTransactionExDto>> _dictHierarchyTransactionExdto
            = new ConcurrentDictionary<string, CacheEntry<AppTransactionExDto>>();

        // Workflow caches
        private static readonly ConcurrentDictionary<string, bool> _dictWorkflowBatchNumberAndIsForceStopped
            = new ConcurrentDictionary<string, bool>();

        // Form data cache with size limit
        private static readonly ConcurrentDictionary<string, CacheEntry<AppMasterDetailDto>> _dictGuidAndAppMasterDetailDto
            = new ConcurrentDictionary<string, CacheEntry<AppMasterDetailDto>>();

        #endregion

        #region Locks for Complex Operations

        private static readonly ReaderWriterLockSlim _schemaRefreshLock = new ReaderWriterLockSlim();
        private static readonly SemaphoreSlim _cacheCleanupSemaphore = new SemaphoreSlim(1, 1);
        private static readonly CancellationTokenSource _cleanupCts = new CancellationTokenSource();

        #endregion

        #region Logger and Constants

        internal static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Host company reserved ID. Canonical value defined in AppSystemConstants.HostCompanyId.
        /// </summary>
        public const int HostCompanyReserveId = AppSystemConstants.HostCompanyId;

        /// <summary>
        /// Backward compatible alias for HostCompanyReserveId.
        /// </summary>
        [Obsolete("Use HostCompanyReserveId instead")]
        public const int HostCompnayReserveId = HostCompanyReserveId;

        /// <summary>
        /// Host company database ID.
        /// </summary>
        public const int HostCompanyDatabaseId = int.MaxValue;

        /// <summary>
        /// Backward compatible alias for HostCompanyDatabaseId.
        /// </summary>
        [Obsolete("Use HostCompanyDatabaseId instead")]
        public const int HostCompayDataBaseID = HostCompanyDatabaseId;

        #endregion

        #region Cache Statistics

        private static long _cacheHits = 0;
        private static long _cacheMisses = 0;

        /// <summary>
        /// Gets cache hit ratio for monitoring.
        /// </summary>
        public static double CacheHitRatio
        {
            get
            {
                long hits = Interlocked.Read(ref _cacheHits);
                long misses = Interlocked.Read(ref _cacheMisses);
                long total = hits + misses;
                return total > 0 ? (double)hits / total : 0;
            }
        }

        #endregion

        #region Static Constructor

        static AppCacheManagerBL()
        {
            Task.Run(() => PeriodicCacheCleanupAsync(_cleanupCts.Token));
        }

        #endregion

        #region Cache Initialization

        /// <summary>
        /// Starts cache initialization asynchronously.
        /// </summary>
        public static async Task StartCacheAsync()
        {
            try
            {
                await Task.Run(() => RefreshAllCustomerDbRegAndFixtureCache());
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to start cache initialization");
                throw;
            }
        }

        /// <summary>
        /// Starts cache initialization. Fire-and-forgets StartCacheAsync.
        /// </summary>
        public static void StartCache()
        {
            Task.Run(async () =>
            {
                try { await StartCacheAsync(); }
                catch (Exception ex) { Logger.Error(ex, "StartCache background init failed"); }
            });
        }

        /// <summary>
        /// Refreshes all database registration and fixture caches.
        /// </summary>
        private static void RefreshAllCustomerDbRegAndFixtureCache()
        {
            Logger.Info("RefreshAllCustomerDbRegAndFixtureCache_Start: {0}", DateTime.Now);

            var userDbDatasourceList = AppDataSourceRegisterBL.RetrieveAllAppDataSourceRegisterEntity();

            // Build all fixtures and table dicts in parallel, outside the write lock
            var results = new System.Collections.Concurrent.ConcurrentBag<(AppDataSourceRegisterEntity datasource, DatabaseFixture fixture, Dictionary<string, DatabaseTable> tableDict)>();

            System.Threading.Tasks.Parallel.ForEach(userDbDatasourceList, datasource =>
            {
                try
                {
                    string connStr = AppConnectionStringEncryptionBL.Decrypt(datasource.ConnectionString);
                    var fixture = new DatabaseFixture(
                        connStr,
                        (EmSqlType)datasource.DataSourceType);

                    var tableDict = SetupDataSourceDataBaseTable(datasource.DataSourceId, fixture);
                    results.Add((datasource, fixture, tableDict));
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to load schema for DataSourceId: {0}", datasource.DataSourceId);
                }
            });

            // Acquire write lock only to swap all results in at once
            _schemaRefreshLock.EnterWriteLock();
            try
            {
                foreach (var (datasource, fixture, tableDict) in results)
                {
                    string decryptedKey = AppConnectionStringEncryptionBL.Decrypt(datasource.ConnectionString).Trim();
                    _dictConnstringDatabaseFixture[decryptedKey] =
                        new CacheEntry<DatabaseFixture>(fixture, SchemaCacheTtl);

                    _dictRegisterIdFixtureInstance[datasource.DataSourceId] =
                        new CacheEntry<DatabaseFixture>(fixture, SchemaCacheTtl);

                    _dictRegisterIdTableBaseTable[datasource.DataSourceId] =
                        new CacheEntry<Dictionary<string, DatabaseTable>>(tableDict, SchemaCacheTtl);
                }
            }
            finally
            {
                _schemaRefreshLock.ExitWriteLock();
            }

            Logger.Info("RefreshAllCustomerDbRegAndFixtureCache_End: {0}", DateTime.Now);
        }

        #endregion

        #region Anonymous Token Cache

        /// <summary>
        /// Gets anonymous token for a company.
        /// </summary>
        public static string GetCurrentCompanyAnonymousToken(int currentWorkingCompanyId)
        {
            if (_dictCompanyIdAnonymousToken.TryGetValue(currentWorkingCompanyId, out var tokenEntry) && !tokenEntry.IsExpired)
            {
                Interlocked.Increment(ref _cacheHits);
                return tokenEntry.Value;
            }

            Interlocked.Increment(ref _cacheMisses);

            try
            {
                string anonymousToken = GetAnonymousExternalToken(currentWorkingCompanyId);
                if (!string.IsNullOrEmpty(anonymousToken))
                {
                    _dictCompanyIdAnonymousToken[currentWorkingCompanyId] = new CacheEntry<string>(anonymousToken, DefaultCacheTtl);
                    _hashsetCompanyAnonymousToken[anonymousToken] = new CacheEntry<bool>(true, DefaultCacheTtl);
                    return anonymousToken;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to get anonymous token for company: {0}", currentWorkingCompanyId);
            }

            return null;
        }

        /// <summary>
        /// Gets all company anonymous tokens.
        /// </summary>
        public static HashSet<string> GetAllCompanyAnonymousToken()
        {
            return new HashSet<string>(
                _hashsetCompanyAnonymousToken
                    .Where(kvp => !kvp.Value.IsExpired)
                    .Select(kvp => kvp.Key));
        }

        private static string GetAnonymousExternalToken(int companyId)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(AppCompanyBL.AppMasterDBConnectionString))
            {
                var filter = new RelationPredicateBucket(
                    AppSecurityUserSessionFields.EmExternalSigninType == (int)EmAppExternalLoginType.Anonymous
                    & AppSecurityUserSessionFields.AppCreatedByCompanyId == companyId);

                var list = new EntityCollection<AppSecurityUserSessionEntity>();
                adapter.FetchEntityCollection(list, filter);

                return list.Count > 0 ? list[0].ExternalAcessToken : null;
            }
        }

        #endregion

        #region Business Partner Cache

        /// <summary>
        /// Gets business partner for a user in a company.
        /// </summary>
        internal static AppBusinessPartnerEntity GetCurrentUserWorkingCompanyBusinessPartner(int workingCompanyId, int userId)
        {
            string cacheKey = $"{workingCompanyId}_{userId}";

            if (_dictCompanyIdInviteUserIdAppBusinessPartnerEntity.TryGetValue(cacheKey, out var entry) && !entry.IsExpired)
            {
                Interlocked.Increment(ref _cacheHits);
                return entry.Value;
            }

            Interlocked.Increment(ref _cacheMisses);

            try
            {
                var appDataSourceRegister = GetCurrentCompanyMasterDataSource(workingCompanyId);
                if (appDataSourceRegister == null)
                {
                    Logger.Warn("No master data source found for company: {0}", workingCompanyId);
                    return null;
                }

                string connectInfo = AppConnectionStringEncryptionBL.Decrypt(appDataSourceRegister.ConnectionString);
                var partnerInviteUserList = new EntityCollection<AppBusinessPartnerInviteUserEntity>();

                var prefetchPath = new PrefetchPath2(EntityType.AppBusinessPartnerInviteUserEntity);
                prefetchPath.Add(AppBusinessPartnerInviteUserEntity.PrefetchPathAppBusinessPartner);

                using (DataAccessAdapter adapter = new DataAccessAdapter(connectInfo))
                {
                    adapter.FetchEntityCollection(
                        partnerInviteUserList,
                        new RelationPredicateBucket(
                            AppBusinessPartnerInviteUserFields.AppCreatedByCompanyId == workingCompanyId
                            & AppBusinessPartnerInviteUserFields.UserId == userId
                            & AppBusinessPartnerInviteUserFields.AppBusinessPartnerId != DBNull.Value),
                        prefetchPath);
                }

                var partnerInviteUserEntity = partnerInviteUserList.FirstOrDefault();
                _dictInviteUserType[cacheKey] = new CacheEntry<int?>(
                    partnerInviteUserEntity?.EmInvitedUserType, DefaultCacheTtl);

                if (partnerInviteUserEntity?.AppBusinessPartner != null)
                {
                    var businessPartner = partnerInviteUserEntity.AppBusinessPartner;
                    _dictCompanyIdInviteUserIdAppBusinessPartnerEntity[cacheKey] =
                        new CacheEntry<AppBusinessPartnerEntity>(businessPartner, DefaultCacheTtl);
                    return businessPartner;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to get business partner for company: {0}, user: {1}", workingCompanyId, userId);
            }

            return null;
        }

        /// <summary>
        /// Gets user ID to partner ID mapping for a company.
        /// </summary>
        internal static Dictionary<int, int> GetOneCompanyUserIdAndPartnerIdDictionary(int companyId)
        {
            if (_dictCompanyIdUserIdPartnerId.TryGetValue(companyId, out var entry) && !entry.IsExpired)
            {
                Interlocked.Increment(ref _cacheHits);
                return entry.Value;
            }

            Interlocked.Increment(ref _cacheMisses);

            try
            {
                var dictUserIdAndPartnerId = new Dictionary<int, int>();
                var appDataSourceRegister = GetCurrentCompanyMasterDataSource(companyId);

                if (appDataSourceRegister == null)
                {
                    Logger.Warn("No master data source found for company: {0}", companyId);
                    return dictUserIdAndPartnerId;
                }

                string connectInfo = AppConnectionStringEncryptionBL.Decrypt(appDataSourceRegister.ConnectionString);
                var partnerInviteUserList = new EntityCollection<AppBusinessPartnerInviteUserEntity>();

                using (DataAccessAdapter adapter = new DataAccessAdapter(connectInfo))
                {
                    adapter.FetchEntityCollection(
                        partnerInviteUserList,
                        new RelationPredicateBucket(
                            AppBusinessPartnerInviteUserFields.AppCompanyId == companyId
                            & AppBusinessPartnerInviteUserFields.AppBusinessPartnerId != DBNull.Value));
                }

                foreach (var entity in partnerInviteUserList)
                {
                    if (entity.UserId.HasValue && entity.AppBusinessPartnerId.HasValue
                        && !dictUserIdAndPartnerId.ContainsKey(entity.UserId.Value))
                    {
                        dictUserIdAndPartnerId[entity.UserId.Value] = entity.AppBusinessPartnerId.Value;
                    }
                }

                _dictCompanyIdUserIdPartnerId[companyId] =
                    new CacheEntry<Dictionary<int, int>>(dictUserIdAndPartnerId, DefaultCacheTtl);

                return dictUserIdAndPartnerId;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to get user-partner mapping for company: {0}", companyId);
                return new Dictionary<int, int>();
            }
        }

        #endregion

        #region User Entity Cache

        /// <summary>
        /// Gets user entity from master data source.
        /// </summary>
        internal static AppSecurityUserEntity GetCurrentUserEntityFromMasterDataSource(int userId)
        {
            if (_dictUserIdMasterDbUserEntity.TryGetValue(userId, out var entry) && !entry.IsExpired)
            {
                Interlocked.Increment(ref _cacheHits);
                return entry.Value;
            }

            Interlocked.Increment(ref _cacheMisses);

            try
            {
                var userEntity = AppSecurityUserBL.RetrieveOneAppSecurityUserSimpleEntity(userId);
                if (userEntity != null)
                {
                    _dictUserIdMasterDbUserEntity[userId] =
                        new CacheEntry<AppSecurityUserEntity>(userEntity, UserCacheTtl);
                }
                return userEntity;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to get user entity for userId: {0}", userId);
                return null;
            }
        }

        /// <summary>
        /// Gets user entity from cache.
        /// </summary>
        public static AppSecurityUserEntity GetOneAppSecurityUserEntityFromCache(int userId)
        {
            if (_dictLoginUserCache.TryGetValue(userId, out var entry) && !entry.IsExpired)
            {
                Interlocked.Increment(ref _cacheHits);
                return entry.Value;
            }

            Interlocked.Increment(ref _cacheMisses);

            try
            {
                var userEntity = AppSecurityUserBL.RetrieveOneAppSecurityUserEntity(userId);
                if (userEntity != null)
                {
                    _dictLoginUserCache[userId] =
                        new CacheEntry<AppSecurityUserEntity>(userEntity, UserCacheTtl);
                }
                return userEntity;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to get user entity from cache for userId: {0}", userId);
                return null;
            }
        }

        /// <summary>
        /// Resets user entity cache for a specific user.
        /// </summary>
        public static void ResetMasterDBAppSecurityUserEntityCache(int userId)
        {
            try
            {
                var userEntity = AppSecurityUserBL.RetrieveOneAppSecurityUserEntity(userId);
                if (userEntity != null)
                {
                    _dictLoginUserCache[userId] =
                        new CacheEntry<AppSecurityUserEntity>(userEntity, UserCacheTtl);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to reset user cache for userId: {0}", userId);
            }
        }

        #endregion

        #region Company Master Data Source Cache

        /// <summary>
        /// Gets master data source for a company.
        /// </summary>
        internal static AppDataSourceRegisterEntity GetCurrentCompanyMasterDataSource(int currentWorkingCompanyId)
        {
            if (_dictCompanyIdUserMasterDbDataSource.TryGetValue(currentWorkingCompanyId, out var entry) && !entry.IsExpired)
            {
                Interlocked.Increment(ref _cacheHits);
                return entry.Value;
            }

            Interlocked.Increment(ref _cacheMisses);

            try
            {
                var companyMasterDbList = new EntityCollection<AppDataSourceRegisterEntity>();

                using (DataAccessAdapter adapter = new DataAccessAdapter(AppCompanyBL.AppMasterDBConnectionString))
                {
                    adapter.FetchEntityCollection(
                        companyMasterDbList,
                        new RelationPredicateBucket(
                            AppDataSourceRegisterFields.DataSourceOwnerCompanyId == currentWorkingCompanyId
                            & AppDataSourceRegisterFields.IsCompanyMasterDb == true));
                }

                var currentCompanyMasterDatasource = companyMasterDbList.FirstOrDefault();
                if (currentCompanyMasterDatasource != null)
                {
                    _dictCompanyIdUserMasterDbDataSource[currentWorkingCompanyId] =
                        new CacheEntry<AppDataSourceRegisterEntity>(currentCompanyMasterDatasource, DefaultCacheTtl);
                }

                return currentCompanyMasterDatasource;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to get master data source for company: {0}", currentWorkingCompanyId);
                return null;
            }
        }

        #endregion

        #region Database Schema Cache

        /// <summary>
        /// Gets database table dictionary for a data source.
        /// </summary>
        internal static Dictionary<string, DatabaseTable> GetDictOwnerTablenameDataTable(int dataSourceRegisterId)
        {
            _schemaRefreshLock.EnterReadLock();
            try
            {
                if (_dictRegisterIdTableBaseTable.TryGetValue(dataSourceRegisterId, out var entry) && !entry.IsExpired)
                {
                    Interlocked.Increment(ref _cacheHits);
                    return entry.Value;
                }
            }
            finally
            {
                _schemaRefreshLock.ExitReadLock();
            }

            Interlocked.Increment(ref _cacheMisses);
            SetupOneDbRegisterFixtureAndDatabaseTableCache(dataSourceRegisterId);

            _schemaRefreshLock.EnterReadLock();
            try
            {
                return _dictRegisterIdTableBaseTable.TryGetValue(dataSourceRegisterId, out var newEntry)
                    ? newEntry.Value
                    : new Dictionary<string, DatabaseTable>();
            }
            finally
            {
                _schemaRefreshLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Gets database fixture for a data source.
        /// </summary>
        internal static DatabaseFixture GetOneDatabaseFixture(int dataSourceRegisterId)
        {
            _schemaRefreshLock.EnterReadLock();
            try
            {
                if (_dictRegisterIdFixtureInstance.TryGetValue(dataSourceRegisterId, out var entry) && !entry.IsExpired)
                {
                    Interlocked.Increment(ref _cacheHits);
                    return entry.Value;
                }
            }
            finally
            {
                _schemaRefreshLock.ExitReadLock();
            }

            Interlocked.Increment(ref _cacheMisses);
            SetupOneDbRegisterFixtureAndDatabaseTableCache(dataSourceRegisterId);

            _schemaRefreshLock.EnterReadLock();
            try
            {
                return _dictRegisterIdFixtureInstance.TryGetValue(dataSourceRegisterId, out var newEntry)
                    ? newEntry.Value
                    : null;
            }
            finally
            {
                _schemaRefreshLock.ExitReadLock();
            }
        }

        // Returns a DatabaseFixture connected to the hosting AppMasterDB (from web.config).
        // Used for entities like AppSecurityUser that live in AppMasterDB, not in tenant DBs.
        internal static DatabaseFixture GetMasterDbFixture()
        {
            string connStr = AppCompanyBL.AppMasterDBConnectionString.Trim();

            _schemaRefreshLock.EnterReadLock();
            try
            {
                if (_dictConnstringDatabaseFixture.TryGetValue(connStr, out var entry) && !entry.IsExpired)
                    return entry.Value;
            }
            finally
            {
                _schemaRefreshLock.ExitReadLock();
            }

            var fixture = new DatabaseFixture(connStr, EmSqlType.SqlServer);

            _schemaRefreshLock.EnterWriteLock();
            try
            {
                _dictConnstringDatabaseFixture[connStr] = new CacheEntry<DatabaseFixture>(fixture, SchemaCacheTtl);
            }
            finally
            {
                _schemaRefreshLock.ExitWriteLock();
            }

            return fixture;
        }

        /// <summary>
        /// Refreshes cache for a specific data source.
        /// </summary>
        internal static void RefreshOneCustomerDbRegAndFixtureCache(int? dataSourceRegisterId)
        {
            if (!dataSourceRegisterId.HasValue) return;
            SetupOneDbRegisterFixtureAndDatabaseTableCache(dataSourceRegisterId.Value, true);
        }

        private static void SetupOneDbRegisterFixtureAndDatabaseTableCache(int dataSourceRegisterId, bool isForceRefreshCache = false)
        {
            // Double-check before doing any expensive work
            _schemaRefreshLock.EnterReadLock();
            try
            {
                if (_dictRegisterIdTableBaseTable.TryGetValue(dataSourceRegisterId, out var existing) && !existing.IsExpired && !isForceRefreshCache)
                    return;
            }
            finally
            {
                _schemaRefreshLock.ExitReadLock();
            }

            // Do all expensive DB work OUTSIDE the write lock so readers are not blocked
            var datasource = AppDataSourceRegisterBL.RetrieveOneAppDataSourceRegisterEntity(dataSourceRegisterId);
            if (datasource == null)
            {
                Logger.Warn("Data source not found: {0}", dataSourceRegisterId);
                return;
            }

            string decryptedConnStr = AppConnectionStringEncryptionBL.Decrypt(datasource.ConnectionString);
            var fixture = new DatabaseFixture(decryptedConnStr, (EmSqlType)datasource.DataSourceType);
            var tableDict = SetupDataSourceDataBaseTable(dataSourceRegisterId, fixture);

            // Acquire write lock only to swap in the result
            _schemaRefreshLock.EnterWriteLock();
            try
            {
                // Double-check: another thread may have loaded while we were doing the DB work above
                if (_dictRegisterIdTableBaseTable.TryGetValue(dataSourceRegisterId, out var existing) && !existing.IsExpired && !isForceRefreshCache)
                    return;

                _dictConnstringDatabaseFixture[decryptedConnStr.Trim()] =
                    new CacheEntry<DatabaseFixture>(fixture, SchemaCacheTtl);

                _dictRegisterIdFixtureInstance[datasource.DataSourceId] =
                    new CacheEntry<DatabaseFixture>(fixture, SchemaCacheTtl);

                _dictRegisterIdTableBaseTable[dataSourceRegisterId] =
                    new CacheEntry<Dictionary<string, DatabaseTable>>(tableDict, SchemaCacheTtl);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to setup database cache for DataSourceId: {0}", dataSourceRegisterId);
                throw;
            }
            finally
            {
                _schemaRefreshLock.ExitWriteLock();
            }
        }

        private static Dictionary<string, DatabaseTable> SetupDataSourceDataBaseTable(
            int dataSourceRegisterId,
            DatabaseFixture userDatabaseFixtureInstance)
        {
            var dictCustomerDataBaseTable = userDatabaseFixtureInstance.AllTables()
                .ToDictionary(
                    o => AppMetaDataBL.GetOwnerTableKey(o.SchemaOwner, o.Name),
                    o => o);

            var viewTable = userDatabaseFixtureInstance.AllViews();
            foreach (var view in viewTable)
            {
                string ownerViewKey = AppMetaDataBL.GetOwnerTableKey(view.SchemaOwner, view.Name);
                dictCustomerDataBaseTable[ownerViewKey] = view;
            }

            // Update each base table resource key
            foreach (var table in dictCustomerDataBaseTable.Values)
            {
                table.DataSourceRegisterId = dataSourceRegisterId;
            }

            return dictCustomerDataBaseTable;
        }

        /// <summary>
        /// Refreshes cache for a specific table.
        /// </summary>
        internal static void RefreshOneTableCache(string tableName, int? dataBaseRegisterId, string schemaOwner)
        {
            if (!dataBaseRegisterId.HasValue) return;

            _schemaRefreshLock.EnterWriteLock();
            try
            {
                if (!_dictRegisterIdFixtureInstance.TryGetValue(dataBaseRegisterId.Value, out var fixtureEntry))
                {
                    Logger.Warn("Fixture not found for DataSourceId: {0}", dataBaseRegisterId.Value);
                    return;
                }

                var customerDatabaseFixture = fixtureEntry.Value;
                customerDatabaseFixture.DatabaseSchema.Owner = schemaOwner;

                var databaseTable = customerDatabaseFixture.Table(tableName);

                if (_dictRegisterIdTableBaseTable.TryGetValue(dataBaseRegisterId.Value, out var tableEntry))
                {
                    string schemaTableKey = AppMetaDataBL.GetOwnerTableKey(schemaOwner, tableName);
                    // Copy the dictionary to avoid mutating a shared reference held by other threads
                    var newDict = new Dictionary<string, DatabaseTable>(tableEntry.Value);
                    newDict[schemaTableKey] = databaseTable;
                    _dictRegisterIdTableBaseTable[dataBaseRegisterId.Value] =
                        new CacheEntry<Dictionary<string, DatabaseTable>>(newDict, SchemaCacheTtl);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to refresh table cache: {0}.{1}", schemaOwner, tableName);
            }
            finally
            {
                _schemaRefreshLock.ExitWriteLock();
            }
        }

        #endregion

        #region Transaction Cache

        /// <summary>
        /// Refreshes transaction hierarchy cache.
        /// </summary>
        internal static void RefreshOneHierarchyTransaction(object transactionId)
        {
            try
            {
                string transactionKey = $"{ServerContext.Instance.CurrentUserDbConnectionString}_{transactionId}";
                var dto = AppTransactionBL.GetHierarchyTranscationFromDatabase(transactionId);

                EnforceCacheSizeLimit(_dictHierarchyTransactionExdto, MaxTransactionCacheSize);

                _dictHierarchyTransactionExdto[transactionKey] =
                    new CacheEntry<AppTransactionExDto>(dto, DefaultCacheTtl);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to refresh transaction cache for: {0}", transactionId);
            }
        }

        /// <summary>
        /// Gets transaction hierarchy from cache.
        /// </summary>
        public static AppTransactionExDto GetOneHierarchyTransactionFromCache(object transactionId)
        {
            string transactionKey = $"{ServerContext.Instance.CurrentUserDbConnectionString}_{transactionId}";

            if (_dictHierarchyTransactionExdto.TryGetValue(transactionKey, out var entry) && !entry.IsExpired)
            {
                Interlocked.Increment(ref _cacheHits);
                return entry.Value;
            }

            Interlocked.Increment(ref _cacheMisses);

            try
            {
                var transactionEntity = AppTransactionBL.RetrieveOneAppTransactionEntity(transactionId);
                if (transactionEntity == null)
                {
                    Logger.Warn("Transaction not found: {0}", transactionId);
                    return null;
                }

                var transactionExDto = AppTransactionBL.ConvertTransactionEntityToExdto(transactionEntity);
                transactionExDto.CommandActionList = AppTransactionCommandBL.RetrieveOneTransactionCommandActionList(transactionExDto);

                AppTransactionBL.SetupHierarchyUnit(transactionExDto);
                AppTransactionBL.PrepareApiTransactionProperties(transactionExDto);

                EnforceCacheSizeLimit(_dictHierarchyTransactionExdto, MaxTransactionCacheSize);

                _dictHierarchyTransactionExdto[transactionKey] =
                    new CacheEntry<AppTransactionExDto>(transactionExDto, DefaultCacheTtl);

                return transactionExDto;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to get transaction from cache: {0}", transactionId);
                return null;
            }
        }

        #endregion

        #region DatabaseTable Getters

        /// <summary>
        /// Gets database table for a transaction unit.
        /// </summary>
        public static DatabaseTable GetDatabaseTable(AppTransactionUnitExDto appTransactionUnit)
        {
            if (appTransactionUnit == null)
            {
                throw new ArgumentNullException(nameof(appTransactionUnit));
            }

            if (!appTransactionUnit.DataSourceFrom.HasValue)
            {
                appTransactionUnit.DataSourceFrom = ServerContext.Instance.DataSourceId;
            }

            string dataBaseTableName = !string.IsNullOrWhiteSpace(appTransactionUnit.BaseDataBaseTableName)
                ? appTransactionUnit.BaseDataBaseTableName
                : appTransactionUnit.DataBaseTableName;

            string schemaTableKey = AppMetaDataBL.GetOwnerTableKey(appTransactionUnit.SchemaOwner, dataBaseTableName);

            var tableDict = GetDictOwnerTablenameDataTable(appTransactionUnit.DataSourceFrom.Value);
            return tableDict.TryGetValue(schemaTableKey, out var table) ? table : null;
        }

        /// <summary>
        /// Gets database table by name.
        /// </summary>
        public static DatabaseTable GetDatabaseTable(string tableName, int? dataBaseRegisterId, string schemaOwner)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                return null;
            }

            if (!dataBaseRegisterId.HasValue)
            {
                dataBaseRegisterId = ServerContext.Instance.DataSourceId;
            }

            if (string.IsNullOrEmpty(schemaOwner))
            {
                var dataBaseFixture = GetOneDatabaseFixture(dataBaseRegisterId.Value);
                schemaOwner = dataBaseFixture?.CurrentOwner ?? "dbo";
            }

            string schemaTableKey = AppMetaDataBL.GetOwnerTableKey(schemaOwner, tableName);
            var dictOwnerTablenameDataTable = GetDictOwnerTablenameDataTable(dataBaseRegisterId.Value);

            return dictOwnerTablenameDataTable.TryGetValue(schemaTableKey, out var table) ? table : null;
        }

        #endregion

        #region Form Data Cache (with size limit)

        /// <summary>
        /// Gets form data from cache by GUID.
        /// </summary>
        public static AppMasterDetailDto GetMasterDetailFormDataFromCacheByGuid(string guid)
        {
            if (string.IsNullOrWhiteSpace(guid)) return null;

            if (_dictGuidAndAppMasterDetailDto.TryGetValue(guid, out var entry) && !entry.IsExpired)
            {
                Interlocked.Increment(ref _cacheHits);
                return entry.Value;
            }

            Interlocked.Increment(ref _cacheMisses);
            return null;
        }

        /// <summary>
        /// Puts form data into cache.
        /// </summary>
        public static string PutMasterDetailFormDataToCacheByGuid(AppMasterDetailDto formData)
        {
            if (formData == null) return null;

            EnforceCacheSizeLimit(_dictGuidAndAppMasterDetailDto, MaxFormDataCacheSize);

            string guid = Guid.NewGuid().ToString();
            _dictGuidAndAppMasterDetailDto[guid] =
                new CacheEntry<AppMasterDetailDto>(formData, TimeSpan.FromMinutes(10));

            return guid;
        }

        /// <summary>
        /// Removes form data from cache.
        /// </summary>
        public static bool RemoveOneKeyFromDictGuidAndAppMasterDetailDto(string guid)
        {
            if (string.IsNullOrWhiteSpace(guid)) return false;
            return _dictGuidAndAppMasterDetailDto.TryRemove(guid, out _);
        }

        // Backward compatibility alias
        public static string PutMasterDetailFormDataFromToCacheByGuid(AppMasterDetailDto formData)
            => PutMasterDetailFormDataToCacheByGuid(formData);

        #endregion

        #region Workflow Cache

        /// <summary>
        /// Gets workflow force stopped status.
        /// </summary>
        internal static bool GetIsWorkflowForceStoppedByBatchIdFromCache(string batchNumber)
        {
            if (string.IsNullOrWhiteSpace(batchNumber)) return false;
            return _dictWorkflowBatchNumberAndIsForceStopped.TryGetValue(batchNumber, out bool value) && value;
        }

        /// <summary>
        /// Sets workflow force stopped status.
        /// </summary>
        internal static void SetIsWorkflowForceStoppedByBatchIdFromCache(string batchNumber, bool isForceStopped)
        {
            if (string.IsNullOrWhiteSpace(batchNumber)) return;
            _dictWorkflowBatchNumberAndIsForceStopped[batchNumber] = isForceStopped;
        }

        #endregion

 

        #region Backward Compatibility Aliases

        // These methods maintain backward compatibility with existing code that uses old method names with typos

        /// <summary>
        /// Backward compatible alias for GetCurrentCompanyAnonymousToken.
        /// </summary>
        [Obsolete("Use GetCurrentCompanyAnonymousToken instead")]
        public static string GetCurrentCompanyAnoymousToken(int currentWorkingCompanyId)
            => GetCurrentCompanyAnonymousToken(currentWorkingCompanyId);

        /// <summary>
        /// Backward compatible alias for GetAllCompanyAnonymousToken.
        /// </summary>
        [Obsolete("Use GetAllCompanyAnonymousToken instead")]
        public static HashSet<string> GetAllCompnayAnoymouToken()
            => GetAllCompanyAnonymousToken();

        internal static int? GetInviteUserType(int workingCompanyId, int userId)
        {
            string cacheKey = $"{workingCompanyId}_{userId}";
            if (_dictInviteUserType.TryGetValue(cacheKey, out var entry) && !entry.IsExpired)
                return entry.Value;
            // warm both caches in one DB call
            GetCurrentUserWorkingCompanyBusinessPartner(workingCompanyId, userId);
            return _dictInviteUserType.TryGetValue(cacheKey, out entry) ? entry.Value : null;
        }

        /// <summary>
        /// Backward compatible alias for GetCurrentUserWorkingCompanyBusinessPartner.
        /// </summary>
        [Obsolete("Use GetCurrentUserWorkingCompanyBusinessPartner instead")]
        internal static AppBusinessPartnerEntity GetCurrentUserWorkingCompanyBinessPartner(int workingCompanyId, int userId)
            => GetCurrentUserWorkingCompanyBusinessPartner(workingCompanyId, userId);

        /// <summary>
        /// Backward compatible alias for RefreshOneHierarchyTransaction.
        /// </summary>
        [Obsolete("Use RefreshOneHierarchyTransaction instead")]
        internal static void RefreshOnetHierarchyTranscation(object transactionId)
            => RefreshOneHierarchyTransaction(transactionId);

        /// <summary>
        /// Backward compatible alias for GetOneHierarchyTransactionFromCache.
        /// </summary>
        [Obsolete("Use GetOneHierarchyTransactionFromCache instead")]
        public static AppTransactionExDto GetOnetHierarchyTranscationFromCache(object transactionId)
            => GetOneHierarchyTransactionFromCache(transactionId);

        #endregion

        #region Cache Maintenance

        /// <summary>
        /// Enforces size limit on a cache dictionary by removing oldest expired entries.
        /// </summary>
        private static void EnforceCacheSizeLimit<TKey, TValue>(
            ConcurrentDictionary<TKey, CacheEntry<TValue>> cache,
            int maxSize)
        {
            if (cache.Count <= maxSize) return;

            // Remove expired entries first
            var expiredKeys = cache
                .Where(kvp => kvp.Value.IsExpired)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                cache.TryRemove(key, out _);
            }

            // If still over limit, remove oldest entries
            if (cache.Count > maxSize)
            {
                var keysToRemove = cache
                    .OrderBy(kvp => kvp.Value.CreatedAt)
                    .Take(cache.Count - maxSize + (maxSize / 10)) // Remove 10% extra
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    cache.TryRemove(key, out _);
                }
            }
        }

        /// <summary>
        /// Periodic background task to clean up expired cache entries.
        /// </summary>
        private static async Task PeriodicCacheCleanupAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);
                    await CleanupExpiredEntriesAsync();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error in periodic cache cleanup");
                }
            }
        }

        /// <summary>
        /// Stops the background cleanup task. Call on application shutdown.
        /// </summary>
        public static void Shutdown()
        {
            _cleanupCts.Cancel();
        }

        /// <summary>
        /// Cleans up expired cache entries.
        /// </summary>
        private static async Task CleanupExpiredEntriesAsync()
        {
            if (!await _cacheCleanupSemaphore.WaitAsync(0)) return;

            try
            {
                CleanupCache(_dictRegisterIdTableBaseTable);
                CleanupCache(_dictRegisterIdFixtureInstance);
                CleanupCache(_dictConnstringDatabaseFixture);
                CleanupCache(_dictCompanyIdUserMasterDbDataSource);
                CleanupCache(_dictCompanyIdAnonymousToken);
                CleanupCache(_hashsetCompanyAnonymousToken);
                CleanupCache(_dictCompanyIdInviteUserIdAppBusinessPartnerEntity);
                CleanupCache(_dictInviteUserType);
                CleanupCache(_dictCompanyIdUserIdPartnerId);
                CleanupCache(_dictUserIdMasterDbUserEntity);
                CleanupCache(_dictLoginUserCache);
                CleanupCache(_dictHierarchyTransactionExdto);
                CleanupCache(_dictGuidAndAppMasterDetailDto);

                Logger.Debug("Cache cleanup completed. Hit ratio: {0:P2}", CacheHitRatio);
            }
            finally
            {
                _cacheCleanupSemaphore.Release();
            }
        }

        private static void CleanupCache<TKey, TValue>(ConcurrentDictionary<TKey, CacheEntry<TValue>> cache)
        {
            var expiredKeys = cache
                .Where(kvp => kvp.Value.IsExpired)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                cache.TryRemove(key, out _);
            }
        }

        /// <summary>
        /// Clears all caches. Use with caution.
        /// </summary>
        public static void ClearAllCaches()
        {
            _schemaRefreshLock.EnterWriteLock();
            try
            {
                _dictRegisterIdTableBaseTable.Clear();
                _dictRegisterIdFixtureInstance.Clear();
                _dictConnstringDatabaseFixture.Clear();
                _dictCompanyIdUserMasterDbDataSource.Clear();
                _dictCompanyIdAnonymousToken.Clear();
                _hashsetCompanyAnonymousToken.Clear();
                _dictCompanyIdInviteUserIdAppBusinessPartnerEntity.Clear();
                _dictInviteUserType.Clear();
                _dictCompanyIdUserIdPartnerId.Clear();
                _dictUserIdMasterDbUserEntity.Clear();
                _dictLoginUserCache.Clear();
                _dictHierarchyTransactionExdto.Clear();
                _dictWorkflowBatchNumberAndIsForceStopped.Clear();
                _dictGuidAndAppMasterDetailDto.Clear();

            

                Interlocked.Exchange(ref _cacheHits, 0);
                Interlocked.Exchange(ref _cacheMisses, 0);

                Logger.Info("All caches cleared");
            }
            finally
            {
                _schemaRefreshLock.ExitWriteLock();
            }
        }

        #endregion
    }
}
