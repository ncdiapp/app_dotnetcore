using System.Collections.Generic;
using System.Linq;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Components.EntityConverter;
using APP.Framework.Communication;
using APP.Framework.Validation;
using APP.LBL;
using System;
using APP.Framework.Collections;
using System.Data;
////using APP.Persistence.Common;
#if NETFRAMEWORK
using Microsoft.Exchange.WebServices.Data;
// TODO-PHASE4: Replace with .NET 10 equivalent
#endif

using APP.Framework;
namespace App.BL
{
    public static class AppBusinessPartnerBL
    {
        public static AppBusinessPartnerEntity RetrieveOneAppBusinessPartnerEntity(object businessPartnerID)
        {
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppBusinessPartnerEntity companyEntity = new AppBusinessPartnerEntity(int.Parse(businessPartnerID.ToString()));
                adpater.FetchEntity(companyEntity);
                return companyEntity;
            }
        }

        public static AppBusinessPartnerExDto RetrieveOneAppBusinessPartnerExDto(int? businessPartnerID)
        {
            AppBusinessPartnerEntity companyEntity = RetrieveOneAppBusinessPartnerEntity(businessPartnerID);
            AppBusinessPartnerExDto aAppBusinessPartnerExDto = AppBusinessPartnerConverter.ConvertEntityToExDto(companyEntity);

            aAppBusinessPartnerExDto.BusinessPartnerUserList = RetrieveCurrentCompanyOneBusinessPartnerAllInviteUserDtoList(businessPartnerID);
            PrepareMappingToBusinessEntityLookUpItemDataList(aAppBusinessPartnerExDto);

            return aAppBusinessPartnerExDto;
        }

        public static AppBusinessPartnerExDto RetrieveCurrentUserAppBusinessPartnerExDto()
        {
            int? companyId = ControlTypeValueConverter.ConvertValueToInt(ServerContext.Instance.CurrentCompanyId);

            if (companyId.HasValue)
            {
                var partnerEntity = AppCacheManagerBL.GetCurrentUserWorkingCompanyBinessPartner(companyId.Value, AppSecurityUserBL.CurrentUserId);
                if (partnerEntity != null)
                {
                    return RetrieveOneAppBusinessPartnerExDto(partnerEntity.AppBusinessPartnerId);
                }
            }

            return null;
        }

        public static AppBusinessPartnerDto RetrieveOneAppBusinessPartnerDto(int? businessPartnerID)
        {
            AppBusinessPartnerEntity companyEntity = RetrieveOneAppBusinessPartnerEntity(businessPartnerID);
            AppBusinessPartnerDto aAppBusinessPartnerDto = AppBusinessPartnerConverter.ConvertEntityToDto(companyEntity);


            return aAppBusinessPartnerDto;
        }


        public static List<AppBusinessPartnerDto> RetrieveCompanyPartnerDtoList(int? compnayId, int? PartnerType)
        {
            var folderEntities = new EntityCollection<AppBusinessPartnerEntity>();

            if (!compnayId.HasValue)
            {
                compnayId = ControlTypeValueConverter.ConvertValueToInt(ServerContext.Instance.CurrentCompanyId);
            }

            using (DataAccessAdapter adapater = AppTenantAdapterBL.GetTenantAdapter())
            {
                IRelationPredicateBucket filter = new RelationPredicateBucket();

                if (PartnerType.HasValue)
                {
                    filter.PredicateExpression.AddWithAnd(AppBusinessPartnerFields.PartnerType == PartnerType.Value);
                }

                adapater.FetchEntityCollection(folderEntities, filter);
            }

            var aDtoList = new List<AppBusinessPartnerDto>();
            foreach (var folderEntity in folderEntities)
            {
                aDtoList.Add(AppBusinessPartnerConverter.ConvertEntityToDto(folderEntity));
            }

            return aDtoList;
        }

        public static List<AppBusinessPartnerDto> RetrieveCurrentCompanyPartnerDtoList(int? partnerType = null)
        {
            int? compnayId = ControlTypeValueConverter.ConvertValueToInt(ServerContext.Instance.CurrentCompanyId);
            return RetrieveCompanyPartnerDtoList(compnayId, partnerType);


        }

        public static List<AppSecurityUserDto> RetrieveCurrentCompanyOneBusinessPartnerAllInviteUserDtoList(int? parternerId)
        {
            if (!parternerId.HasValue)
            {
                return new List<AppSecurityUserDto>();
            }

            List<int> userIds = AppComOrgBL.GetInviteUserIdsByBusinessPartnerIds(new List<int> { parternerId.Value });
            return AppComOrgBL.BuildUserDtoListFromMasterUserIds(userIds);
        }




