using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Components.Dto;
using DatabaseSchemaMrg;
using APP.Components.EntityDto;

#if NETFRAMEWORK
using System.Management.Automation.Language;
#endif

using APP.Framework;
namespace App.BL
{
    public static partial class AppEntityInfoBL
    {


        public static List<LookupItemDto> GetLookupItemListByCode(string entityInfoCode, string pkInClause)
        {

            List<LookupItemDto> toReturn = new List<LookupItemDto>();

            AppEntityInfoEntity EntityInfo = RetrieveOneAppEntityInfoEntityWithCode(entityInfoCode);



            toReturn = GetEntityInLookupItem(toReturn, EntityInfo, pkInClause);


            //if(string.IsNullOrEmpty (EntityInfo.

            //TODO---------

            //toReturn.Sort((x, y) => x.Display.CompareTo(y.Display));

            return toReturn.OrderBy(o => o.Display).ToList();



        }

        public static List<LookupItemDto> GetLookupItemList(int entityInfoID, string pkInClause, bool isNeedToCheckSeccurity = true)
        {

            List<LookupItemDto> toReturn = new List<LookupItemDto>();

            AppEntityInfoEntity EntityInfo = RetrieveOneAppEntityInfoEntity(entityInfoID);


            //isNeedToCheckSeccurity
            EntityInfo.IsSharedbyMutipleCompany = isNeedToCheckSeccurity;

            toReturn = GetEntityInLookupItem(toReturn, EntityInfo, pkInClause);


            return toReturn;

        }

        public static List<LookupItemDto> AddOneLookupItemList(int entityInfoID, string displayField1)
        {

            List<LookupItemDto> toReturn = new List<LookupItemDto>();

            AppEntityInfoEntity EntityInfo = RetrieveOneAppEntityInfoEntity(entityInfoID);
            if (EntityInfo.EntityType == (int)EmAppEntityType.SystemDefineTable)
            {
                string insert = string.Format(@"INSERT INTO [{0}] ([{1}]) VALUES (@displayField1)", EntityInfo.TableName, EntityInfo.DisplayFiled1);
                List<SqlParameter> listParamters = new List<SqlParameter>();

                var sqlParameter = new SqlParameter("@displayField1", displayField1);
                listParamters.Add(sqlParameter);

                try
                {
                    using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
                    {
                        adpater.ExecuteScalarQuery(insert, listParamters);

                    }
                }
                catch (Exception ex)
                {
                }
            }


            return GetEntityInLookupItem(toReturn, EntityInfo, "");

        }


        //public static List<LookupItemDto> RetrieveAutoCompleteDDLEntityItemSource(int? entityId, string queryText)
        //{
        //    List<LookupItemDto> toReturn = new List<LookupItemDto>();

        //    if (entityId.HasValue && !string.IsNullOrWhiteSpace(queryText))
        //    {
        //        var fullList = GetLookupItemList(entityId.Value, string.Empty);

        //        toReturn = fullList.Where(o => o.Display.ToLower().Contains(queryText.ToLower())).Take(200).ToList();
        //    }

        //    return toReturn;
        //}


