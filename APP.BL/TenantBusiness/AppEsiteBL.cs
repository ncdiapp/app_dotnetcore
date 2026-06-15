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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.ComponentModel.Design;
using System.IO;

using APP.Framework;
namespace App.BL
{
    public static class AppEsiteBL
    {
        public static readonly string MappingKey_Subtotal = "Subtotal";
        public static readonly string MappingKey_Total = "Total";
        public static readonly string MappingKey_TotalTax = "TotalTax";
        public static readonly string MappingKey_ShippingCost = "ShippingCost";

        public static readonly string MappingKey_ShippingAdress_StreetAddress1 = "ShippingAdress->StreetAddress1";
        public static readonly string MappingKey_ShippingAdress_StreetAddress2 = "ShippingAdress->StreetAddress2";
        public static readonly string MappingKey_ShippingAdress_City = "ShippingAdress->City";
        public static readonly string MappingKey_ShippingAdress_Province = "ShippingAdress->Province";
        public static readonly string MappingKey_ShippingAdress_Country = "ShippingAdress->Country";
        public static readonly string MappingKey_ShippingAdress_County = "ShippingAdress->County";
        public static readonly string MappingKey_ShippingAdress_ZipCode = "ShippingAdress->ZipCode";
        public static readonly string MappingKey_ShippingAdress_PhoneNumber = "ShippingAdress->PhoneNumber";
        public static readonly string MappingKey_ShippingAdress_EmailAddress = "ShippingAdress->EmailAddress";
        public static readonly string MappingKey_ShippingAdress_FirstName = "ShippingAdress->FirstName";
        public static readonly string MappingKey_ShippingAdress_LastName = "ShippingAdress->LastName";
        public static readonly string MappingKey_BillingAdress_StreetAddress1 = "BillingAdress->StreetAddress1";
        public static readonly string MappingKey_BillingAdress_StreetAddress2 = "BillingAdress->StreetAddress2";
        public static readonly string MappingKey_BillingAdress_City = "BillingAdress->City";
        public static readonly string MappingKey_BillingAdress_Province = "BillingAdress->Province";
        public static readonly string MappingKey_BillingAdress_Country = "BillingAdress->Country";
        public static readonly string MappingKey_BillingAdress_County = "BillingAdress->County";
        public static readonly string MappingKey_BillingAdress_ZipCode = "BillingAdress->ZipCode";
        public static readonly string MappingKey_BillingAdress_PhoneNumber = "BillingAdress->PhoneNumber";
        public static readonly string MappingKey_BillingAdress_EmailAddress = "BillingAdress->EmailAddress";
        public static readonly string MappingKey_BillingAdress_FirstName = "BillingAdress->FirstName";
        public static readonly string MappingKey_BillingAdress_LastName = "BillingAdress->LastName";
        public static readonly string MappingKey_IsBillingAddressSameAsShippingAddress = "IsBillingAddressSameAsShippingAddress";



