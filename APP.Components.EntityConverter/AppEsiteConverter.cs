using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

using APP.Framework;
using APP.Framework.Collections;
using APP.LBL.EntityClasses;
using APP.Components.EntityDto;



namespace APP.Components.EntityConverter
{
    /// <summary>
    /// Convert Properties between  AppEsiteEntity and  AppEsiteDto
    /// </summary>
    public static partial class AppEsiteConverter 
    {
         /// <summary>
        ///  Convert AppEsiteEntity To  AppEsiteDto
        /// </summary>
        public static AppEsiteDto ConvertEntityToDto(AppEsiteEntity aAppEsiteEntity)
        {        
    		AppEsiteDto aAppEsiteDto = new AppEsiteDto();
    		CopyEntityPropertyToDto( aAppEsiteEntity, aAppEsiteDto);          
			return aAppEsiteDto;
        }
		 /// <summary>
        ///  Convert AppEsiteEntity To  AppEsiteExDto
        /// </summary>
        public static AppEsiteExDto ConvertEntityToExDto(AppEsiteEntity aAppEsiteEntity)
        {        
    		AppEsiteExDto aAppEsiteExDto = new AppEsiteExDto();
			CopyEntityPropertyToDto( aAppEsiteEntity, aAppEsiteExDto);
			
			
			
            return aAppEsiteExDto;
        }
		
