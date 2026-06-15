using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Validation;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System;
using APP.LBL;
using System.Data.SqlClient;

using APP.Framework;
namespace App.BL
{
    public static class AppSecuritySysObjGroupUserBL
    {
        public static ObservableSet<AppSecuritySysObjGroupUserExDto> RetrieveOrganizationPrivilegeDtoByType(int objType)
        {
            ObservableSet<AppSecuritySysObjGroupUserExDto> aSet = new ObservableSet<AppSecuritySysObjGroupUserExDto>();
            EntityCollection<AppSecuritySysObjGroupUserEntity> list = RetrieveOrganizationPrivilegeEntitiesByType(objType);
            foreach (var o in list)
            {
                AppSecuritySysObjGroupUserExDto aDto = AppSecuritySysObjGroupUserConverter.ConvertEntityToExDto(o);
                aSet.Add(aDto);
            }

            PrepareSecurityObjectResourceDetailString(objType, aSet, list, null);

            return aSet;
        }


        public static ObservableSet<AppSecuritySysObjGroupUserExDto> RetrieveUserTypePrivilegeDtoByType(int userType, int objType)
        {
            ObservableSet<AppSecuritySysObjGroupUserExDto> aSet = new ObservableSet<AppSecuritySysObjGroupUserExDto>();
            EntityCollection<AppSecuritySysObjGroupUserEntity> list = RetrieveUserTypePrivilegeEntitiesByType(userType, objType);
            foreach (var o in list)
            {
                AppSecuritySysObjGroupUserExDto aDto = AppSecuritySysObjGroupUserConverter.ConvertEntityToExDto(o);
                aSet.Add(aDto);
            }

            PrepareSecurityObjectResourceDetailString(objType, aSet, list, userType);

            return aSet;
        }