        public static List<LookupItemDto> RetrieveAutoCompleteDDLEntityItemSource(AppMasterDetailDto formDataDto)
        {
            List<LookupItemDto> toReturn = new List<LookupItemDto>();


            AppTransactionExDto hierarchyTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(formDataDto.TransactionId);
            List<AppTransactionFieldExDto> allFiedExdtoList = hierarchyTransactionExDto.DictAllTransactionField.Values.ToList();
            Dictionary<int, AppTransactionFieldExDto> dictTransField = allFiedExdtoList.ToDictionary(o => (int)o.Id, o => o);
      
            var rootUnit = hierarchyTransactionExDto.RootMasterUnit;

            int? fieldId = formDataDto.CurrentAutoCompleteFieldId;
            //int? unitId = formDataDto.CurrentAutoCompleteUnitId;
            string queryText = formDataDto.CurrentAutoCompleteFieldQueryText;


            if (fieldId.HasValue)
            {
                AppTransactionFieldExDto fieldExDto = dictTransField[fieldId.Value];

                if (fieldExDto.EntityId.HasValue)
                {
                    int entityId = fieldExDto.EntityId.Value;

                    bool isCascadingChild = fieldExDto.DdlparentLevelId.HasValue;

                    if (isCascadingChild)
                    {
                        int? cascadingTrigerFieldId = fieldExDto.DdlparentLevelId;
                        if (cascadingTrigerFieldId.HasValue)
                        {
                            var cascadingTrigerFieldExDto = dictTransField[cascadingTrigerFieldId.Value];
                            cascadingTrigerFieldExDto.CascadngChildren = new List<AppTransactionFieldExDto>();

                            cascadingTrigerFieldExDto.CascadngChildren.Add(fieldExDto);

                            var cascadingTrigerUnitExDto = hierarchyTransactionExDto.DictAllTransactionUnitIdExDto[cascadingTrigerFieldExDto.TransactionUnitId.ToString()];

                            if (formDataDto.CurrentEditRowDto == null) // Root Unit
                            {
                                formDataDto.DictCascadingFiledDataSource = new Dictionary<string, List<LookupItemDto>>();
                                AppCascadingBL.SetupOneUnitCascadingDataSource(formDataDto.DictCascadingFiledDataSource, formDataDto.DictOneToOneFields, cascadingTrigerUnitExDto, cascadingTrigerFieldExDto, true, formDataDto, null, false);

                                if (formDataDto.DictCascadingFiledDataSource.ContainsKey(fieldId.Value.ToString()))
                                {
                                    toReturn = formDataDto.DictCascadingFiledDataSource[fieldId.Value.ToString()];
                                }

                            }
                            else
                            {
                                formDataDto.CurrentEditRowDto.DictCascadingFiledDataSource = new Dictionary<string, List<LookupItemDto>>();
                                AppCascadingBL.SetupOneUnitCascadingDataSource(formDataDto.CurrentEditRowDto.DictCascadingFiledDataSource, formDataDto.CurrentEditRowDto.DictOneToOneFields, cascadingTrigerUnitExDto, cascadingTrigerFieldExDto, true, formDataDto, null, false);

                                if (formDataDto.CurrentEditRowDto.DictCascadingFiledDataSource.ContainsKey(fieldId.Value.ToString()))
                                {
                                    toReturn = formDataDto.CurrentEditRowDto.DictCascadingFiledDataSource[fieldId.Value.ToString()];
                                }
                            }
                            

                            

                        }
                    }
                    else
                    {
                        toReturn = GetLookupItemList(entityId, string.Empty);
                        
                    }                   

                  

                }



            }

            if (!string.IsNullOrWhiteSpace(queryText))
            { 
                toReturn = toReturn.Where(o => o.Display.ToLower().Contains(queryText.ToLower())).Take(200).ToList();
            }


            

            return toReturn;



        }



        public static List<string> GetEntityFirstDisplayFiedList(int entityId, List<object> valuIds)
        {
            List<string> listString = new List<string>();


            var dictLookitList = AppEntityInfoBL.GetLookupItemList(entityId, "")
                .ToDictionary(o => o.Id.ToString(), o => o.FirstField); ;
            List<string> valuIdStrings = valuIds.Select(o => o.ToString()).ToList();

            foreach (string key in valuIdStrings)
            {
                if (dictLookitList.ContainsKey(key))
                {
                    listString.Add(dictLookitList[key]);
                }

            }

            //listString = dictLookitList.Where(o => valuIds.Contains(o.Key)).Select(o => o.Value).ToList(); ;

            // var resul = valuIdStrings.Any() && valuIdStrings.All(key => lookitList.ContainsKey(key));

            return listString;
        }