        public static OperationCallResult<AppBusinessPartnerExDto> SaveOneAppBusinessPartnerExDto(AppBusinessPartnerExDto aAppBusinessPartnerExDto)
        {
            OperationCallResult<AppBusinessPartnerExDto> aOperationCallResult = new OperationCallResult<AppBusinessPartnerExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            // prepare Data
            if (aAppBusinessPartnerExDto.IsNew)
            {
                if (aAppBusinessPartnerExDto.AssociatedEsiteId.HasValue && !string.IsNullOrWhiteSpace(aAppBusinessPartnerExDto.DefaultUserEmail))
                {
                    CreateNewPartnerAndDefaultUserByEsiteSetting(aAppBusinessPartnerExDto, aValidationResult);
                }
                else
                {
                    aValidationResult.Merge(SaveNewPartner(aAppBusinessPartnerExDto));
                }
            }

            else if (aAppBusinessPartnerExDto.IsRelatedEntitiesModified())
            {
                aValidationResult.Merge(ProcessDirtyAppBusinessPartnerExDto(aAppBusinessPartnerExDto));
            }


            // if no any errors, refresh all entity from DBMS server
            if (!aValidationResult.HasErrors)
            {
                aOperationCallResult.Object = RetrieveOneAppBusinessPartnerExDto(aAppBusinessPartnerExDto.Id as int?);
            }

            return aOperationCallResult;


        }

        internal static AppBusinessPartnerDto GetCurrentCompanyPartnerDtoByUserId(int userId)
        {
            int? companyId = ControlTypeValueConverter.ConvertValueToInt(ServerContext.Instance.CurrentCompanyId);

            if (companyId.HasValue)
            {
                Dictionary<int, int> dictUserIdAndPartnerId = AppCacheManagerBL.GetOneCompanyUserIdAndPartnerIdDictionary(companyId.Value);

                if (dictUserIdAndPartnerId.ContainsKey(userId))
                {
                    int partnerId = dictUserIdAndPartnerId[userId];

                    return AppBusinessPartnerBL.RetrieveOneAppBusinessPartnerDto(partnerId);

                }
            }



            return null;
        }


        private static void CreateNewPartnerAndDefaultUserByEsiteSetting(AppBusinessPartnerExDto aAppBusinessPartnerExDto, ValidationResult aValidationResult)
        {
            AppSecurityUserExDto appSecurityUserExDto = new AppSecurityUserExDto();
            appSecurityUserExDto.UserName = aAppBusinessPartnerExDto.DefaultUserFirstName + " " + aAppBusinessPartnerExDto.DefaultUserLastName;
            appSecurityUserExDto.LoginName = aAppBusinessPartnerExDto.DefaultUserEmail;
            appSecurityUserExDto.Email = aAppBusinessPartnerExDto.DefaultUserEmail;
            appSecurityUserExDto.Password = appSecurityUserExDto.ConfirmPassword = "nopassword";
            appSecurityUserExDto.IsQuickRegistration = true;
            appSecurityUserExDto.IsNeedActivePartnerUserByEmail = false;
            appSecurityUserExDto.IsActive = false;
            appSecurityUserExDto.NewUserPartnerType = appSecurityUserExDto.DomainId = aAppBusinessPartnerExDto.PartnerType.Value;
            appSecurityUserExDto.RegisterFromEsiteId = aAppBusinessPartnerExDto.AssociatedEsiteId;

            OperationCallResult<AppSecurityUserExDto> regResult = AppEsiteBL.EStoreUserRegistration(appSecurityUserExDto);

            if (regResult.Object != null)
            {
                aAppBusinessPartnerExDto.Id = regResult.Object.NewBusinessAccountId;
            }

            aValidationResult.Merge(regResult.ValidationResult);
        }

