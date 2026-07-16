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
using DatabaseSchemaMrg.DataSchema;
using DatabaseSchemaMrg;
using System.Data.Common;


// Caculation result could trigger  cascading triger, need to disable cscading after assignment
using APP.Framework;
namespace App.BL
{

    public static class AppCascadingBL
    {
        public static void SetupIntialCscadingFieldDataSource(AppTransactionExDto hierarchyTransactionExDto, AppMasterDetailDto aAppformDataDto, bool isCascadingFromUIOrCreationNew = false)
        {

            // need to cache   AppTransactionExDto in the future
            //	AppTransactionExDto hierarchyTransactionExDto = AppTransactionBL.GetOneHierarchyTransaction(aAppformDataDto.TransactionId);

            aAppformDataDto.IsChangedNeedToCascadingFiedIds = new List<string>();
            aAppformDataDto.IsUsedCascadingDataSourceFiedIds = new List<string>();
            // aAppformDataDto.IsUsedCascadingDataSourceUnitIds = new List<string>();

            var allUnitRootLevelTriggerFieldList = SetupTranscationCascadingFieldIds(aAppformDataDto.IsChangedNeedToCascadingFiedIds, aAppformDataDto.IsUsedCascadingDataSourceFiedIds, hierarchyTransactionExDto);
            // get All Unit Intial trigger fields 
            Dictionary<int, List<AppTransactionFieldExDto>> dictAllUnitInitialLevelTriggerFieldList = allUnitRootLevelTriggerFieldList.GroupBy(o => o.TransactionUnitId).ToDictionary(g => g.Key, g => g.ToList());



            //Root level -----keyFieldId
            var rootMasterUnit = hierarchyTransactionExDto.RootMasterUnit;
            SetupRootUnitCascadingLookupItemsource(aAppformDataDto, isCascadingFromUIOrCreationNew, rootMasterUnit, dictAllUnitInitialLevelTriggerFieldList);



            //Child and   GrandChild
            SetupChildAndGrandChildCascadingLookupItemsource(hierarchyTransactionExDto, aAppformDataDto, isCascadingFromUIOrCreationNew, dictAllUnitInitialLevelTriggerFieldList);

            // setup AvailableSourceFilterByParentTransactionFieldID

            foreach (var unitDto in hierarchyTransactionExDto.DictAllTransactionUnitIdExDto.Values)
            {
                if (unitDto.AvailableSourceFilterByParentTransactionFieldId.HasValue)
                {
                    aAppformDataDto.IsChangedNeedToCascadingFiedIds.Add(unitDto.AvailableSourceFilterByParentTransactionFieldId.ToString());

                    //???
                    // aAppformDataDto.IsUsedCascadingDataSourceUnitIds.Add(unitDto.Id.ToString () );
                }
            }


        }

        private static void SetupRootUnitCascadingLookupItemsource(AppMasterDetailDto aAppformDataDto, bool isCascadingFromUIOrCreationNew, AppTransactionUnitExDto rootMasterUnit, Dictionary<int, List<AppTransactionFieldExDto>> dictAllUnitInitialLevelTriggerFieldList)
        {
            if (rootMasterUnit != null)
            {
                if (dictAllUnitInitialLevelTriggerFieldList.ContainsKey((int)rootMasterUnit.Id))
                {

                    List<AppTransactionFieldExDto> rootUnitInitialCascadingFields = dictAllUnitInitialLevelTriggerFieldList[(int)rootMasterUnit.Id];

                    foreach (AppTransactionFieldExDto rootlevelCascadingFieldExDto in rootUnitInitialCascadingFields)
                    {
                        SetupOneUnitCascadingDataSource(
                            aAppformDataDto.DictCascadingFiledDataSource,
                            aAppformDataDto.DictOneToOneFields,
                            rootMasterUnit,
                            rootlevelCascadingFieldExDto,
                            isCascadingFromUIOrCreationNew,
                            aAppformDataDto,
                            null
                            );

                    }
                }
            }
        }

        private static void SetupChildAndGrandChildCascadingLookupItemsource(AppTransactionExDto hierarchyTransactionExDto, AppMasterDetailDto aAppformDataDto, bool isCascadingFromUIOrCreationNew, Dictionary<int, List<AppTransactionFieldExDto>> dictAllUnitInitialLevelTriggerFieldList)
        {
            // Child Level  level
            foreach (string childUnitId in aAppformDataDto.DictOneToManyFields.Keys)
            {

                var chidUnitDto = hierarchyTransactionExDto.DictAllTransactionUnitIdExDto[childUnitId];
                if (chidUnitDto.IsUsedForLoadingAvailableSource.HasValue && chidUnitDto.IsUsedForLoadingAvailableSource.Value)
                {
                    //skip  IsUsedForLoadingAvailableSource lookite sessting
                    continue;
                }

                List<AppChildDataDto> childDtoList = aAppformDataDto.DictOneToManyFields[childUnitId];
                SetupOneChildUnitInitalCascadingLookupItemsource(hierarchyTransactionExDto, aAppformDataDto, isCascadingFromUIOrCreationNew, dictAllUnitInitialLevelTriggerFieldList, childUnitId, childDtoList);


                //Grand Child level Data source process

                foreach (AppChildDataDto aAppformChildDataDto in childDtoList)
                {

                    Dictionary<String, List<AppChildDataDto>> dictGrandChildUnitDataRowList = aAppformChildDataDto.DictOneToManyFields;

                    foreach (string grandChildUnit in dictGrandChildUnitDataRowList.Keys)
                    {
                        List<AppChildDataDto> grandChildDtoList = dictGrandChildUnitDataRowList[grandChildUnit];

                        SetupOneChildUnitInitalCascadingLookupItemsource(hierarchyTransactionExDto, aAppformDataDto, isCascadingFromUIOrCreationNew, dictAllUnitInitialLevelTriggerFieldList,
                            grandChildUnit, grandChildDtoList);

                    }

                }

            }
        }

        private static void SetupOneChildUnitInitalCascadingLookupItemsource(AppTransactionExDto hierarchyTransactionExDto,
            AppMasterDetailDto aAppformDataDto, bool isCascadingFromUIOrCreationNew,
            Dictionary<int, List<AppTransactionFieldExDto>> dictAllUnitInitialLevelTriggerFieldList,
            string childUnitId,
             List<AppChildDataDto> childDtoList
            )
        {
            AppTransactionUnitExDto childAppTransactionUnitExDto = hierarchyTransactionExDto.DictAllTransactionUnitIdExDto[childUnitId];

            int intChildUnitIntId = int.Parse(childUnitId);

            if (dictAllUnitInitialLevelTriggerFieldList.ContainsKey(intChildUnitIntId))
            {

                var childLevelCascadingList = dictAllUnitInitialLevelTriggerFieldList[intChildUnitIntId];

                foreach (AppChildDataDto aAppformChildDataDto in childDtoList)
                {
                    aAppformChildDataDto.DictCascadingFiledDataSource = new Dictionary<string, List<LookupItemDto>>();

                    foreach (AppTransactionFieldExDto childRootLevelCascadingFieldExDto in childLevelCascadingList)
                    {
                        SetupOneUnitCascadingDataSource(
                        aAppformChildDataDto.DictCascadingFiledDataSource, aAppformChildDataDto.DictOneToOneFields, childAppTransactionUnitExDto, childRootLevelCascadingFieldExDto, isCascadingFromUIOrCreationNew,
                         aAppformDataDto,
                         aAppformChildDataDto
                        );

                    }
                }
            }

            // SetupCascadinTrigerLockingCondition 
            foreach (AppChildDataDto aAppformChildDataDto in childDtoList)
            {

                foreach (var filedDto in childAppTransactionUnitExDto.AppTransactionFieldList)
                {
                    if (!filedDto.AppConditionalAction__List.IsEmpty())
                    {
                        SetupCascadinTrigerLockingCondition(aAppformChildDataDto, hierarchyTransactionExDto, filedDto.Id as int?);
                    }
                }

            }
        }

