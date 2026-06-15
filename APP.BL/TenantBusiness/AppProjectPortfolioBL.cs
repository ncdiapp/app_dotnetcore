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
using System;

using APP.Framework;
namespace App.BL
{
    public static class AppProjectPortfolioBL
    {
        public static readonly string App_ProjectPortfolioEntity_Save_OK = "App_ProjectPortfolioEntity_Save_OK";
        public static readonly string App_ProjectPortfolioEntity_Save_Failed = "App_ProjectPortfolioEntity_Save_Failed";
        public static readonly string App_ProjectPortfolioEntityUILayout_Save_OK = "App_ProjectPortfolioEntityUILayout_Save_OK";
        public static readonly string App_ProjectPortfolioEntityUILayout_Save_Failed = "App_ProjectPortfolioEntityUILayout_Save_Failed";
        public static readonly string App_ProjectPortfolioEntity_Delete_Ok = "App_ProjectPortfolioEntity_Delete_Ok";
        public static readonly string App_ProjectPortfolioEntity_Delete_Failed = "App_ProjectPortfolioEntity_Delete_Failed";

        public static ObservableSet<AppProjectPortfolioDto> RetrieveAllAppProjectPortfolioDto()
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppProjectPortfolioEntity> list = new EntityCollection<AppProjectPortfolioEntity>();

                SortExpression expression = new SortExpression(AppProjectPortfolioFields.PortfilioName | SortOperator.Ascending);
                adapter.FetchEntityCollection(list, null, 0, expression);

                var aDtoList = new ObservableSet<AppProjectPortfolioDto>();

                foreach (var o in list)
                {
                    aDtoList.Add(AppProjectPortfolioConverter.ConvertEntityToDto(o));
                }

