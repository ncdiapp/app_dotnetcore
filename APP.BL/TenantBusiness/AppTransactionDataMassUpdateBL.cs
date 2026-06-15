using System.Collections.Generic;
using System.Data;
using System.Linq;
using APP.LBL;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Globalization;
using APP.Framework.Validation;
using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;
using System.Text.RegularExpressions;
using System;
using DatabaseSchemaMrg.DataSchema;
using System.Data.SqlClient;
using DatabaseSchemaMrg;
//using APP.Persistence.Common;
using System.Data.Common;

using APP.Framework;
namespace App.BL
{
    public static class AppTransactionDataMassUpdateBL
    {
        /// <summary>
        ///   Setting: Search view Definition (is mass update
        ///    a:  aAppSearchViewEntity.UpdateTransctionId , aAppSearchViewEntity.UpdateBaseTranscationUnitId ( mapping filed)
        ///    b:  aAppTransactionExDto.TransactionOrganizedTyp ( mapping filed)
        ///   process flow:
        ///  1) get the search resut Dto
        ///  2) check if is mass update view if yes
        ///  3) create a javascipt object  MassUpdateSaveDto
        ///  4) call SaveMassUpdateResult
        ///  5) return mass update result ( error message
        ///  6
        /// 
        /// </summary>
        /// <param name="massUpdateSaveDto"></param>
        /// <returns></returns>
        // return Operation Result
        public static OperationCallResult<StaticSearchResultRowJsonDto> SaveMassUpdateResult(MassUpdateSaveDto massUpdateSaveDto)
        {
            OperationCallResult<StaticSearchResultRowJsonDto> toReturn = new OperationCallResult<StaticSearchResultRowJsonDto>();
            ValidationResult validationResult = new ValidationResult();
            toReturn.ValidationResult = validationResult;

            if (massUpdateSaveDto.IsListEditSimpleMassUpdate)
            {
                if (massUpdateSaveDto.MassUpdateAppListDataDto != null)
                {
                    massUpdateSaveDto.MassUpdateAppListDataDto.IsMassUpdate = true;
                    var saveResult = AppListEditFormDataLoadBL.SaveListEditFormData(massUpdateSaveDto.MassUpdateAppListDataDto);
                    validationResult.Merge(saveResult.ValidationResult);
                }
            }
            else
            {
                if (massUpdateSaveDto.SearchViewId.HasValue)
                {

                    //AppCacheManagerBL.gEt 

                    AppSearchViewEntity aAppSearchViewEntity = AppSearchViewConfigBL.RetrieveOneAppSearchViewEntity(massUpdateSaveDto.SearchViewId);



                    // Unit level massupdate 
                    if (aAppSearchViewEntity.UpdateTransctionId.HasValue)
                    {
                        if (massUpdateSaveDto.ModifiedSearchResult != null && massUpdateSaveDto.SearchViewId.HasValue)
                        {
                            SearchResultDto calculationResult = new SearchResultDto();
                            calculationResult.SearchResultRowList = massUpdateSaveDto.ModifiedSearchResult;
                            AppTransactionFormulaBL.CaculateOneSearchResult(calculationResult, massUpdateSaveDto.SearchViewId.Value);
                            massUpdateSaveDto.ModifiedSearchResult = calculationResult.SearchResultRowList.ToList();
                        }
                        


                        //var transcationDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(aAppSearchViewEntity.UpdateTransctionId);

                        AppTransactionExDto aAppTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(aAppSearchViewEntity.UpdateTransctionId);




                        Dictionary<int, String> dictMassUpdateViewFiledTranscaDbFiled = new Dictionary<int, String>();

                        Dictionary<int, AppTransactionFieldExDto> dictMassUpdateViewFiledTransactionFieldExDto = new Dictionary<int, AppTransactionFieldExDto>();

                        foreach (var viewFiledEntity in aAppSearchViewEntity.AppSearchViewField)
                        {
                            if (viewFiledEntity.MassUpdateTransactionFieldId.HasValue)
                            {
                                var tranFieldDto = aAppTransactionExDto.DictAllTransactionField[viewFiledEntity.MassUpdateTransactionFieldId.Value];
                                dictMassUpdateViewFiledTranscaDbFiled[viewFiledEntity.SearchViewFieldId] = tranFieldDto.DataBaseFieldName;

                                dictMassUpdateViewFiledTransactionFieldExDto[viewFiledEntity.SearchViewFieldId] = tranFieldDto;
                            }

                        }

                        // need to Classif
                        List<int> unitIdList = dictMassUpdateViewFiledTransactionFieldExDto.Values.Select(o => o.TransactionUnitId).Distinct().ToList();

                        // in the unit level
                        if (aAppSearchViewEntity.UpdateBaseTranscationUnitId.HasValue)
                        {
                            if (aAppTransactionExDto.OtherOptions != null && aAppTransactionExDto.OtherOptions.IsApiIntegrationTransaction)
                            {
                                if (aAppTransactionExDto.RootMasterUnit != null && (int)aAppTransactionExDto.RootMasterUnit.Id == aAppSearchViewEntity.UpdateBaseTranscationUnitId.Value)
                                {
                                    PorcessApiMasterDetailTransactionRootUnit(validationResult, massUpdateSaveDto, aAppSearchViewEntity, aAppTransactionExDto, dictMassUpdateViewFiledTranscaDbFiled);
                                }
                            }
                            else
                            {
                                DatabaseFixture databaseFixtureInstance = AppCacheManagerBL.GetOneDatabaseFixture(aAppTransactionExDto.DataSourceFrom.Value);

                                AppTransactionUnitExDto transactionUnitExDto = aAppTransactionExDto.DictAllTransactionUnitIdExDto[aAppSearchViewEntity.UpdateBaseTranscationUnitId.ToString()];

                                PorcessOneUnit(validationResult, massUpdateSaveDto, aAppSearchViewEntity, transactionUnitExDto, databaseFixtureInstance,
                                 dictMassUpdateViewFiledTranscaDbFiled);

                            }
                        }

                    }

                }

            }




            List<StaticSearchResultRowJsonDto> result = new List<StaticSearchResultRowJsonDto>();
            toReturn.ObjectList = result;

            return toReturn;

        }

       