        private static List<LookupItemDto> GetEntityInLookupItem(List<LookupItemDto> toReturn, AppEntityInfoEntity entityInfo, string pkInClause)
        {
            if (entityInfo.EntityType == (int)EmAppEntityType.SystemDefineTable)
            {
                GetSystemTableNoQueryLookupItem(toReturn, entityInfo, pkInClause);

                toReturn.Sort((x, y) => x.Display.CompareTo(y.Display));


               
             





            }
            else if (entityInfo.EntityType == (int)EmAppEntityType.SimpleQuery)
            {
                if (!string.IsNullOrWhiteSpace(entityInfo.QueryText))
                {
                    GetSimpleQueryLookupItem(toReturn, entityInfo, pkInClause);


                }

            }
            else if (entityInfo.EntityType == (int)EmAppEntityType.Enum)
            {


                toReturn = GetEnumValueList(entityInfo);

                toReturn.Sort((x, y) => x.Display.CompareTo(y.Display));


            }



            else if (entityInfo.EntityType == (int)EmAppEntityType.SimpleValueList)
            {


                toReturn = GetSimpleValueList(entityInfo);

                //toReturn.Sort((x, y) => x.Display.CompareTo(y.Display));


            }
            // entityInfo.IsSharedbyMutipleCompany = isNeedToCheckSecurity

            if (entityInfo.IsSharedbyMutipleCompany.HasValue && entityInfo.IsSharedbyMutipleCompany.Value && ServerContext.Instance.CurrnetClientIdentity != null)
            {
                // only show customerId
                if (ServerContext.Instance.CurrnetClientIdentity.CurrentLoginUserType == (int)EmAppUserType.Customer)
                {
                    int? mappingToExTableEntity = AppTenantSettingBL.GetIntValue(EmTenantSettings.CustomerEntity);

                    if (mappingToExTableEntity.HasValue && mappingToExTableEntity.Value == entityInfo.EntityInfoId)
                    {
                        string curretnPartnerId = ServerContext.Instance.CurrnetClientIdentity.CurrentPartnerId.ToString(); //as string;

                        if (!string.IsNullOrEmpty(curretnPartnerId))
                        {
                            toReturn = toReturn.Where(o => o.Id.ToString() == curretnPartnerId.Trim()).ToList();
                        }
                    }
                }

                else if (ServerContext.Instance.CurrnetClientIdentity.CurrentLoginUserType == (int)EmAppUserType.Supplier)
                {
                    int? mappingToExTableEntity = AppTenantSettingBL.GetIntValue(EmTenantSettings.SupplierEntity);

                    if (mappingToExTableEntity.HasValue && mappingToExTableEntity.Value == entityInfo.EntityInfoId)
                    {
                        string curretnPartnerId = ServerContext.Instance.CurrnetClientIdentity.CurrentPartnerId.ToString();

                        if (!string.IsNullOrEmpty(curretnPartnerId))
                        {
                            toReturn = toReturn.Where(o => o.Id.ToString() == curretnPartnerId.Trim()).ToList();
                        }
                    }
                }

                else if (ServerContext.Instance.CurrnetClientIdentity.CurrentLoginUserType == (int)EmAppUserType.ClientAgent)
                {
                    int? mappingToExTableEntity = AppTenantSettingBL.GetIntValue(EmTenantSettings.CustomerAgentEntity);

                    if (mappingToExTableEntity.HasValue && mappingToExTableEntity.Value == entityInfo.EntityInfoId)
                    {
                        string curretnPartnerId = ServerContext.Instance.CurrnetClientIdentity.CurrentPartnerId.ToString();

                        if (!string.IsNullOrEmpty(curretnPartnerId))
                        {
                            toReturn = toReturn.Where(o => o.Id.ToString() == curretnPartnerId.Trim()).ToList();
                        }
                    }
                }

                else if (ServerContext.Instance.CurrnetClientIdentity.CurrentLoginUserType == (int)EmAppUserType.SupplierAgent)
                {
                    int? mappingToExTableEntity = AppTenantSettingBL.GetIntValue(EmTenantSettings.SupplierAgentEntity);

                    if (mappingToExTableEntity.HasValue && mappingToExTableEntity.Value == entityInfo.EntityInfoId)
                    {
                        string curretnPartnerId = ServerContext.Instance.CurrnetClientIdentity.CurrentPartnerId.ToString();

                        if (!string.IsNullOrEmpty(curretnPartnerId))
                        {
                            toReturn = toReturn.Where(o => o.Id.ToString() == curretnPartnerId.Trim()).ToList();
                        }
                    }
                }

                //else if (ServerContext.Instance.CurrnetClientIdentity.CurrentLoginUserType == (int)EmAppUserType.Employee)
                //{
                //    int? mappingToExTableEntity = AppTenantSettingBL.GetIntValue(EmTenantSettings.EmployeeEntity);

                //    if (mappingToExTableEntity.HasValue && mappingToExTableEntity.Value == entityInfo.EntityInfoId)
                //    {
                //        string curretnEmployeeId = ServerContext.Instance.CurrnetClientIdentity.RuningTimeBusinessAccountId as string;

                //        if (!string.IsNullOrEmpty(curretnEmployeeId))
                //        {
                //            toReturn = toReturn.Where(o => o.Id.ToString() == curretnEmployeeId.Trim()).ToList();
                //        }
                //    }
                //}

            }




            return toReturn;
        }