        public static OperationCallResult<AppSecuritySysObjGroupUserExDto> SaveNewUserTypePrivilegeByType(ObservableSet<AppSecuritySysObjGroupUserExDto> aSet, int userType, int objType)
        {
            OperationCallResult<AppSecuritySysObjGroupUserExDto> aOperationCallResult = new OperationCallResult<AppSecuritySysObjGroupUserExDto>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;

            if (!validationResult.HasErrors)
            {
                foreach (var dto in aSet)
                {
                    dto.EmUserType = userType;
                }

                if (aSet.Count > 0)
                {
                    var result = ProcessNewDtos(aSet.ToList(), true);
                    validationResult.Merge(result);
                }
            }



            // if no any errors, refresh all entity from DBMS server
            if (!validationResult.HasErrors)
            {
                validationResult.Items.Clear();
                validationResult.Items.Add(new ValidationItem(typeof(AppSecuritySysObjGroupUserExDto), "App_AppSecuritySysObjGroupUserEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                aOperationCallResult.ObjectList = RetrieveUserTypePrivilegeDtoByType(userType, objType);
            }

            return aOperationCallResult;
        }


        public static OperationCallResult<AppSecuritySysObjGroupUserExDto> DeleteUserTypePrivilegeByType(ObservableSet<AppSecuritySysObjGroupUserExDto> aSet, int userType, int objType)
        {
            OperationCallResult<AppSecuritySysObjGroupUserExDto> aOperationCallResult = new OperationCallResult<AppSecuritySysObjGroupUserExDto>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;

            validationResult.Merge(DeleteUserTypePrivilegeDetails(userType, objType, aSet));

            if (!validationResult.HasErrors)
            {
                List<int> securityRightIds = aSet.Where(o => o.Id != null).Select(o => (int)o.Id).ToList();
                validationResult.Merge(DeletePrivilegeBySecurityRightIds(securityRightIds));
            }

            // if no any errors, refresh all entity from DBMS server
            if (!validationResult.HasErrors)
            {
                validationResult.Items.Clear();
                validationResult.Items.Add(new ValidationItem(typeof(AppSecuritySysObjGroupUserExDto), "App_AppSecuritySysObjGroupUserEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                aOperationCallResult.ObjectList = RetrieveUserTypePrivilegeDtoByType(userType, objType);
            }

            return aOperationCallResult;
        }

        public static OperationCallResult<AppSecuritySysObjGroupUserExDto> SaveNewOrganizationPrivilegeByType(ObservableSet<AppSecuritySysObjGroupUserExDto> aSet, int objType)
        {
            OperationCallResult<AppSecuritySysObjGroupUserExDto> aOperationCallResult = new OperationCallResult<AppSecuritySysObjGroupUserExDto>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;

            List<AppSecuritySysObjGroupUserExDto> needToSaveNewDtos = new List<AppSecuritySysObjGroupUserExDto>();

            foreach (var dto in aSet)
            {
                needToSaveNewDtos.Add(dto);
            }

            if (aSet.Count > 0)
            {
                var result = ProcessNewDtos(aSet.ToList(), true);
                validationResult.Merge(result);
            }

            if (!validationResult.HasErrors)
            {
                validationResult.Items.Clear();
                validationResult.Items.Add(new ValidationItem(typeof(AppSecuritySysObjGroupUserExDto), "App_AppSecuritySysObjGroupUserEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                aOperationCallResult.ObjectList = RetrieveOrganizationPrivilegeDtoByType(objType);
            }

            return aOperationCallResult;
        }

        public static OperationCallResult<AppSecuritySysObjGroupUserExDto> DeleteOrganizationPrivilegeByType(ObservableSet<AppSecuritySysObjGroupUserExDto> aSet, int objType)
        {
            OperationCallResult<AppSecuritySysObjGroupUserExDto> aOperationCallResult = new OperationCallResult<AppSecuritySysObjGroupUserExDto>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;

            List<int> orgUserIds = AppComOrgBL.GetOrganizationUserDtoList(true).Select(o => (int)o.Id).ToList();
            List<int> orgGroupIds = AppComOrgBL.GetOrganizationGroupList().Select(o => (int)o.Id).ToList();

            List<int> securityRightIds = aSet.Where(o => o.Id != null).Select(o => (int)o.Id).ToList();

            validationResult.Merge(DeletePrivilegeBySecurityRightIds(securityRightIds));

            if (!validationResult.HasErrors)
            {
                if (objType == (int)EmAppSecuritySysObjType.Report)
                {
                    List<int> needToDelteReportId = aSet.Where(o => o.ReportId.HasValue).Select(o => o.ReportId.Value).ToList();
                    DeleteOrgReportDetail(validationResult, orgUserIds, orgGroupIds, needToDelteReportId);
                }
                else if (objType == (int)EmAppSecuritySysObjType.Search)
                {

                    List<int> needTodelteSearhcId = aSet.Where(o => o.SearchId.HasValue).Select(o => o.SearchId.Value).ToList();
                    DeleteOrgSearhDetail(validationResult, orgUserIds, orgGroupIds, needTodelteSearhcId);

                }
                else if (objType == (int)EmAppSecuritySysObjType.SearchView)
                {
                    List<int> needTodelserchViewIdList = aSet.Where(o => o.SearchViewId.HasValue).Select(o => o.SearchViewId.Value).ToList();
                    DeleteOrgViewDetail(validationResult, orgUserIds, orgGroupIds, needTodelserchViewIdList);
                }
                else if (objType == (int)EmAppSecuritySysObjType.Transaction)
                {
                    List<int> needTodeltransactionIdList = aSet.Where(o => o.TransactionId.HasValue).Select(o => o.TransactionId.Value).ToList();
                    DeleteOrgTransactionDetail(validationResult, orgUserIds, orgGroupIds, needTodeltransactionIdList);
                }

                else if (objType == (int)EmAppSecuritySysObjType.Dashboard)
                {
                    List<int> needTodesktopList = aSet.Where(o => o.DesktopId.HasValue).Select(o => o.DesktopId.Value).ToList();
                    DeleteOrgDashboardDetail(validationResult, orgUserIds, orgGroupIds, needTodesktopList);
                }
            }

            // if no any errors, refresh all entity from DBMS server
            if (!validationResult.HasErrors)
            {
                validationResult.Items.Clear();
                validationResult.Items.Add(new ValidationItem(typeof(AppSecuritySysObjGroupUserExDto), "App_AppSecuritySysObjGroupUserEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                aOperationCallResult.ObjectList = RetrieveOrganizationPrivilegeDtoByType(objType);
            }

            return aOperationCallResult;
        }




        private static void DeleteOrgViewDetail(ValidationResult validationResult, List<int> orgUserIds, List<int> orgGroupIds, List<int> needTodelteViewId)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    RelationPredicateBucket filter = new RelationPredicateBucket(
                        AppSecuritySysObjGroupUserFields.SearchViewId == needTodelteViewId &

                        (
                            AppSecuritySysObjGroupUserFields.UserId == orgUserIds
                            | AppSecuritySysObjGroupUserFields.GroupId == orgGroupIds)

                        );


                    adapter.DeleteEntitiesDirectly(typeof(AppSecuritySysObjGroupUserEntity), filter);

                }
                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    validationResult.Items.Add(new ValidationItem(typeof(AppSecuritySysObjGroupUserExDto), "App_DataSetEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));


                }
            }
        }


        private static void DeleteOrgTransactionDetail(ValidationResult validationResult, List<int> orgUserIds, List<int> orgGroupIds, List<int> needTodelteTransactionId)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "DeleteDetail");

                    // user ,group transcation restriction 
                    RelationPredicateBucket filterUserTransaction = new RelationPredicateBucket(
                        AppSecuritySysObjGroupUserFields.TransactionId == needTodelteTransactionId &

                        (
                            AppSecuritySysObjGroupUserFields.UserId == orgUserIds
                            | AppSecuritySysObjGroupUserFields.GroupId == orgGroupIds)

                        );

                    adapter.DeleteEntitiesDirectly(typeof(AppSecuritySysObjGroupUserEntity), filterUserTransaction);




                    //------- transaction user Action
                    RelationPredicateBucket filterUserActionTransaction = new RelationPredicateBucket(
                        AppSecuritySysObjGroupUserFields.UserActionTransactionId == needTodelteTransactionId &

                        (
                            AppSecuritySysObjGroupUserFields.UserId == orgUserIds
                            | AppSecuritySysObjGroupUserFields.GroupId == orgGroupIds)

                        );

                    adapter.DeleteEntitiesDirectly(typeof(AppSecuritySysObjGroupUserEntity), filterUserActionTransaction);


                    EntityCollection<AppTransactionUnitEntity> UnitList = new EntityCollection<AppTransactionUnitEntity>();
                    adapter.FetchEntityCollection(UnitList, new RelationPredicateBucket(AppTransactionUnitFields.TransactionId == needTodelteTransactionId));
                    List<int> unitIdList = UnitList.Select(o => o.TransactionUnitId).ToList();


                    //------ Unit action clear
                    RelationPredicateBucket filterUserActionUnit = new RelationPredicateBucket(
                        AppSecuritySysObjGroupUserFields.UserActionTransactionUnitId == unitIdList &

                        (
                            AppSecuritySysObjGroupUserFields.UserId == orgUserIds
                            | AppSecuritySysObjGroupUserFields.GroupId == orgGroupIds)

                        );
                    adapter.DeleteEntitiesDirectly(typeof(AppSecuritySysObjGroupUserEntity), filterUserActionUnit);


                    //--------- Unit acess clear
                    RelationPredicateBucket filterUserUnitAcess = new RelationPredicateBucket(
                    AppSecuritySysObjGroupUserFields.TransactionUnitId == unitIdList &

                    (
                        AppSecuritySysObjGroupUserFields.UserId == orgUserIds
                        | AppSecuritySysObjGroupUserFields.GroupId == orgGroupIds)

                    );
                    adapter.DeleteEntitiesDirectly(typeof(AppSecuritySysObjGroupUserEntity), filterUserUnitAcess);



                    //------------- Field acess clear

                    //EntityCollection<AppTransactionFieldEntity> fieldsList = new EntityCollection<AppTransactionFieldEntity>();
                    //adapter.FetchEntityCollection(fieldsList, new RelationPredicateBucket(AppTransactionFieldFields.TransactionUnitId == unitIdList));
                    //List<int> ufiledIdList = fieldsList.Select(o => o.TransactionFieldId).ToList();

                    //RelationPredicateBucket filterUserFieldAcess = new RelationPredicateBucket(
                    //   AppSecuritySysObjGroupUserFields.TransactionFieldId == ufiledIdList &

                    //   (
                    //       AppSecuritySysObjGroupUserFields.UserId == orgUserIds
                    //       | AppSecuritySysObjGroupUserFields.GroupId == orgGroupIds)

                    //   );
                    //adapter.DeleteEntitiesDirectly(typeof(AppSecuritySysObjGroupUserEntity), filterUserFieldAcess);

                    // ********** Exeption: Parameter ufiledIdList is too long **********


                    EntityCollection<AppTransactionFieldEntity> fieldsList = new EntityCollection<AppTransactionFieldEntity>();
                    adapter.FetchEntityCollection(fieldsList, new RelationPredicateBucket(AppTransactionFieldFields.TransactionUnitId == unitIdList));
                    List<int> ufiledIdList = fieldsList.Select(o => o.TransactionFieldId).ToList();

                    int fieldIdCount = 0;
                    List<int> subfiledIdList = new List<int>();

                    foreach (int fieldId in ufiledIdList)
                    {
                        fieldIdCount++;
                        subfiledIdList.Add(fieldId);

                        if ((fieldIdCount % 500 == 0) || (fieldIdCount == ufiledIdList.Count))
                        {
                            RelationPredicateBucket filterUserFieldAcess = new RelationPredicateBucket(
                               AppSecuritySysObjGroupUserFields.TransactionFieldId == subfiledIdList &
                               (
                                   AppSecuritySysObjGroupUserFields.UserId == orgUserIds
                                   | AppSecuritySysObjGroupUserFields.GroupId == orgGroupIds)

                               );

                            adapter.DeleteEntitiesDirectly(typeof(AppSecuritySysObjGroupUserEntity), filterUserFieldAcess);

                            subfiledIdList = new List<int>();
                        }
                    }


                    adapter.Commit();
                }
                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    validationResult.Items.Add(new ValidationItem(typeof(AppSecuritySysObjGroupUserExDto), "App_DataSetEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));

                }
            }
        }

        public static TransactionPrivilegeDto GetOneAppTransactionAvailablePrivileges(int? transactionId, int? partnerType)
        {
            TransactionPrivilegeDto toReturn = new TransactionPrivilegeDto();

            if (transactionId.HasValue)
            {
                var transactioDto = AppTransactionBL.RetrieveOneAppTransactionExDto(transactionId.Value);
                PrepareTransactionPrivilegePlaceHolder(transactionId, toReturn, transactioDto, partnerType);
                //InitialUserTypeRestrictedPrivileges(transactionId, userType, toReturn, transactioDto);
            }

            return toReturn;
        }



        private static void PrepareTransactionPrivilegePlaceHolder(int? transactionId, TransactionPrivilegeDto toReturn, AppTransactionExDto transactioDto, int? partnerType)
        {
            if (transactioDto != null && transactioDto.TransactionOrganizedType.HasValue)
            {

                toReturn.Transaction = new LookupItemDto() { Id = transactioDto.Id, Display = transactioDto.TransactionName };

                toReturn.TransactionActionPrivileges = new List<AppSecuritySysObjGroupUserDto>();
                toReturn.TransactionUnitActionPrivileges = new List<AppSecuritySysObjGroupUserDto>();
                toReturn.TransactionUnitPrivileges = new List<AppSecuritySysObjGroupUserDto>();
                toReturn.TransactionFieldPrivileges = new List<AppSecuritySysObjGroupUserDto>();
                toReturn.TransactionCommandPrivileges = new List<AppSecuritySysObjGroupUserDto>();


                List<LookupItemDto> users = AppEntityInfoBL.GetLookupItemListByCode(EmAppEntityLookupInfoCode.AppSecurityUser.ToString(), "");
                List<LookupItemDto> roles = AppEntityInfoBL.GetLookupItemListByCode(EmAppEntityLookupInfoCode.AppSecurityGroup.ToString(), "");

                foreach (var item in Enum.GetValues(typeof(EmAppSysTransactionActionCode)))
                {
                    int actionCode = (int)item;
                    string actionName = item.ToString();
                    AppSecuritySysObjGroupUserDto securityDto = new AppSecuritySysObjGroupUserDto();
                    securityDto.IsRestrict = false;
                    securityDto.UserActionTransactionCode = actionCode;
                    securityDto.UserActionTransactionId = transactionId.Value;
                    securityDto.Display = actionName;
                    securityDto.TransactionId = (int)transactioDto.Id;
                    securityDto.IsSpecialPermission = true;
                    toReturn.TransactionActionPrivileges.Add(securityDto);

                    List<AppSecuritySysObjGroupUserEntity> list = RetrieveOneObjectTypeSysObjGroupUserEntity(securityDto.TransactionId, (int)EmAppSecuritySysObjType.TransactionAction, actionCode, false, partnerType).ToList();
                    PrepareOneSecurityObjectReousrceString(users, roles, securityDto, list);
                }


                foreach (var aUnit in transactioDto.AppTransactionUnitList)
                {
                    foreach (var item in Enum.GetValues(typeof(EmAppSysTransactionUnitActionCode)))
                    {
                        int actionCode = (int)item;
                        string actionName = item.ToString();
                        AppSecuritySysObjGroupUserDto unitActionSecurityDto = new AppSecuritySysObjGroupUserDto();
                        unitActionSecurityDto.IsRestrict = false;
                        unitActionSecurityDto.UserActionTransactionUnitCode = actionCode;
                        unitActionSecurityDto.Display = actionName;
                        unitActionSecurityDto.UserActionTransactionUnitId = (int)aUnit.Id;
                        unitActionSecurityDto.UnitUiName = aUnit.Id + ": " + aUnit.UnitDisplayName;
                        unitActionSecurityDto.Description = unitActionSecurityDto.UnitUiName + ": " + actionName;
                        unitActionSecurityDto.TransactionId = (int)transactioDto.Id;
                        unitActionSecurityDto.IsSpecialPermission = true;

                        if ((transactioDto.TransactionOrganizedType.Value == (int)EmTransactionOrganizedType.MasterDetail) && !aUnit.ParentTransactionUnitId.HasValue)
                        {
                            if (actionCode == (int)EmAppSysTransactionUnitActionCode.OpenLinkedSearch
                                    || actionCode == (int)EmAppSysTransactionUnitActionCode.CallExternalMethodAction
                                    || actionCode == (int)EmAppSysTransactionUnitActionCode.EditUserLoginAction)
                            {
                                toReturn.TransactionUnitActionPrivileges.Add(unitActionSecurityDto);
                            }
                        }
                        else if (transactioDto.TransactionOrganizedType.Value == (int)EmTransactionOrganizedType.List)
                        {
                            if (!(actionCode == (int)EmAppSysTransactionUnitActionCode.CreateFolder
                                || actionCode == (int)EmAppSysTransactionUnitActionCode.DeleteFolder
                                || actionCode == (int)EmAppSysTransactionUnitActionCode.EditFolder
                                || actionCode == (int)EmAppSysTransactionUnitActionCode.EditFolderSecurity))
                            {
                                toReturn.TransactionUnitActionPrivileges.Add(unitActionSecurityDto);
                            }
                        }

                        List<AppSecuritySysObjGroupUserEntity> list = RetrieveOneObjectTypeSysObjGroupUserEntity(aUnit.Id, (int)EmAppSecuritySysObjType.TransactionUnitAction, actionCode, false, partnerType).ToList();
                        PrepareOneSecurityObjectReousrceString(users, roles, unitActionSecurityDto, list);
                    }

                    AppSecuritySysObjGroupUserDto unitSecurityDto = new AppSecuritySysObjGroupUserDto();
                    unitSecurityDto.IsRestrict = false;
                    unitSecurityDto.UnitUiName = aUnit.Id + ": " + aUnit.UnitDisplayName;
                    unitSecurityDto.Display = unitSecurityDto.UnitUiName;
                    unitSecurityDto.TransactionUnitId = (int)aUnit.Id;
                    unitSecurityDto.TransactionId = (int)transactioDto.Id;
                    unitSecurityDto.IsSpecialPermission = true;
                    toReturn.TransactionUnitPrivileges.Add(unitSecurityDto);

                    List<AppSecuritySysObjGroupUserEntity> transUnitRestrictionList = RetrieveOneObjectTypeSysObjGroupUserEntity(aUnit.Id, (int)EmAppSecuritySysObjType.TransactionUnit, null, false, partnerType).ToList();
                    PrepareOneSecurityObjectReousrceString(users, roles, unitSecurityDto, transUnitRestrictionList);

                    foreach (var aTransField in aUnit.AppTransactionFieldList)
                    {
                        AppSecuritySysObjGroupUserDto transfieldSecurityDto = new AppSecuritySysObjGroupUserDto();
                        transfieldSecurityDto.TransactionFieldId = (int)aTransField.Id;
                        transfieldSecurityDto.Display = aTransField.Id + ": " + aTransField.DisplayName;
                        transfieldSecurityDto.UnitUiName = unitSecurityDto.Display;
                        transfieldSecurityDto.TransactionId = (int)transactioDto.Id;
                        transfieldSecurityDto.IsSpecialPermission = true;
                        toReturn.TransactionFieldPrivileges.Add(transfieldSecurityDto);

                        List<AppSecuritySysObjGroupUserEntity> transFieldRistrictionList = RetrieveOneObjectTypeSysObjGroupUserEntity(transfieldSecurityDto.TransactionFieldId, (int)EmAppSecuritySysObjType.TransactionField, null, false, partnerType).ToList();
                        PrepareOneSecurityObjectReousrceString(users, roles, transfieldSecurityDto, transFieldRistrictionList);
                    }
                }



                foreach (var commandDto in transactioDto.CommandActionList
                    .Where(o => o.ActionAttribute != null && o.ActionAttribute.LinkToUI.HasValue && o.ActionAttribute.LinkToUI.Value))
                {
                    AppSecuritySysObjGroupUserDto unitSecurityDto = new AppSecuritySysObjGroupUserDto();
                    unitSecurityDto.IsRestrict = false;
                    unitSecurityDto.CommandUiName = commandDto.Id + ": " + commandDto.Name;
                    unitSecurityDto.Display = unitSecurityDto.CommandUiName;
                    unitSecurityDto.CommandId = (int)commandDto.Id;
                    unitSecurityDto.TransactionId = (int)transactioDto.Id;
                    unitSecurityDto.IsSpecialPermission = true;
                    toReturn.TransactionCommandPrivileges.Add(unitSecurityDto);

                    List<AppSecuritySysObjGroupUserEntity> transCommandRestrictionList = RetrieveOneObjectTypeSysObjGroupUserEntity(commandDto.Id, (int)EmAppSecuritySysObjType.TransactionCommand, null, false, partnerType).ToList();
                    PrepareOneSecurityObjectReousrceString(users, roles, unitSecurityDto, transCommandRestrictionList);
                }

            }
        }



        private static void DeleteOrgDashboardDetail(ValidationResult validationResult, List<int> orgUserIds, List<int> orgGroupIds, List<int> needTodelteSearhcId)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    RelationPredicateBucket filter = new RelationPredicateBucket(
                        AppSecuritySysObjGroupUserFields.DesktopId == needTodelteSearhcId &

                        (
                            AppSecuritySysObjGroupUserFields.UserId == orgUserIds
                            | AppSecuritySysObjGroupUserFields.GroupId == orgGroupIds)

                        );


                    adapter.DeleteEntitiesDirectly(typeof(AppSecuritySysObjGroupUserEntity), filter);

                }
                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    validationResult.Items.Add(new ValidationItem(typeof(AppSecuritySysObjGroupUserExDto), "App_DataSetEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));


                }
            }
        }

        private static void DeleteOrgSearhDetail(ValidationResult validationResult, List<int> orgUserIds, List<int> orgGroupIds, List<int> needTodelteSearhcId)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    RelationPredicateBucket filter = new RelationPredicateBucket(
                        AppSecuritySysObjGroupUserFields.SearchId == needTodelteSearhcId &

                        (
                            AppSecuritySysObjGroupUserFields.UserId == orgUserIds
                            | AppSecuritySysObjGroupUserFields.GroupId == orgGroupIds)

                        );


                    adapter.DeleteEntitiesDirectly(typeof(AppSecuritySysObjGroupUserEntity), filter);

                }
                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    validationResult.Items.Add(new ValidationItem(typeof(AppSecuritySysObjGroupUserExDto), "App_DataSetEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));


                }
            }
        }

