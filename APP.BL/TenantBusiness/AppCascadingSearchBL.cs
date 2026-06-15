using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using APP.LBL;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
using APP.LBL.RelationClasses;
////using APP.Persistence.Common;
using Newtonsoft.Json;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;
using DatabaseSchemaMrg;

// Caculation result could trigger  cascading triger, need to disable cscading after assignment
using APP.Framework;
namespace App.BL
{

    public static class AppCascadingSearchBL
    {
        public static void SetupIntialCscadingSearchCretiaDataSource(SearchDto searchUIDto, bool isCascadingFromUIOrCreationNew = false)
        {


            AppSearchExDto appSearchExdto = AppSearchConfigBL.RetrieveOneAppSearchExDto(searchUIDto.Id);



            // get all SearchRootTriggerFiedls
            List<AppSearchFieldExDto> searchRootTriggerFieldList = SetupSearchCascadingFieldIds(searchUIDto, appSearchExdto);


            // setup Cascadingparent and Children

            foreach (AppSearchFieldExDto rootCascadingFile in searchRootTriggerFieldList)
            {
                ProcessCasadingChildren(appSearchExdto.AppSearchFieldList.ToList(), rootCascadingFile);

            }


            // For cascading, always take the first value

            Dictionary<int, object> dictOriginalOneToOneFields = searchUIDto.Criterias.ToDictionary(o => o.SearcDCUID, o => o.Value);



            Dictionary<int, object> dictModifiedOneToOneFields = new Dictionary<int, object>();
            Dictionary<int, List<LookupItemDto>> dictCascadingChildFieldIdAndDataSource = new Dictionary<int, List<LookupItemDto>>();

            AppDataSetEntity aAppDataSetEntity = AppDataSetBL.RetrieveOneAppDataSetEntity(appSearchExdto.DataSetId);
            DatabaseFixture dbFixture = AppCacheManagerBL.GetOneDatabaseFixture(aAppDataSetEntity.DataSourceFrom.Value);



            foreach (AppSearchFieldExDto rootCascadingFile in searchRootTriggerFieldList)
            {
                SetupCascadingDataSource(dictCascadingChildFieldIdAndDataSource, dictOriginalOneToOneFields, dictModifiedOneToOneFields, rootCascadingFile, dbFixture, appSearchExdto, isCascadingFromUIOrCreationNew);

            }



            // update 

            foreach (var critera in searchUIDto.Criterias)
            {
                int searchFieldId = critera.SearcDCUID;
                if (dictModifiedOneToOneFields.ContainsKey(searchFieldId))
                {
                    object newValue = dictModifiedOneToOneFields[critera.SearcDCUID];

                    critera.Values.Clear();
                    critera.Value = newValue;


                }

                if (dictCascadingChildFieldIdAndDataSource.ContainsKey(searchFieldId))
                {
                    critera.ItemsSource = dictCascadingChildFieldIdAndDataSource[searchFieldId];
                }


            }

        }


        public static void SetupOneSearchFiledCscadingSearchCretiaDataSource(SearchCascdingDto searchDto)
        {

            AppSearchExDto appSearchExdto = AppSearchConfigBL.RetrieveOneAppSearchExDto(searchDto.SearchId);

            Dictionary<int, AppSearchFieldExDto> dictSearchFieldIdExDto = appSearchExdto.AppSearchFieldList.ToDictionary(o => (int)o.Id, o => o);

            AppSearchFieldExDto rootCascadingFile = dictSearchFieldIdExDto[searchDto.CurrentCascadingTriggerSearchCriteriaId.Value];

            ProcessCasadingChildren(appSearchExdto.AppSearchFieldList.ToList(), rootCascadingFile);




            // For cascading, always take the first value

            Dictionary<int, object> dictOriginalOneToOneFields = new Dictionary<int, object>();
            dictOriginalOneToOneFields[searchDto.CurrentCascadingTriggerSearchCriteriaId.Value] = searchDto.CurrentChangedValue;




            Dictionary<int, object> dictModifiedOneToOneFields = new Dictionary<int, object>();
            Dictionary<int, List<LookupItemDto>> dictCascadingChildFieldIdAndDataSource = new Dictionary<int, List<LookupItemDto>>();

            AppDataSetEntity aAppDataSetEntity = AppDataSetBL.RetrieveOneAppDataSetEntity(appSearchExdto.DataSetId);
            DatabaseFixture dbFixture = AppCacheManagerBL.GetOneDatabaseFixture(aAppDataSetEntity.DataSourceFrom.Value);

            SetupCascadingDataSource(dictCascadingChildFieldIdAndDataSource, dictOriginalOneToOneFields, dictModifiedOneToOneFields, rootCascadingFile, dbFixture, appSearchExdto, true);



            searchDto.DictReturnCascadingChildFieldIdAndDataSource = dictCascadingChildFieldIdAndDataSource;
            searchDto.DictReturnCriteriaIdValue = dictModifiedOneToOneFields;





        }