        //  grid cell interface( need post



        public static AppMasterDetailDto GetRootUnitFieldTriggerCascadingDataSource(AppMasterDetailDto aCascadingAppformDataDto)
        {

            AppTransactionExDto hierarchyTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(aCascadingAppformDataDto.TransactionId);
            List<AppTransactionFieldExDto> allFiedExdtoList = hierarchyTransactionExDto.DictAllTransactionField.Values.ToList();
            Dictionary<int, AppTransactionFieldExDto> dictTransField = allFiedExdtoList.ToDictionary(o => (int)o.Id, o => o);

            var rootUnit = hierarchyTransactionExDto.RootMasterUnit;

            if (rootUnit != null)
            {

                int? cascadingTrigerFieldId = aCascadingAppformDataDto.CurrentCascadingFieldId;
                int? unitId = aCascadingAppformDataDto.CurrentCascadingUnitId;



                if (cascadingTrigerFieldId.HasValue)
                {

                    AppTransactionFieldExDto cascadingTrigerFieldExDto = dictTransField[cascadingTrigerFieldId.Value];
                    SetupCasadingFiledChildrenHairarchy(allFiedExdtoList, cascadingTrigerFieldExDto);

                    // var cascadingTrigerFieldExDto = dictTransField[cascadingTrigerFieldId.Value];//rootUnit.AppTransactionFieldList.FirstOrDefault(o => (int?)o.Id == cascadingTrigerFieldId);
                    aCascadingAppformDataDto.DictCascadingFiledDataSource = new Dictionary<string, List<LookupItemDto>>();

                    SetupOneUnitCascadingDataSource(aCascadingAppformDataDto.DictCascadingFiledDataSource, aCascadingAppformDataDto.DictOneToOneFields, rootUnit, cascadingTrigerFieldExDto, true, aCascadingAppformDataDto, null);

                    AppMasterDetailFormDataLoadBL.SetupDdlQueryLookItem(hierarchyTransactionExDto, aCascadingAppformDataDto);

                    //

                    //foreach (var unitDto in hierarchyTransactionExDto.DictAllTransactionUnitIdExDto.Values)
                    //{
                    //    if (unitDto.AvailableSourceFilterByParentTransactionFieldId.HasValue)
                    //    {
                    //        aAppformDataDto.IsChangedNeedToCascadingFiedIds.Add(unitDto.AvailableSourceFilterByParentTransactionFieldId.ToString());


                    //        aAppformDataDto.IsUsedCascadingDataSourceUnitIds.Add(unitDto.Id.ToString());
                    //    }
                    //}
                    var avialbeDataSourceUnitExdtoList = hierarchyTransactionExDto.DictAllTransactionUnitIdExDto.Values
                          .Where(o => o.AvailableSourceFilterByParentTransactionFieldId.HasValue
                     && o.AvailableSourceFilterByParentTransactionFieldId == cascadingTrigerFieldId).ToList();

                    foreach (var subAvialbeUnitExdto in avialbeDataSourceUnitExdtoList)
                    {
                        List<AppChildDataDto> childAppformChildDataDto = AppMasterDetailFormDataLoadBL.LoadChildAvialbeDataSource(aCascadingAppformDataDto, rootUnit, subAvialbeUnitExdto);

                        aCascadingAppformDataDto.DictOneToManyFields[subAvialbeUnitExdto.Id.ToString()] = childAppformChildDataDto;
                    }


                }


            }
            return aCascadingAppformDataDto;


        }

        public static AppChildDataDto GetChildOrGrandChildUnitFieldTriggerCascadingDataSource(AppChildDataDto triggerChildRowDataDto, AppMasterDetailDto appMasterDetailDto)
        {

            int? cascadingUnitIdId = triggerChildRowDataDto.CascadingUnitId;
            int? cascadingTrigerFieldId = triggerChildRowDataDto.CascadingFieldId;


            AppTransactionExDto hierarchyTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(appMasterDetailDto.TransactionId);

            var cascadingTrigerUnitDto = hierarchyTransactionExDto.DictAllTransactionUnitIdExDto[cascadingUnitIdId.ToString()];


            SetupCascadinTrigerLockingCondition(triggerChildRowDataDto, hierarchyTransactionExDto, cascadingTrigerFieldId);

            Dictionary<int, AppTransactionFieldExDto> dictTransField = hierarchyTransactionExDto.DictAllTransactionField;//allFiedExdtoList.ToDictionary(o => (int)o.Id, o => o);

            List<AppTransactionFieldExDto> allFiedExdtoList = hierarchyTransactionExDto.DictAllTransactionField.Values.ToList();
            //  Dictionary<int, AppTransactionFieldExDto> dictTransField = hierarchyTransactionExDto.DictAllTransactionField;//allFiedExdtoList.ToDictionary(o => (int)o.Id, o => o);

            //	var rootUnit = hierarchyTransactionExDto.RootMasterUnit;

            // do Regular Cascading 
            if (cascadingTrigerFieldId.HasValue)
            {
                if (triggerChildRowDataDto.DictCascadingFiledDataSource.IsEmpty())
                {
                    triggerChildRowDataDto.DictCascadingFiledDataSource = new Dictionary<string, List<LookupItemDto>>();

                }


                var cascadingTrigerFieldExDto = dictTransField[cascadingTrigerFieldId.Value]; // oneUnitDto.AppTransactionFieldList.FirstOrDefault(o => (int?)o.Id == cascadingTrigerFieldId);

                SetupCasadingFiledChildrenHairarchy(allFiedExdtoList, dictTransField[cascadingTrigerFieldId.Value]);

                SetupOneUnitCascadingDataSource(triggerChildRowDataDto.DictCascadingFiledDataSource, triggerChildRowDataDto.DictOneToOneFields, cascadingTrigerUnitDto, cascadingTrigerFieldExDto, true, appMasterDetailDto, triggerChildRowDataDto);

            }


            return triggerChildRowDataDto;


        }

