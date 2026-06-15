using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using APP.LBL;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Framework.Validation;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework.Collections;
using APP.Components.EntityConverter;
using System.Data;
using APP.Framework.Communication;

using APP.Framework;
namespace App.BL
{


    public static class AppSecurityUserContactBL
    {

        public static List<AppSecurityUserContactExDto> RetrieveOneUserAllContactEntityDto(int userId)
        {
            EntityCollection<AppSecurityUserContactEntity> folderEntities = RetrieveOneUserContactEntitytList(userId);

            var aDtoList = new List<AppSecurityUserContactExDto>();
            foreach (var folderEntity in folderEntities)
            {
                aDtoList.Add(AppSecurityUserContactConverter.ConvertEntityToExDto(folderEntity));
            }

            return aDtoList;
        }

        public static List<AppSecurityUserContactExDto> RetrieveUserContactDtoListByUserIdsAndContactType(List<int> userIds, int? contactType = null)
        {
            var aDtoList = new List<AppSecurityUserContactExDto>();

            if (userIds != null && userIds.Count > 0)
            {
                var entityList = new EntityCollection<AppSecurityUserContactEntity>();

                using (DataAccessAdapter adapater = AppTenantAdapterBL.GetTenantAdapter())
                {

                    IRelationPredicateBucket filter = new RelationPredicateBucket(AppSecurityUserContactFields.UserId == userIds.ToArray());

                    if (contactType.HasValue)
                    {
                        filter.PredicateExpression.AddWithAnd(AppSecurityUserContactFields.ContactType == contactType.Value);
                    }


                    adapater.FetchEntityCollection(entityList, filter);
                }



                foreach (var anEntity in entityList)
                {
                    aDtoList.Add(AppSecurityUserContactConverter.ConvertEntityToExDto(anEntity));
                }
            }

            return aDtoList;
        }


        public static OperationCallResult<AppSecurityUserContactExDto> SaveAllAppSecurityUserContactEntityDto(ObservableSet<AppSecurityUserContactExDto> aSet, int userId)
        {
            OperationCallResult<AppSecurityUserContactExDto> aOperationCallResult = new OperationCallResult<AppSecurityUserContactExDto>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;


            var allRoleEntity = RetrieveOneUserContactEntitytList(userId);
            Dictionary<int, AppSecurityUserContactEntity> dictDbAppSecurityUserContact = allRoleEntity.ToDictionary(o => o.ContactId, o => o);
            Dictionary<int, AppSecurityUserContactExDto> dictDbAppSecurityUserContactExdto = aSet.Where(o => !o.IsNew).ToDictionary(o => int.Parse(o.Id.ToString()), o => o);

            List<int> groupIdDbms = dictDbAppSecurityUserContact.Keys.ToList();

            List<int> groupIdUi = dictDbAppSecurityUserContactExdto.Keys.ToList();


            //Delete Id
            List<int> deletAppSecurityUserContactIDs = groupIdDbms.Except(groupIdUi).ToList();


            //new Entity
            foreach (var dto in aSet)
            {
                if (dto.IsNew)
                {

                    AppSecurityUserContactEntity aParentAppSecurityUserContactEntity = new AppSecurityUserContactEntity();

                    AppSecurityUserContactConverter.CopyDtoToEntity(aParentAppSecurityUserContactEntity, dto);

                    allRoleEntity.Add(aParentAppSecurityUserContactEntity);

                }
                else // update 
                {
                    if (dictDbAppSecurityUserContact.ContainsKey((int)dto.Id))
                    {
                        var entity = dictDbAppSecurityUserContact[(int)dto.Id];

                        AppSecurityUserContactConverter.CopyDtoToEntity(entity, dto);

                    }

                }


            }


            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {

                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntityCollection(allRoleEntity);


                    adapter.DeleteEntitiesDirectly(typeof(AppSecurityUserContactEntity), new RelationPredicateBucket(AppSecurityUserContactFields.ContactId == deletAppSecurityUserContactIDs));
                    //}

                    adapter.Commit();
                    validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserContactExDto), "App_SecurityUserContact_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                }


                catch (ORMQueryExecutionException ex)
                {

                    adapter.Rollback();
                    validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserContactExDto), "App_SecurityUserContact_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));


                }


            }



            // if no any errors, refresh all entity from DBMS server
            if (!validationResult.HasErrors)
            {
                validationResult.Items.Clear();
                validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserContactExDto), "App_AppSecurityUserContactEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                aOperationCallResult.ObjectList = RetrieveOneUserAllContactEntityDto(userId);
            }

            return aOperationCallResult;
        }



        internal static EntityCollection<AppSecurityUserContactEntity> RetrieveOneUserContactEntitytList(int userId)
        {
            var timeSheetEntities = new EntityCollection<AppSecurityUserContactEntity>();

            using (DataAccessAdapter adapater = AppTenantAdapterBL.GetTenantAdapter())
            {

                IRelationPredicateBucket filter = new RelationPredicateBucket(AppSecurityUserContactFields.UserId == userId);


                adapater.FetchEntityCollection(timeSheetEntities, filter);
            }

            return timeSheetEntities;
        }




    }


}