        //private static void OldHairaykeyUodate(MassUpdateSaveDto massUpdateSaveDto, AppSearchViewEntity aAppSearchViewEntity, AppTransactionExDto aAppTransactionExDto, DatabaseFixture databaseFixtureInstance, Dictionary<int, string> dictMassUpdateViewFiledTranscaDbFiled, List<int> unitIdList)
        //{
        //    // one unt update
        //    if (unitIdList.Count == 1)
        //    {
        //        string unitId = unitIdList[0].ToString();

        //        AppTransactionUnitExDto transactionUnitExDto = aAppTransactionExDto.DictAllTransactionUnitIdExDto[unitId];

        //        PorcessOneUnit(massUpdateSaveDto, aAppSearchViewEntity, transactionUnitExDto, databaseFixtureInstance,
        //         dictMassUpdateViewFiledTranscaDbFiled);

        //    }
        //    // it is Master Unit Id , only update two uniut
        //    else if (unitIdList.Count == 2)
        //    {
        //        int rootUnid = (int)aAppTransactionExDto.RootMasterUnit.Id;

        //        if (unitIdList.IndexOf(rootUnid) != -1)
        //        {
        //            // get all roor Unite Fileds

        //            int childUnitId = unitIdList.Where(o => o != rootUnid).First();


        //            //	dictMassUpdateViewFiledTransactionFieldExDto



        //        }

        //    }
        //}