        private static void SetupCascadingDataSource(Dictionary<int, List<LookupItemDto>> dictCascadingFiledDataSource, Dictionary<int, object> dictOriginalOneToOneFields, Dictionary<int, object> dictModifiedOneToOneFields, AppSearchFieldExDto parentCascadingFieldExDto, DatabaseFixture dbFixture, AppSearchExDto appSearchExdto, bool isCascadingFromUIOrCreationNew)
        {


            object parenFiledValue = dictOriginalOneToOneFields[(int)parentCascadingFieldExDto.Id];

            List<AppSearchFieldExDto> InnerEntitySubscribeFileds = new List<AppSearchFieldExDto>();

            if (appSearchExdto.DictInnerEntityChildFieldExdto.ContainsKey((int)parentCascadingFieldExDto.Id))
            {
                InnerEntitySubscribeFileds = appSearchExdto.DictInnerEntityChildFieldExdto[(int)parentCascadingFieldExDto.Id];

            }

            // need to clear up child select value and datasource
            if ((parenFiledValue == null || string.IsNullOrEmpty(parenFiledValue.ToString())) && !parentCascadingFieldExDto.CascadngChildren.IsEmpty())
            {
                foreach (var cascadingChildDto in parentCascadingFieldExDto.CascadngChildren)
                {


                    List<LookupItemDto> childAllList = new List<LookupItemDto>();

                    if (cascadingChildDto.EntityId.HasValue)
                    {
                        childAllList = AppEntityInfoBL.GetLookupItemList(cascadingChildDto.EntityId.Value, "");
                    }


                    int cascadingChildFiedId = (int)cascadingChildDto.Id;

                    if (dictCascadingFiledDataSource.ContainsKey(cascadingChildFiedId))
                    {
                        dictCascadingFiledDataSource.Remove(cascadingChildFiedId);
                    }


                    dictCascadingFiledDataSource.Add(cascadingChildFiedId, childAllList);
                    dictModifiedOneToOneFields[(int)cascadingChildDto.Id] = null;


                    //recursive to get all child cascading lookititem
                    if (!cascadingChildDto.CascadngChildren.IsEmpty())
                    {
                        SetupCascadingDataSource(dictCascadingFiledDataSource, dictOriginalOneToOneFields, dictModifiedOneToOneFields, parentCascadingFieldExDto, dbFixture, appSearchExdto, isCascadingFromUIOrCreationNew);

                    }




                }

                foreach (var cascadingChildDto in InnerEntitySubscribeFileds)
                {
                    dictModifiedOneToOneFields[(int)cascadingChildDto.Id] = null;

                }


            }
            else // parenFiledValue has value
            {
                if (!parentCascadingFieldExDto.CascadngChildren.IsEmpty())
                {

                    foreach (var cascadingChildDto in parentCascadingFieldExDto.CascadngChildren)
                    {

                        //  string tableCascaTable = cascadingChildDto.CascadingRelationTable;
                                              

                        string tableCascaTable = AppMetaDataBL.GetQulifiedTableName(dbFixture.CurrentOwner, cascadingChildDto.CascadingRelationTable, dbFixture.SqlServerType.Value);

                        string parentKey = cascadingChildDto.CascadingRelationTableParentKeyField;
                        string transFieldDatasetFiledmapping = cascadingChildDto.CascadingRelationTableChildKeyField;


                        List<LookupItemDto> childAllList = new List<LookupItemDto>();



                        string query = string.Format(@"SELECT {0} from {1} where {2}={3}", transFieldDatasetFiledmapping, tableCascaTable, parentKey, parenFiledValue);

                        childAllList = AppEntityInfoBL.GetLookupItemList((int)cascadingChildDto.EntityId, query);




                        int cascadingChildFiedId = (int)cascadingChildDto.Id;
                        if (dictCascadingFiledDataSource.ContainsKey(cascadingChildFiedId))
                        {
                            dictCascadingFiledDataSource.Remove(cascadingChildFiedId);
                        }
                        dictCascadingFiledDataSource.Add(cascadingChildFiedId, childAllList);

                        if (isCascadingFromUIOrCreationNew)
                        {

                            dictModifiedOneToOneFields[(int)cascadingChildDto.Id] = null;



                            // need to check Ineer
                        }

                        //recursive to get all child cascading lookititem
                        if (!cascadingChildDto.CascadngChildren.IsEmpty())
                        {
                            SetupCascadingDataSource(dictCascadingFiledDataSource, dictOriginalOneToOneFields, dictModifiedOneToOneFields, parentCascadingFieldExDto, dbFixture, appSearchExdto, isCascadingFromUIOrCreationNew);
                        }





                    }
                }

                // 
                if (InnerEntitySubscribeFileds.Count > 0)
                {

                    SetupInnerEntityRelationValue(dictOriginalOneToOneFields, dictModifiedOneToOneFields, parentCascadingFieldExDto, parenFiledValue, InnerEntitySubscribeFileds);



                }



            }


        }