        private static void SetupCascadinTrigerLockingCondition(AppChildDataDto triggerChildRowDataDto, AppTransactionExDto hierarchyTransactionExDto, int? cascadingTrigerFieldId)
        {
            var transcaFiledDto = hierarchyTransactionExDto.DictAllTransactionField[cascadingTrigerFieldId.Value];

            if (!transcaFiledDto.AppConditionalAction__List.IsEmpty())
            {

                var unitDto = hierarchyTransactionExDto.DictAllTransactionUnitIdExDto[transcaFiledDto.TransactionUnitId.ToString()];


                triggerChildRowDataDto.CascadingNeedToBeLockedFields = new List<string>();

                var conditionList = transcaFiledDto.AppConditionalAction__List.OrderBy(o => o.Name).ToList();

                foreach (var condtion in conditionList)
                {

                    string formula = condtion.BooleanConditionFormula;


                    Dictionary<int, object> unitFiledValue = AppTransactionFormulaBL.ConvertUnitOneRowDbFileNameToFileId(unitDto, triggerChildRowDataDto.DictOneToOneFields);

                    string rightSideEXpress = AppTransactionFormulaBL.RightSideAssignmentEXpressWithRealValue(hierarchyTransactionExDto.DictAllTransactionField, formula, unitFiledValue);

                    object exResult = AppTransactionFormulaBL.ParseAndEvaluteExpress(rightSideEXpress);

                    Boolean? result = ControlTypeValueConverter.ConvertValueToBoolean(exResult);

                    if (result.HasValue && result.Value)
                    {

                        if (condtion.LockingTransactionFieldId.HasValue)
                        {
                            var lockingFiedDto = hierarchyTransactionExDto.DictAllTransactionField[condtion.LockingTransactionFieldId.Value];
                            triggerChildRowDataDto.CascadingNeedToBeLockedFields.Add(lockingFiedDto.DataBaseFieldName);

                        }
                    }



                }
                // need to parse formula:..

            }
        }


        private static List<AppTransactionFieldExDto> SetupTranscationCascadingFieldIds(List<string> IsChangedNeedToCascadingFiedIds, List<string> IsUsedCascadingDataSourceFiedIds,
            AppTransactionExDto hierarchyTransactionExDto)
        {

            List<AppTransactionFieldExDto> allUnitRootCascadinfFieldTriggerFieldList = new List<AppTransactionFieldExDto>();

            List<AppTransactionFieldExDto> allFiedExdtoList = hierarchyTransactionExDto.DictAllTransactionField.Values.ToList();

            var dictTransField = hierarchyTransactionExDto.DictAllTransactionField;



            var dictCascadedIdParentField = GetCascadedFieldParentFiedIds(hierarchyTransactionExDto); ;


            foreach (string parentfiedId in dictCascadedIdParentField.Values.Distinct())
            {
                AppTransactionFieldExDto parentFieldExDto = dictTransField[int.Parse(parentfiedId)];

                parentFieldExDto.IsChangedNeedToCascading = true;

                if (
                        (!parentFieldExDto.DdlparentLevelId.HasValue)
                       && (!parentFieldExDto.MasterEntityFieldlId.HasValue)
                )
                {
                    allUnitRootCascadinfFieldTriggerFieldList.Add(parentFieldExDto);


                }

            }


            foreach (var rootCascadingFile in allUnitRootCascadinfFieldTriggerFieldList)
            {

                SetupCasadingFiledChildrenHairarchy(allFiedExdtoList, rootCascadingFile);

            }

            IsChangedNeedToCascadingFiedIds.AddRange(
                 allFiedExdtoList.Where(o => o.IsChangedNeedToCascading)
                 .Select(o => o.Id.ToString())
                 );

            IsUsedCascadingDataSourceFiedIds.AddRange(
             allFiedExdtoList.Where(o =>

                     o.DdlparentLevelId.HasValue ||
                    o.MasterEntityFieldlId.HasValue ||
                    !string.IsNullOrWhiteSpace(o.DdlQueryText)

             )
             .Select(o => o.Id.ToString())
             );


            return allUnitRootCascadinfFieldTriggerFieldList;

        }

        internal static Dictionary<String, string> GetCascadedFieldParentFiedIds(AppTransactionExDto hierarchyTransactionExDto)
        {

            Dictionary<String, string> dictCascadedIdParentField = new Dictionary<string, string>();




            foreach (var unitDto in hierarchyTransactionExDto.DictAllTransactionUnitIdExDto.Values)
            {
                foreach (AppTransactionFieldExDto filed in unitDto.AppTransactionFieldList)
                {
                    if (filed.DdlparentLevelId.HasValue &&
                        (
                        filed.ControlType == (int)EmAppControlType.DDL
                        || filed.ControlType == (int)EmAppControlType.AutoComplete
                        || filed.ControlType == (int)EmAppControlType.SearchAbleDDL))
                    {
                        dictCascadedIdParentField[filed.Id.ToString()] = filed.DdlparentLevelId.ToString();
                        // parentFiedIdList.Add(filed.DdlparentLevelId.Value);
                    }

                    if (filed.MasterEntityFieldlId.HasValue)
                    {
                        dictCascadedIdParentField[filed.Id.ToString()] = filed.MasterEntityFieldlId.ToString();


                    }




                }
                //if( unitDto.AvailableSourceFilterByParentTransactionFieldId.HasValue )
                // {
                //     dictCascadedIdParentField
                // }
                // need to check avaialbe source unit 



            }


            return dictCascadedIdParentField;
        }