		 /// <summary>
        ///  Convert AppEsiteEntity To  AppEsiteDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppEsiteEntity aAppEsiteEntity,AppEsiteDto aAppEsiteDto)
        {        
    		
           // aAppEsiteDto.StopChangeTracking();
 			aAppEsiteDto.Id = aAppEsiteEntity.EsiteId;
 			aAppEsiteDto.Sort = aAppEsiteEntity.Sort;
 			aAppEsiteDto.Name = aAppEsiteEntity.Name;
 			aAppEsiteDto.Description = aAppEsiteEntity.Description;
 			aAppEsiteDto.EmAppEsiteTheme = aAppEsiteEntity.EmAppEsiteTheme;
 			aAppEsiteDto.LogoImageId1 = aAppEsiteEntity.LogoImageId1;
 			aAppEsiteDto.LogoImageId2 = aAppEsiteEntity.LogoImageId2;
 			aAppEsiteDto.IsAllowGuestCheckout = aAppEsiteEntity.IsAllowGuestCheckout;
 			aAppEsiteDto.MyOrderListSearchId = aAppEsiteEntity.MyOrderListSearchId;
 			aAppEsiteDto.CustomerInfoDataModelId = aAppEsiteEntity.CustomerInfoDataModelId;
 			aAppEsiteDto.CustomerInfoDbtableName = aAppEsiteEntity.CustomerInfoDbtableName;
 			aAppEsiteDto.CustomerInfoCustomerIdDbfieldName = aAppEsiteEntity.CustomerInfoCustomerIdDbfieldName;
 			aAppEsiteDto.CustomerInfoEmailDbfieldName = aAppEsiteEntity.CustomerInfoEmailDbfieldName;
 			aAppEsiteDto.CustomerInfoDataTransferId = aAppEsiteEntity.CustomerInfoDataTransferId;
 			aAppEsiteDto.SaveCustomerInfoPostActionId = aAppEsiteEntity.SaveCustomerInfoPostActionId;
 			aAppEsiteDto.CustomerShippingAddressSearchId = aAppEsiteEntity.CustomerShippingAddressSearchId;
 			aAppEsiteDto.CustomerShippingAddressDataModelId = aAppEsiteEntity.CustomerShippingAddressDataModelId;
 			aAppEsiteDto.CustomerShippingAddressDataTransferId = aAppEsiteEntity.CustomerShippingAddressDataTransferId;
 			aAppEsiteDto.SaveCustomerShippingAddressPostActionId = aAppEsiteEntity.SaveCustomerShippingAddressPostActionId;
 			aAppEsiteDto.CustomerOrderListSearchId = aAppEsiteEntity.CustomerOrderListSearchId;
 			aAppEsiteDto.OrderDataModelId = aAppEsiteEntity.OrderDataModelId;
 			aAppEsiteDto.InvoiceDataModelId = aAppEsiteEntity.InvoiceDataModelId;
 			aAppEsiteDto.InvoiceReportId = aAppEsiteEntity.InvoiceReportId;
 			aAppEsiteDto.OrderDataTransferId = aAppEsiteEntity.OrderDataTransferId;
 			aAppEsiteDto.SaveOrderPostActionId = aAppEsiteEntity.SaveOrderPostActionId;
 			aAppEsiteDto.CustomerPaymentHistorySearchId = aAppEsiteEntity.CustomerPaymentHistorySearchId;
 			aAppEsiteDto.CustomerPaymentHistoryDataModelId = aAppEsiteEntity.CustomerPaymentHistoryDataModelId;
 			aAppEsiteDto.CustomerPaymentHistoryDataTransferId = aAppEsiteEntity.CustomerPaymentHistoryDataTransferId;
 			aAppEsiteDto.PaymentSuccessfulPostActionActionId = aAppEsiteEntity.PaymentSuccessfulPostActionActionId;
 			aAppEsiteDto.PaymentFailedPostActionActionId = aAppEsiteEntity.PaymentFailedPostActionActionId;
 			aAppEsiteDto.EnablePaypalPayment = aAppEsiteEntity.EnablePaypalPayment;
 			aAppEsiteDto.PaypalPaymentApiBaseUrl = aAppEsiteEntity.PaypalPaymentApiBaseUrl;
 			aAppEsiteDto.PaypalPaymentSbClientId = aAppEsiteEntity.PaypalPaymentSbClientId;
 			aAppEsiteDto.PaypalPaymentDefaultCurrencyCode = aAppEsiteEntity.PaypalPaymentDefaultCurrencyCode;
 			aAppEsiteDto.EnableVisaPayment = aAppEsiteEntity.EnableVisaPayment;
 			aAppEsiteDto.VisaPaymentApiBaseUrl = aAppEsiteEntity.VisaPaymentApiBaseUrl;
 			aAppEsiteDto.VisaPaymentApiParam1 = aAppEsiteEntity.VisaPaymentApiParam1;
 			aAppEsiteDto.VisaPaymentApiParam2 = aAppEsiteEntity.VisaPaymentApiParam2;
 			aAppEsiteDto.VisaPaymentApiParam3 = aAppEsiteEntity.VisaPaymentApiParam3;
 			aAppEsiteDto.VisaPaymentApiParam4 = aAppEsiteEntity.VisaPaymentApiParam4;
 			aAppEsiteDto.VisaPaymentApiParam5 = aAppEsiteEntity.VisaPaymentApiParam5;
 			aAppEsiteDto.AppCreatedByCompanyId = aAppEsiteEntity.AppCreatedByCompanyId;
 			aAppEsiteDto.AppCreatedById = aAppEsiteEntity.AppCreatedById;
 			aAppEsiteDto.AppCreatedDate = aAppEsiteEntity.AppCreatedDate;
 			aAppEsiteDto.AppModifiedDate = aAppEsiteEntity.AppModifiedDate;
 			aAppEsiteDto.AppModifiedById = aAppEsiteEntity.AppModifiedById;
 			aAppEsiteDto.MasteSiteHostLayoutHtmlContent = aAppEsiteEntity.MasteSiteHostLayoutHtmlContent;
 			aAppEsiteDto.MasteSiteCustNavigationJavaScripControl = aAppEsiteEntity.MasteSiteCustNavigationJavaScripControl;
 			aAppEsiteDto.EmApplicationType = aAppEsiteEntity.EmApplicationType;
 			aAppEsiteDto.SaasApplicationId = aAppEsiteEntity.SaasApplicationId;
 			aAppEsiteDto.SupplierInfoDataModelId = aAppEsiteEntity.SupplierInfoDataModelId;
 			aAppEsiteDto.SupplierInfoDbtableName = aAppEsiteEntity.SupplierInfoDbtableName;
 			aAppEsiteDto.SupplierInfoIdDbfieldName = aAppEsiteEntity.SupplierInfoIdDbfieldName;
 			aAppEsiteDto.SupplierInfoEmailDbfieldName = aAppEsiteEntity.SupplierInfoEmailDbfieldName;
 			aAppEsiteDto.SupplierInfoDataTransferId = aAppEsiteEntity.SupplierInfoDataTransferId;
 			aAppEsiteDto.SaveSupplierInfoPostActionId = aAppEsiteEntity.SaveSupplierInfoPostActionId;
 			aAppEsiteDto.SitePublishedBaseUrl = aAppEsiteEntity.SitePublishedBaseUrl;
 			aAppEsiteDto.SitePublishedLoginUrl = aAppEsiteEntity.SitePublishedLoginUrl;
 			aAppEsiteDto.StartPage = aAppEsiteEntity.StartPage;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppEsiteDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppEsiteEntity.AppCreatedDate);
                aAppEsiteDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppEsiteEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppEsiteEntity, aAppEsiteDto);
		}
		
		 /// <summary>
        ///  Copy AppEsiteDto Properties to   AppEsiteEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppEsiteEntity aAppEsiteEntity,AppEsiteDto aAppEsiteDto)
        {        
 
      			aAppEsiteEntity.Sort = aAppEsiteDto.Sort;
      			aAppEsiteEntity.Name = aAppEsiteDto.Name;
      			aAppEsiteEntity.Description = aAppEsiteDto.Description;
      			aAppEsiteEntity.EmAppEsiteTheme = aAppEsiteDto.EmAppEsiteTheme;
      			aAppEsiteEntity.LogoImageId1 = aAppEsiteDto.LogoImageId1;
      			aAppEsiteEntity.LogoImageId2 = aAppEsiteDto.LogoImageId2;
      			aAppEsiteEntity.IsAllowGuestCheckout = aAppEsiteDto.IsAllowGuestCheckout;
      			aAppEsiteEntity.MyOrderListSearchId = aAppEsiteDto.MyOrderListSearchId;
      			aAppEsiteEntity.CustomerInfoDataModelId = aAppEsiteDto.CustomerInfoDataModelId;
      			aAppEsiteEntity.CustomerInfoDbtableName = aAppEsiteDto.CustomerInfoDbtableName;
      			aAppEsiteEntity.CustomerInfoCustomerIdDbfieldName = aAppEsiteDto.CustomerInfoCustomerIdDbfieldName;
      			aAppEsiteEntity.CustomerInfoEmailDbfieldName = aAppEsiteDto.CustomerInfoEmailDbfieldName;
      			aAppEsiteEntity.CustomerInfoDataTransferId = aAppEsiteDto.CustomerInfoDataTransferId;
      			aAppEsiteEntity.SaveCustomerInfoPostActionId = aAppEsiteDto.SaveCustomerInfoPostActionId;
      			aAppEsiteEntity.CustomerShippingAddressSearchId = aAppEsiteDto.CustomerShippingAddressSearchId;
      			aAppEsiteEntity.CustomerShippingAddressDataModelId = aAppEsiteDto.CustomerShippingAddressDataModelId;
      			aAppEsiteEntity.CustomerShippingAddressDataTransferId = aAppEsiteDto.CustomerShippingAddressDataTransferId;
      			aAppEsiteEntity.SaveCustomerShippingAddressPostActionId = aAppEsiteDto.SaveCustomerShippingAddressPostActionId;
      			aAppEsiteEntity.CustomerOrderListSearchId = aAppEsiteDto.CustomerOrderListSearchId;
      			aAppEsiteEntity.OrderDataModelId = aAppEsiteDto.OrderDataModelId;
      			aAppEsiteEntity.InvoiceDataModelId = aAppEsiteDto.InvoiceDataModelId;
      			aAppEsiteEntity.InvoiceReportId = aAppEsiteDto.InvoiceReportId;
      			aAppEsiteEntity.OrderDataTransferId = aAppEsiteDto.OrderDataTransferId;
      			aAppEsiteEntity.SaveOrderPostActionId = aAppEsiteDto.SaveOrderPostActionId;
      			aAppEsiteEntity.CustomerPaymentHistorySearchId = aAppEsiteDto.CustomerPaymentHistorySearchId;
      			aAppEsiteEntity.CustomerPaymentHistoryDataModelId = aAppEsiteDto.CustomerPaymentHistoryDataModelId;
      			aAppEsiteEntity.CustomerPaymentHistoryDataTransferId = aAppEsiteDto.CustomerPaymentHistoryDataTransferId;
      			aAppEsiteEntity.PaymentSuccessfulPostActionActionId = aAppEsiteDto.PaymentSuccessfulPostActionActionId;
      			aAppEsiteEntity.PaymentFailedPostActionActionId = aAppEsiteDto.PaymentFailedPostActionActionId;
      			aAppEsiteEntity.EnablePaypalPayment = aAppEsiteDto.EnablePaypalPayment;
      			aAppEsiteEntity.PaypalPaymentApiBaseUrl = aAppEsiteDto.PaypalPaymentApiBaseUrl;
      			aAppEsiteEntity.PaypalPaymentSbClientId = aAppEsiteDto.PaypalPaymentSbClientId;
      			aAppEsiteEntity.PaypalPaymentDefaultCurrencyCode = aAppEsiteDto.PaypalPaymentDefaultCurrencyCode;
      			aAppEsiteEntity.EnableVisaPayment = aAppEsiteDto.EnableVisaPayment;
      			aAppEsiteEntity.VisaPaymentApiBaseUrl = aAppEsiteDto.VisaPaymentApiBaseUrl;
      			aAppEsiteEntity.VisaPaymentApiParam1 = aAppEsiteDto.VisaPaymentApiParam1;
      			aAppEsiteEntity.VisaPaymentApiParam2 = aAppEsiteDto.VisaPaymentApiParam2;
      			aAppEsiteEntity.VisaPaymentApiParam3 = aAppEsiteDto.VisaPaymentApiParam3;
      			aAppEsiteEntity.VisaPaymentApiParam4 = aAppEsiteDto.VisaPaymentApiParam4;
      			aAppEsiteEntity.VisaPaymentApiParam5 = aAppEsiteDto.VisaPaymentApiParam5;
      			aAppEsiteEntity.AppCreatedByCompanyId = aAppEsiteDto.AppCreatedByCompanyId;
 
  
   
    
      			aAppEsiteEntity.MasteSiteHostLayoutHtmlContent = aAppEsiteDto.MasteSiteHostLayoutHtmlContent;
      			aAppEsiteEntity.MasteSiteCustNavigationJavaScripControl = aAppEsiteDto.MasteSiteCustNavigationJavaScripControl;
      			aAppEsiteEntity.EmApplicationType = aAppEsiteDto.EmApplicationType;
      			aAppEsiteEntity.SaasApplicationId = aAppEsiteDto.SaasApplicationId;
      			aAppEsiteEntity.SupplierInfoDataModelId = aAppEsiteDto.SupplierInfoDataModelId;
      			aAppEsiteEntity.SupplierInfoDbtableName = aAppEsiteDto.SupplierInfoDbtableName;
      			aAppEsiteEntity.SupplierInfoIdDbfieldName = aAppEsiteDto.SupplierInfoIdDbfieldName;
      			aAppEsiteEntity.SupplierInfoEmailDbfieldName = aAppEsiteDto.SupplierInfoEmailDbfieldName;
      			aAppEsiteEntity.SupplierInfoDataTransferId = aAppEsiteDto.SupplierInfoDataTransferId;
      			aAppEsiteEntity.SaveSupplierInfoPostActionId = aAppEsiteDto.SaveSupplierInfoPostActionId;
      			aAppEsiteEntity.SitePublishedBaseUrl = aAppEsiteDto.SitePublishedBaseUrl;
      			aAppEsiteEntity.SitePublishedLoginUrl = aAppEsiteDto.SitePublishedLoginUrl;
      			aAppEsiteEntity.StartPage = aAppEsiteDto.StartPage;
			
			if(aAppEsiteDto.Id == null)
			{
				aAppEsiteEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppEsiteEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppEsiteEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppEsiteEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppEsiteEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppEsiteEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppEsiteEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppEsiteEntity, aAppEsiteDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppEsiteEntity aAppEsiteEntity,AppEsiteDto aAppEsiteDto);
		
		static partial void OnCopyDtoToEntityDone(AppEsiteEntity aAppEsiteEntity,AppEsiteDto aAppEsiteDto);
		
   
       
    }
}

 