        private static void ProcessCasadingChildren(List<AppSearchFieldExDto> appTransactionFieldList, AppSearchFieldExDto rootCascadingFile)
        {

            List<AppSearchFieldExDto> children = GetChilds(appTransactionFieldList, rootCascadingFile);

            if (!children.IsEmpty())
            {
                rootCascadingFile.CascadngChildren = children;

                rootCascadingFile.CascadngChildren.ForAll(c => ProcessCasadingChildren(appTransactionFieldList, c));

            }


            // throw new NotImplementedException();
        }


        private static List<AppSearchFieldExDto> GetChilds(List<AppSearchFieldExDto> appTransactionFieldList, AppSearchFieldExDto rootCascadingFile)
        {
            return appTransactionFieldList.Where(f =>
             (
                     (f.ParentFieldId.HasValue && f.ParentFieldId.Value == (int)rootCascadingFile.Id))



            ).ToList();
        }

        private static List<AppSearchFieldExDto> SetupSearchCascadingFieldIds(SearchDto searchUIDto, AppSearchExDto appSearchExDto)
        {

            searchUIDto.IsChangedNeedToCascadingSearchCriteriaIds = new List<int>();
            searchUIDto.IsChangedNeedTTriggerExecutionSearchCriteriaIds = new List<int>();

            Dictionary<int, AppSearchFieldExDto> dictSearchFieldEntity = appSearchExDto.AppSearchFieldList.ToDictionary(o => (int)o.Id, o => o);

            List<int> allTriggerFiedIdList = GeAllSearchTriggerFiedIds(appSearchExDto);
            searchUIDto.IsChangedNeedToCascadingSearchCriteriaIds.AddRange(allTriggerFiedIdList);



            var searchRootCasadingFieldList = new List<AppSearchFieldExDto>();

            foreach (int parentfiedId in allTriggerFiedIdList)
            {
                var parentFieldEntity = dictSearchFieldEntity[parentfiedId];


                if ((!parentFieldEntity.ParentFieldId.HasValue) && (!parentFieldEntity.MasterEntityFieldlId.HasValue))
                {
                    searchRootCasadingFieldList.Add(parentFieldEntity);

                }

            }


            var isChangedNeedTTriggerExecutionSearchCriteriaIds = dictSearchFieldEntity.Values.Where(o => o.IsChangedAutoExecute.HasValue && o.IsChangedAutoExecute.Value).Select(o => (int)o.Id);

            searchUIDto.IsChangedNeedTTriggerExecutionSearchCriteriaIds.AddRange(isChangedNeedTTriggerExecutionSearchCriteriaIds);





            return searchRootCasadingFieldList;

        }