        //cascadingTrigerUnitDto, cascadingTrigerFieldExDto
        internal static void SetupOneUnitCascadingDataSource(Dictionary<string, List<LookupItemDto>> dictCascadingFiledDataSource,
            Dictionary<string, object> currentRow, AppTransactionUnitExDto cascadingTrigerUnitDto,
            AppTransactionFieldExDto cascadingTrigerFieldExDto,
            bool isCascadingFromUIOrCreationNew,
            AppMasterDetailDto aAppformDataDto,
            AppChildDataDto triggerChildRowDataDto,
            bool isNeedToEmptyDataSourceForCascadingChildAutoCompleteField = true

            )
        {
            var dbFixture = AppCacheManagerBL.GetOneDatabaseFixture(cascadingTrigerUnitDto.DataSourceFrom.Value);


            if (cascadingTrigerFieldExDto.TransactionUnitId.ToString() != cascadingTrigerUnitDto.Id.ToString())
            {
                return;
            }

            //????
            object parenFiledValue = currentRow[cascadingTrigerFieldExDto.DataBaseFieldName];




            List<AppTransactionFieldExDto> InnerEntitySubscribeFileds = new List<AppTransactionFieldExDto>();

            if (cascadingTrigerUnitDto.DictInnerEntityChildFieldExdto.ContainsKey((int)cascadingTrigerFieldExDto.Id))
            {
                InnerEntitySubscribeFileds = cascadingTrigerUnitDto.DictInnerEntityChildFieldExdto[(int)cascadingTrigerFieldExDto.Id];
            }

            List<AppTransactionFieldExDto> innerEntitySubscribeFileds_Value = InnerEntitySubscribeFileds.Where(o => !string.IsNullOrWhiteSpace(o.InnerEntitySubscribeFiled)).ToList();
            List<AppTransactionFieldExDto> innerEntitySubscribeFileds_Label = InnerEntitySubscribeFileds.Where(o => !string.IsNullOrWhiteSpace(o.InnerEntityLabelSubscribeFiled)).ToList();

            // need to clear up child select value and datasource
            if ((parenFiledValue == null || string.IsNullOrEmpty(parenFiledValue.ToString())) && !cascadingTrigerFieldExDto.CascadngChildren.IsEmpty())
            {
                EnEmptyChildFiledValue(dictCascadingFiledDataSource, currentRow, cascadingTrigerUnitDto, cascadingTrigerFieldExDto, isCascadingFromUIOrCreationNew, aAppformDataDto, InnerEntitySubscribeFileds, triggerChildRowDataDto);

            }
            else // parenFiledValue has value
            {
                if (!cascadingTrigerFieldExDto.CascadngChildren.IsEmpty())
                {

                    //  triggerChildRowDataDto

                    AppTransactionExDto hierarchyTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(cascadingTrigerUnitDto.TransactionId);

                    Dictionary<int, AppTransactionFieldExDto> dictAllTransactionField = hierarchyTransactionExDto.DictAllTransactionField;

                    Dictionary<string, AppTransactionUnitExDto> dictAllTransactionUnitIdExDt = hierarchyTransactionExDto.DictAllTransactionUnitIdExDto;



                    foreach (AppTransactionFieldExDto cascadingChildFiledDto in cascadingTrigerFieldExDto.CascadngChildren)
                    {

                        //  string tableCascaTable = cascadingChildDto.CascadingRelationTable;

                        string childUnitId = cascadingChildFiledDto.TransactionUnitId.ToString();
                        AppTransactionUnitExDto cascadingChildUnitDto = dictAllTransactionUnitIdExDt[childUnitId];

                        bool isUsedForLoadingAvailableSource = cascadingChildUnitDto.IsUsedForLoadingAvailableSource.HasValue && cascadingChildUnitDto.IsUsedForLoadingAvailableSource.Value;

                        if (isUsedForLoadingAvailableSource)
                        {
                            if (cascadingChildFiledDto.DdlparentLevelId.HasValue)
                            {
                                DataTable childTAble = GetChildDataTable(parenFiledValue, cascadingChildFiledDto.DataBaseFieldName, cascadingChildUnitDto);

                                List<string> subscribeUnitIds = dictAllTransactionUnitIdExDt.Values
                                     .Where(o => o.AvailableSourceUnitId.HasValue && o.AvailableSourceUnitId.ToString() == childUnitId)
                                     .Select(o => o.Id.ToString()).ToList();

                                //   List<Dictionary<string, object>> dictChildUnitListValue = AppMasterDetailFormDataLoadBL.ConvertOneDataTableToDataRowDict(childTAble, cascadingChildUnitDto);

                                List<AppChildDataDto> childDataList = AppMasterDetailFormDataLoadBL.ConvertChildAndGradnChildTableToAppChildDataDtoList(cascadingChildUnitDto, childTAble, null);

                                // cascadgin parent is root unit , need to update child unit leve
                                if (triggerChildRowDataDto == null)
                                {
                                    aAppformDataDto.DictOneToManyFields[childUnitId] = childDataList;





                                }
                                else  // cascadgin parent is child unit , need to update grand child unit
                                {


                                    if (triggerChildRowDataDto.ListSubscribeDataChangeUnitIds == null)
                                    {
                                        triggerChildRowDataDto.ListSubscribeDataChangeUnitIds = new List<string>();


                                    }
                                    triggerChildRowDataDto.ListSubscribeDataChangeUnitIds.AddRange(subscribeUnitIds);


                                    if (triggerChildRowDataDto.ListCascadingAvailableDataChangeUnitIds == null)
                                    {
                                        triggerChildRowDataDto.ListCascadingAvailableDataChangeUnitIds = new List<string>();


                                    }
                                    if (!triggerChildRowDataDto.ListCascadingAvailableDataChangeUnitIds.Contains(childUnitId))
                                    {
                                        triggerChildRowDataDto.ListCascadingAvailableDataChangeUnitIds.Add(childUnitId);


                                    }


                                    if (triggerChildRowDataDto.DictOneToManyFields == null)
                                    {
                                        triggerChildRowDataDto.DictOneToManyFields = new Dictionary<string, List<AppChildDataDto>>();


                                    }

                                    triggerChildRowDataDto.DictOneToManyFields[childUnitId] = childDataList;





                                    // triggerChildRowDataDto.DictOneToOneFields
                                }

                            }


                        }
                        else
                        {
                            PorcessNormalCascadingChild(dictCascadingFiledDataSource, currentRow, cascadingTrigerUnitDto, isCascadingFromUIOrCreationNew, aAppformDataDto, triggerChildRowDataDto, dbFixture, parenFiledValue, cascadingChildFiledDto);


                            if (cascadingChildFiledDto.ControlType == (int)EmAppControlType.AutoComplete || cascadingChildFiledDto.ControlType == (int)EmAppControlType.SearchAbleDDL)
                            {
                                if (isNeedToEmptyDataSourceForCascadingChildAutoCompleteField)
                                {
                                    if (dictCascadingFiledDataSource.ContainsKey(cascadingChildFiledDto.Id.ToString()))
                                    {
                                        dictCascadingFiledDataSource.Remove(cascadingChildFiledDto.Id.ToString());
                                    }

                                }
                            }
                        }

                    }
                }

                // 
                if (innerEntitySubscribeFileds_Value.Count > 0 && isCascadingFromUIOrCreationNew)
                {
                    SetupInnerEntityRelationValue(currentRow, cascadingTrigerFieldExDto, parenFiledValue, innerEntitySubscribeFileds_Value, aAppformDataDto);
                }

                if (innerEntitySubscribeFileds_Label.Count > 0)      
                {
                    SetupInnerEntityRelationLabel(currentRow, cascadingTrigerFieldExDto, parenFiledValue, innerEntitySubscribeFileds_Label, aAppformDataDto);
                }

            }


        }

        private static void PorcessNormalCascadingChild(Dictionary<string, List<LookupItemDto>> dictCascadingFiledDataSource, Dictionary<string, object> currentRow, AppTransactionUnitExDto cascadingTrigerUnitDto, bool isCascadingFromUIOrCreationNew, AppMasterDetailDto aAppformDataDto, AppChildDataDto triggerChildRowDataDto, DatabaseSchemaMrg.DatabaseFixture dbFixture, object parenFiledValue, AppTransactionFieldExDto cascadingChildFiledDto)
        {
            string tableCascaTable = AppMetaDataBL.GetQulifiedTableName(cascadingChildFiledDto.CascadingRelationTableSchemaOwner, cascadingChildFiledDto.CascadingRelationTable, dbFixture.SqlServerType.Value);

            string parentKey = cascadingChildFiledDto.CascadingRelationTableParentKeyField;
            string transFieldDatasetFiledmapping = cascadingChildFiledDto.CascadingRelationTableChildKeyField;


            List<LookupItemDto> childAllList = new List<LookupItemDto>();

            if (cascadingChildFiledDto.DataRetrieveType.HasValue && cascadingChildFiledDto.DataRetrieveType.Value == (int)EmAppCascadingSourceType.RelationalTable)
            {


                string query = string.Format(@" SELECT {0} from {1} where {2}={3}", transFieldDatasetFiledmapping, tableCascaTable, parentKey, parenFiledValue);

                if (parenFiledValue is string)
                {
                    query = string.Format(@" SELECT {0} from {1} where {2}='{3}'", transFieldDatasetFiledmapping, tableCascaTable, parentKey, parenFiledValue);
                }

                childAllList = AppEntityInfoBL.GetLookupItemList((int)cascadingChildFiledDto.EntityId, query);
            }

            else if (cascadingChildFiledDto.DataRetrieveType.HasValue && cascadingChildFiledDto.DataRetrieveType.Value == (int)EmAppCascadingSourceType.ExternalOneToManyMapping)
            {

                string queryStorcname = tableCascaTable;
                List<SqlParameter> sqlParamterList = new List<SqlParameter>();

                SqlParameter aSqlParameter = new SqlParameter();
                // aSqlParameter.IsNullable = true;
                aSqlParameter.ParameterName = parentKey;
                // need to convert?
                aSqlParameter.Value = parenFiledValue;
                sqlParamterList.Add(aSqlParameter);

                DataTable fillDataTable = new DataTable();

                using (DataAccessAdapter aDataAccessAdapterWithDataSource = AppTenantAdapterBL.GetTenantAdapter())
                {
                    aDataAccessAdapterWithDataSource.CallRetrievalStoredProcedure(queryStorcname, sqlParamterList.ToArray(), fillDataTable);

                }


                ConvertDataRowToLookupItemWithDepdendenColumn(transFieldDatasetFiledmapping, childAllList, fillDataTable);
            }


            else if (cascadingChildFiledDto.DataRetrieveType.HasValue && cascadingChildFiledDto.DataRetrieveType.Value == (int)EmAppCascadingSourceType.QueryStatement)
            {

                string query = tableCascaTable;

                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    IDataReader reader = adapter.FetchDataReader(new RetrievalQuery(new SqlCommand(query)), CommandBehavior.SingleResult);
                    while (reader.Read())
                    {

                        childAllList.Add(new LookupItemDto() { Id = reader.GetValue(0), Display = reader.GetValue(1).ToString() });
                    }


                }



            }