                return aDtoList;
            }
        }

        public static AppProjectPortfolioExDto RetrieveOneAppProjectPortfolioExDto(object PortfolioId)
        {
            AppProjectPortfolioEntity aAppProjectPortfolioEntity = RetrieveOneAppProjectPortfolioEntity(PortfolioId);
            AppProjectPortfolioExDto aProjectPortfolioDto = AppProjectPortfolioConverter.ConvertEntityToExDto(aAppProjectPortfolioEntity);


            foreach (var o in aAppProjectPortfolioEntity.AppProjectPortfolioBoard)
            {
                AppProjectPortfolioBoardExDto aAppProjectPortfolioKeyExDto = AppProjectPortfolioBoardConverter.ConvertEntityToExDto(o);

             
                aProjectPortfolioDto.AppProjectPortfolioBoardList.Add(aAppProjectPortfolioKeyExDto);
            }


            return aProjectPortfolioDto;
        }


        public static AppProjectPortfolioEntity RetrieveOneAppProjectPortfolioEntity(object PortfolioId)
        {
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppProjectPortfolioEntity ProjectPortfolioEntity = new AppProjectPortfolioEntity(int.Parse(PortfolioId.ToString()));

                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppProjectPortfolioEntity);




                rootPath.Add(AppProjectPortfolioEntity.PrefetchPathAppProjectPortfolioBoard);

                adpater.FetchEntity(ProjectPortfolioEntity, rootPath);
                return ProjectPortfolioEntity;
            }
        }

        public static OperationCallResult<AppProjectPortfolioExDto> SaveAppProjectPortfolioExDto(AppProjectPortfolioExDto aAppProjectPortfolioExDto)
        {
            OperationCallResult<AppProjectPortfolioExDto> aOperationCallResult = new OperationCallResult<AppProjectPortfolioExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppProjectPortfolioEntity aAppProjectPortfolioEntity;

            // prepare Data
            if (aAppProjectPortfolioExDto.IsNew)
            {
                aAppProjectPortfolioEntity = new AppProjectPortfolioEntity();
                AppProjectPortfolioConverter.CopyDtoToEntity(aAppProjectPortfolioEntity, aAppProjectPortfolioExDto);



                foreach (var templatefieldDto in aAppProjectPortfolioExDto.AppProjectPortfolioBoardList)
                {
                    AppProjectPortfolioBoardEntity aAppProjectPortfolioBoardEntity = new AppProjectPortfolioBoardEntity();
                    AppProjectPortfolioBoardConverter.CopyDtoToEntity(aAppProjectPortfolioBoardEntity, templatefieldDto);
                    aAppProjectPortfolioEntity.AppProjectPortfolioBoard.Add(aAppProjectPortfolioBoardEntity);
                }
                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    try
                    {
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                        adapter.SaveEntity(aAppProjectPortfolioEntity);
                        adapter.Commit();
						//PortfolioId
						aAppProjectPortfolioExDto.Id = aAppProjectPortfolioEntity.PortfolioId;
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppProjectPortfolioEntity), "App_ProjectPortfolioEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                    }

               
                    // Database FK Exeption ........
                    catch (ORMQueryExecutionException ex)
                    {
                        adapter.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppProjectPortfolioEntity), "App_ProjectPortfolioEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    }
                }
            }

            else if (aAppProjectPortfolioExDto.IsRelatedEntitiesModified())
            {
                aValidationResult.Merge(ProcessDirtyAppProjectPortfolioExDto(aAppProjectPortfolioExDto));
            }

            // if no any errors, refresh all entity from DBMS server
            if (!aValidationResult.HasErrors)
            {
                aOperationCallResult.Object = RetrieveOneAppProjectPortfolioExDto(aAppProjectPortfolioExDto.Id);
            }

            return aOperationCallResult;
        }


        public static OperationCallResult<object> DeleteOneAppProjectPortfolio(object PortfolioId)
        {
            OperationCallResult<object> aOperationCallResult = new OperationCallResult<object>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;


            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.DeleteEntitiesDirectly(typeof(AppProjectPortfolioBoardEntity), new RelationPredicateBucket(AppProjectPortfolioBoardFields.PortfolioId == PortfolioId));
                    adapter.DeleteEntitiesDirectly(typeof(AppProjectPortfolioEntity), new RelationPredicateBucket(AppProjectPortfolioFields.PortfolioId == PortfolioId));
                    string message = StringLocalizer.Localize(App_ProjectPortfolioEntity_Delete_Ok, "ProjectPortfolio Delete Successful");
                    aValidationResult.Items.Add(new ValidationItem(null, App_ProjectPortfolioEntity_Delete_Ok, ValidationItemType.Message, message));

                    adapter.Commit();
                }

                // FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    string message = StringLocalizer.Localize(App_ProjectPortfolioEntity_Delete_Failed, "ProjectPortfolio Delete Failed" + ex.ToString());
                    aValidationResult.Items.Add(new ValidationItem(null, App_ProjectPortfolioEntity_Delete_Failed, ValidationItemType.Error, message));
                }
            }

            // if no any errors
            if (!aOperationCallResult.ValidationResult.HasErrors)
            {
                aOperationCallResult.Object = PortfolioId;
            }

            return aOperationCallResult;
        }

        private static ValidationResult ProcessDirtyAppProjectPortfolioExDto(AppProjectPortfolioExDto aAppProjectPortfolioExDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            int[] dirtyFieldIds = aAppProjectPortfolioExDto.AppProjectPortfolioBoardList.FindModifiedItems().Select(o => o.Id).Cast<int>().ToArray();

            AppProjectPortfolioEntity aAppProjectPortfolioEntity = RetrieveOneAppProjectPortfolioEntity(aAppProjectPortfolioExDto.Id);

			//BoardItmeId
			Dictionary<int, AppProjectPortfolioBoardEntity> dictAppProjectPortfolioBoardFromDbms = aAppProjectPortfolioEntity.AppProjectPortfolioBoard.ToDictionary(o => o.BoardItmeId, o => o);

            AppProjectPortfolioConverter.CopyDtoToEntity(aAppProjectPortfolioEntity, aAppProjectPortfolioExDto);
            //  aAppProjectPortfolioEntity.ModifiedDate = System.DateTime.UtcNow;
            //  aAppProjectPortfolioEntity.ModifiedBy = (int)ServerContext.Instance.CurrentUid;

            //------- check  AppProjectPortfolioBoard

            // new Items
            foreach (var aChildDto in aAppProjectPortfolioExDto.AppProjectPortfolioBoardList.FindNewItems())
            {
                AppProjectPortfolioBoardEntity aNewChildEntity = new AppProjectPortfolioBoardEntity();
                AppProjectPortfolioBoardConverter.CopyDtoToEntity(aNewChildEntity, aChildDto);
                aAppProjectPortfolioEntity.AppProjectPortfolioBoard.Add(aNewChildEntity);
            }

            // Dirty items
            foreach (var modifyitem in aAppProjectPortfolioExDto.AppProjectPortfolioBoardList.FindModifiedItems())
            {
                int dtoKey = int.Parse(modifyitem.Id.ToString());
                if (dictAppProjectPortfolioBoardFromDbms.ContainsKey(dtoKey))
                {
                    AppProjectPortfolioBoardConverter.CopyDtoToEntity(dictAppProjectPortfolioBoardFromDbms[dtoKey], modifyitem);
                }
            }

            // deletedIDs
            int[] deleteFieldIDs = aAppProjectPortfolioExDto.AppProjectPortfolioBoardList.FindDeletedItemIds().Cast<int>().ToArray();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppProjectPortfolioEntity);

                    // Need to delete AppProjectPortfolioBoardFields
                    if (deleteFieldIDs.Count() > 0)
                    {
                        adapter.DeleteEntitiesDirectly(typeof(AppProjectPortfolioBoardEntity), new RelationPredicateBucket(AppProjectPortfolioBoardFields.BoardItmeId == deleteFieldIDs));
                    }

                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppProjectPortfolioEntity), "App_ProjectPortfolioEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }

                     
                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppProjectPortfolioEntity), "App_ProjectPortfolioEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }


    }
}