        private static List<int> GeAllSearchTriggerFiedIds(AppSearchExDto appSearchEntity)
        {

            List<int> parentFiedIdList = new List<int>();

            foreach (var filed in appSearchEntity.AppSearchFieldList)
            {
                if (filed.ParentFieldId.HasValue && filed.ControlType == (int)EmAppControlType.DDL)
                {
                    parentFiedIdList.Add(filed.ParentFieldId.Value);
                }

                if (filed.MasterEntityFieldlId.HasValue)
                {
                    parentFiedIdList.Add(filed.MasterEntityFieldlId.Value);

                }

            }

            return parentFiedIdList.Distinct().ToList();
        }



        internal static void SetupInnerEntityRelationValue(Dictionary<int, object> origialOneToOneFields, Dictionary<int, object> dictModifiedOneToOneFields, AppSearchFieldExDto parentCascadingFieldExDto, object parenFiledValue, List<AppSearchFieldExDto> InnerEntitySubscribeFileds)
        {


            if (!ControlTypeValueConverter.ConvertValueToInt(parenFiledValue).HasValue)
            {

                foreach (var inerentityChildDto in InnerEntitySubscribeFileds)
                {

                    dictModifiedOneToOneFields[(int)inerentityChildDto.Id] = null;

                }

                return;

            }



            if (parenFiledValue == null)
            {

                foreach (var inerentityChildDto in InnerEntitySubscribeFileds)
                {

                    dictModifiedOneToOneFields[(int)inerentityChildDto.Id] = null;

                }

                return;

            }


            //
            var entityInfo = AppEntityInfoBL.RetrieveOneAppEntityInfoExDto(parentCascadingFieldExDto.EntityId);
            string tablename = entityInfo.TableName;


            string selectField = string.Empty;
            foreach (var inerentityChildDto in InnerEntitySubscribeFileds)
            {

                selectField = selectField + "[" + inerentityChildDto.InnerEntitySubscribeFiled + "],";

                //  aAppformDataDto.DictOneToOneFields[inerentityChildDto.DataBaseFieldName] = "";

            }

            selectField = selectField.Substring(0, selectField.Length - 1);

            string query = @" select " + selectField + " From " + tablename + " where  " + entityInfo.IdentityField + "=@parenFiledValue";

            List<SqlParameter> lsitparamter = new List<SqlParameter>();
            lsitparamter.Add(new SqlParameter("@parenFiledValue", parenFiledValue));


            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                DataTable result = adapter.ExecuteDataTableRetrievalQuery(query, lsitparamter);

                if (result.Rows.Count > 0)
                {
                    DataRow row = result.Rows[0];
                    foreach (var inerentityChildDto in InnerEntitySubscribeFileds)
                    {

                        dictModifiedOneToOneFields[(int)inerentityChildDto.Id] = row[inerentityChildDto.InnerEntitySubscribeFiled];

                    }
                }
            }
        }


        private static void ProcessCasadingChildren(IEnumerable<AppTransactionFieldExDto> appTransactionFieldList, AppTransactionFieldExDto rootCascadingFile)
        {

            List<AppTransactionFieldExDto> children = GetChilds(appTransactionFieldList, rootCascadingFile);

            if (!children.IsEmpty())
            {
                rootCascadingFile.CascadngChildren = children;

                rootCascadingFile.CascadngChildren.ForAll(c => ProcessCasadingChildren(appTransactionFieldList, c));

            }


            // throw new NotImplementedException();
        }

        private static List<AppTransactionFieldExDto> GetChilds(IEnumerable<AppTransactionFieldExDto> appTransactionFieldList, AppTransactionFieldExDto rootCascadingFile)
        {
            return appTransactionFieldList.Where(f =>
             (
                     (f.DdlparentLevelId.HasValue && f.DdlparentLevelId.Value == (int)rootCascadingFile.Id))



            ).ToList();
        }


    }
}