        internal static ValidationResult SaveNewPartner(AppBusinessPartnerExDto aAppBusinessPartnerExDto)
        {
            ValidationResult aValidationResult = new ValidationResult();
            AppBusinessPartnerEntity aAppBusinessPartnerEntity = new AppBusinessPartnerEntity();
            AppBusinessPartnerConverter.CopyDtoToEntity(aAppBusinessPartnerEntity, aAppBusinessPartnerExDto);


            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {

                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppBusinessPartnerEntity);
                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppBusinessPartnerExDto), "App_SecurityGroupEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                    aAppBusinessPartnerExDto.Id = aAppBusinessPartnerEntity.AppBusinessPartnerId;

                }


                catch (ORMQueryExecutionException ex)
                {

                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppBusinessPartnerExDto), "App_SecurityGroupEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            if (aAppBusinessPartnerEntity.AppBusinessPartnerId != 0)
            {
                CreateNewPartnerBuiltInRole(aAppBusinessPartnerExDto, aValidationResult);
            }



            if (!aValidationResult.HasErrors)
            {
                //AppSecurityGroupExDto newGroupDto = new AppSecurityGroupExDto();
                //AppSecurityGroupBL.SaveAppSecurityGroupExDto(AppSecurityGroupExDto )


            }

            return aValidationResult;
        }


        public static OperationCallResult<AppBusinessPartnerExDto> RemoveOneCurrentCompanyBusinessParterneUser(int? parternerId, int? userId)
        {
            // To Do

            OperationCallResult<AppBusinessPartnerExDto> aOperationCallResult = new OperationCallResult<AppBusinessPartnerExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;



            return aOperationCallResult;
        }

        private static void CreateNewPartnerBuiltInRole(AppBusinessPartnerExDto aAppBusinessPartnerExDto, ValidationResult aValidationResult)
        {
            AppSecurityGroupEntity securityGroupEntity = new AppSecurityGroupEntity()
            {
                GroupName = aAppBusinessPartnerExDto.Code,
                Description = aAppBusinessPartnerExDto.ShortName,
                IsBuiltIn = true,
                IsSharedbyMutipleCompany = false,
                GroupUsage = (int)EmAppGroupUsage.SecurityGroup,
                RoleUserTypeId = aAppBusinessPartnerExDto.PartnerType,
                BusinessPartnerId = (int)aAppBusinessPartnerExDto.Id,
            };

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(securityGroupEntity);
                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppBusinessPartnerExDto), "App_SecurityGroupEntity_Save_OK", ValidationItemType.Message, "Saved Role Successfully"));
                }
                catch (ORMQueryExecutionException ex)
                {

                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppBusinessPartnerExDto), "App_SecurityGroupEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }
        }

        private static ValidationResult ProcessDirtyAppBusinessPartnerExDto(AppBusinessPartnerExDto AppBusinessPartnerExDto)
        {
            ValidationResult aValidationResult = new ValidationResult();


            AppBusinessPartnerEntity AppBusinessPartnerEntity = RetrieveOneAppBusinessPartnerEntity(AppBusinessPartnerExDto.Id);
            AppBusinessPartnerConverter.CopyDtoToEntity(AppBusinessPartnerEntity, AppBusinessPartnerExDto);




            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {

                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(AppBusinessPartnerEntity);

                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppProjectTeamMemberExDto), "App_ProjectTeamMember_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                }


                catch (ORMQueryExecutionException ex)
                {

                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppProjectTeamMemberExDto), "App_ProjectTeamMember_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));


                }


            }

            return aValidationResult;
        }

        private static void PrepareMappingToBusinessEntityLookUpItemDataList(AppBusinessPartnerExDto aAppBusinessPartnerExDto)
        {
            int? mappingToBusinessEntityId = null;

            if (aAppBusinessPartnerExDto.PartnerType.HasValue)
            {
                if (aAppBusinessPartnerExDto.PartnerType.Value == (int)EmAppUserType.Customer)
                {
                    mappingToBusinessEntityId = AppTenantSettingBL.GetIntValue(EmTenantSettings.CustomerEntity);
                }
                else if (aAppBusinessPartnerExDto.PartnerType.Value == (int)EmAppUserType.Supplier)
                {
                    mappingToBusinessEntityId = AppTenantSettingBL.GetIntValue(EmTenantSettings.SupplierEntity);
                }
                else if (aAppBusinessPartnerExDto.PartnerType.Value == (int)EmAppUserType.ClientAgent)
                {
                    mappingToBusinessEntityId = AppTenantSettingBL.GetIntValue(EmTenantSettings.CustomerAgentEntity);
                }
                else if (aAppBusinessPartnerExDto.PartnerType.Value == (int)EmAppUserType.SupplierAgent)
                {
                    mappingToBusinessEntityId = AppTenantSettingBL.GetIntValue(EmTenantSettings.SupplierAgentEntity);
                }
            }

            if (mappingToBusinessEntityId.HasValue)
            {
                aAppBusinessPartnerExDto.ExternalBusinessEntityLookUpItemList = AppEntityInfoBL.GetLookupItemList(mappingToBusinessEntityId.Value, string.Empty);
                aAppBusinessPartnerExDto.ExternalBusinessEntityLookUpItemList.ForAll(o => o.Id = o.Id.ToString());
            }
        }


    }
}