            string cascadingChildFiedId = cascadingChildFiledDto.Id.ToString();
            if (dictCascadingFiledDataSource.ContainsKey(cascadingChildFiedId))
            {
                dictCascadingFiledDataSource.Remove(cascadingChildFiedId);
            }
            dictCascadingFiledDataSource.Add(cascadingChildFiedId, childAllList);

            if (isCascadingFromUIOrCreationNew)
            {
                if (currentRow.ContainsKey(cascadingChildFiledDto.DataBaseFieldName))
                {
                    currentRow[cascadingChildFiledDto.DataBaseFieldName] = null;
                }


                // need to check Ineer
            }

            //recursive to get all child cascading lookititem
            if (!cascadingChildFiledDto.CascadngChildren.IsEmpty())
                SetupOneUnitCascadingDataSource(dictCascadingFiledDataSource, currentRow, cascadingTrigerUnitDto, cascadingChildFiledDto, isCascadingFromUIOrCreationNew, aAppformDataDto, triggerChildRowDataDto);

        }

        private static DataTable GetChildDataTable(object parentFiledValue, string childUnitDBColumnName, AppTransactionUnitExDto aChildTransactionUnitExDto)
        {
            string childtableName = aChildTransactionUnitExDto.DataBaseTableName;
            DatabaseTable childdatabaseTable = AppCacheManagerBL.GetDatabaseTable(childtableName, aChildTransactionUnitExDto.DataSourceFrom, aChildTransactionUnitExDto.SchemaOwner);

            // Master primary key only has one key, for sibling unit alway take first one
            DatabaseFixture databaseFixtureInstance = AppCacheManagerBL.GetOneDatabaseFixture(aChildTransactionUnitExDto.DataSourceFrom.Value);


            SqlWriter sqlWriter = new SqlWriter(childdatabaseTable, databaseFixtureInstance.SqlServerType.Value);

            string childQuery = sqlWriter.SelectAllSql();


            List<DbParameter> paraList = new List<DbParameter>();
            string parameName = string.Format("{0}", childUnitDBColumnName);
            DbParameter parameter = databaseFixtureInstance.CreateParameter(parameName.Replace(" ", ""));
            parameter.Value = parentFiledValue;
            paraList.Add(parameter);

            string whereCaluse = string.Format(" WHERE  {0}={1}", childUnitDBColumnName, parameter.ParameterName);

            childQuery = childQuery + whereCaluse;

            DataTable childdataTble = databaseFixtureInstance.RetriveDataTable(childQuery, paraList); //adpater.ExecuteDataTableRetrievalQuery(childQuery, listParamters);


            return childdataTble;
        }

        //   //cascadingTrigerUnitDto, cascadingTrigerFieldExDto
        private static void EnEmptyChildFiledValue(
            Dictionary<string, List<LookupItemDto>> dictCascadingFiledDataSource,
            Dictionary<string, object> currentRow,
            AppTransactionUnitExDto cascadingTrigerUnitDto,
            AppTransactionFieldExDto cascadingTrigerFieldExDto,
            bool isCascadingFromUIOrCreationNew,
            AppMasterDetailDto aAppformDataDto,
            List<AppTransactionFieldExDto> InnerEntitySubscribeFileds,
            AppChildDataDto triggerChildRowDataDto

            )
        {

            AppTransactionExDto hierarchyTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(cascadingTrigerUnitDto.TransactionId);

            Dictionary<int, AppTransactionFieldExDto> dictAllTransactionField = hierarchyTransactionExDto.DictAllTransactionField;

            foreach (AppTransactionFieldExDto cascadingChildDto in cascadingTrigerFieldExDto.CascadngChildren)
            {


                List<LookupItemDto> childAllList = new List<LookupItemDto>();
                string cascadingChildFiedId = cascadingChildDto.Id.ToString();

                if (dictCascadingFiledDataSource.ContainsKey(cascadingChildFiedId))
                {
                    dictCascadingFiledDataSource.Remove(cascadingChildFiedId);
                }


                dictCascadingFiledDataSource.Add(cascadingChildFiedId, childAllList);
                currentRow[cascadingChildDto.DataBaseFieldName] = null;


                //recursive to get all child cascading lookititem
                if (!cascadingChildDto.CascadngChildren.IsEmpty())
                {
                    if (cascadingChildDto.TransactionUnitId.ToString() == cascadingTrigerUnitDto.Id.ToString())
                    {
                        SetupOneUnitCascadingDataSource(dictCascadingFiledDataSource, currentRow, cascadingTrigerUnitDto, cascadingChildDto, isCascadingFromUIOrCreationNew, aAppformDataDto, triggerChildRowDataDto);

                    }

                }

                // process avaialbe data source
                if (cascadingChildDto.MappingToAvailableSourceUnitTransactionFieldId.HasValue)
                {
                    AppTransactionFieldExDto aAppTransactionFieldExDto = dictAllTransactionField[cascadingChildDto.MappingToAvailableSourceUnitTransactionFieldId.Value];
                    //  AppChildDataDto.

                }



            }

            foreach (var cascadingChildDto in InnerEntitySubscribeFileds)
            {
                if (!string.IsNullOrWhiteSpace(cascadingChildDto.InnerEntitySubscribeFiled))
                {
                    currentRow[cascadingChildDto.DataBaseFieldName] = null;
                }

                if (!string.IsNullOrWhiteSpace(cascadingChildDto.InnerEntityLabelSubscribeFiled))
                {
                    if (aAppformDataDto.DictCascadingFieldIdAndLabel != null && aAppformDataDto.DictCascadingFieldIdAndLabel.ContainsKey(cascadingChildDto.Id.ToString()))
                    {
                        aAppformDataDto.DictCascadingFieldIdAndLabel.Remove(cascadingChildDto.Id.ToString());
                    }
                }

            }
        }