        public static OperationCallResult<AppSecurityUserExDto> ESitePartnerUserThirdPartAccountLoginPostProcess(string email, int? newUserPartnerType, int? eSiteId, string postEmailActivationRedirectUrl, string timeZoneInfoToken)
        {
            OperationCallResult<AppSecurityUserExDto> operationCallResult = new OperationCallResult<AppSecurityUserExDto>();
            operationCallResult.ValidationResult = new ValidationResult();

            if (!string.IsNullOrWhiteSpace(email))
            {
                AppSecurityUserEntity existUserEntity = AppSecurityUserBL.FindUserEntityByEmailAddress(email);

                if (existUserEntity != null)
                {
                    var saasUserDto = AppSecurityUserBL.RetrieveOneAppSecurityUserExDto(existUserEntity.UserId);
                    saasUserDto.IsNewCompanyUser = false;
                    operationCallResult.Object = saasUserDto;
                }
                else
                {
                    var createUserResult = CreateESitePartnerUserByEmail(email, newUserPartnerType, eSiteId, postEmailActivationRedirectUrl, timeZoneInfoToken);
                    if (createUserResult.IsSuccessfulWithResult)
                    {
                        createUserResult.Object.IsNewCompanyUser = true;
                        operationCallResult.Object = createUserResult.Object;
                    }
                }
            }

            if (!operationCallResult.IsSuccessfulWithResult)
            {
                if (!operationCallResult.ValidationResult.HasErrors)
                {
                    operationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppEshopBagDto), "App_EStoreUserRegistration_AccessUserAccountError", ValidationItemType.Error, "Access User Account Failed"));
                }
            }

            return operationCallResult;
        }

        private static void AssignEshotBagFirstAndLastNameFromUserName(string username, AppEshopBagDto appEshopBagDto)
        {
            if (!string.IsNullOrEmpty(username))
            {
                string[] firstAndLastName = username.Split(' ');

                appEshopBagDto.ShippingAdress.FirstName = firstAndLastName[0];

                if (firstAndLastName.Length >= 2)
                {
                    appEshopBagDto.ShippingAdress.LastName = firstAndLastName[1];
                }
            }
        }



        //public static OperationCallResult<AppSecurityUserExDto> ESitePartnerUserRegistration(AppSecurityUserExDto aAppSecurityUserExDto)
        //{
        //    OperationCallResult<AppSecurityUserExDto> operationCallResult = new OperationCallResult<AppSecurityUserExDto>();

        //    aAppSecurityUserExDto.UserName = aAppSecurityUserExDto.LoginName;

        //    // *** Must be Vendor or Customer            
        //    aAppSecurityUserExDto.DomainId = aAppSecurityUserExDto.NewUserPartnerType.Value;

        //    operationCallResult.ValidationResult.Merge(AppSecurityUserBL.IsUserLoginNameUnique(aAppSecurityUserExDto.LoginName, null));

        //    if (operationCallResult.ValidationResult.HasErrors)
        //    {
        //        return operationCallResult;
        //    }

        //    operationCallResult.ValidationResult.Merge(AppSecurityUserBL.IsUserEmailUnique(aAppSecurityUserExDto.Email, null));

        //    if (operationCallResult.ValidationResult.HasErrors)
        //    {
        //        return operationCallResult;
        //    }

        //    AppEsiteExDto eSiteExDto = AppEsiteConfigBL.RetrieveOneAppEsiteExDto(aAppSecurityUserExDto.RegisterFromEsiteId.Value);

        //    int? customerId = FindEsiteExistingCustomerIdByEmail(aAppSecurityUserExDto.Email, eSiteExDto);

        //    if (!customerId.HasValue)
        //    {
        //        AppEshopBagDto appEshopBagDto = new AppEshopBagDto();
        //        appEshopBagDto.ShippingAdress = new AppEshopShippingAdressDto();
        //        appEshopBagDto.ShippingAdress.FirstName = aAppSecurityUserExDto.LoginName;
        //        appEshopBagDto.ShippingAdress.EmailAddress = aAppSecurityUserExDto.Email;
        //        customerId = CreateNewGuestCustomerByDataTransfer(appEshopBagDto, eSiteExDto);
        //    }

        //    if (customerId.HasValue)
        //    {
        //        // aAppSecurityUserExDto.NewBusinessAccountId = customerId.Value;
        //        OperationCallResult<AppSecurityUserExDto> saveUserResult = AppSaasAccountUserBL.CreateSaasPartnerEndUser(aAppSecurityUserExDto, customerId.Value);

        //        if (saveUserResult.IsSuccessfulWithResult)
        //        {
        //            operationCallResult.Object = saveUserResult.Object;
        //        }
        //        else
        //        {
        //            operationCallResult.ValidationResult = saveUserResult.ValidationResult;
        //        }

        //    }
        //    else
        //    {
        //        operationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppEshopBagDto), "App_EStoreUserRegistration_AccessUserAccountError", ValidationItemType.Error, "Access User Account Failed"));
        //    }

        //    return operationCallResult;
        //}


        public static OperationCallResult<AppSecurityUserExDto> EStoreUserRegistration(AppSecurityUserExDto aAppSecurityUserExDto)
        {
            OperationCallResult<AppSecurityUserExDto> toReturn = new OperationCallResult<AppSecurityUserExDto>();

            if (string.IsNullOrWhiteSpace(aAppSecurityUserExDto.LoginName))
            {
                aAppSecurityUserExDto.LoginName = aAppSecurityUserExDto.Email;
            }

            if (string.IsNullOrWhiteSpace(aAppSecurityUserExDto.UserName))
            {
                aAppSecurityUserExDto.UserName = aAppSecurityUserExDto.LoginName;
            }

            //aAppSecurityUserExDto.DomainId = (int)EmAppUserType.Customer;


            aAppSecurityUserExDto.NewUserPartnerType = aAppSecurityUserExDto.DomainId;

            AppEshopBagDto appEshopBagDto = new AppEshopBagDto();
            appEshopBagDto.ESiteId = 1;
            appEshopBagDto.ShippingAdress = new AppEshopShippingAdressDto();
            appEshopBagDto.BillingAdress = new AppEshopShippingAdressDto();

            AssignEshotBagFirstAndLastNameFromUserName(aAppSecurityUserExDto.UserName, appEshopBagDto);


            appEshopBagDto.ShippingAdress.EmailAddress = aAppSecurityUserExDto.Email;
            appEshopBagDto.ShippingAdress.PhoneNumber = aAppSecurityUserExDto.Phone;

            if (aAppSecurityUserExDto.RegisterFromEsiteId.HasValue)
            {
                appEshopBagDto.ESiteId = aAppSecurityUserExDto.RegisterFromEsiteId.Value;
            }

            AppEsiteExDto eSiteExDto = AppEsiteConfigBL.RetrieveOneAppEsiteExDto(appEshopBagDto.ESiteId);
            int? partnerId = null;

            bool isInPartnerAdminRole = false;

            if (aAppSecurityUserExDto.NewUserPartnerType == (int)EmAppUserType.Customer)
            {
                if (!string.IsNullOrWhiteSpace(eSiteExDto.CustomerInfoDbtableName)
                    && !string.IsNullOrWhiteSpace(eSiteExDto.CustomerInfoCustomerIdDbfieldName)
                    && !string.IsNullOrWhiteSpace(eSiteExDto.CustomerInfoEmailDbfieldName)
                    && eSiteExDto.CustomerInfoDataModelId.HasValue
                    && eSiteExDto.CustomerInfoDataTransferId.HasValue)
                {
                    partnerId = FindEsiteExistingCustomerIdByEmail(appEshopBagDto.ShippingAdress.EmailAddress, eSiteExDto);
                    if (!partnerId.HasValue)
                    {
                        partnerId = CreateNewGuestCustomerByDataTransfer(appEshopBagDto, eSiteExDto);
                        isInPartnerAdminRole = true;
                    }
                }
                //else
                //{
                //    partnerId = CreateSimplePartnerByUserInfo(aAppSecurityUserExDto);
                //}
            }
            else if (aAppSecurityUserExDto.NewUserPartnerType == (int)EmAppUserType.Supplier)
            {
                int? supplierDataTransferId = ControlTypeValueConverter.ConvertValueToInt(eSiteExDto.SupplierInfoDataTransferId);

                if (!string.IsNullOrWhiteSpace(eSiteExDto.SupplierInfoDbtableName)
                   && !string.IsNullOrWhiteSpace(eSiteExDto.SupplierInfoIdDbfieldName)
                   && !string.IsNullOrWhiteSpace(eSiteExDto.SupplierInfoEmailDbfieldName)
                   && eSiteExDto.SupplierInfoDataModelId.HasValue
                   && supplierDataTransferId.HasValue)
                {
                    partnerId = FindEsiteExistingSupplierIdByEmail(appEshopBagDto.ShippingAdress.EmailAddress, eSiteExDto);
                    if (!partnerId.HasValue)
                    {
                        partnerId = CreateNewSupplierByDataTransfer(appEshopBagDto, eSiteExDto);
                        isInPartnerAdminRole = true;
                    }
                }
                //else
                //{
                //    partnerId = CreateSimplePartnerByUserInfo(aAppSecurityUserExDto);
                //}
            }
            else if (aAppSecurityUserExDto.NewUserPartnerType == (int)EmAppUserType.ClientAgent)
            {
                // To Do Agent

            }
            else if (aAppSecurityUserExDto.NewUserPartnerType == (int)EmAppUserType.SupplierAgent)
            {
                // To Do Agent

            }


            //if (partnerId.HasValue)
            //{
            // aAppSecurityUserExDto.NewBusinessAccountId = customerId.Value;
            OperationCallResult<AppSecurityUserExDto> saveUserResult = AppSaasAccountUserBL.CreateUserForExistingPartner(aAppSecurityUserExDto, partnerId, isInPartnerAdminRole);

            if (saveUserResult.IsSuccessfulWithResult)
            {
                toReturn.Object = saveUserResult.Object;
                //toReturn.Object.NewBusinessAccountId = partnerId;
            }
            else
            {
                toReturn.ValidationResult = saveUserResult.ValidationResult;
            }

            //}
            //else
            //{
            //    toReturn.ValidationResult.Items.Add(new ValidationItem(typeof(AppEshopBagDto), "App_EStoreUserRegistration_CreateCustomerAccountError", ValidationItemType.Error, "Create Customer Account Failed"));
            //}

            return toReturn;
        }

        //private static int? CreateSimplePartnerByUserInfo(AppSecurityUserExDto aAppSecurityUserExDto)
        //{
        //    int? partnerId = null;


        //    AppBusinessPartnerExDto newPartner = AppSaasAccountUserBL.CreatePartnerFromUserDto(aAppSecurityUserExDto.UserName, aAppSecurityUserExDto.NewUserPartnerType,
        //                            (int)ServerContext.Instance.CurrentCompanyId, null);

        //    if (newPartner != null && newPartner.Id != null)
        //    {
        //        partnerId = ControlTypeValueConverter.ConvertValueToInt(newPartner.Id);
        //    }

        //    return partnerId;
        //}

        public static OperationCallResult<AppEshopBagDto> ProcessPaymentSuccessTransaction(AppEshopBagDto appEshopBagDto)
        {
            OperationCallResult<AppEshopBagDto> operationCallResult = new OperationCallResult<AppEshopBagDto>();
            ValidationResult validationResult = new ValidationResult();
            operationCallResult.ValidationResult = validationResult;

            if (appEshopBagDto != null && appEshopBagDto.OrderId.HasValue)
            {
                int orderId = appEshopBagDto.OrderId.Value;

                AppEsiteExDto eSiteExDto = AppEsiteConfigBL.RetrieveOneAppEsiteExDto(appEshopBagDto.ESiteId);

                if (eSiteExDto != null)
                {
                    if (eSiteExDto.OrderDataModelId.HasValue && eSiteExDto.PaymentSuccessfulPostActionActionId.HasValue)
                    {
                        appEshopBagDto.OrderDataModelId = eSiteExDto.OrderDataModelId.Value;
                        AppMasterDetailDto formDataDto = AppMasterDetailFormDataLoadBL.GetMasterDetailFormData(eSiteExDto.OrderDataModelId.Value, orderId);

                        if (formDataDto != null)
                        {
                            if (eSiteExDto.SaveOrderPostActionId.HasValue)
                            {
                                formDataDto.TransactionCommandId = eSiteExDto.PaymentSuccessfulPostActionActionId.Value;
                                var commandResult = AppTransactionCommandBL.ExecuteTransactionRootCommand(formDataDto);

                                if (commandResult.IsSuccessfulWithResult)
                                {

                                    RefreshEshopBadDtoFromOrderData(appEshopBagDto, formDataDto.RootPrimaryKeyValue, eSiteExDto.OrderDataTransferId.Value);
                                    appEshopBagDto.InvoiceDataModelId = eSiteExDto.InvoiceDataModelId;
                                    operationCallResult.Object = appEshopBagDto;
                                }
                                else
                                {
                                    validationResult.Items.Add(new ValidationItem(typeof(AppEshopBagDto), "App_ProcessPaymentSuccessTransaction_SaveError", ValidationItemType.Error, "Complete Order Failed"));
                                }
                            }
                        }
                    }
                }
            }

            return operationCallResult;
        }

        public static OperationCallResult<AppEshopBagDto> SubmitGuestUserAddressInfoAndEmail(AppEshopBagDto appEshopBagDto)
        {
            OperationCallResult<AppEshopBagDto> operationCallResult = new OperationCallResult<AppEshopBagDto>();
            ValidationResult validationResult = new ValidationResult();
            operationCallResult.ValidationResult = validationResult;

            if (appEshopBagDto != null)
            {
                if (appEshopBagDto.ShippingAdress != null && !string.IsNullOrWhiteSpace(appEshopBagDto.ShippingAdress.EmailAddress))
                {
                    AppEsiteExDto eSiteExDto = AppEsiteConfigBL.RetrieveOneAppEsiteExDto(appEshopBagDto.ESiteId);

                    if (eSiteExDto != null)
                    {
                        if (!string.IsNullOrWhiteSpace(eSiteExDto.CustomerInfoDbtableName)
                            && !string.IsNullOrWhiteSpace(eSiteExDto.CustomerInfoCustomerIdDbfieldName)
                            && !string.IsNullOrWhiteSpace(eSiteExDto.CustomerInfoEmailDbfieldName))
                        {
                            int? customerId = FindEsiteExistingCustomerIdByEmail(appEshopBagDto.ShippingAdress.EmailAddress, eSiteExDto);

                            if (!customerId.HasValue)
                            {
                                customerId = CreateNewGuestCustomerByDataTransfer(appEshopBagDto, eSiteExDto);
                            }

                            if (customerId.HasValue)
                            {
                                appEshopBagDto.CustomerDataModelId = eSiteExDto.CustomerInfoDataModelId;
                                appEshopBagDto.CustomerId = customerId;

                                var createOrderResult = CreateOrderFromShopBag(appEshopBagDto);

                                if (createOrderResult.IsSuccessfulWithResult)
                                {
                                    operationCallResult.Object = createOrderResult.Object;
                                }
                                else
                                {
                                    validationResult.Merge(createOrderResult.ValidationResult);
                                }
                            }
                        }
                    }
                }
            }


            return operationCallResult;
        }


        public static OperationCallResult<AppEshopBagDto> SubmitLoggedInUserAddressInfo(AppEshopBagDto appEshopBagDto)
        {
            OperationCallResult<AppEshopBagDto> operationCallResult = new OperationCallResult<AppEshopBagDto>();
            ValidationResult validationResult = new ValidationResult();
            operationCallResult.ValidationResult = validationResult;

            if (appEshopBagDto != null && appEshopBagDto.CustomerId.HasValue)
            {
                int customerId = appEshopBagDto.CustomerId.Value;

                if (appEshopBagDto.ShippingAdress != null)
                {
                    var updateCustomerInfoResult = UpdateCurrentUserCustomerAddressInfo(appEshopBagDto);

                    if (updateCustomerInfoResult.IsSuccessful)
                    {
                        var createOrderResult = CreateOrderFromShopBag(appEshopBagDto);

                        if (createOrderResult.IsSuccessfulWithResult)
                        {
                            operationCallResult.Object = createOrderResult.Object;
                        }
                        else
                        {
                            validationResult.Merge(createOrderResult.ValidationResult);
                        }
                    }
                }
            }


            return operationCallResult;
        }


        public static OperationCallResult<AppEshopBagDto> GetCurrentLoginUserCustomerInfo(AppEshopBagDto appEshopBagDto)
        {
            OperationCallResult<AppEshopBagDto> operationCallResult = new OperationCallResult<AppEshopBagDto>();
            ValidationResult validationResult = new ValidationResult();
            operationCallResult.ValidationResult = validationResult;

            //string curretnCustoemrId = ServerContext.Instance.CurrnetClientIdentity.CurrentPartnerId as string;

            appEshopBagDto.CustomerId = ServerContext.Instance.CurrnetClientIdentity.CurrentPartnerId;


            if (appEshopBagDto != null && appEshopBagDto.CustomerId.HasValue)
            {
                appEshopBagDto.ShippingAdress = new AppEshopShippingAdressDto();
                appEshopBagDto.BillingAdress = new AppEshopShippingAdressDto();
                appEshopBagDto.IsBillingAddressSameAsShippingAddress = true;

                int? customerId = appEshopBagDto.CustomerId;

                AppEsiteExDto eSiteExDto = AppEsiteConfigBL.RetrieveOneAppEsiteExDto(appEshopBagDto.ESiteId);

                if (eSiteExDto != null)
                {
                    if (eSiteExDto.CustomerInfoDataModelId.HasValue && eSiteExDto.CustomerInfoDataTransferId.HasValue)
                    {
                        JObject appEshopBagJObj = AppMasterDetailFormDataLoadBL.ProcessFormToApiObjDataTransfer(eSiteExDto.CustomerInfoDataTransferId.Value, customerId.Value);

                        if (appEshopBagJObj != null)
                        {
                            AppEshopBagDto newBagData = JsonConvert.DeserializeObject<AppEshopBagDto>(appEshopBagJObj.ToString());
                            if (newBagData != null)
                            {
                                if (newBagData.ShippingAdress != null)
                                {
                                    appEshopBagDto.ShippingAdress = newBagData.ShippingAdress;

                                    if (newBagData.BillingAdress != null
                                            && !string.IsNullOrWhiteSpace(newBagData.BillingAdress.StreetAddress1))
                                    {
                                        //appEshopBagDto.IsBillingAddressSameAsShippingAddress = false;
                                        appEshopBagDto.BillingAdress = newBagData.BillingAdress;
                                    }

                                    operationCallResult.Object = appEshopBagDto;
                                }

                            }
                        }
                    }
                }

            }


            return operationCallResult;
        }

        

        public static OperationCallResult<bool> UpdateCurrentUserCustomerAddressInfo(AppEshopBagDto appEshopBagDto)
        {
            OperationCallResult<bool> operationCallResult = new OperationCallResult<bool>();
            ValidationResult validationResult = new ValidationResult();
            operationCallResult.ValidationResult = validationResult;

            if (appEshopBagDto != null && appEshopBagDto.CustomerId.HasValue)
            {
                if (appEshopBagDto.ShippingAdress != null)
                {
                    AppEsiteExDto eSiteExDto = AppEsiteConfigBL.RetrieveOneAppEsiteExDto(appEshopBagDto.ESiteId);

                    if (eSiteExDto != null)
                    {
                        var updateResult = UpdateCustomerAddressByDataTransfer(appEshopBagDto, eSiteExDto);


                        if (updateResult.IsSuccessfulWithResult)
                        {
                            operationCallResult.Object = true;
                        }
                        else
                        {
                            validationResult.Merge(updateResult.ValidationResult);
                        }
                    }
                }
            }


            return operationCallResult;
        }



        public static OperationCallResult<AppEshopBagDto> CreateOrderFromShopBag(AppEshopBagDto appEshopBagDto)
        {
            OperationCallResult<AppEshopBagDto> operationCallResult = new OperationCallResult<AppEshopBagDto>();
            ValidationResult validationResult = new ValidationResult();
            operationCallResult.ValidationResult = validationResult;

            if (appEshopBagDto != null && appEshopBagDto.CustomerId.HasValue)
            {
                AppEsiteExDto eSiteExDto = AppEsiteConfigBL.RetrieveOneAppEsiteExDto(appEshopBagDto.ESiteId);

                if (eSiteExDto != null)
                {
                    if (eSiteExDto.OrderDataTransferId.HasValue)
                    {
                        JObject appEshopBagJObj = JObject.Parse(JsonConvert.SerializeObject(appEshopBagDto));
                        AppMasterDetailDto newOrderFormDataDto = AppMasterDetailFormDataLoadBL.GetNewFormDataByApiToFormDataTransfer(eSiteExDto.OrderDataTransferId.Value, appEshopBagJObj);

                        if (newOrderFormDataDto != null)
                        {
                            var saveOrderResult = AppMasterDetailFormDataSaveBL.SaveTransactionData(newOrderFormDataDto);

                            if (saveOrderResult.IsSuccessfulWithResult)
                            {
                                var formDataDto = saveOrderResult.Object;
                                if (eSiteExDto.SaveOrderPostActionId.HasValue)
                                {
                                    formDataDto.TransactionCommandId = eSiteExDto.SaveOrderPostActionId.Value;
                                    var commandResult = AppTransactionCommandBL.ExecuteTransactionRootCommand(formDataDto);

                                    if (commandResult.IsSuccessfulWithResult)
                                    {
                                        appEshopBagDto.OrderId = ControlTypeValueConverter.ConvertValueToInt(formDataDto.RootPrimaryKeyValue);
                                        operationCallResult.Object = appEshopBagDto;

                                        RefreshEshopBadDtoFromOrderData(appEshopBagDto, formDataDto.RootPrimaryKeyValue, eSiteExDto.OrderDataTransferId.Value);
                                    }
                                    else
                                    {
                                        validationResult.Merge(saveOrderResult.ValidationResult);
                                    }
                                }
                            }
                            else
                            {
                                validationResult.Merge(saveOrderResult.ValidationResult);
                            }
                        }
                    }
                }
            }

            return operationCallResult;
        }



        public static List<AppEshopOrderDto> GetCurrentCustomerOrderList(int? esitId)
        {
            List<AppEshopOrderDto> toReturn = new List<AppEshopOrderDto>();

            AppEsiteExDto eSiteExDto = AppEsiteConfigBL.RetrieveOneAppEsiteExDto(esitId);
            // string curretnCustoemrId = ServerContext.Instance.CurrnetClientIdentity.RuningTimeBusinessAccountId as string;
            int? customerId = ServerContext.Instance.CurrnetClientIdentity.CurrentPartnerId;

            if (eSiteExDto != null && eSiteExDto.CustomerOrderListSearchId.HasValue && customerId.HasValue)
            {
                var aSearchDto = AppSearchBL.RetrieveOneSearchDto(eSiteExDto.CustomerOrderListSearchId.Value, false, false);
                AppSearchViewExDto orderListViewDto = AppSearchViewConfigBL.RetrieveOneAppSearchViewExDto(aSearchDto.DefaultView.Id);

                if (orderListViewDto != null && orderListViewDto.ViewType == (int)EmAppViewType.EShopOrderListView)
                {
                    ReferenceViewDefinitionDto referenceViewDefinitionDto = new ReferenceViewDefinitionDto();
                    referenceViewDefinitionDto.IsMassUpdate = false;
                    referenceViewDefinitionDto.Id = aSearchDto.DefaultView.Id;
                    aSearchDto.ReferenceViewDefinitionDto = referenceViewDefinitionDto;
                    //AppSearchBL.ConvertSearchCriteriaDateFromUTCToClient(aSearchDto);
                    SearchResultDto searchResult = AppSearchBL.RetrieveSearchResult(aSearchDto);

                    if (searchResult != null && !searchResult.SearchResultRowList.IsEmpty())
                    {
                        var resultRowList = searchResult.SearchResultRowList;

                        var orderIdField = orderListViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.EmInternalCodeRegistration.HasValue && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistrationForESiteOrderList.OrderId);
                        var orderNumberField = orderListViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.EmInternalCodeRegistration.HasValue && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistrationForESiteOrderList.OrderNumber);
                        var orderDescriptionField = orderListViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.EmInternalCodeRegistration.HasValue && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistrationForESiteOrderList.OrderDescription);
                        //var orderCustomerIdField = orderListViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.EmInternalCodeRegistration.HasValue && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistrationForESiteOrderList.OrderCustomerId);
                        var orderSubTotalField = orderListViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.EmInternalCodeRegistration.HasValue && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistrationForESiteOrderList.OrderSubTotal);
                        var orderTotalShippingField = orderListViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.EmInternalCodeRegistration.HasValue && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistrationForESiteOrderList.OrderTotalShipping);
                        var orderTotalTaxField = orderListViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.EmInternalCodeRegistration.HasValue && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistrationForESiteOrderList.OrderTotalTax);


                        var orderTotalPriceField = orderListViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.EmInternalCodeRegistration.HasValue && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistrationForESiteOrderList.OrderTotalPrice);
                        var orderInvoiceIdField = orderListViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.EmInternalCodeRegistration.HasValue && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistrationForESiteOrderList.OrderInvoiceId);
                        var orderStatusField = orderListViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.EmInternalCodeRegistration.HasValue && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistrationForESiteOrderList.OrderStatus);
                        var orderShippingStatusField = orderListViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.EmInternalCodeRegistration.HasValue && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistrationForESiteOrderList.OrderShippingStatus);
                        var orderPlacedDataField = orderListViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.EmInternalCodeRegistration.HasValue && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistrationForESiteOrderList.OrderPlacedData);
                        var orderShippedDataField = orderListViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.EmInternalCodeRegistration.HasValue && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistrationForESiteOrderList.OrderShippedData);
                        var orderDeliveredDataField = orderListViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.EmInternalCodeRegistration.HasValue && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistrationForESiteOrderList.OrderDeliveredData);
                        var orderCanceledDataField = orderListViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.EmInternalCodeRegistration.HasValue && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistrationForESiteOrderList.OrderCanceledData);
                        var orderExpectedDeliverDateField = orderListViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.EmInternalCodeRegistration.HasValue && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistrationForESiteOrderList.OrderExpectedDeliverDate);
                        var orderIsBillingAddressSameAsShippingAddressField = orderListViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.EmInternalCodeRegistration.HasValue && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistrationForESiteOrderList.OrderIsBillingAddressSameAsShippingAddress);
                        var orderShippingCountryField = orderListViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.EmInternalCodeRegistration.HasValue && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistrationForESiteOrderList.OrderShippingCountry);
                        var orderShippingProvinceField = orderListViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.EmInternalCodeRegistration.HasValue && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistrationForESiteOrderList.OrderShippingProvince);
                        var orderShippingCityField = orderListViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.EmInternalCodeRegistration.HasValue && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistrationForESiteOrderList.OrderShippingCity);
                        var orderShippingAddress1Field = orderListViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.EmInternalCodeRegistration.HasValue && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistrationForESiteOrderList.OrderShippingAddress1);
                        var orderShippingAddress2Field = orderListViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.EmInternalCodeRegistration.HasValue && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistrationForESiteOrderList.OrderShippingAddress2);
                        var orderShippingZipCodeField = orderListViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.EmInternalCodeRegistration.HasValue && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistrationForESiteOrderList.OrderShippingZipCode);
                        var orderShippingFirstNameField = orderListViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.EmInternalCodeRegistration.HasValue && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistrationForESiteOrderList.OrderShippingFirstName);
                        var orderShippingLastNameField = orderListViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.EmInternalCodeRegistration.HasValue && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistrationForESiteOrderList.OrderShippingLastName);
                        var orderShippingEmailField = orderListViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.EmInternalCodeRegistration.HasValue && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistrationForESiteOrderList.OrderShippingEmail);
                        var orderShippingPhoneNumberField = orderListViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.EmInternalCodeRegistration.HasValue && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistrationForESiteOrderList.OrderShippingPhoneNumber);
                        var orderBillingCountryField = orderListViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.EmInternalCodeRegistration.HasValue && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistrationForESiteOrderList.OrderBillingCountry);
                        var orderBillingProvinceField = orderListViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.EmInternalCodeRegistration.HasValue && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistrationForESiteOrderList.OrderBillingProvince);
                        var orderBillingCityField = orderListViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.EmInternalCodeRegistration.HasValue && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistrationForESiteOrderList.OrderBillingCity);
                        var orderBillingAddress1Field = orderListViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.EmInternalCodeRegistration.HasValue && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistrationForESiteOrderList.OrderBillingAddress1);
                        var orderBillingAddress2Field = orderListViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.EmInternalCodeRegistration.HasValue && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistrationForESiteOrderList.OrderBillingAddress2);
                        var orderBillingZipCodeField = orderListViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.EmInternalCodeRegistration.HasValue && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistrationForESiteOrderList.OrderBillingZipCode);
                        var orderBillingFirstNameField = orderListViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.EmInternalCodeRegistration.HasValue && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistrationForESiteOrderList.OrderBillingFirstName);
                        var orderBillingLastNameField = orderListViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.EmInternalCodeRegistration.HasValue && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistrationForESiteOrderList.OrderBillingLastName);
                        var orderBillingEmailField = orderListViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.EmInternalCodeRegistration.HasValue && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistrationForESiteOrderList.OrderBillingEmail);
                        var orderBillingPhoneNumberField = orderListViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.EmInternalCodeRegistration.HasValue && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistrationForESiteOrderList.OrderBillingPhoneNumber);

                        var orderItemIdField = orderListViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.EmInternalCodeRegistration.HasValue && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistrationForESiteOrderList.OrderItemId);
                        var orderItemDescription1Field = orderListViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.EmInternalCodeRegistration.HasValue && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistrationForESiteOrderList.OrderItemDescription1);
                        var orderItemDescription2Field = orderListViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.EmInternalCodeRegistration.HasValue && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistrationForESiteOrderList.OrderItemDescription2);
                        var orderItemDescription3Field = orderListViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.EmInternalCodeRegistration.HasValue && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistrationForESiteOrderList.OrderItemDescription3);
                        var orderItemDescription4Field = orderListViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.EmInternalCodeRegistration.HasValue && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistrationForESiteOrderList.OrderItemDescription4);
                        var orderItemDescription5Field = orderListViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.EmInternalCodeRegistration.HasValue && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistrationForESiteOrderList.OrderItemDescription5);
                        var orderItemImageIdField = orderListViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.EmInternalCodeRegistration.HasValue && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistrationForESiteOrderList.OrderItemImageId);
                        var orderItemPriceField = orderListViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.EmInternalCodeRegistration.HasValue && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistrationForESiteOrderList.OrderItemPrice);
                        var orderItemQtyField = orderListViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.EmInternalCodeRegistration.HasValue && o.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistrationForESiteOrderList.OrderItemQty);


                        if (orderIdField != null)
                        {
                            Dictionary<string, List<StaticSearchResultRowJsonDto>> dictOrderIdAndOrderList = resultRowList.Where(o => o[(int)orderIdField.Id] != null)
                                 .GroupBy(o => o[(int)orderIdField.Id]).ToDictionary(o => o.Key.ToString(), g => g.ToList());

                            foreach (string orderIdKey in dictOrderIdAndOrderList.Keys)
                            {
                                var firstRowOfGroup = dictOrderIdAndOrderList[orderIdKey].First();

                                AppEshopOrderDto eshopOrderDto = new AppEshopOrderDto();
                                eshopOrderDto.ShippingAdress = new AppEshopShippingAdressDto();
                                eshopOrderDto.BillingAdress = new AppEshopShippingAdressDto();
                                eshopOrderDto.OrderItemList = new List<AppEshopBagItemDto>();

                                eshopOrderDto.OrderId = ControlTypeValueConverter.ConvertValueToInt(orderIdKey);
                                eshopOrderDto.CustomerId = customerId;


                                if (orderNumberField != null)
                                {
                                    eshopOrderDto.OrderNumber = firstRowOfGroup[(int)orderNumberField.Id] as string;
                                }

                                if (orderDescriptionField != null)
                                {
                                    eshopOrderDto.OrderDescription = firstRowOfGroup[(int)orderDescriptionField.Id] as string;
                                }

                                if (orderSubTotalField != null)
                                {
                                    eshopOrderDto.Subtotal = ControlTypeValueConverter.ConvertValueToDecimal(firstRowOfGroup[(int)orderSubTotalField.Id]);
                                }

                                if (orderTotalShippingField != null)
                                {
                                    eshopOrderDto.ShippingCost = ControlTypeValueConverter.ConvertValueToDecimal(firstRowOfGroup[(int)orderTotalShippingField.Id]);
                                }

                                if (orderTotalTaxField != null)
                                {
                                    eshopOrderDto.TotalTax = ControlTypeValueConverter.ConvertValueToDecimal(firstRowOfGroup[(int)orderTotalTaxField.Id]);
                                }

                                if (orderTotalPriceField != null)
                                {
                                    eshopOrderDto.Total = ControlTypeValueConverter.ConvertValueToDecimal(firstRowOfGroup[(int)orderTotalPriceField.Id]);
                                }

                                if (orderInvoiceIdField != null)
                                {
                                    eshopOrderDto.InvoiceId = ControlTypeValueConverter.ConvertValueToInt(firstRowOfGroup[(int)orderInvoiceIdField.Id]);
                                }

                                if (orderStatusField != null)
                                {
                                    eshopOrderDto.OrderStatus = ControlTypeValueConverter.ConvertValueToInt(firstRowOfGroup[(int)orderStatusField.Id]);
                                }

                                if (orderShippingStatusField != null)
                                {
                                    eshopOrderDto.ShippingStatus = ControlTypeValueConverter.ConvertValueToInt(firstRowOfGroup[(int)orderShippingStatusField.Id]);
                                }

                                if (orderPlacedDataField != null)
                                {
                                    eshopOrderDto.OrderPlacedDate = ControlTypeValueConverter.ConvertValueToDate(firstRowOfGroup[(int)orderPlacedDataField.Id]);
                                }

                                if (orderShippedDataField != null)
                                {
                                    eshopOrderDto.OrderShipDate = ControlTypeValueConverter.ConvertValueToDate(firstRowOfGroup[(int)orderShippedDataField.Id]);
                                }

                                if (orderCanceledDataField != null)
                                {
                                    eshopOrderDto.OrderCanceledDate = ControlTypeValueConverter.ConvertValueToDate(firstRowOfGroup[(int)orderCanceledDataField.Id]);
                                }

                                if (orderDeliveredDataField != null)
                                {
                                    eshopOrderDto.OrderDeliveredDate = ControlTypeValueConverter.ConvertValueToDate(firstRowOfGroup[(int)orderDeliveredDataField.Id]);
                                }

                                if (orderExpectedDeliverDateField != null)
                                {
                                    eshopOrderDto.OrderExpectedDeliverDate = ControlTypeValueConverter.ConvertValueToDate(firstRowOfGroup[(int)orderExpectedDeliverDateField.Id]);
                                }

                                if (orderIsBillingAddressSameAsShippingAddressField != null)
                                {
                                    eshopOrderDto.IsBillingAddressSameAsShippingAddress = ControlTypeValueConverter.ConvertValueToBoolean(firstRowOfGroup[(int)orderIsBillingAddressSameAsShippingAddressField.Id]);
                                }

                                if (orderShippingCountryField != null)
                                {
                                    eshopOrderDto.ShippingAdress.Country = firstRowOfGroup[(int)orderShippingCountryField.Id] as string;
                                }

                                if (orderShippingProvinceField != null)
                                {
                                    eshopOrderDto.ShippingAdress.Province = firstRowOfGroup[(int)orderShippingProvinceField.Id] as string;
                                }

                                if (orderShippingCityField != null)
                                {
                                    eshopOrderDto.ShippingAdress.City = firstRowOfGroup[(int)orderShippingCityField.Id] as string;
                                }

                                if (orderShippingAddress1Field != null)
                                {
                                    eshopOrderDto.ShippingAdress.StreetAddress1 = firstRowOfGroup[(int)orderShippingAddress1Field.Id] as string;
                                }

                                if (orderShippingAddress2Field != null)
                                {
                                    eshopOrderDto.ShippingAdress.StreetAddress2 = firstRowOfGroup[(int)orderShippingAddress2Field.Id] as string;
                                }

                                if (orderShippingZipCodeField != null)
                                {
                                    eshopOrderDto.ShippingAdress.ZipCode = firstRowOfGroup[(int)orderShippingZipCodeField.Id] as string;
                                }

                                if (orderShippingFirstNameField != null)
                                {
                                    eshopOrderDto.ShippingAdress.FirstName = firstRowOfGroup[(int)orderShippingFirstNameField.Id] as string;
                                }

                                if (orderShippingLastNameField != null)
                                {
                                    eshopOrderDto.ShippingAdress.LastName = firstRowOfGroup[(int)orderShippingLastNameField.Id] as string;
                                }

                                if (orderShippingEmailField != null)
                                {
                                    eshopOrderDto.ShippingAdress.EmailAddress = firstRowOfGroup[(int)orderShippingEmailField.Id] as string;
                                }

                                if (orderShippingPhoneNumberField != null)
                                {
                                    eshopOrderDto.ShippingAdress.PhoneNumber = firstRowOfGroup[(int)orderShippingPhoneNumberField.Id] as string;
                                }


                                if (orderBillingCountryField != null)
                                {
                                    eshopOrderDto.BillingAdress.Country = firstRowOfGroup[(int)orderBillingCountryField.Id] as string;
                                }

                                if (orderBillingProvinceField != null)
                                {
                                    eshopOrderDto.BillingAdress.Province = firstRowOfGroup[(int)orderBillingProvinceField.Id] as string;
                                }

                                if (orderBillingCityField != null)
                                {
                                    eshopOrderDto.BillingAdress.City = firstRowOfGroup[(int)orderBillingCityField.Id] as string;
                                }

                                if (orderBillingAddress1Field != null)
                                {
                                    eshopOrderDto.BillingAdress.StreetAddress1 = firstRowOfGroup[(int)orderBillingAddress1Field.Id] as string;
                                }

                                if (orderBillingAddress2Field != null)
                                {
                                    eshopOrderDto.BillingAdress.StreetAddress2 = firstRowOfGroup[(int)orderBillingAddress2Field.Id] as string;
                                }

                                if (orderBillingZipCodeField != null)
                                {
                                    eshopOrderDto.BillingAdress.ZipCode = firstRowOfGroup[(int)orderBillingZipCodeField.Id] as string;
                                }

                                if (orderBillingFirstNameField != null)
                                {
                                    eshopOrderDto.BillingAdress.FirstName = firstRowOfGroup[(int)orderBillingFirstNameField.Id] as string;
                                }

                                if (orderBillingLastNameField != null)
                                {
                                    eshopOrderDto.BillingAdress.LastName = firstRowOfGroup[(int)orderBillingLastNameField.Id] as string;
                                }

                                if (orderBillingEmailField != null)
                                {
                                    eshopOrderDto.BillingAdress.EmailAddress = firstRowOfGroup[(int)orderBillingEmailField.Id] as string;
                                }

                                if (orderBillingPhoneNumberField != null)
                                {
                                    eshopOrderDto.BillingAdress.PhoneNumber = firstRowOfGroup[(int)orderBillingPhoneNumberField.Id] as string;
                                }

                                foreach (var orderItemRow in dictOrderIdAndOrderList[orderIdKey])
                                {
                                    AppEshopBagItemDto orderItemDto = new AppEshopBagItemDto();
                                    orderItemDto.ProductDisplay = string.Empty;

                                    if (orderItemIdField != null)
                                    {
                                        orderItemDto.DetailId = orderItemRow[(int)orderItemIdField.Id] as string;
                                    }

                                    if (orderItemDescription1Field != null)
                                    {
                                        orderItemDto.ProductDisplay += orderItemRow[(int)orderItemDescription1Field.Id] as string + "\n";
                                    }

                                    if (orderItemDescription2Field != null)
                                    {
                                        orderItemDto.ProductDisplay += orderItemRow[(int)orderItemDescription2Field.Id] as string + "\n";
                                    }

                                    if (orderItemDescription3Field != null)
                                    {
                                        orderItemDto.ProductDisplay += orderItemRow[(int)orderItemDescription3Field.Id] as string + "\n";
                                    }

                                    if (orderItemDescription4Field != null)
                                    {
                                        orderItemDto.ProductDisplay += orderItemRow[(int)orderItemDescription4Field.Id] as string + "\n";
                                    }

                                    if (orderItemDescription5Field != null)
                                    {
                                        orderItemDto.ProductDisplay += orderItemRow[(int)orderItemDescription5Field.Id] as string + "\n";
                                    }

                                    if (orderItemImageIdField != null)
                                    {
                                        orderItemDto.ImageUrl += orderItemRow[(int)orderItemImageIdField.Id] as string;
                                    }

                                    if (orderItemPriceField != null)
                                    {
                                        orderItemDto.Price = ControlTypeValueConverter.ConvertValueToDecimal(orderItemRow[(int)orderItemPriceField.Id]);
                                    }

                                    if (orderItemQtyField != null)
                                    {
                                        orderItemDto.SelectedQuantity = ControlTypeValueConverter.ConvertValueToDecimal(orderItemRow[(int)orderItemQtyField.Id]);
                                    }

                                    eshopOrderDto.OrderItemList.Add(orderItemDto);

                                }

                                toReturn.Add(eshopOrderDto);
                            }
                        }
                    }
                }
            }



            return toReturn.Where(o => o.OrderPlacedDate.HasValue && o.InvoiceId.HasValue).OrderByDescending(o => o.OrderPlacedDate).ToList();
        }

        private static int? FindEsiteExistingCustomerIdByEmail(string email, AppEsiteExDto appEsiteExDto)
        {
            int? customerId = null;

            if (!string.IsNullOrWhiteSpace(appEsiteExDto.CustomerInfoDbtableName)
                && !string.IsNullOrWhiteSpace(appEsiteExDto.CustomerInfoCustomerIdDbfieldName)
                && !string.IsNullOrWhiteSpace(appEsiteExDto.CustomerInfoEmailDbfieldName))
            {
                DataTable dataTable = new DataTable();
                string queryFindExistCustomer = string.Format(@"select top 1 {0} from {1} where {2} = '{3}'",
                    appEsiteExDto.CustomerInfoCustomerIdDbfieldName,
                    appEsiteExDto.CustomerInfoDbtableName,
                    appEsiteExDto.CustomerInfoEmailDbfieldName,
                    email);

                using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
                {
                    dataTable = adpater.ExecuteDataTableRetrievalQuery(queryFindExistCustomer, new List<System.Data.SqlClient.SqlParameter>());

                    if (dataTable.Rows.Count == 1)
                    {
                        customerId = ControlTypeValueConverter.ConvertValueToInt(dataTable.Rows[0][appEsiteExDto.CustomerInfoCustomerIdDbfieldName]);
                    }
                }

            }

            return customerId;
        }

        private static int? FindEsiteExistingSupplierIdByEmail(string email, AppEsiteExDto appEsiteExDto)
        {
            int? SupplierId = null;

            if (!string.IsNullOrWhiteSpace(appEsiteExDto.SupplierInfoDbtableName)
                && !string.IsNullOrWhiteSpace(appEsiteExDto.SupplierInfoIdDbfieldName)
                && !string.IsNullOrWhiteSpace(appEsiteExDto.SupplierInfoEmailDbfieldName))
            {
                DataTable dataTable = new DataTable();
                string queryFindExistSupplier = string.Format(@"select top 1 {0} from {1} where {2} = '{3}'",
                    appEsiteExDto.SupplierInfoIdDbfieldName,
                    appEsiteExDto.SupplierInfoDbtableName,
                    appEsiteExDto.SupplierInfoEmailDbfieldName,
                    email);

                using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
                {
                    dataTable = adpater.ExecuteDataTableRetrievalQuery(queryFindExistSupplier, new List<System.Data.SqlClient.SqlParameter>());

                    if (dataTable.Rows.Count == 1)
                    {
                        SupplierId = ControlTypeValueConverter.ConvertValueToInt(dataTable.Rows[0][appEsiteExDto.SupplierInfoIdDbfieldName]);
                    }
                }

            }

            return SupplierId;
        }


        private static int? CreateNewGuestCustomerByDataTransfer(AppEshopBagDto appEshopBagDto, AppEsiteExDto appEsiteExDto)
        {
            int? customerId = null;
            if (appEsiteExDto.CustomerInfoDataModelId.HasValue && appEsiteExDto.CustomerInfoDataTransferId.HasValue)
            {
                JObject appEshopBagJObj = JObject.Parse(JsonConvert.SerializeObject(appEshopBagDto));

                AppMasterDetailDto newFormDataDto = AppMasterDetailFormDataLoadBL.GetNewFormDataByApiToFormDataTransfer(appEsiteExDto.CustomerInfoDataTransferId.Value, appEshopBagJObj);

                if (newFormDataDto != null)
                {
                    var newCustomerFormDto = AppMasterDetailFormDataSaveBL.SaveTransactionData(newFormDataDto).Object;

                    if (newCustomerFormDto != null && newCustomerFormDto.RootPrimaryKeyValue != null)
                    {
                        customerId = ControlTypeValueConverter.ConvertValueToInt(newCustomerFormDto.RootPrimaryKeyValue);
                    }
                }
            }

            return customerId;
        }

        private static int? CreateNewSupplierByDataTransfer(AppEshopBagDto appEshopBagDto, AppEsiteExDto appEsiteExDto)
        {
            int? supplierId = null;
            int? supplierDataTransferId = ControlTypeValueConverter.ConvertValueToInt(appEsiteExDto.SupplierInfoDataTransferId);

            if (appEsiteExDto.SupplierInfoDataModelId.HasValue && supplierDataTransferId.HasValue)
            {
                JObject appEshopBagJObj = JObject.Parse(JsonConvert.SerializeObject(appEshopBagDto));

                AppMasterDetailDto newFormDataDto = AppMasterDetailFormDataLoadBL.GetNewFormDataByApiToFormDataTransfer(supplierDataTransferId.Value, appEshopBagJObj);

                if (newFormDataDto != null)
                {
                    var newSupplierFormDto = AppMasterDetailFormDataSaveBL.SaveTransactionData(newFormDataDto).Object;

                    if (newSupplierFormDto != null && newSupplierFormDto.RootPrimaryKeyValue != null)
                    {
                        supplierId = ControlTypeValueConverter.ConvertValueToInt(newSupplierFormDto.RootPrimaryKeyValue);
                    }
                }
            }

            return supplierId;
        }

        private static OperationCallResult<AppMasterDetailDto> UpdateCustomerAddressByDataTransfer(AppEshopBagDto appEshopBagDto, AppEsiteExDto appEsiteExDto)
        {
            if (appEshopBagDto.CustomerId.HasValue && appEsiteExDto.CustomerInfoDataModelId.HasValue && appEsiteExDto.CustomerInfoDataTransferId.HasValue)
            {
                int? customerId = appEshopBagDto.CustomerId;

                JObject appEshopBagJObj = JObject.Parse(JsonConvert.SerializeObject(appEshopBagDto));

                AppTransactionDataTransferSettingExDto dataTransferDto = AppTransactionDataTransferSettingBL.RetrieveOneAppTransactionDataTransferSettingExDto(appEsiteExDto.CustomerInfoDataTransferId.Value);

                if (dataTransferDto != null && dataTransferDto.DestinationTransactionId.HasValue && dataTransferDto.DataTransferMappingList != null)
                {
                    AppMasterDetailDto tgtFormData = AppMasterDetailFormDataLoadBL.GetMasterDetailFormData(dataTransferDto.DestinationTransactionId.Value, customerId.Value);

                    if (tgtFormData != null)
                    {
                        List<string> needToUpdateMappingKeys = new List<string>() {
                            MappingKey_ShippingAdress_StreetAddress1,
                            MappingKey_ShippingAdress_StreetAddress2,
                            MappingKey_ShippingAdress_City,
                            MappingKey_ShippingAdress_Province,
                            MappingKey_ShippingAdress_Country,
                            MappingKey_ShippingAdress_County,
                            MappingKey_ShippingAdress_ZipCode,
                            MappingKey_ShippingAdress_PhoneNumber,
                            MappingKey_ShippingAdress_EmailAddress,
                            MappingKey_ShippingAdress_FirstName,
                            MappingKey_ShippingAdress_LastName,
                            MappingKey_BillingAdress_StreetAddress1,
                            MappingKey_BillingAdress_StreetAddress2,
                            MappingKey_BillingAdress_City,
                            MappingKey_BillingAdress_Province,
                            MappingKey_BillingAdress_Country,
                            MappingKey_BillingAdress_County,
                            MappingKey_BillingAdress_ZipCode,
                            MappingKey_BillingAdress_PhoneNumber,
                            MappingKey_BillingAdress_EmailAddress,
                            MappingKey_BillingAdress_FirstName,
                            MappingKey_BillingAdress_LastName,
                        };

                        AppMasterDetailFormDataLoadBL.ModifyFormDataByApiToFormDataTransfer(appEsiteExDto.CustomerInfoDataTransferId.Value, appEshopBagJObj, tgtFormData, needToUpdateMappingKeys);

                        return AppMasterDetailFormDataSaveBL.SaveTransactionData(tgtFormData);
                    }
                }

            }

            OperationCallResult<AppMasterDetailDto> defaultErrorResult = new OperationCallResult<AppMasterDetailDto>();
            defaultErrorResult.ValidationResult = new ValidationResult();
            defaultErrorResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_UpdateCustomerAddressByDataTransfer_SaveError", ValidationItemType.Error, "Update Customer Info Failed"));

            return defaultErrorResult;
        }

        private static void RefreshEshopBadDtoFromOrderData(AppEshopBagDto appEshopBagDto, object srcTranactionRId, int dataTransferId)
        {
            if (appEshopBagDto != null && srcTranactionRId != null)
            {
                JObject appEshopBagJObj = AppMasterDetailFormDataLoadBL.ProcessFormToApiObjDataTransfer(dataTransferId, srcTranactionRId);

                if (appEshopBagJObj != null)
                {
                    AppEshopBagDto newBagData = JsonConvert.DeserializeObject<AppEshopBagDto>(appEshopBagJObj.ToString());
                    if (newBagData != null)
                    {
                        appEshopBagDto.Subtotal = newBagData.Subtotal;
                        appEshopBagDto.ShippingCost = newBagData.ShippingCost;
                        appEshopBagDto.TotalTax = newBagData.TotalTax;
                        appEshopBagDto.Total = newBagData.Total;

                        appEshopBagDto.InvoiceId = newBagData.InvoiceId;

                    }
                }
            }

        }



        private static OperationCallResult<AppSecurityUserExDto> CreateESitePartnerUserByEmail(string email, int? newUserPartnerType, int? eSiteId, string postEmailActivationRedirectUrl, string timeZoneInfoToken)
        {
            OperationCallResult<AppSecurityUserExDto> operationCallResult = new OperationCallResult<AppSecurityUserExDto>();

            if (!string.IsNullOrWhiteSpace(email) && newUserPartnerType.HasValue && eSiteId.HasValue)
            {
                AppSecurityUserExDto aAppSecurityUserExDto = new AppSecurityUserExDto();
                // *** Must be Vendor or Customer   
                aAppSecurityUserExDto.Email = email;
                aAppSecurityUserExDto.NewUserPartnerType = newUserPartnerType;
                aAppSecurityUserExDto.DomainId = newUserPartnerType.Value;

                aAppSecurityUserExDto.LoginName = email;
                aAppSecurityUserExDto.UserName = email.Split('@')[0];
                aAppSecurityUserExDto.Password = "password";
                aAppSecurityUserExDto.IsActive = true;
                aAppSecurityUserExDto.RegisterFromEsiteId = eSiteId;
                aAppSecurityUserExDto.PostEmailActivationRedirectUrl = postEmailActivationRedirectUrl;

                if (!string.IsNullOrWhiteSpace(timeZoneInfoToken))
                {
                    aAppSecurityUserExDto.TimeZoneInfoToken = timeZoneInfoToken;
                }


                operationCallResult.ValidationResult.Merge(AppSecurityUserBL.IsUserLoginNameUnique(aAppSecurityUserExDto.LoginName, null));

                if (operationCallResult.ValidationResult.HasErrors)
                {
                    return operationCallResult;
                }

                operationCallResult.ValidationResult.Merge(AppSecurityUserBL.IsUserEmailUnique(aAppSecurityUserExDto.Email, null));

                if (operationCallResult.ValidationResult.HasErrors)
                {
                    return operationCallResult;
                }

                AppEsiteExDto eSiteExDto = AppEsiteConfigBL.RetrieveOneAppEsiteExDto(aAppSecurityUserExDto.RegisterFromEsiteId.Value);

                AppEshopBagDto appEshopBagDto = new AppEshopBagDto();
                appEshopBagDto.ShippingAdress = new AppEshopShippingAdressDto();
                AssignEshotBagFirstAndLastNameFromUserName(aAppSecurityUserExDto.UserName, appEshopBagDto);
                appEshopBagDto.ShippingAdress.EmailAddress = aAppSecurityUserExDto.Email;

                int? partnerId = null;

                if (aAppSecurityUserExDto.NewUserPartnerType == (int)EmAppUserType.Customer)
                {
                    partnerId = FindEsiteExistingCustomerIdByEmail(aAppSecurityUserExDto.Email, eSiteExDto);

                    if (!partnerId.HasValue)
                    {
                        partnerId = CreateNewGuestCustomerByDataTransfer(appEshopBagDto, eSiteExDto);
                    }
                }
                else if (aAppSecurityUserExDto.NewUserPartnerType == (int)EmAppUserType.Supplier)
                {
                    partnerId = FindEsiteExistingSupplierIdByEmail(appEshopBagDto.ShippingAdress.EmailAddress, eSiteExDto);

                    if (!partnerId.HasValue)
                    {
                        partnerId = CreateNewSupplierByDataTransfer(appEshopBagDto, eSiteExDto);
                    }
                }
                else if (aAppSecurityUserExDto.NewUserPartnerType == (int)EmAppUserType.ClientAgent)
                {
                    // To Do Agent
                }
                else if (aAppSecurityUserExDto.NewUserPartnerType == (int)EmAppUserType.SupplierAgent)
                {
                    // To Do Agent
                }

                if (partnerId.HasValue)
                {
                    //aAppSecurityUserExDto.NewBusinessAccountId = customerId.Value;
                    OperationCallResult<AppSecurityUserExDto> saveUserResult = AppSaasAccountUserBL.CreateUserForExistingPartner(aAppSecurityUserExDto, partnerId.Value);

                    if (saveUserResult.IsSuccessfulWithResult)
                    {
                        operationCallResult.Object = saveUserResult.Object;
                    }
                    else
                    {
                        operationCallResult.ValidationResult = saveUserResult.ValidationResult;
                    }

                }
                else
                {
                    operationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppEshopBagDto), "App_EStoreUserRegistration_CreateCustomerAccountError", ValidationItemType.Error, "Create Customer Account Failed"));
                }

            }

            return operationCallResult;
        }

       
    }
}
