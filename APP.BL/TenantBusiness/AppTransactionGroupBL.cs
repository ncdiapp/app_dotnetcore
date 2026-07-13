using APP.LBL;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Framework.Communication;
using APP.Framework.Validation;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;
using APP.Components.Dto;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System;

using APP.Framework;
namespace App.BL
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks></remarks>
    public static class AppTransactionGroupBL
    {

		

		

        public static List<AppTransactionGroupDto> RetrieveAllAppTransactionGroupDto(int? applicationId)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppTransactionGroupEntity> list = new EntityCollection<AppTransactionGroupEntity>();
                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppTransactionGroupEntity);
                rootPath.Add(AppTransactionGroupEntity.PrefetchPathAppTransactionGroupItem);
      
                adapter.FetchEntityCollection(list, null,rootPath);

           
                var aDtoList = new List<AppTransactionGroupDto>();

                foreach (var o in list)
                {
                    AppTransactionGroupDto group = AppTransactionGroupConverter.ConvertEntityToDto(o);
                    aDtoList.Add(group);
      
                }

                if (applicationId.HasValue)
                {
                    aDtoList = aDtoList.Where(o => o.SaasApplicationId.HasValue && o.SaasApplicationId.Value == applicationId.Value).ToList();
                }

                return aDtoList;

            }

        }        

  
		public static AppTransactionGroupExDto RetrieveOneAppTransactionGroupExDto(object groupId)
        {
            AppTransactionGroupEntity aAppTransactionGroupEntity = RetrieveOneAppTransactionGroupEntity(groupId);

            AppTransactionGroupExDto aAppTransactionGroupExDto = AppTransactionGroupConverter.ConvertEntityToExDto(aAppTransactionGroupEntity);

            foreach (AppTransactionGroupItemEntity o in aAppTransactionGroupEntity.AppTransactionGroupItem.OrderBy(o => o.TransactionLayoutOrder))
            {
                AppTransactionGroupItemExDto aAppTransactionGroupItemExDto = AppTransactionGroupItemConverter.ConvertEntityToExDto(o);
                aAppTransactionGroupExDto.AppTransactionGroupItemList.Add(aAppTransactionGroupItemExDto);

                if (o.AppTransactionItem != null)
                {
                    aAppTransactionGroupItemExDto.ForeignAppTransactionItemExDto = AppTransactionItemConverter.ConvertEntityToExDto(o.AppTransactionItem);
                }                
            }

            return aAppTransactionGroupExDto;


        }

        /// <summary>
        /// Transaction IDs that are TemplateHeader on any Data Model Template Search link target.
        /// Used when AppTransactionGroupItem.IsGroupSharedHeader was not set at DW import time.
        /// </summary>
        public static HashSet<int> ResolveTemplateHeaderTransactionIds(IEnumerable<int> transactionIds)
        {
            var result = new HashSet<int>();
            var idList = (transactionIds ?? Enumerable.Empty<int>()).Distinct().Where(id => id > 0).ToList();
            if (idList.Count == 0)
                return result;

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppFormLinkTargetEntity> list = new EntityCollection<AppFormLinkTargetEntity>();
                adapter.FetchEntityCollection(
                    list,
                    new RelationPredicateBucket(AppFormLinkTargetFields.LinkTargetTransactionId == idList));

                int headerType = (int)EmAppTransactionTemplateItemType.TemplateHeader;
                foreach (var entity in list)
                {
                    if (!entity.LinkTargetTransactionId.HasValue)
                        continue;
                    string other = entity.OtherSettings;
                    if (string.IsNullOrWhiteSpace(other))
                        continue;
                    // OtherSettings JSON: "TemplateItemType":2 (TemplateHeader)
                    if (other.IndexOf("\"TemplateItemType\":" + headerType, StringComparison.Ordinal) >= 0
                        || other.IndexOf("\"TemplateItemType\": " + headerType, StringComparison.Ordinal) >= 0)
                    {
                        result.Add(entity.LinkTargetTransactionId.Value);
                    }
                }
            }

            return result;
        }

       

        public static OperationCallResult<AppTransactionGroupExDto> SaveAppTransactionGroupExDto(AppTransactionGroupExDto aAppTransactionGroupExDto)
        {
            OperationCallResult<AppTransactionGroupExDto> aOperationCallResult = new OperationCallResult<AppTransactionGroupExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppTransactionGroupEntity aAppTransactionGroupEntity;

         
                // prepare Data
                if (aAppTransactionGroupExDto.IsNew)
                {
                    aAppTransactionGroupEntity = new AppTransactionGroupEntity();
                    AppTransactionGroupConverter.CopyDtoToEntity(aAppTransactionGroupEntity, aAppTransactionGroupExDto);
                    foreach (var securityUserDto in aAppTransactionGroupExDto.AppTransactionGroupItemList)
                    {

                        AppTransactionGroupItemEntity aAppTransactionGroupItemEntity = new AppTransactionGroupItemEntity();
                        AppTransactionGroupItemConverter.CopyDtoToEntity(aAppTransactionGroupItemEntity, securityUserDto);
                        aAppTransactionGroupEntity.AppTransactionGroupItem.Add(aAppTransactionGroupItemEntity);
                    }

                  
                    using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                    {

                        try
                        {
                            adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                            adapter.SaveEntity(aAppTransactionGroupEntity);


                            adapter.Commit();


                            aAppTransactionGroupExDto.Id = aAppTransactionGroupEntity.TransactionGroupId;
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionGroupExDto), "App_SecurityGroupEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                        }

                    
                        catch (ORMQueryExecutionException ex)
                        {

                            adapter.Rollback();
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionGroupExDto), "App_SecurityGroupEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                        }
                    }
                }

                else if (aAppTransactionGroupExDto.IsRelatedEntitiesModified())
                {
                    aValidationResult.Merge(ProcessDirtyAppTransactionGroupExDto(aAppTransactionGroupExDto));
                }
           

            // if no any errors, refresh all entity from DBMS server
            if (!aValidationResult.HasErrors)
            {
                aOperationCallResult.Object = RetrieveOneAppTransactionGroupExDto(aAppTransactionGroupExDto.Id);
            }

            return aOperationCallResult;


        }

        private static AppTransactionGroupEntity RetrieveOneAppTransactionGroupEntity(object groupId)
        {
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppTransactionGroupEntity userGroupEntity = new AppTransactionGroupEntity(int.Parse(groupId.ToString()));

                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppTransactionGroupEntity);

                IPrefetchPathElement2 groupItemPathElement = rootPath.Add(AppTransactionGroupEntity.PrefetchPathAppTransactionGroupItem);
                groupItemPathElement.SubPath.Add(AppTransactionGroupItemEntity.PrefetchPathAppTransactionItem);

                adpater.FetchEntity(userGroupEntity, rootPath);
                return userGroupEntity;
            }
        }

        private static ValidationResult ProcessDirtyAppTransactionGroupExDto(AppTransactionGroupExDto aAppTransactionGroupExDto)
        {
            ValidationResult aValidationResult = new ValidationResult();


             int[] dirtyAppTransactionGroupItemIds = aAppTransactionGroupExDto.AppTransactionGroupItemList.FindModifiedItems().Select(o => o.Id).Cast<int>().ToArray();


            AppTransactionGroupEntity aAppTransactionGroupEntity = RetrieveOneAppTransactionGroupEntity(aAppTransactionGroupExDto.Id);

            Dictionary<int, AppTransactionGroupItemEntity> dictAppTransactionGroupItemFromDbms = aAppTransactionGroupEntity.AppTransactionGroupItem.ToDictionary(o => o.GroupItemId , o => o);
            AppTransactionGroupConverter.CopyDtoToEntity(aAppTransactionGroupEntity, aAppTransactionGroupExDto);


            //------- check  Group  member

            // new Items
            foreach (AppTransactionGroupItemExDto aChildDto in aAppTransactionGroupExDto.AppTransactionGroupItemList.FindNewItems())
            {

                AppTransactionGroupItemEntity aNewChildEntity = new AppTransactionGroupItemEntity();
                AppTransactionGroupItemConverter.CopyDtoToEntity(aNewChildEntity, aChildDto);
                aAppTransactionGroupEntity.AppTransactionGroupItem.Add(aNewChildEntity);



            }


            // Dirty items, only the update item remove from dbms, no need to update that itmes
            foreach (var modifyitem in aAppTransactionGroupExDto.AppTransactionGroupItemList.FindModifiedItems())
            {
                if (!modifyitem.IsNew)
                {

                    int dtoKey = int.Parse(modifyitem.Id.ToString());
                    if (dictAppTransactionGroupItemFromDbms.ContainsKey(dtoKey))
                    {

                        AppTransactionGroupItemConverter.CopyDtoToEntity(dictAppTransactionGroupItemFromDbms[dtoKey], modifyitem);


                    }

                }
            }


            // deletedIDs      
            int[] deletAppTransactionGroupItemIDs = aAppTransactionGroupExDto.AppTransactionGroupItemList.FindDeletedItemIds().Cast<int>().ToArray();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {

                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppTransactionGroupEntity);

                  

                    if (deletAppTransactionGroupItemIDs.Count() > 0)
                    {

                        adapter.DeleteEntitiesDirectly(typeof(AppTransactionGroupItemEntity), new RelationPredicateBucket(AppTransactionGroupItemFields.GroupItemId == deletAppTransactionGroupItemIDs));
                    }

                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionGroupExDto), "App_SecurityGroupEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                }

            
                catch (ORMQueryExecutionException ex)
                {

                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionGroupExDto), "App_SecurityGroupEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));


                }


            }

            return aValidationResult;
        }



        public static OperationCallResult<bool> DeleteOneAppTransactionGroup(object groupId)
        {
            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
            var aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;
                      
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {               
             
                try
                {
                    adpater.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adpater.DeleteEntitiesDirectly(typeof(AppTransactionGroupItemEntity), new RelationPredicateBucket(AppTransactionGroupItemFields.TransactionGroupId == groupId));
                    adpater.DeleteEntitiesDirectly(typeof(AppTransactionGroupEntity), new RelationPredicateBucket(AppTransactionGroupFields.TransactionGroupId == groupId));

                    adpater.Commit();

                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionGroupEntity), "App_AppTransactionGroup_Delete_OK", ValidationItemType.Message, "Delete Successful"));
                }


                catch (Exception ex)
                {
                    adpater.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionGroupEntity), "App_AppTransactionGroup_Delete_Error", ValidationItemType.Error, ex.ToString()));

                }
            }

            return aOperationCallResult;
        }


        //public static void SetupTransactionGroup(int? transGroupId, AppTransactionExDto appMainTransactionExDto)
        //{
        //	AppTransactionGroupEntity aAppTransactionGroupEntity = AppTransactionGroupBL.RetrieveOneAppTransactionGroupEntity(transGroupId);

        //	List<int> transactionHeaderIds = aAppTransactionGroupEntity.AppTransactionGroupItem.Where(o => o.IsGroupSharedHeader.HasValue && o.IsGroupSharedHeader.Value).Select(o => o.TransactionId.Value).ToList();
        //	List<int> transactionCrossHeadIds = aAppTransactionGroupEntity.AppTransactionGroupItem.Where(o => o.IsCrossGroupSharedHeader.HasValue && o.IsCrossGroupSharedHeader.Value).Select(o => o.TransactionId.Value).ToList();

        //	List<AppTransactionExDto> listTransactionHeaderDto = new List<AppTransactionExDto>();
        //	List<AppTransactionExDto> listTransactionCrossHeaderDto = new List<AppTransactionExDto>();

        //	// same root key value
        //	foreach (var tranid in transactionHeaderIds)
        //	{
        //		var aTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(tranid);
        //		listTransactionHeaderDto.Add(aTransactionExDto);

        //	}
        //	// for different root key value !!
        //	foreach (var tranCrossid in listTransactionCrossHeaderDto)
        //	{
        //		var aTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(tranCrossid);
        //		listTransactionCrossHeaderDto.Add(aTransactionExDto);

        //	}

        //	appMainTransactionExDto.TransactionHeader = listTransactionHeaderDto;
        //	appMainTransactionExDto.TransactionCrossHeader = listTransactionCrossHeaderDto;
        //}


        //      public static List<AppTransactionGroupExDto> RetrieveMutipleAppTransactionGroupExDto(List<int> groupId)
        //{

        //	List<AppTransactionGroupExDto> toReturnList = new List<AppTransactionGroupExDto>();


        //	toReturnList.Add(RetrieveOneAppTransactionGroupExDto(groupId));


        //	return toReturnList;


        //}
    }
}