        internal static void ConvertDataRowToLookupItemWithDepdendenColumn(string transFieldDatasetFiledmapping, List<LookupItemDto> childAllList, DataTable fillDataTable)
        {
            Dictionary<string, string> dictransasFieldDataSetTMapping = AppTransactionFieldExDto.SplitInputStringAsDictTransFieldSqlParameterMapping(transFieldDatasetFiledmapping);


            foreach (DataRow dataRow in fillDataTable.Rows)
            {
                LookupItemDto aLookupItemDto = new LookupItemDto() { Id = dataRow[0], Display = dataRow[1].ToString() };

                if (dictransasFieldDataSetTMapping.Count > 0)
                {
                    aLookupItemDto.DictDependentFieldValue = new Dictionary<string, object>();

                    foreach (string transFiledKey in dictransasFieldDataSetTMapping.Keys)
                    {

                        string dataSetField = dictransasFieldDataSetTMapping[transFiledKey];

                        object value = dataRow[dataSetField];



                        if (value == DBNull.Value)
                        {
                            value = null;
                        }

                        aLookupItemDto.DictDependentFieldValue[transFiledKey] = value; ;




                    }

                }


                childAllList.Add(aLookupItemDto);



            }

        }

        //internal static Dictionary<string, string> SplitInputStringAsDictTransFieldSqlParameterMapping(string childKey)
        //{
        //    Dictionary<string, string> dictDataSetTransasFieldMapping = new Dictionary<string, string>();

        //    if (!string.IsNullOrEmpty(childKey))
        //    {

        //        string[] listString = childKey.Split("|".ToArray());
        //        foreach (string dataSetTransFieldString in listString)
        //        {
        //            string[] dataSetTransField = dataSetTransFieldString.Split(":".ToArray());
        //            dictDataSetTransasFieldMapping.Add(dataSetTransField[0], dataSetTransField[1]);



        //        }

        //    }
        //    return dictDataSetTransasFieldMapping;
        //}

        internal static void SetupInnerEntityRelationValue(Dictionary<string, object> DictOneToOneFields, AppTransactionFieldExDto parentCascadingFieldExDto, object parenFiledValue,
        List<AppTransactionFieldExDto> innerEntityValueSubscribeFiledExDtoList,
        AppMasterDetailDto aAppformDataDto

        )
        {

            if (parenFiledValue == null)
            {

                foreach (var inerentityChildDto in innerEntityValueSubscribeFiledExDtoList)
                {

                    DictOneToOneFields[inerentityChildDto.DataBaseFieldName] = null;

                }

                return;

            }


            // from ForeighUnit Key DataSource
            if (!parentCascadingFieldExDto.EntityId.HasValue && parentCascadingFieldExDto.DdlForeignUnitId.HasValue)
            {
                int foreignUnitId = parentCascadingFieldExDto.DdlForeignUnitId.Value;


                AppTransactionExDto transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(aAppformDataDto.TransactionId);

                var foreignnitExdto = transactionExDto.DictAllTransactionUnitIdExDto[foreignUnitId.ToString()];

                List<AppChildDataDto> foreighUnitDataRowList = aAppformDataDto.DictOneToManyFields[foreignUnitId.ToString()];

                string logicaKey = foreignnitExdto.AppTransactionFieldList.Where(o => o.IsUnique.HasValue && o.IsUnique.Value).Select(o => o.DataBaseFieldName).FirstOrDefault();

                AppChildDataDto foreignUnitAppChildDataDto = foreighUnitDataRowList.Where(o => o.DictOneToOneFields[logicaKey].ToString() == parenFiledValue.ToString()).FirstOrDefault();

                if (foreignUnitAppChildDataDto != null)
                {
                    var dictForeignUnitRowDataFileds = foreignUnitAppChildDataDto.DictOneToOneFields;
                    foreach (var inerentityChildDto in innerEntityValueSubscribeFiledExDtoList)
                    {
                        if (dictForeignUnitRowDataFileds.ContainsKey(inerentityChildDto.InnerEntitySubscribeFiled))
                        {
                            DictOneToOneFields[inerentityChildDto.DataBaseFieldName] = dictForeignUnitRowDataFileds[inerentityChildDto.InnerEntitySubscribeFiled];
                        }
                    }
                }
            }
            else
            {
                SetupStandAlonInnerEntityColumn_Value(DictOneToOneFields, parentCascadingFieldExDto, parenFiledValue, innerEntityValueSubscribeFiledExDtoList, aAppformDataDto);
            }

        }


        internal static void SetupInnerEntityRelationLabel(Dictionary<string, object> DictOneToOneFields, AppTransactionFieldExDto parentCascadingFieldExDto, object parenFiledValue,
        List<AppTransactionFieldExDto> innerEntityLabelSubscribeFiledExDtoList,
        AppMasterDetailDto aAppformDataDto

        )
        {
            if (aAppformDataDto.DictCascadingFieldIdAndLabel == null)
            {
                aAppformDataDto.DictCascadingFieldIdAndLabel = new Dictionary<string, string>();
            }

            if (parenFiledValue == null)
            {
                foreach (var inerentityChildDto in innerEntityLabelSubscribeFiledExDtoList)
                {
                    if (aAppformDataDto.DictCascadingFieldIdAndLabel.ContainsKey(inerentityChildDto.Id.ToString()))
                    {
                        aAppformDataDto.DictCascadingFieldIdAndLabel.Remove(inerentityChildDto.Id.ToString());
                    }
                }

                return;
            }


            // from ForeighUnit Key DataSource
            if (!parentCascadingFieldExDto.EntityId.HasValue && parentCascadingFieldExDto.DdlForeignUnitId.HasValue)
            {
                int foreignUnitId = parentCascadingFieldExDto.DdlForeignUnitId.Value;
                //var foreignnitExdto = transactionExDto.DictAllTransactionUnitIdExDto[foreignUnitId.ToString()];


                AppTransactionExDto transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(aAppformDataDto.TransactionId);

                var foreignnitExdto = transactionExDto.DictAllTransactionUnitIdExDto[foreignUnitId.ToString()];

                List<AppChildDataDto> foreighUnitDataRowList = aAppformDataDto.DictOneToManyFields[foreignUnitId.ToString()];

                string logicaKey = foreignnitExdto.AppTransactionFieldList.Where(o => o.IsUnique.HasValue && o.IsUnique.Value).Select(o => o.DataBaseFieldName).FirstOrDefault();

                AppChildDataDto foreignUnitAppChildDataDto = foreighUnitDataRowList.Where(o => o.DictOneToOneFields[logicaKey].ToString() == parenFiledValue.ToString()).FirstOrDefault();

                if (foreignUnitAppChildDataDto != null)
                {
                    var dictForeignUnitRowDataFileds = foreignUnitAppChildDataDto.DictOneToOneFields;
                    foreach (var inerentityChildDto in innerEntityLabelSubscribeFiledExDtoList)
                    {
                        if (dictForeignUnitRowDataFileds.ContainsKey(inerentityChildDto.InnerEntityLabelSubscribeFiled))
                        {
                            var subScribeFieldUnit = transactionExDto.DictAllTransactionUnitIdExDto[inerentityChildDto.TransactionUnitId.ToString()];

                            if (subScribeFieldUnit == transactionExDto.RootMasterUnit
                                || (subScribeFieldUnit.IsMasterSiblingUnit.HasValue && subScribeFieldUnit.IsMasterSiblingUnit.Value))
                            {
                                aAppformDataDto.DictCascadingFieldIdAndLabel[inerentityChildDto.Id.ToString()] = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(dictForeignUnitRowDataFileds[inerentityChildDto.InnerEntityLabelSubscribeFiled]);
                            }
                        }
                    }
                }
            }
            else
            {
                SetupStandAlonInnerEntityColumn_Label(DictOneToOneFields, parentCascadingFieldExDto, parenFiledValue, innerEntityLabelSubscribeFiledExDtoList, aAppformDataDto);
            }

            //

        }