        private static void DeleteOrgReportDetail(ValidationResult validationResult, List<int> orgUserIds, List<int> orgGroupIds, List<int> needToDelteReportId)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    RelationPredicateBucket filter = new RelationPredicateBucket(
                        AppSecuritySysObjGroupUserFields.ReportId == needToDelteReportId &

                        (
                            AppSecuritySysObjGroupUserFields.UserId == orgUserIds
                            | AppSecuritySysObjGroupUserFields.GroupId == orgGroupIds)

                        );


                    adapter.DeleteEntitiesDirectly(typeof(AppSecuritySysObjGroupUserEntity), filter);

                }
                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    validationResult.Items.Add(new ValidationItem(typeof(AppSecuritySysObjGroupUserExDto), "App_DataSetEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));


                }
            }
        }

        public static ObservableSet<AppSecuritySysObjGroupUserExDto> RetrieveOrganizationDetailGroupUserPrivilegeDtoByType(object objectID, int objType, object actionCode, bool isIgnoreCurrentUserFilterSetup, int? partnerType)
        {
            ObservableSet<AppSecuritySysObjGroupUserExDto> aSet = new ObservableSet<AppSecuritySysObjGroupUserExDto>();
            EntityCollection<AppSecuritySysObjGroupUserEntity> list = RetrieveOneObjectTypeSysObjGroupUserEntity(objectID, objType, actionCode, isIgnoreCurrentUserFilterSetup, partnerType);
            foreach (var o in list)
            {
                AppSecuritySysObjGroupUserExDto aDto = AppSecuritySysObjGroupUserConverter.ConvertEntityToExDto(o);
                aSet.Add(aDto);
            }

            return aSet;
        }

        public static RoleAndUserListDto RetrieveSecurityObjectAvailableOrganizationAllRoleAndUser(int? objId, int? objType, int? actionCode)
        {
            RoleAndUserListDto roleAndUserListDto = new RoleAndUserListDto();

            List<int> availableOrganizationIds = new List<int>();
            List<int> restrictedRoleIds = new List<int>();
            List<int> restrictedUserIds = new List<int>();

            List<AppSecurityGroupDto> allGroupDtos = AppSecurityGroupBL.RetrieveAllAppSecurityGroupDto().ToList();
            List<AppSecurityUserDto> allUserDtos = AppSecurityUserBL.RetrieveAllAppSecurityUserDto().ToList();

            List<AppSecurityGroupDto> AvailableGroupDtos = new List<AppSecurityGroupDto>();
            List<AppSecurityUserDto> AvailableUserDtos = new List<AppSecurityUserDto>();

            if (objId.HasValue && objType.HasValue)
            {
                //using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                //{
                //    EntityCollection<AppSecuritySysObjGroupUserEntity> list = new EntityCollection<AppSecuritySysObjGroupUserEntity>();

                //    RelationPredicateBucket filter = new RelationPredicateBucket();
                //    filter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.OrganizationId != System.DBNull.Value);

                //    bool isConditionWrong = false;

                //    if (objType.Value == (int)EmAppSecuritySysObjType.Dashboard)
                //    {
                //        filter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.DesktopId == objId.Value);
                //    }
                //    else if (objType.Value == (int)EmAppSecuritySysObjType.Report)
                //    {
                //        filter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.ReportId == objId.Value);
                //    }
                //    else if (objType.Value == (int)EmAppSecuritySysObjType.Search)
                //    {
                //        filter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.SearchId == objId.Value);
                //    }
                //    else if (objType.Value == (int)EmAppSecuritySysObjType.SearchView)
                //    {
                //        filter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.SearchViewId == objId.Value);
                //    }
                //    else if (objType.Value == (int)EmAppSecuritySysObjType.Transaction)
                //    {
                //        filter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.TransactionId == objId.Value);
                //    }
                //    else if (objType.Value == (int)EmAppSecuritySysObjType.TransactionUnit)
                //    {
                //        filter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.TransactionUnitId == objId.Value);
                //    }
                //    else if (objType.Value == (int)EmAppSecuritySysObjType.TransactionField)
                //    {
                //        filter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.TransactionFieldId == objId.Value);
                //    }
                //    else if (objType.Value == (int)EmAppSecuritySysObjType.TransactionAction && actionCode.HasValue)
                //    {
                //        filter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.UserActionTransactionId == objId.Value & AppSecuritySysObjGroupUserFields.UserActionTransactionCode == actionCode.Value);
                //    }
                //    else if (objType.Value == (int)EmAppSecuritySysObjType.TransactionUnitAction && actionCode.HasValue)
                //    {
                //        filter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.UserActionTransactionUnitId == objId.Value & AppSecuritySysObjGroupUserFields.UserActionTransactionUnitCode == actionCode.Value);
                //    }
                //    else
                //    {
                //        isConditionWrong = true; ;
                //    }

                //    if (!isConditionWrong)
                //    {
                //        adapter.FetchEntityCollection(list, filter);
                //        availableOrganizationIds = list.Where(o => o.OrganizationId.HasValue).Select(o => o.OrganizationId.Value).ToList();
                //    }
                //}


                foreach (var roleDto in allGroupDtos)
                {
                    if (roleDto.RoleUserTypeId.HasValue && (
                        roleDto.RoleUserTypeId.Value == (int)EmAppUserType.Employee
                        || roleDto.RoleUserTypeId.Value == (int)EmAppUserType.SaasCompanyAdmin
                        || roleDto.RoleUserTypeId.Value == (int)EmAppUserType.AllUser
                        ))
                    {
                        AvailableGroupDtos.Add(roleDto);
                    }
                }

                foreach (var userDto in allUserDtos)
                {
                    if (userDto.DomainId == (int)EmAppUserType.Employee
                        || userDto.DomainId == (int)EmAppUserType.SaasCompanyAdmin)
                    {
                        AvailableUserDtos.Add(userDto);
                    }
                }

            }

            roleAndUserListDto.RoleList = AvailableGroupDtos;
            roleAndUserListDto.UserList = AvailableUserDtos;
            roleAndUserListDto.OrganizationIdList = availableOrganizationIds;

            return roleAndUserListDto;
        }



        public static RoleAndUserListDto RetrieveTransactionAvailableOrganizationRoleAndUser(int transactionId)
        {
            RoleAndUserListDto roleAndUserListDto = new RoleAndUserListDto();

            List<int> availableOrganizationIds = new List<int>();
            List<int> restrictedRoleIds = new List<int>();
            List<int> restrictedUserIds = new List<int>();

            List<AppSecurityGroupDto> allGroupDtos = AppSecurityGroupBL.RetrieveAppSecurityGroupDtoByUsageType(1).ToList();
            List<AppSecurityUserDto> allUserDtos = AppSecurityUserBL.RetrieveAllAppSecurityUserDto().Where(o => !(o.IsBuiltIntUser.HasValue && o.IsBuiltIntUser.Value)).ToList();

            List<AppSecurityGroupDto> AvailableGroupDtos = new List<AppSecurityGroupDto>();
            List<AppSecurityUserDto> AvailableUserDtos = new List<AppSecurityUserDto>();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppSecuritySysObjGroupUserEntity> list = new EntityCollection<AppSecuritySysObjGroupUserEntity>();
                RelationPredicateBucket filter = new RelationPredicateBucket(AppSecuritySysObjGroupUserFields.TransactionId == transactionId);
                adapter.FetchEntityCollection(list, filter);
                availableOrganizationIds = list.Where(o => o.OrganizationId.HasValue).Select(o => o.OrganizationId.Value).ToList();
                restrictedRoleIds = list.Where(o => o.GroupId.HasValue && o.IsInVisible.HasValue && o.IsInVisible.Value).Select(o => o.GroupId.Value).ToList();
                restrictedUserIds = list.Where(o => o.UserId.HasValue && o.IsInVisible.HasValue && o.IsInVisible.Value).Select(o => o.UserId.Value).ToList();
            }

            foreach (var roleDto in allGroupDtos)
            {
                if (roleDto.OrganizationId.HasValue && availableOrganizationIds.Contains(roleDto.OrganizationId.Value) || !roleDto.OrganizationId.HasValue)
                {
                    if (!restrictedRoleIds.Contains((int)roleDto.Id))
                    {
                        AvailableGroupDtos.Add(roleDto);
                    }
                }
            }


            foreach (var userDto in allUserDtos)
            {
                if (userDto.OrganizationId.HasValue && availableOrganizationIds.Contains(userDto.OrganizationId.Value) || !userDto.OrganizationId.HasValue)
                {
                    if (!restrictedUserIds.Contains((int)userDto.Id))
                    {
                        AvailableUserDtos.Add(userDto);
                    }
                }
            }

            roleAndUserListDto.RoleList = AvailableGroupDtos;
            roleAndUserListDto.UserList = AvailableUserDtos;
            roleAndUserListDto.OrganizationIdList = availableOrganizationIds;

            return roleAndUserListDto;
        }


        public static OperationCallResult<AppSecuritySysObjGroupUserExDto> SavOrganizationDetailLevelUserRowbyType(SecurityObjSetDto securityObjSetDto)
        {
            ObservableSet<AppSecuritySysObjGroupUserExDto> aSet = securityObjSetDto.AppSecuritySysObjGroupUserSet;
            object objectID = securityObjSetDto.AppSecuritySysObjId;
            int objType = securityObjSetDto.AppSecuritySysObjType;

            int? partnerType = securityObjSetDto.PartnerType;
            object actionCode = securityObjSetDto.ActionCode;
            bool isIgnoreCurrentUserFilterSetup = securityObjSetDto.IsIgnoreCurrentUserFilterSetup;

            OperationCallResult<AppSecuritySysObjGroupUserExDto> aOperationCallResult = new OperationCallResult<AppSecuritySysObjGroupUserExDto>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;



            ValidationResult ver = DeleteOneAppSecuritySysObjGroupUserEntity(objectID, objType, actionCode, isIgnoreCurrentUserFilterSetup, partnerType);

            if (!ver.HasErrors)
            {
                List<AppSecuritySysObjGroupUserExDto> needToSaveNewDtos = new List<AppSecuritySysObjGroupUserExDto>();

                foreach (var dto in aSet)
                {
                    dto.OrganizationId = null;

                    if (objType == (int)EmAppSecuritySysObjType.Report)
                    {
                        dto.ReportId = objectID as int?;
                    }
                    else if (objType == (int)EmAppSecuritySysObjType.Search)
                    {
                        dto.SearchId = objectID as int?;
                    }
                    if (objType == (int)EmAppSecuritySysObjType.SearchView)
                    {
                        dto.SearchViewId = objectID as int?;
                    }
                    if (objType == (int)EmAppSecuritySysObjType.Transaction)
                    {
                        dto.TransactionId = objectID as int?;
                    }
                    if (objType == (int)EmAppSecuritySysObjType.TransactionUnit)
                    {
                        dto.TransactionUnitId = objectID as int?;
                    }
                    if (objType == (int)EmAppSecuritySysObjType.TransactionField)
                    {
                        dto.TransactionFieldId = objectID as int?;
                    }
                    if (objType == (int)EmAppSecuritySysObjType.TransactionUnitLinkedSearch)
                    {
                        dto.TransactionUnitLinkedSearchId = objectID as int?;
                    }
                    if (objType == (int)EmAppSecuritySysObjType.Dashboard)
                    {
                        dto.DesktopId = objectID as int?;
                    }
                    if (objType == (int)EmAppSecuritySysObjType.TransactionAction)
                    {
                        dto.UserActionTransactionId = objectID as int?;
                        dto.UserActionTransactionCode = actionCode as int?;
                    }
                    if (objType == (int)EmAppSecuritySysObjType.TransactionUnitAction)
                    {
                        dto.UserActionTransactionUnitId = objectID as int?;
                        dto.UserActionTransactionUnitCode = actionCode as int?;
                    }
                    if (objType == (int)EmAppSecuritySysObjType.TransactionCommand)
                    {
                        dto.CommandId = objectID as int?;
                    }


                    if (isIgnoreCurrentUserFilterSetup)
                    {
                        dto.IsIgnoreFilterBy = true;
                    }

                    needToSaveNewDtos.Add(dto);
                }

                if (needToSaveNewDtos.Count > 0)
                {
                    var result = ProcessNewDtos(needToSaveNewDtos, false);
                    validationResult.Merge(result);
                }

                // if no any errors, refresh all entity from DBMS server
                if (!validationResult.HasErrors)
                {
                    validationResult.Items.Clear();
                    validationResult.Items.Add(new ValidationItem(typeof(AppSecuritySysObjGroupUserExDto), "App_AppSecuritySysObjGroupUserEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                    aOperationCallResult.ObjectList = RetrieveOrganizationDetailGroupUserPrivilegeDtoByType(objectID, objType, actionCode, isIgnoreCurrentUserFilterSetup, partnerType);
                }
            }

            return aOperationCallResult;
        }

        private static EntityCollection<AppSecuritySysObjGroupUserEntity> RetrieveOneObjectTypeSysObjGroupUserEntity(object objectID, int objType, object actionCode, bool isIgnoreCurrentUserFilterSetup, int? partnerType)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppSecuritySysObjGroupUserEntity> list = new EntityCollection<AppSecuritySysObjGroupUserEntity>();

                RelationPredicateBucket filter = new RelationPredicateBucket();

                SetupRetrieveOneObjectTypeSysObjGroupUserFilter(objectID, objType, filter, actionCode, isIgnoreCurrentUserFilterSetup, partnerType);

                adapter.FetchEntityCollection(list, filter);

                return list;
            }
        }

        private static ValidationResult DeleteOneAppSecuritySysObjGroupUserEntity(object objectID, int objType, object actionCode, bool isIgnoreCurrentUserFilterSetup, int? partnerType)
        {
            ValidationResult aValidationResult = new ValidationResult();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    RelationPredicateBucket filter = new RelationPredicateBucket();

                    SetupRetrieveOneObjectTypeSysObjGroupUserFilter(objectID, objType, filter, actionCode, isIgnoreCurrentUserFilterSetup, partnerType);

                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "AppSecuritySysObjGroupUserEntity");
                    adapter.DeleteEntitiesDirectly(typeof(AppSecuritySysObjGroupUserEntity), filter);
                    adapter.Commit();
                }

                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSecuritySysObjGroupUserExDto), "App_DataSetEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));

                    adapter.Rollback();
                }
            }

            return aValidationResult;
        }


        private static ValidationResult ProcessNewDtos(List<AppSecuritySysObjGroupUserExDto> needToSaveNewDtos, bool needAutoAddBuiltInRoles)
        {
            ValidationResult aValidationResult = new ValidationResult();

            if (!needToSaveNewDtos.IsEmpty())
            {
                EntityCollection<AppSecuritySysObjGroupUserEntity> needToSaveEntityList = new EntityCollection<AppSecuritySysObjGroupUserEntity>();

                foreach (var aDto in needToSaveNewDtos)
                {
                    AppSecuritySysObjGroupUserEntity aEntity = new AppSecuritySysObjGroupUserEntity();

                    AppSecuritySysObjGroupUserConverter.CopyDtoToEntity(aEntity, aDto);

                    needToSaveEntityList.Add(aEntity);

                    if (needAutoAddBuiltInRoles && aDto.Id == null)
                    {
                        PrepareDefaultBuiltInRoleToOrgOrPartnerTypeNode(aDto, needToSaveEntityList);
                    }
                    else
                    {

                    }

                }


                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    try
                    {
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                        adapter.SaveEntityCollection(needToSaveEntityList);
                        adapter.Commit();
                    }
                    catch (ORMQueryExecutionException ex)
                    {
                        adapter.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppSecuritySysObjGroupUserExDto), "App_DataSetEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    }
                }
            }


            return aValidationResult;
        }

        //private static ValidationResult ProcessNewDto(AppSecuritySysObjGroupUserExDto aDto)
        //{
        //    ValidationResult aValidationResult = new ValidationResult();

        //    AppSecuritySysObjGroupUserEntity aAppSecuritySysObjGroupUserEntity = new AppSecuritySysObjGroupUserEntity();

        //    AppSecuritySysObjGroupUserConverter.CopyDtoToEntity(aAppSecuritySysObjGroupUserEntity, aDto);

        //    EntityCollection<AppSecuritySysObjGroupUserEntity> needToSaveEntityList = new EntityCollection<AppSecuritySysObjGroupUserEntity>();
        //    needToSaveEntityList.Add(aAppSecuritySysObjGroupUserEntity);
        //    PrepareDefaultBuiltInRoleToNewSecurityObj(aDto, needToSaveEntityList);

        //    using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
        //    {
        //        try
        //        {
        //            adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
        //            adapter.SaveEntityCollection(needToSaveEntityList);

        //            adapter.Commit();
        //            //aValidationResult.Items.Add(new ValidationItem(typeof(AppSecuritySysObjGroupUserExDto), "App_DataSetEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
        //            //aDto.Id = aAppSecuritySysObjGroupUserEntity.SecurityRightId;
        //        }
        //        catch (ORMQueryExecutionException ex)
        //        {
        //            adapter.Rollback();
        //            aValidationResult.Items.Add(new ValidationItem(typeof(AppSecuritySysObjGroupUserExDto), "App_DataSetEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
        //        }
        //    }

        //    return aValidationResult;
        //}

        private static void PrepareDefaultBuiltInRoleToOrgOrPartnerTypeNode(AppSecuritySysObjGroupUserExDto aDto, EntityCollection<AppSecuritySysObjGroupUserEntity> needToSaveEntityList)
        {
            AppSecurityGroupEntity genericBuiltInRole = null;
            AppSecurityGroupEntity specificBuiltInRole = null;


            if (aDto.EmUserType.HasValue && !aDto.GroupId.HasValue &&
                (aDto.EmUserType.Value == (int)EmAppUserType.Customer
                || aDto.EmUserType.Value == (int)EmAppUserType.Supplier
                || aDto.EmUserType.Value == (int)EmAppUserType.ClientAgent
                 || aDto.EmUserType.Value == (int)EmAppUserType.SupplierAgent))
            {
                genericBuiltInRole = AppSecurityGroupBL.RetriveGenericBuiltInRole((EmAppUserType)aDto.EmUserType.Value);
            }
            else
            {
                //genericBuiltInRole = AppSecurityGroupBL.RetriveGenericBuiltInRole(EmAppUserType.Employee);
                specificBuiltInRole = AppSecurityGroupBL.RetriveSpecificOrganizationBuiltInRole();
            }

            if (genericBuiltInRole != null)
            {
                if (needToSaveEntityList.FirstOrDefault(o => o.GroupId.HasValue && o.GroupId.Value == genericBuiltInRole.GroupId
                    && (o.TransactionId == aDto.TransactionId && o.SearchId == aDto.SearchId && o.SearchViewId == aDto.SearchViewId && o.DesktopId == aDto.DesktopId && o.ReportId == aDto.ReportId)
                    ) == null)
                {
                    AppSecuritySysObjGroupUserEntity aEntity = new AppSecuritySysObjGroupUserEntity();
                    AppSecuritySysObjGroupUserConverter.CopyDtoToEntity(aEntity, aDto);
                    aEntity.GroupId = genericBuiltInRole.GroupId;
                    aEntity.OrganizationId = null;

                    needToSaveEntityList.Add(aEntity);
                }
            }

            if (specificBuiltInRole != null)
            {
                if (needToSaveEntityList.FirstOrDefault(o => o.GroupId.HasValue && o.GroupId.Value == specificBuiltInRole.GroupId
                    && (o.TransactionId == aDto.TransactionId && o.SearchId == aDto.SearchId && o.SearchViewId == aDto.SearchViewId && o.DesktopId == aDto.DesktopId && o.ReportId == aDto.ReportId)
                    ) == null)
                {
                    AppSecuritySysObjGroupUserEntity aEntity = new AppSecuritySysObjGroupUserEntity();
                    AppSecuritySysObjGroupUserConverter.CopyDtoToEntity(aEntity, aDto);
                    aEntity.GroupId = specificBuiltInRole.GroupId;
                    aEntity.OrganizationId = null;

                    needToSaveEntityList.Add(aEntity);
                }
            }
        }

        private static void SetupRetrieveOneObjectTypeSysObjGroupUserFilter(object objectID, int objType, RelationPredicateBucket filter, object actionCode, bool isIgnoreCurrentUserFilterSetup, int? partnerType)
        {
            if (objType == (int)EmAppSecuritySysObjType.Search)
            {
                filter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.SearchId == objectID);
            }
            if (objType == (int)EmAppSecuritySysObjType.SearchView)
            {
                filter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.SearchViewId == objectID);
            }
            if (objType == (int)EmAppSecuritySysObjType.Transaction)
            {
                filter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.TransactionId == objectID);
            }
            if (objType == (int)EmAppSecuritySysObjType.TransactionUnit)
            {
                filter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.TransactionUnitId == objectID);
            }
            if (objType == (int)EmAppSecuritySysObjType.TransactionField)
            {
                filter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.TransactionFieldId == objectID);
            }
            if (objType == (int)EmAppSecuritySysObjType.TransactionUnitLinkedSearch)
            {
                filter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.TransactionUnitLinkedSearchId == objectID);
            }
            if (objType == (int)EmAppSecuritySysObjType.Dashboard)
            {
                filter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.DesktopId == objectID);
            }
            if (objType == (int)EmAppSecuritySysObjType.TransactionAction)
            {
                filter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.UserActionTransactionId == objectID & AppSecuritySysObjGroupUserFields.UserActionTransactionCode == actionCode);
            }
            if (objType == (int)EmAppSecuritySysObjType.TransactionUnitAction)
            {
                filter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.UserActionTransactionUnitId == objectID & AppSecuritySysObjGroupUserFields.UserActionTransactionUnitCode == actionCode);
            }
            if (objType == (int)EmAppSecuritySysObjType.Report)
            {
                filter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.ReportId == objectID);
            }
            if (objType == (int)EmAppSecuritySysObjType.TransactionCommand)
            {
                filter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.CommandId == objectID);
            }

            if (isIgnoreCurrentUserFilterSetup)
            {
                filter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.IsIgnoreFilterBy != System.DBNull.Value);
            }
            else
            {
                filter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.IsIgnoreFilterBy == System.DBNull.Value);
            }

            filter.PredicateExpression.AddWithAnd(AppSecuritySysObjGroupUserFields.UserId != System.DBNull.Value | AppSecuritySysObjGroupUserFields.GroupId != System.DBNull.Value);


            if (partnerType.HasValue)
            {
                filter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.EmUserType == partnerType.Value);
            }
            else
            {
                filter.Relations.Add(AppSecurityUserEntity.Relations.AppSecuritySysObjGroupUserEntityUsingUserId, JoinHint.Right);
                filter.Relations.Add(AppSecurityGroupEntity.Relations.AppSecuritySysObjGroupUserEntityUsingGroupId, JoinHint.Right);

            }
        }



        private static EntityCollection<AppSecuritySysObjGroupUserEntity> RetrieveUserTypePrivilegeEntitiesByType(int useType, int objType)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppSecuritySysObjGroupUserEntity> list = new EntityCollection<AppSecuritySysObjGroupUserEntity>();

                RelationPredicateBucket filter = new RelationPredicateBucket(AppSecuritySysObjGroupUserFields.EmUserType == useType
                    & AppSecuritySysObjGroupUserFields.UserId == System.DBNull.Value
                    & AppSecuritySysObjGroupUserFields.GroupId == System.DBNull.Value);

                SetupPrivilegeByTypeRetrieveFilter(objType, filter);

                adapter.FetchEntityCollection(list, filter);

                return list;
            }
        }


        private static EntityCollection<AppSecuritySysObjGroupUserEntity> RetrieveUserTypeTransactionPrivilegeEntitiesByType(int useType, int objType, int transactionId)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppSecuritySysObjGroupUserEntity> list = new EntityCollection<AppSecuritySysObjGroupUserEntity>();

                RelationPredicateBucket filter = new RelationPredicateBucket(AppSecuritySysObjGroupUserFields.EmUserType == useType
                    & AppSecuritySysObjGroupUserFields.TransactionId == transactionId
                    & AppSecuritySysObjGroupUserFields.IsSpecialPermission == true);

                SetupPrivilegeByTypeRetrieveFilter(objType, filter);

                adapter.FetchEntityCollection(list, filter);

                return list;
            }
        }



        private static EntityCollection<AppSecuritySysObjGroupUserEntity> RetrieveOrganizationPrivilegeEntitiesByType(int objType)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppSecuritySysObjGroupUserEntity> list = new EntityCollection<AppSecuritySysObjGroupUserEntity>();

                RelationPredicateBucket filter = new RelationPredicateBucket((AppSecuritySysObjGroupUserFields.EmUserType == System.DBNull.Value | AppSecuritySysObjGroupUserFields.EmUserType == (int)EmAppUserType.Employee)
                       & AppSecuritySysObjGroupUserFields.UserId == System.DBNull.Value
                    & AppSecuritySysObjGroupUserFields.GroupId == System.DBNull.Value);

                SetupPrivilegeByTypeRetrieveFilter(objType, filter);

                adapter.FetchEntityCollection(list, filter);

                return list;
            }
        }

        private static void SetupPrivilegeByTypeRetrieveFilter(int objType, RelationPredicateBucket filter)
        {
            if (objType == (int)EmAppSecuritySysObjType.Search)
            {
                filter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.SearchId != System.DBNull.Value);
            }
            if (objType == (int)EmAppSecuritySysObjType.SearchView)
            {
                filter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.SearchViewId != System.DBNull.Value);
            }
            if (objType == (int)EmAppSecuritySysObjType.Transaction)
            {
                filter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.TransactionId != System.DBNull.Value
                    & (AppSecuritySysObjGroupUserFields.IsSpecialPermission == System.DBNull.Value | AppSecuritySysObjGroupUserFields.IsSpecialPermission == false));
            }

            if (objType == (int)EmAppSecuritySysObjType.Dashboard)
            {
                filter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.DesktopId != System.DBNull.Value);
            }

            if (objType == (int)EmAppSecuritySysObjType.TransactionUnit)
            {
                filter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.TransactionUnitId != System.DBNull.Value);
            }
            if (objType == (int)EmAppSecuritySysObjType.TransactionField)
            {
                filter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.TransactionFieldId != System.DBNull.Value);
            }
            if (objType == (int)EmAppSecuritySysObjType.TransactionAction)
            {
                filter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.UserActionTransactionId != System.DBNull.Value & AppSecuritySysObjGroupUserFields.UserActionTransactionCode != System.DBNull.Value);
            }
            if (objType == (int)EmAppSecuritySysObjType.TransactionUnitAction)
            {
                filter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.UserActionTransactionUnitId != System.DBNull.Value & AppSecuritySysObjGroupUserFields.UserActionTransactionUnitCode != System.DBNull.Value);
            }
            if (objType == (int)EmAppSecuritySysObjType.Report)
            {
                filter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.ReportId != System.DBNull.Value);
            }
        }

        private static ValidationResult DeleteUserTypePrivilegeDetails(int userType, int objType, ObservableSet<AppSecuritySysObjGroupUserExDto> aSet)
        {
            ValidationResult aValidationResult = new ValidationResult();


            RelationPredicateBucket filter = new RelationPredicateBucket();
            bool isNeedToDelete = false;

            if (objType == (int)EmAppSecuritySysObjType.Report)
            {
                List<int> needToDelteReportId = aSet.Where(o => o.ReportId.HasValue).Select(o => o.ReportId.Value).ToList();

                if (needToDelteReportId.Count > 0)
                {
                    isNeedToDelete = true;
                    filter.PredicateExpression.AddWithOr(AppSecuritySysObjGroupUserFields.EmUserType == userType & AppSecuritySysObjGroupUserFields.ReportId == needToDelteReportId);
                }
            }
            else if (objType == (int)EmAppSecuritySysObjType.Search)
            {
                List<int> needTodelteSearhcId = aSet.Where(o => o.SearchId.HasValue).Select(o => o.SearchId.Value).ToList();

                if (needTodelteSearhcId.Count > 0)
                {
                    isNeedToDelete = true;
                    filter.PredicateExpression.AddWithOr(AppSecuritySysObjGroupUserFields.EmUserType == userType & AppSecuritySysObjGroupUserFields.SearchId == needTodelteSearhcId);
                }
            }
            else if (objType == (int)EmAppSecuritySysObjType.SearchView)
            {
                List<int> needTodelserchViewIdList = aSet.Where(o => o.SearchViewId.HasValue).Select(o => o.SearchViewId.Value).ToList();

                if (needTodelserchViewIdList.Count > 0)
                {
                    isNeedToDelete = true;
                    filter.PredicateExpression.AddWithOr(AppSecuritySysObjGroupUserFields.EmUserType == userType & AppSecuritySysObjGroupUserFields.SearchViewId == needTodelserchViewIdList);
                }
            }
            else if (objType == (int)EmAppSecuritySysObjType.Transaction)
            {
                List<int> needTodeltransactionIdList = aSet.Where(o => o.TransactionId.HasValue).Select(o => o.TransactionId.Value).ToList();

                if (needTodeltransactionIdList.Count > 0)
                {
                    isNeedToDelete = true;
                    filter.PredicateExpression.AddWithOr(AppSecuritySysObjGroupUserFields.EmUserType == userType & AppSecuritySysObjGroupUserFields.TransactionId == needTodeltransactionIdList);
                }
            }

            else if (objType == (int)EmAppSecuritySysObjType.Dashboard)
            {

                List<int> needTodesktopList = aSet.Where(o => o.DesktopId.HasValue).Select(o => o.DesktopId.Value).ToList();

                if (needTodesktopList.Count > 0)
                {
                    isNeedToDelete = true;
                    filter.PredicateExpression.AddWithOr(AppSecuritySysObjGroupUserFields.EmUserType == userType & AppSecuritySysObjGroupUserFields.DesktopId == needTodesktopList);
                }
            }

            if (isNeedToDelete)
            {
                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    try
                    {
                        SetupPrivilegeByTypeRetrieveFilter(objType, filter);
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "AppSecuritySysObjGroupUserEntity");
                        adapter.DeleteEntitiesDirectly(typeof(AppSecuritySysObjGroupUserEntity), filter);
                        adapter.Commit();
                    }

                    // Database FK Exception .......
                    catch (ORMQueryExecutionException ex)
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppSecuritySysObjGroupUserExDto), "App_DataSetEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));

                        adapter.Rollback();
                    }
                }
            }
            return aValidationResult;
        }

        private static ValidationResult DeleteUserTypePrivilegeByType(int userType, int objType, ObservableSet<AppSecuritySysObjGroupUserExDto> aSet)
        {
            ValidationResult aValidationResult = new ValidationResult();

            var list = RetrieveUserTypePrivilegeEntitiesByType(userType, objType);

            RelationPredicateBucket filter = new RelationPredicateBucket();
            bool isNeedToDelete = false;

            if (objType == (int)EmAppSecuritySysObjType.Report)
            {
                //List<int> reportIdList = list.Where(o => o.ReportId.HasValue).Select(o => o.ReportId.Value).ToList();
            }
            else if (objType == (int)EmAppSecuritySysObjType.Search)
            {
                List<int> serchIdOldList = list.Where(o => o.SearchId.HasValue).Select(o => o.SearchId.Value).ToList();
                List<int> serchIdList = aSet.Where(o => o.SearchId.HasValue).Select(o => o.SearchId.Value).ToList();

                var needTodelteSearhcId = serchIdOldList.Except(serchIdList).ToList();

                if (needTodelteSearhcId.Count > 0)
                {
                    isNeedToDelete = true;
                    filter.PredicateExpression.AddWithOr(AppSecuritySysObjGroupUserFields.EmUserType == userType & AppSecuritySysObjGroupUserFields.SearchId == needTodelteSearhcId);
                }
            }
            else if (objType == (int)EmAppSecuritySysObjType.SearchView)
            {
                List<int> serchViewOldIdList = list.Where(o => o.SearchViewId.HasValue).Select(o => o.SearchViewId.Value).ToList();

                List<int> serchViewIdList = aSet.Where(o => o.SearchViewId.HasValue).Select(o => o.SearchViewId.Value).ToList();

                var needTodelserchViewIdList = serchViewOldIdList.Except(serchViewIdList).ToList();

                if (needTodelserchViewIdList.Count > 0)
                {
                    isNeedToDelete = true;
                    filter.PredicateExpression.AddWithOr(AppSecuritySysObjGroupUserFields.EmUserType == userType & AppSecuritySysObjGroupUserFields.SearchViewId == needTodelserchViewIdList);
                }
            }
            else if (objType == (int)EmAppSecuritySysObjType.Transaction)
            {
                List<int> transactionOldIdList = list.Where(o => o.TransactionId.HasValue).Select(o => o.TransactionId.Value).ToList();

                List<int> transactionIdList = aSet.Where(o => o.TransactionId.HasValue).Select(o => o.TransactionId.Value).ToList();

                var needTodeltransactionIdList = transactionOldIdList.Except(transactionIdList).ToList();

                if (needTodeltransactionIdList.Count > 0)
                {
                    isNeedToDelete = true;
                    filter.PredicateExpression.AddWithOr(AppSecuritySysObjGroupUserFields.EmUserType == userType & AppSecuritySysObjGroupUserFields.TransactionId == needTodeltransactionIdList);
                }
            }

            else if (objType == (int)EmAppSecuritySysObjType.Dashboard)
            {
                List<int> desktopOldIdList = list.Where(o => o.DesktopId.HasValue).Select(o => o.DesktopId.Value).ToList();

                List<int> desktopIdList = aSet.Where(o => o.DesktopId.HasValue).Select(o => o.DesktopId.Value).ToList();

                var needTodesktopList = desktopOldIdList.Except(desktopIdList).ToList();

                if (needTodesktopList.Count > 0)
                {
                    isNeedToDelete = true;
                    filter.PredicateExpression.AddWithOr(AppSecuritySysObjGroupUserFields.EmUserType == userType & AppSecuritySysObjGroupUserFields.DesktopId == needTodesktopList);
                }
            }

            if (isNeedToDelete)
            {
                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    try
                    {
                        SetupPrivilegeByTypeRetrieveFilter(objType, filter);
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "AppSecuritySysObjGroupUserEntity");
                        adapter.DeleteEntitiesDirectly(typeof(AppSecuritySysObjGroupUserEntity), filter);
                        adapter.Commit();
                    }

                    // Database FK Exception .......
                    catch (ORMQueryExecutionException ex)
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppSecuritySysObjGroupUserExDto), "App_DataSetEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));

                        adapter.Rollback();
                    }
                }
            }
            return aValidationResult;
        }

        private static ValidationResult DeletePrivilegeBySecurityRightIds(List<int> securityRightIds)
        {
            ValidationResult aValidationResult = new ValidationResult();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    RelationPredicateBucket filter = new RelationPredicateBucket(AppSecuritySysObjGroupUserFields.SecurityRightId == securityRightIds);
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "AppSecuritySysObjGroupUserEntity");
                    adapter.DeleteEntitiesDirectly(typeof(AppSecuritySysObjGroupUserEntity), filter);
                    adapter.Commit();
                }

                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSecuritySysObjGroupUserExDto), "App_DataSetEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));

                    adapter.Rollback();
                }
            }

            return aValidationResult;
        }


        private static void PrepareSecurityObjectResourceDetailString(int objType, ObservableSet<AppSecuritySysObjGroupUserExDto> aSet, EntityCollection<AppSecuritySysObjGroupUserEntity> list, int? userType)
        {
            RelationPredicateBucket resourceFilter = new RelationPredicateBucket();

            if (userType.HasValue)
            {
                resourceFilter.PredicateExpression.AddWithAnd(AppSecuritySysObjGroupUserFields.EmUserType == userType.Value
                     & (AppSecuritySysObjGroupUserFields.GroupId != System.DBNull.Value | AppSecuritySysObjGroupUserFields.UserId != System.DBNull.Value));
            }
            else
            {
                resourceFilter.PredicateExpression.AddWithAnd(AppSecuritySysObjGroupUserFields.EmUserType == System.DBNull.Value
                    & (AppSecuritySysObjGroupUserFields.GroupId != System.DBNull.Value | AppSecuritySysObjGroupUserFields.UserId != System.DBNull.Value));

                resourceFilter.Relations.Add(AppSecurityUserEntity.Relations.AppSecuritySysObjGroupUserEntityUsingUserId, JoinHint.Right);
                resourceFilter.Relations.Add(AppSecurityGroupEntity.Relations.AppSecuritySysObjGroupUserEntityUsingGroupId, JoinHint.Right);

            }



            EntityCollection<AppSecuritySysObjGroupUserEntity> securityDetailList = new EntityCollection<AppSecuritySysObjGroupUserEntity>();
            List<LookupItemDto> users = AppEntityInfoBL.GetLookupItemListByCode(EmAppEntityLookupInfoCode.AppSecurityUser.ToString(), "");
            List<LookupItemDto> roles = AppEntityInfoBL.GetLookupItemListByCode(EmAppEntityLookupInfoCode.AppSecurityGroup.ToString(), "");

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                if (objType == (int)EmAppSecuritySysObjType.Search)
                {
                    List<int> searchIdList = list.Where(o => o.SearchId.HasValue).Select(o => o.SearchId.Value).ToList();
                    resourceFilter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.SearchId == searchIdList);
                    adapter.FetchEntityCollection(securityDetailList, resourceFilter);

                    foreach (AppSecuritySysObjGroupUserExDto aDto in aSet)
                    {
                        List<AppSecuritySysObjGroupUserEntity> relatedSecurityObjList = securityDetailList.Where(o => o.SearchId.HasValue && aDto.SearchId.HasValue && o.SearchId.Value == aDto.SearchId.Value).ToList();
                        PrepareOneSecurityObjectReousrceString(users, roles, aDto, relatedSecurityObjList);
                    }
                }
                if (objType == (int)EmAppSecuritySysObjType.SearchView)
                {
                    List<int> searchViewIdList = list.Where(o => o.SearchViewId.HasValue).Select(o => o.SearchViewId.Value).ToList();
                    resourceFilter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.SearchViewId == searchViewIdList);
                    adapter.FetchEntityCollection(securityDetailList, resourceFilter);

                    foreach (AppSecuritySysObjGroupUserExDto aDto in aSet)
                    {
                        List<AppSecuritySysObjGroupUserEntity> relatedSecurityObjList = securityDetailList.Where(o => o.SearchViewId.HasValue && aDto.SearchViewId.HasValue && o.SearchViewId.Value == aDto.SearchViewId.Value).ToList();
                        PrepareOneSecurityObjectReousrceString(users, roles, aDto, relatedSecurityObjList);
                    }
                }
                if (objType == (int)EmAppSecuritySysObjType.Transaction)
                {
                    List<int> transactionIdList = list.Where(o => o.TransactionId.HasValue).Select(o => o.TransactionId.Value).ToList();
                    resourceFilter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.TransactionId == transactionIdList);
                    adapter.FetchEntityCollection(securityDetailList, resourceFilter);

                    foreach (AppSecuritySysObjGroupUserExDto aDto in aSet)
                    {
                        List<AppSecuritySysObjGroupUserEntity> relatedSecurityObjList = securityDetailList.Where(o => o.TransactionId.HasValue && aDto.TransactionId.HasValue && o.TransactionId.Value == aDto.TransactionId.Value).ToList();
                        PrepareOneSecurityObjectReousrceString(users, roles, aDto, relatedSecurityObjList);
                    }
                }
                if (objType == (int)EmAppSecuritySysObjType.Dashboard)
                {
                    List<int> desktopIdList = list.Where(o => o.DesktopId.HasValue).Select(o => o.DesktopId.Value).ToList();
                    resourceFilter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.DesktopId == desktopIdList);
                    adapter.FetchEntityCollection(securityDetailList, resourceFilter);

                    foreach (AppSecuritySysObjGroupUserExDto aDto in aSet)
                    {
                        List<AppSecuritySysObjGroupUserEntity> relatedSecurityObjList = securityDetailList.Where(o => o.DesktopId.HasValue && aDto.DesktopId.HasValue && o.DesktopId.Value == aDto.DesktopId.Value).ToList();
                        PrepareOneSecurityObjectReousrceString(users, roles, aDto, relatedSecurityObjList);
                    }
                }
                if (objType == (int)EmAppSecuritySysObjType.TransactionUnit)
                {
                    List<int> transactionUnitIdList = list.Where(o => o.TransactionUnitId.HasValue).Select(o => o.TransactionUnitId.Value).ToList();
                    resourceFilter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.TransactionUnitId == transactionUnitIdList);
                    adapter.FetchEntityCollection(securityDetailList, resourceFilter);

                    foreach (AppSecuritySysObjGroupUserExDto aDto in aSet)
                    {
                        List<AppSecuritySysObjGroupUserEntity> relatedSecurityObjList = securityDetailList.Where(o => o.TransactionUnitId.HasValue && aDto.TransactionUnitId.HasValue && o.TransactionUnitId.Value == aDto.TransactionUnitId.Value).ToList();
                        PrepareOneSecurityObjectReousrceString(users, roles, aDto, relatedSecurityObjList);
                    }
                }
                if (objType == (int)EmAppSecuritySysObjType.TransactionField)
                {
                    List<int> transactionFieldIdList = list.Where(o => o.TransactionFieldId.HasValue).Select(o => o.TransactionFieldId.Value).ToList();
                    resourceFilter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.TransactionFieldId == transactionFieldIdList);
                    adapter.FetchEntityCollection(securityDetailList, resourceFilter);

                    foreach (AppSecuritySysObjGroupUserExDto aDto in aSet)
                    {
                        List<AppSecuritySysObjGroupUserEntity> relatedSecurityObjList = securityDetailList.Where(o => o.TransactionFieldId.HasValue && aDto.TransactionFieldId.HasValue && o.TransactionFieldId.Value == aDto.TransactionFieldId.Value).ToList();
                        PrepareOneSecurityObjectReousrceString(users, roles, aDto, relatedSecurityObjList);
                    }
                }
                if (objType == (int)EmAppSecuritySysObjType.TransactionAction)
                {
                    //   filter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.UserActionTransactionId != System.DBNull.Value & AppSecuritySysObjGroupUserFields.UserActionTransactionCode != System.DBNull.Value);
                }
                if (objType == (int)EmAppSecuritySysObjType.TransactionUnitAction)
                {
                    // filter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.UserActionTransactionUnitId != System.DBNull.Value & AppSecuritySysObjGroupUserFields.UserActionTransactionUnitCode != System.DBNull.Value);
                }
                if (objType == (int)EmAppSecuritySysObjType.Report)
                {
                    List<int> reportIdList = list.Where(o => o.ReportId.HasValue).Select(o => o.ReportId.Value).ToList();
                    resourceFilter.PredicateExpression.Add(AppSecuritySysObjGroupUserFields.ReportId == reportIdList);
                    adapter.FetchEntityCollection(securityDetailList, resourceFilter);

                    foreach (AppSecuritySysObjGroupUserExDto aDto in aSet)
                    {
                        List<AppSecuritySysObjGroupUserEntity> relatedSecurityObjList = securityDetailList.Where(o => o.ReportId.HasValue && aDto.ReportId.HasValue && o.ReportId.Value == aDto.ReportId.Value).ToList();
                        PrepareOneSecurityObjectReousrceString(users, roles, aDto, relatedSecurityObjList);
                    }
                }
            }
        }


        private static void PrepareOneSecurityObjectReousrceString(List<LookupItemDto> users, List<LookupItemDto> roles, AppSecuritySysObjGroupUserDto aDto, List<AppSecuritySysObjGroupUserEntity> relatedSecurityObjList)
        {
            List<int> roleIds = new List<int>();
            List<int> userIds = new List<int>();
            relatedSecurityObjList.ForAll(o =>
            {
                if (o.GroupId.HasValue)
                {
                    roleIds.Add(o.GroupId.Value);
                }
                else if (o.UserId.HasValue)
                {
                    userIds.Add(o.UserId.Value);
                }
            });

            List<LookupItemDto> roleLookupList = roles.Where(d => roleIds.Contains((int)d.Id)).ToList();
            List<LookupItemDto> userLookupList = users.Where(d => userIds.Contains((int)d.Id)).ToList();

            aDto.ResourceString = string.Empty;

            if (roleLookupList.Count > 0)
            {
                aDto.ResourceString += "Roles: " + EntityHelper.ConvertLookupListToString(roleLookupList) + "; ";
            }

            if (userLookupList.Count > 0)
            {
                aDto.ResourceString += "Users: " + EntityHelper.ConvertLookupListToString(userLookupList);
            }
        }


    }
}