        private static void GetSystemTableNoQueryLookupItem(List<LookupItemDto> toReturn, AppEntityInfoEntity entityInfo, string pkInClause)
        {
            string splitToken = "+' | '+";

            DatabaseFixture databaseFixtureInstance = AppCacheManagerBL.GetOneDatabaseFixture(entityInfo.DataSourceFrom.Value);

            // Three-level routing when the target table is absent from the DataSourceFrom fixture.
            // Needed because DataSourceFrom can be stale (template DB id after tenant copy) or the
            // table may be a master-DB-only platform table (e.g. AppSecurityUser).
            //   1. DataSourceFrom DB  — normal case (post-repair or table lives there)
            //   2. Identity's registered DB — handles stale DataSourceFrom (custom tenant tables)
            //   3. Hosting AppMasterDB      — handles platform tables (AppSecurityUser etc.)
            string tableKey = AppMetaDataBL.GetOwnerTableKey(entityInfo.SchemaOwner, entityInfo.TableName);
            var tableDict = AppCacheManagerBL.GetDictOwnerTablenameDataTable(entityInfo.DataSourceFrom.Value);
            if (tableDict == null || !tableDict.ContainsKey(tableKey))
            {
                int? identityDataSourceId = ServerContext.Instance.DataSourceId;
                if (identityDataSourceId.HasValue && identityDataSourceId.Value != entityInfo.DataSourceFrom.Value)
                {
                    var identityTableDict = AppCacheManagerBL.GetDictOwnerTablenameDataTable(identityDataSourceId.Value);
                    if (identityTableDict != null && identityTableDict.ContainsKey(tableKey))
                        databaseFixtureInstance = AppCacheManagerBL.GetOneDatabaseFixture(identityDataSourceId.Value);
                    else
                        databaseFixtureInstance = AppCacheManagerBL.GetMasterDbFixture();
                }
                else
                {
                    databaseFixtureInstance = AppCacheManagerBL.GetMasterDbFixture();
                }
            }

            string aselectIdQuery = string.Empty;

            aselectIdQuery = " SELECT [" + entityInfo.IdentityField + "] as Id, ";

            List<string> displayList = GetDisplayColumnList(entityInfo);

            string aDisplay = "";
            if (displayList.Count>0)
            {
                 aDisplay = displayList.Aggregate((i, j) => $@"{i},{j}");
            }
           

            string colorCodeStatement = string.Empty;

            if (!string.IsNullOrWhiteSpace(entityInfo.ColorCodeField))
            {
                colorCodeStatement = ", [" + entityInfo.ColorCodeField + "] as ColorCode ";
            }


            string qulifyTablename = AppMetaDataBL.GetQulifiedTableName(entityInfo.SchemaOwner, entityInfo.TableName, databaseFixtureInstance.SqlServerType.Value);

            var selectStatment = aselectIdQuery + aDisplay  + " FROM  " + qulifyTablename;

            if (!string.IsNullOrWhiteSpace(colorCodeStatement))
            {
                selectStatment = aselectIdQuery + aDisplay +  colorCodeStatement + " FROM  " + qulifyTablename;
            }

            string partnerWhereCause = "";

            if (!string.IsNullOrWhiteSpace(entityInfo.PartnerFilterFiled))
            {
                if (AppSecurityUserBL.CurrentUserEntity.DomainId == (int)EmAppUserType.Customer
                    || AppSecurityUserBL.CurrentUserEntity.DomainId == (int)EmAppUserType.Supplier
                    || AppSecurityUserBL.CurrentUserEntity.DomainId == (int)EmAppUserType.ClientAgent
                    || AppSecurityUserBL.CurrentUserEntity.DomainId == (int)EmAppUserType.SupplierAgent)
                {
                    if (ServerContext.Instance != null && ServerContext.Instance.CurrnetClientIdentity != null)
                    {
                        int? partnerId = ServerContext.Instance.CurrnetClientIdentity.CurrentPartnerId;

                        if (partnerId.HasValue)
                        {
                            partnerWhereCause = entityInfo.PartnerFilterFiled + " = " + partnerId.Value.ToString();
                        }
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(pkInClause))
            {
                selectStatment = selectStatment + " WHERE [" + entityInfo.IdentityField + "] IN  ( " + pkInClause + " )";

                if (!string.IsNullOrWhiteSpace(partnerWhereCause))
                {
                    selectStatment = selectStatment + " AND " + partnerWhereCause;
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(partnerWhereCause))
                {
                    selectStatment = selectStatment + " WHERE " + partnerWhereCause;
                }


            }

            //	string connectInfo = AppMetaDataBL.GetConnectInfo(entityInfo.DataSourceFrom );



            DataTable result = databaseFixtureInstance.RetriveDataTable(selectStatment, new List<System.Data.Common.DbParameter>());

            // why weed colorCode 
            //int? colorCodeColumnIndex = null;

            //foreach (DataColumn column in result.Columns)
            //{
            //    if (column.ColumnName == "ColorCode")
            //    {
            //        colorCodeColumnIndex = column.Ordinal;
            //    }
            //}

            //toReturn.AddRange(result.AsEnumerable().Select(r => new LookupItemDto()
            //{
            //    Id = r[0],
            //    Display = r[1].ToString(),
            //    ColorCode = colorCodeColumnIndex.HasValue ? r[colorCodeColumnIndex.Value] as string : string.Empty
            //}));

            foreach(DataRow row in result.AsEnumerable())
            {
                var a = new LookupItemDto();
                a.Id = row[0];
                int count = 0;
                foreach(string displayCol in displayList)
                {
                    if(count==0)
                    {
                        a.Display = row[displayCol] + "";
                    }
                    else
                    {
                        a.Display = a.Display + ";" + row[displayCol];
                    }
                    count++;


                }
                //if(!string.IsNullOrEmpty(a.Display))
                //{

                //}


                toReturn.Add(a);
            }

            //using (DataAccessAdapter adapter = new DataAccessAdapter(connectInfo))
            //         {
            //             IDataReader reader = adapter.FetchDataReader(new RetrievalQuery(new SqlCommand(selectStatment)), CommandBehavior.SingleResult);
            //             while (reader.Read())
            //             {
            //                 toReturn.Add(new LookupItemDto() { Id = reader.GetValue(0), Display = reader.GetValue(1).ToString() });
            //             }


            //         }
        }

        private static void GetSimpleQueryLookupItem(List<LookupItemDto> toReturn, AppEntityInfoEntity entityInfo, string pkInClause)
        {

            string selectStatment = entityInfo.QueryText;

            string currentUserId = "null";
            string currentPartnerId = "null";

            if (AppSecurityUserBL.CurrentUserEntity != null)
            {
                currentUserId = AppSecurityUserBL.CurrentUserEntity.UserId.ToString();
            }

            if (ServerContext.Instance.CurrnetClientIdentity != null && ServerContext.Instance.CurrnetClientIdentity.CurrentPartnerId != null)
            {
                currentPartnerId = ServerContext.Instance.CurrnetClientIdentity.CurrentPartnerId.ToString();
            }

            selectStatment = selectStatment.Replace("[" + EmAppMessagePlaceHolderToken.CurrentUserId.ToString() + "]", currentUserId);
            selectStatment = selectStatment.Replace("[" + EmAppMessagePlaceHolderToken.CurrentPartnerId.ToString() + "]", currentPartnerId);


            if (!string.IsNullOrWhiteSpace(pkInClause))
            {

                string inClause = "(" + entityInfo.IdentityField + " in ( " + pkInClause + " )" + ")";

                if (selectStatment.ContainsExt("where", StringComparison.InvariantCultureIgnoreCase))
                {
                    Regex.Replace(selectStatment, "where", " WHERE " + inClause + " AND ", RegexOptions.IgnoreCase);

                }
                // NOT INLCUDE WHERE
                else if (selectStatment.ContainsExt("order", StringComparison.InvariantCultureIgnoreCase))
                {

                    Regex.Replace(selectStatment, "order", " WHERE " + inClause + " ORDER ", RegexOptions.IgnoreCase);

                }



                // selectStatment = selectStatment + " WHERE " + EntityInfo.IdentityField + " in ( " + pkInClause + " )";
            }






            DatabaseFixture databaseFixtureInstance = AppCacheManagerBL.GetOneDatabaseFixture(entityInfo.DataSourceFrom.Value);


            DataTable result = databaseFixtureInstance.RetriveDataTable(selectStatment, new List<System.Data.Common.DbParameter>());
            if(result != null && result.Rows.Count > 0)
            {
                if(result.Columns.Count > 1)
                {
                    toReturn.AddRange(result.AsEnumerable().Select(r => new LookupItemDto() { Id = r[0], Display = r[1].ToString() }));
                }
               else
                {
                    toReturn.AddRange(result.AsEnumerable().Select(r => new LookupItemDto() { Id = r[0], Display = r[0].ToString() }));
                }
            }

            


            //using (DataAccessAdapter adapter = new DataAccessAdapter(connectInfo))
            //         {
            //             IDataReader reader = adapter.FetchDataReader(new RetrievalQuery(new SqlCommand(selectStatment)), CommandBehavior.SingleResult);
            //             while (reader.Read())
            //             {
            //                 toReturn.Add(new LookupItemDto() { Id = reader.GetValue(0), Display = reader.GetValue(1).ToString() });
            //             }


            //         }
        }

        internal static List<LookupItemDto> GetEnumValueList(AppEntityInfoEntity entityInfo)
        {

            int entityid = entityInfo.EntityInfoId;

            List<LookupItemDto> toReturn = new List<LookupItemDto>();
            var selectStatment = @"select EnumKey, EnumValue from AppEntityEnumValue where EntityInfoID=" + entityid;



            DatabaseFixture databaseFixtureInstance = AppCacheManagerBL.GetOneDatabaseFixture(entityInfo.DataSourceFrom.Value);


            DataTable result = databaseFixtureInstance.RetriveDataTable(selectStatment, new List<System.Data.Common.DbParameter>());

            toReturn.AddRange(result.AsEnumerable().Select(r => new LookupItemDto() { Id = r[0], Display = r[1] as string }));

            //string connectInfo = AppMetaDataBL.GetConnectInfo(entityInfo.DataSourceFrom);

            //using (DataAccessAdapter adapter = new DataAccessAdapter(connectInfo))
            //         {
            //             IDataReader reader = adapter.FetchDataReader(new RetrievalQuery(new SqlCommand(selectStatment)), CommandBehavior.SingleResult);
            //             while (reader.Read())
            //             {
            //                 toReturn.Add(new LookupItemDto() { Id = reader.GetValue(0), Display = reader.GetValue(1).ToString() });
            //             }


            //         }

            return toReturn;

            // throw new System.NotImplementedException();
        }




        internal static List<LookupItemDto> GetSimpleValueList(AppEntityInfoEntity EntityInfo)
        {
            List<LookupItemDto> toReturn = new List<LookupItemDto>();
            var selectStatment = @"SELECT InternalKey      ,Code      ,Description, Sort   ,EntityInfoID   FROM dbo.AppEntitySimpleListValue   WHERE EntityInfoID="
             + EntityInfo.EntityInfoId + " ORDER BY Sort";


            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                IDataReader reader = adapter.FetchDataReader(new RetrievalQuery(new SqlCommand(selectStatment)), CommandBehavior.SingleResult);
                while (reader.Read())
                {
                    toReturn.Add(new LookupItemDto() { Id = reader.GetValue(0), Display = reader.GetValue(1).ToString() });
                }


            }

            return toReturn;

            // throw new System.NotImplementedException();
        }

        private static List<string> GetDisplayColumnList(AppEntityInfoEntity EntityInfo)
        {
            List<string> toReturn = new List<string>();

            if (!string.IsNullOrEmpty(EntityInfo.DisplayFiled1))
            {
                toReturn.Add(EntityInfo.DisplayFiled1);
            }

            if (!string.IsNullOrEmpty(EntityInfo.DisplayFiled2))
            {
                toReturn.Add(EntityInfo.DisplayFiled2);
            }


            if (!string.IsNullOrEmpty(EntityInfo.DisplayFiled3))
            {
                toReturn.Add(EntityInfo.DisplayFiled3);
            }




          
            return toReturn;
        }


        private static string GetDisplayColumn(AppEntityInfoEntity EntityInfo, string splitToken)
        {
            string aDisplay = string.Empty;
            string orderby = string.Empty;


            if (!string.IsNullOrEmpty(EntityInfo.DisplayFiled1))
            {
                aDisplay = aDisplay + " IsNull(  cast ( [" + EntityInfo.DisplayFiled1 + "] as nvarchar(MAX)  ) , '' )" + splitToken;
            }

            if (!string.IsNullOrEmpty(EntityInfo.DisplayFiled2))
            {
                aDisplay = aDisplay + " IsNull(  cast ( [" + EntityInfo.DisplayFiled2 + "] as nvarchar(MAX)  ) , '' )" + splitToken;
            }


            if (!string.IsNullOrEmpty(EntityInfo.DisplayFiled3))
            {
                aDisplay = aDisplay + " IsNull(  cast ( [" + EntityInfo.DisplayFiled3 + "] as nvarchar(MAX)  ) , '' )" + splitToken;
            }




            if (aDisplay != string.Empty)
            {

                aDisplay = aDisplay.Substring(0, aDisplay.Length - splitToken.Length);

            }
            return aDisplay;
        }


        public static DataTable GetEntitySelectColumnRowValue(int entityID, List<object> keyValues, List<string> selectColumnList)
        {



            if ((!keyValues.IsEmpty() && keyValues.Where(o => o != null).Count() > 0) && (!selectColumnList.IsEmpty()))
            {
                //string selectColumns = string.Empty;
                //foreach (var columnName in selectColumnList)
                //{
                //    if (columnName != string.Empty)
                //    {
                //        selectColumns = selectColumns + columnName + " ,";
                //    }
                //}
                //selectColumns = selectColumns.Substring(0, selectColumns.Length - 1);

                string selectColumns = selectColumnList.Aggregate((i, j) => i + "," + j);
                // need to cache strcture
                AppEntityInfoEntity entityInfo = RetrieveOneAppEntityInfoEntity(entityID);


                object firstValue = keyValues[0];
                bool isStringType = false;
                if (firstValue is string)
                {
                    isStringType = true;

                }

                List<string> valueIds;

                if (isStringType)
                {
                    valueIds = keyValues.Where(o => o != null).Select(o => string.Format("'{0}'", (o as string).Replace("\'", @"\'\'"))).ToList();
                }
                else
                {
                    valueIds = keyValues.Where(o => o != null).Select(o => o.ToString()).ToList();
                }


                string pkInClause = valueIds.Aggregate((i, j) => i + "," + j);



                string idInclause = "(" + entityInfo.IdentityField + " in ( " + pkInClause + " )" + ")";

                DatabaseFixture databaseFixtureInstance = AppCacheManagerBL.GetOneDatabaseFixture(entityInfo.DataSourceFrom.Value);

                string aselectIdQuery = string.Empty;

                string qulifyTablename = AppMetaDataBL.GetQulifiedTableName(entityInfo.SchemaOwner, entityInfo.TableName, databaseFixtureInstance.SqlServerType.Value);

                aselectIdQuery = entityInfo.IdentityField + " as Id, ";
                string selectStatment = "select distinct " + aselectIdQuery + selectColumns
                              + " from " + qulifyTablename + "  where " + idInclause;


                return databaseFixtureInstance.RetriveDataTable(selectStatment, new List<System.Data.Common.DbParameter>());


            }

            return new DataTable();
        }

        public static IDictionary<EmAppEntityLookupInfoCode, IEnumerable<LookupItemDto>> RetrieveMassAppEntitiesLookupItem(IEnumerable<EmAppEntityLookupInfoCode> entitycodes)
        {
            IDictionary<EmAppEntityLookupInfoCode, IEnumerable<LookupItemDto>> toRetrun = new Dictionary<EmAppEntityLookupInfoCode, IEnumerable<LookupItemDto>>();
            entitycodes.ForAll(a => toRetrun.Add(a, GetLookupItemListByCode(a.ToString(), string.Empty)));
            return toRetrun;
        }

        public static IDictionary<string, IEnumerable<LookupItemDto>> RetrieveMassAppEntitiesLookupItemByEntityCodes(IEnumerable<string> entitycodes)
        {
            IDictionary<string, IEnumerable<LookupItemDto>> toRetrun = new Dictionary<string, IEnumerable<LookupItemDto>>();
            entitycodes.ForAll(a => toRetrun.Add(a, GetLookupItemListByCode(a, string.Empty)));
            return toRetrun;
        }

        public static string GetLookupItemDisplayByLookupItemId(List<LookupItemDto> lookupItemList, object LookupItemId)
        {
            if (lookupItemList != null && LookupItemId != null)
            {
                var foundItem = lookupItemList.FirstOrDefault(o => object.Equals(o.Id, LookupItemId));
                if (foundItem != null)
                {
                    return foundItem.Display;
                }
            }
            return string.Empty;
        }

    }
}