        private static void PorcessOneUnit(ValidationResult validationResult, MassUpdateSaveDto massUpdateSaveDto, AppSearchViewEntity aAppSearchViewEntity,
        AppTransactionUnitExDto transactionUnitExDto,
          DatabaseFixture databaseFixtureInstance,
          Dictionary<int, string> dictMassUpdateViewFiledTranscaDbFiled)
        {

            int transcationUnitId = aAppSearchViewEntity.UpdateBaseTranscationUnitId.Value;

            List<Dictionary<string, object>> listDictNewOneToOneFields = new List<Dictionary<string, object>>();
            List<Dictionary<string, object>> listDictUpdateOneToOneFields = new List<Dictionary<string, object>>();
            List<Dictionary<string, object>> listDictDeleteOneToOneFields = new List<Dictionary<string, object>>();

            PorcessOneUnit_PrepareTransactionDataDictionary(massUpdateSaveDto, dictMassUpdateViewFiledTranscaDbFiled, listDictNewOneToOneFields, listDictUpdateOneToOneFields, listDictDeleteOneToOneFields);

            var factory = databaseFixtureInstance.DbProviderFactory;

            using (var connection = factory.CreateConnection())
            {
                connection.ConnectionString = databaseFixtureInstance.ConnectionString;
                connection.Open();

                // DbTransaction trans = connection.BeginTransaction();

                DbTransaction trans = null;

                try
                {
                    using (trans = connection.BeginTransaction())
                    {

                        foreach (var newRowDict in listDictNewOneToOneFields)
                        {

                            AppMasterDetailFormDataSaveBL.InsertOneUnitChild(databaseFixtureInstance,transactionUnitExDto, newRowDict, trans);
                        }

                        foreach (var updateRowDict in listDictUpdateOneToOneFields)
                        {

                            AppMasterDetailFormDataSaveBL.UpdateOneUnitRecord(databaseFixtureInstance,updateRowDict, transactionUnitExDto, trans);
                        }


                        AppMasterDetailFormDataSaveBL.DeleteOneUnit(databaseFixtureInstance,trans, transactionUnitExDto, listDictDeleteOneToOneFields);



                        trans.Commit();

                    }



                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    validationResult.Items.Add(new ValidationItem(typeof(AppVersionEditionModuleExDto), "App_SaveMassUpdateResult_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

        }

        private static void PorcessOneUnit_PrepareTransactionDataDictionary(MassUpdateSaveDto massUpdateSaveDto, Dictionary<int, string> dictMassUpdateViewFiledTranscaDbFiled, List<Dictionary<string, object>> listDictNewOneToOneFields, List<Dictionary<string, object>> listDictUpdateOneToOneFields, List<Dictionary<string, object>> listDictDeleteOneToOneFields)
        {
            foreach (StaticSearchResultRowJsonDto row in massUpdateSaveDto.ModifiedSearchResult)
            {
                Dictionary<string, object> unitOneToOneFields = new Dictionary<string, object>();

                foreach (int filedKey in dictMassUpdateViewFiledTranscaDbFiled.Keys)
                {
                    string dbFileName = dictMassUpdateViewFiledTranscaDbFiled[filedKey];
                    unitOneToOneFields[dbFileName] = row[filedKey];

                }

                if (row.IsNew)
                {
                    listDictNewOneToOneFields.Add(unitOneToOneFields);
                }
                else if (row.IsChanged)
                {

                    listDictUpdateOneToOneFields.Add(unitOneToOneFields);
                }
            }
            foreach (StaticSearchResultRowJsonDto row in massUpdateSaveDto.DeletedSearchResult)
            {
                Dictionary<string, object> unitOneToOneFields = new Dictionary<string, object>();

                foreach (int filedKey in dictMassUpdateViewFiledTranscaDbFiled.Keys)
                {
                    string dbFileName = dictMassUpdateViewFiledTranscaDbFiled[filedKey];
                    unitOneToOneFields[dbFileName] = row[filedKey];

                }
                // need to filter new added row
                if (!row.IsNew)
                {
                    listDictDeleteOneToOneFields.Add(unitOneToOneFields);
                }

            }
        }

        private static void PorcessApiMasterDetailTransactionRootUnit(ValidationResult validationResult, MassUpdateSaveDto massUpdateSaveDto, AppSearchViewEntity aAppSearchViewEntity, AppTransactionExDto aAppTransactionExDto, Dictionary<int, string> dictMassUpdateViewFiledTranscaDbFiled)
        {
            int transcationUnitId = aAppSearchViewEntity.UpdateBaseTranscationUnitId.Value;

            List<Dictionary<string, object>> listDictNewOneToOneFields = new List<Dictionary<string, object>>();
            List<Dictionary<string, object>> listDictUpdateOneToOneFields = new List<Dictionary<string, object>>();
            List<Dictionary<string, object>> listDictDeleteOneToOneFields = new List<Dictionary<string, object>>();

            PorcessOneUnit_PrepareTransactionDataDictionary(massUpdateSaveDto, dictMassUpdateViewFiledTranscaDbFiled, listDictNewOneToOneFields, listDictUpdateOneToOneFields, listDictDeleteOneToOneFields);

            foreach (Dictionary<string, object> unitOneToOneFields in listDictUpdateOneToOneFields)
            {
                var forMData = AppMasterDetailApiFormDataLoadBL.GetNewFormData(aAppTransactionExDto);
                
                foreach (string key in unitOneToOneFields.Keys)
                {
                    forMData.DictOneToOneFields[key] = unitOneToOneFields[key];
                }

                var saveResult = AppMasterDetailApiFormDataSaveBL.SaveTransactionData(forMData);

                if (!saveResult.IsSuccessful)
                {
                    validationResult.Merge(saveResult.ValidationResult);
                }
                
            }

        }
    }
}