        private static void SetupStandAlonInnerEntityColumn_Value(Dictionary<string, object> DictOneToOneFields, AppTransactionFieldExDto parentCascadingFieldExDto, object parenFiledValue, List<AppTransactionFieldExDto> InnerEntitySubscribeFileds, AppMasterDetailDto aAppformDataDto)
        {
            var entityInfo = AppEntityInfoBL.RetrieveOneAppEntityInfoExDto(parentCascadingFieldExDto.EntityId);
            string tablename = entityInfo.TableName;

            List<string> selectFieldDbNameList = new List<string>();

            foreach (var inerentityChildDto in InnerEntitySubscribeFileds)
            {
                if (!string.IsNullOrWhiteSpace(inerentityChildDto.InnerEntitySubscribeFiled))
                {
                    selectFieldDbNameList.Add(inerentityChildDto.InnerEntitySubscribeFiled);
                }                
            }

            selectFieldDbNameList = selectFieldDbNameList.Distinct().ToList();

            if (selectFieldDbNameList.Count > 0)
            {
                string selectField = "[" + string.Join("], [", selectFieldDbNameList) + "]";

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
                            if (!string.IsNullOrWhiteSpace(inerentityChildDto.InnerEntitySubscribeFiled))
                            {
                                DictOneToOneFields[inerentityChildDto.DataBaseFieldName] = row[inerentityChildDto.InnerEntitySubscribeFiled];
                            } 
                        }
                    }

                }
            }
        }

        private static void SetupStandAlonInnerEntityColumn_Label(Dictionary<string, object> DictOneToOneFields, AppTransactionFieldExDto parentCascadingFieldExDto, object parenFiledValue, List<AppTransactionFieldExDto> InnerEntitySubscribeFileds, AppMasterDetailDto aAppformDataDto)
        {
            var entityInfo = AppEntityInfoBL.RetrieveOneAppEntityInfoExDto(parentCascadingFieldExDto.EntityId);
            string tablename = entityInfo.TableName;



            List<string> selectFieldDbNameList = new List<string>();

            foreach (var inerentityChildDto in InnerEntitySubscribeFileds)
            {   
                if (!string.IsNullOrWhiteSpace(inerentityChildDto.InnerEntityLabelSubscribeFiled))
                {
                    selectFieldDbNameList.Add(inerentityChildDto.InnerEntityLabelSubscribeFiled);
                }
            }

            selectFieldDbNameList = selectFieldDbNameList.Distinct().ToList();

            if (selectFieldDbNameList.Count > 0)
            {
                string selectField = "[" + string.Join("], [", selectFieldDbNameList) + "]";

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
                            if (!string.IsNullOrWhiteSpace(inerentityChildDto.InnerEntityLabelSubscribeFiled))
                            {
                                aAppformDataDto.DictCascadingFieldIdAndLabel[inerentityChildDto.Id.ToString()] = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(row[inerentityChildDto.InnerEntityLabelSubscribeFiled]);
                            }

                        }
                    }

                }
            }
        }


        private static void SetupCasadingFiledChildrenHairarchy(IEnumerable<AppTransactionFieldExDto> appTransactionFieldList, AppTransactionFieldExDto cascadingTriggerFieldDto)
        {

            List<AppTransactionFieldExDto> children = GetChilds(appTransactionFieldList, cascadingTriggerFieldDto);

            if (!children.IsEmpty())
            {
                cascadingTriggerFieldDto.CascadngChildren = children;

                cascadingTriggerFieldDto.CascadngChildren.ForAll(c => SetupCasadingFiledChildrenHairarchy(appTransactionFieldList, c));

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

        public static Dictionary<string, object> GetDateTimeUnitFieldCascading(object data)
        {
            dynamic result = JsonConvert.DeserializeObject(data.ToString());
            int unitId = result.unitId;
            int fileId = result.fieldId;
            object dataTimeValue = result.value;

            Dictionary<string, object> toRetrun = new Dictionary<string, object>();

            DateTime? dateTeimValeu = ControlTypeValueConverter.ConvertValueToDate(result.value);
            if (dateTeimValeu.HasValue)
            {
                var unitDXdto = AppTransactionBL.RetrieveOneAppTransactionUnitExDto(unitId);

                if (unitDXdto.DictInnerEntityChildFieldExdto.ContainsKey(fileId))
                {

                    var childDtoList = unitDXdto.DictInnerEntityChildFieldExdto[fileId];

                    int year = dateTeimValeu.Value.Year;
                    int month = dateTeimValeu.Value.Month;
                    int day = dateTeimValeu.Value.Day;
                    DayOfWeek dayofWeek = dateTeimValeu.Value.DayOfWeek;

                    foreach (var child in childDtoList)
                    {
                        if (child.InnerEntitySubscribeFiled == EmAppDateTimeProperties.YearNumber.ToString())
                        {
                            toRetrun.Add(child.DataBaseFieldName, year);

                        }
                        else if (child.InnerEntitySubscribeFiled == EmAppDateTimeProperties.MonthNumber.ToString())
                        {
                            toRetrun.Add(child.DataBaseFieldName, month);

                        }

                        else if (child.InnerEntitySubscribeFiled == EmAppDateTimeProperties.DayOfMonthNumber.ToString())
                        {
                            toRetrun.Add(child.DataBaseFieldName, day);
                        }

                        else if (child.InnerEntitySubscribeFiled == EmAppDateTimeProperties.DayOfWeekName.ToString())
                        {
                            toRetrun.Add(child.DataBaseFieldName, dayofWeek.ToString());

                        }


                    }

                }

            }
            return toRetrun;
        }



        // RelationTable: SP_GetRegistrationOrderDetailForIndividualClassHours_callDataSet

        //pararentCscading: @startDate:StartDate|@endDate:EndDate; unitId:1234->@CourseCalendar=column1:ChildTransFiled1 | column2: ChildTransFiled2 | | column3: ChildTransFiled3;

        //ChildCascading:   unitId:1234->@ TotalHours:Qty|CostRate:CostRate|TotalCost:LineItemTotal


        public static AppRetrieveMutipleColumnDataSourceDto AppRetrieveMutipleColumnDataSourceDto(AppRetrieveMutipleColumnDataSourceDto appRetrieveMutipleColumnDataSourceDto)
        {
            return test();

        }

        private static APP.Components.EntityDto.AppRetrieveMutipleColumnDataSourceDto test()
        {
            AppRetrieveMutipleColumnDataSourceDto testData = new AppRetrieveMutipleColumnDataSourceDto();
            testData.ReturnRowData = new Dictionary<string, object>();
            testData.ReturnRowData.Add("Qty", 10);
            testData.ReturnRowData.Add("CostRate", 100);
            testData.ReturnRowData.Add("LineItemTotal", 1000);


            testData.DictReturnFieldDataSet = new Dictionary<int, List<LookupItemDto>>();
            List<LookupItemDto> aList = new List<LookupItemDto>();
            aList.Add(new LookupItemDto() { Id = 1, Display = "AAA" });
            aList.Add(new LookupItemDto() { Id = 2, Display = "BBB" });

            testData.DictReturnFieldDataSet.Add(2192, aList);

            return testData;
        }




        public static void SetupIntialAutoCompleteFieldDataSource(AppTransactionExDto hierarchyTransactionExDto, AppMasterDetailDto aAppformDataDto, bool isFromUIOrCreationNew = false)
        {
            aAppformDataDto.DictAutoCompleteFieldDataSource = new Dictionary<string, List<LookupItemDto>>();


            List<AppTransactionFieldExDto> rootLevelAutoCompleteFieldList = new List<AppTransactionFieldExDto>();

            foreach (var rootLevelUnit in hierarchyTransactionExDto.AppTransactionUnitList)
            {
                foreach (var afield in rootLevelUnit.AppTransactionFieldList.OrderBy(o => o.SortOrder).ToList())
                {
                    if (afield.ControlType == (int)EmAppControlType.AutoComplete || afield.ControlType == (int)EmAppControlType.SearchAbleDDL)
                    {
                        rootLevelAutoCompleteFieldList.Add(afield);
                    }
                }
            }

            foreach (AppTransactionFieldExDto rootlevelutoCompleteFieldExDto in rootLevelAutoCompleteFieldList)
            {
                object filedValue = aAppformDataDto.DictOneToOneFields[rootlevelutoCompleteFieldExDto.DataBaseFieldName];
                List<object> filedValueList = new List<object>();

                if (filedValue != null && !string.IsNullOrWhiteSpace(filedValue.ToString()))
                {
                    filedValueList.Add(filedValue);
                }

                SetupIntialAutoCompleteFieldDataSource_ProcessOneTransField(filedValueList, aAppformDataDto.DictAutoCompleteFieldDataSource, isFromUIOrCreationNew, rootlevelutoCompleteFieldExDto);
            }

            // ToDO: Child and GrandChild Level

            //Child and   GrandChild
            foreach (string childUnitId in aAppformDataDto.DictOneToManyFields.Keys)
            {

                var chidUnitDto = hierarchyTransactionExDto.DictAllTransactionUnitIdExDto[childUnitId];

                List<AppChildDataDto> childDtoList = aAppformDataDto.DictOneToManyFields[childUnitId];

                foreach (var childFieldDto in chidUnitDto.AppTransactionFieldList.OrderBy(o => o.SortOrder).ToList())
                {
                    if (childFieldDto.ControlType == (int)EmAppControlType.AutoComplete || childFieldDto.ControlType == (int)EmAppControlType.SearchAbleDDL)
                    {
                        List<object> filedValueList = new List<object>();

                        foreach (AppChildDataDto childRowDto in childDtoList)
                        {
                            object filedValue = childRowDto.DictOneToOneFields[childFieldDto.DataBaseFieldName];

                            if (filedValue != null && !string.IsNullOrWhiteSpace(filedValue.ToString()))
                            {
                                filedValueList.Add(filedValue);
                            }
                        }

                        SetupIntialAutoCompleteFieldDataSource_ProcessOneTransField(filedValueList, aAppformDataDto.DictAutoCompleteFieldDataSource, isFromUIOrCreationNew, childFieldDto);
                    }
                }


                // Process GrandChild Grid

                foreach (AppChildDataDto childRowDto in childDtoList)
                {
                    Dictionary<String, List<AppChildDataDto>> dictGrandChildUnitDataRowList = childRowDto.DictOneToManyFields;

                    foreach (string grandChildUnit in dictGrandChildUnitDataRowList.Keys)
                    {
                        var grandchidUnitDto = hierarchyTransactionExDto.DictAllTransactionUnitIdExDto[childUnitId];

                        List<AppChildDataDto> grandChildDtoList = dictGrandChildUnitDataRowList[grandChildUnit];


                        foreach (var grandchildFieldDto in grandchidUnitDto.AppTransactionFieldList.OrderBy(o => o.SortOrder).ToList())
                        {
                            if (grandchildFieldDto.ControlType == (int)EmAppControlType.AutoComplete || grandchildFieldDto.ControlType == (int)EmAppControlType.SearchAbleDDL)
                            {
                                List<object> filedValueList = new List<object>();

                                foreach (AppChildDataDto grandchildRowDto in childDtoList)
                                {
                                    object filedValue = grandchildRowDto.DictOneToOneFields[grandchildFieldDto.DataBaseFieldName];

                                    if (filedValue != null && !string.IsNullOrWhiteSpace(filedValue.ToString()))
                                    {
                                        filedValueList.Add(filedValue);
                                    }
                                }

                                SetupIntialAutoCompleteFieldDataSource_ProcessOneTransField(filedValueList, aAppformDataDto.DictAutoCompleteFieldDataSource, isFromUIOrCreationNew, grandchildFieldDto);
                            }
                        }
                    }
                }
            }


        }

        private static void SetupIntialAutoCompleteFieldDataSource_ProcessOneTransField(List<object> filedValueList, Dictionary<string, List<LookupItemDto>> dictAutoCompleteFieldDataSource, bool isFromUIOrCreationNew, AppTransactionFieldExDto fieldExDto)
        {

            string fieldId = fieldExDto.Id.ToString();

            if (dictAutoCompleteFieldDataSource.ContainsKey(fieldId))
            {
                dictAutoCompleteFieldDataSource.Remove(fieldId);
            }


            if (isFromUIOrCreationNew || filedValueList.Count == 0)
            {
                dictAutoCompleteFieldDataSource.Add(fieldId, new List<LookupItemDto>());
            }
            else
            {
                if (filedValueList.Count <= 1000)
                {
                    string pkInClause = filedValueList.Select(o => o.ToString()).Aggregate((i, j) => i + "," + j);


                    List<LookupItemDto> lookupItemList = AppEntityInfoBL.GetLookupItemList((int)fieldExDto.EntityId, pkInClause);
                    dictAutoCompleteFieldDataSource.Add(fieldId, lookupItemList);
                }
                else if (filedValueList.Count > 1000)
                {
                    var valueStringList = filedValueList.Select(o => o.ToString());

                    List<LookupItemDto> allLookupItemList = AppEntityInfoBL.GetLookupItemList((int)fieldExDto.EntityId, "");
                    List<LookupItemDto> lookupItemList = new List<LookupItemDto>();

                    foreach (var lookupItem in allLookupItemList)
                    {
                        if (valueStringList.Contains(lookupItem.Id.ToString()))
                        {
                            lookupItemList.Add(lookupItem);
                        }
                    }


                    dictAutoCompleteFieldDataSource.Add(fieldId, lookupItemList);
                }
            }



        }
    }
}