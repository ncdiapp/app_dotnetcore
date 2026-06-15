using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Framework;
using APP.Framework.Validation;
using APP.Components.Dto;
using Newtonsoft.Json;

namespace APP.Components.EntityDto
{
    /// <summary>
    /// DTO class for the entity 'AppEsite'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppEsiteDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string SortProperty = ObjectInfoHelper.GetName<AppEsiteDto ,Nullable<System.Int32>>(o => o.Sort);
        public static readonly string NameProperty = ObjectInfoHelper.GetName<AppEsiteDto ,System.String>(o => o.Name);
        public static readonly string DescriptionProperty = ObjectInfoHelper.GetName<AppEsiteDto ,System.String>(o => o.Description);
        public static readonly string EmAppEsiteThemeProperty = ObjectInfoHelper.GetName<AppEsiteDto ,Nullable<System.Int32>>(o => o.EmAppEsiteTheme);
        public static readonly string LogoImageId1Property = ObjectInfoHelper.GetName<AppEsiteDto ,Nullable<System.Int32>>(o => o.LogoImageId1);
        public static readonly string LogoImageId2Property = ObjectInfoHelper.GetName<AppEsiteDto ,Nullable<System.Int32>>(o => o.LogoImageId2);
        public static readonly string IsAllowGuestCheckoutProperty = ObjectInfoHelper.GetName<AppEsiteDto ,Nullable<System.Boolean>>(o => o.IsAllowGuestCheckout);
        public static readonly string MyOrderListSearchIdProperty = ObjectInfoHelper.GetName<AppEsiteDto ,Nullable<System.Int32>>(o => o.MyOrderListSearchId);
        public static readonly string CustomerInfoDataModelIdProperty = ObjectInfoHelper.GetName<AppEsiteDto ,Nullable<System.Int32>>(o => o.CustomerInfoDataModelId);
        public static readonly string CustomerInfoDbtableNameProperty = ObjectInfoHelper.GetName<AppEsiteDto ,System.String>(o => o.CustomerInfoDbtableName);
        public static readonly string CustomerInfoCustomerIdDbfieldNameProperty = ObjectInfoHelper.GetName<AppEsiteDto ,System.String>(o => o.CustomerInfoCustomerIdDbfieldName);
        public static readonly string CustomerInfoEmailDbfieldNameProperty = ObjectInfoHelper.GetName<AppEsiteDto ,System.String>(o => o.CustomerInfoEmailDbfieldName);
        public static readonly string CustomerInfoDataTransferIdProperty = ObjectInfoHelper.GetName<AppEsiteDto ,Nullable<System.Int32>>(o => o.CustomerInfoDataTransferId);
        public static readonly string SaveCustomerInfoPostActionIdProperty = ObjectInfoHelper.GetName<AppEsiteDto ,Nullable<System.Int32>>(o => o.SaveCustomerInfoPostActionId);
        public static readonly string CustomerShippingAddressSearchIdProperty = ObjectInfoHelper.GetName<AppEsiteDto ,Nullable<System.Int32>>(o => o.CustomerShippingAddressSearchId);
        public static readonly string CustomerShippingAddressDataModelIdProperty = ObjectInfoHelper.GetName<AppEsiteDto ,Nullable<System.Int32>>(o => o.CustomerShippingAddressDataModelId);
        public static readonly string CustomerShippingAddressDataTransferIdProperty = ObjectInfoHelper.GetName<AppEsiteDto ,Nullable<System.Int32>>(o => o.CustomerShippingAddressDataTransferId);
        public static readonly string SaveCustomerShippingAddressPostActionIdProperty = ObjectInfoHelper.GetName<AppEsiteDto ,Nullable<System.Int32>>(o => o.SaveCustomerShippingAddressPostActionId);
        public static readonly string CustomerOrderListSearchIdProperty = ObjectInfoHelper.GetName<AppEsiteDto ,Nullable<System.Int32>>(o => o.CustomerOrderListSearchId);
        public static readonly string OrderDataModelIdProperty = ObjectInfoHelper.GetName<AppEsiteDto ,Nullable<System.Int32>>(o => o.OrderDataModelId);
        public static readonly string InvoiceDataModelIdProperty = ObjectInfoHelper.GetName<AppEsiteDto ,Nullable<System.Int32>>(o => o.InvoiceDataModelId);
        public static readonly string InvoiceReportIdProperty = ObjectInfoHelper.GetName<AppEsiteDto ,Nullable<System.Int32>>(o => o.InvoiceReportId);
        public static readonly string OrderDataTransferIdProperty = ObjectInfoHelper.GetName<AppEsiteDto ,Nullable<System.Int32>>(o => o.OrderDataTransferId);
        public static readonly string SaveOrderPostActionIdProperty = ObjectInfoHelper.GetName<AppEsiteDto ,Nullable<System.Int32>>(o => o.SaveOrderPostActionId);
        public static readonly string CustomerPaymentHistorySearchIdProperty = ObjectInfoHelper.GetName<AppEsiteDto ,Nullable<System.Int32>>(o => o.CustomerPaymentHistorySearchId);
        public static readonly string CustomerPaymentHistoryDataModelIdProperty = ObjectInfoHelper.GetName<AppEsiteDto ,Nullable<System.Int32>>(o => o.CustomerPaymentHistoryDataModelId);
        public static readonly string CustomerPaymentHistoryDataTransferIdProperty = ObjectInfoHelper.GetName<AppEsiteDto ,Nullable<System.Int32>>(o => o.CustomerPaymentHistoryDataTransferId);
        public static readonly string PaymentSuccessfulPostActionActionIdProperty = ObjectInfoHelper.GetName<AppEsiteDto ,Nullable<System.Int32>>(o => o.PaymentSuccessfulPostActionActionId);
        public static readonly string PaymentFailedPostActionActionIdProperty = ObjectInfoHelper.GetName<AppEsiteDto ,Nullable<System.Int32>>(o => o.PaymentFailedPostActionActionId);
        public static readonly string EnablePaypalPaymentProperty = ObjectInfoHelper.GetName<AppEsiteDto ,Nullable<System.Boolean>>(o => o.EnablePaypalPayment);
        public static readonly string PaypalPaymentApiBaseUrlProperty = ObjectInfoHelper.GetName<AppEsiteDto ,System.String>(o => o.PaypalPaymentApiBaseUrl);
        public static readonly string PaypalPaymentSbClientIdProperty = ObjectInfoHelper.GetName<AppEsiteDto ,System.String>(o => o.PaypalPaymentSbClientId);
        public static readonly string PaypalPaymentDefaultCurrencyCodeProperty = ObjectInfoHelper.GetName<AppEsiteDto ,System.String>(o => o.PaypalPaymentDefaultCurrencyCode);
        public static readonly string EnableVisaPaymentProperty = ObjectInfoHelper.GetName<AppEsiteDto ,Nullable<System.Boolean>>(o => o.EnableVisaPayment);
        public static readonly string VisaPaymentApiBaseUrlProperty = ObjectInfoHelper.GetName<AppEsiteDto ,System.String>(o => o.VisaPaymentApiBaseUrl);
        public static readonly string VisaPaymentApiParam1Property = ObjectInfoHelper.GetName<AppEsiteDto ,System.String>(o => o.VisaPaymentApiParam1);
        public static readonly string VisaPaymentApiParam2Property = ObjectInfoHelper.GetName<AppEsiteDto ,System.String>(o => o.VisaPaymentApiParam2);
        public static readonly string VisaPaymentApiParam3Property = ObjectInfoHelper.GetName<AppEsiteDto ,System.String>(o => o.VisaPaymentApiParam3);
        public static readonly string VisaPaymentApiParam4Property = ObjectInfoHelper.GetName<AppEsiteDto ,System.String>(o => o.VisaPaymentApiParam4);
        public static readonly string VisaPaymentApiParam5Property = ObjectInfoHelper.GetName<AppEsiteDto ,System.String>(o => o.VisaPaymentApiParam5);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppEsiteDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppEsiteDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppEsiteDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppEsiteDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppEsiteDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string MasteSiteHostLayoutHtmlContentProperty = ObjectInfoHelper.GetName<AppEsiteDto ,System.String>(o => o.MasteSiteHostLayoutHtmlContent);
        public static readonly string MasteSiteCustNavigationJavaScripControlProperty = ObjectInfoHelper.GetName<AppEsiteDto ,System.String>(o => o.MasteSiteCustNavigationJavaScripControl);
        public static readonly string EmApplicationTypeProperty = ObjectInfoHelper.GetName<AppEsiteDto ,Nullable<System.Int32>>(o => o.EmApplicationType);
        public static readonly string SaasApplicationIdProperty = ObjectInfoHelper.GetName<AppEsiteDto ,Nullable<System.Int32>>(o => o.SaasApplicationId);
        public static readonly string SupplierInfoDataModelIdProperty = ObjectInfoHelper.GetName<AppEsiteDto ,Nullable<System.Int32>>(o => o.SupplierInfoDataModelId);
        public static readonly string SupplierInfoDbtableNameProperty = ObjectInfoHelper.GetName<AppEsiteDto ,System.String>(o => o.SupplierInfoDbtableName);
        public static readonly string SupplierInfoIdDbfieldNameProperty = ObjectInfoHelper.GetName<AppEsiteDto ,System.String>(o => o.SupplierInfoIdDbfieldName);
        public static readonly string SupplierInfoEmailDbfieldNameProperty = ObjectInfoHelper.GetName<AppEsiteDto ,System.String>(o => o.SupplierInfoEmailDbfieldName);
        public static readonly string SupplierInfoDataTransferIdProperty = ObjectInfoHelper.GetName<AppEsiteDto ,System.String>(o => o.SupplierInfoDataTransferId);
        public static readonly string SaveSupplierInfoPostActionIdProperty = ObjectInfoHelper.GetName<AppEsiteDto ,System.String>(o => o.SaveSupplierInfoPostActionId);
        public static readonly string SitePublishedBaseUrlProperty = ObjectInfoHelper.GetName<AppEsiteDto ,System.String>(o => o.SitePublishedBaseUrl);
        public static readonly string SitePublishedLoginUrlProperty = ObjectInfoHelper.GetName<AppEsiteDto ,System.String>(o => o.SitePublishedLoginUrl);
        public static readonly string StartPageProperty = ObjectInfoHelper.GetName<AppEsiteDto ,System.String>(o => o.StartPage);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppEsiteDto()
        {        
        }
		
		static AppEsiteDto()
        {
                                                                       
			                                                  
			ForeignKeyProperties.Add(SaasApplicationIdProperty);          		
               
			DictStringPropertyMaxLength.Add(NameProperty,500); 
			DictStringPropertyMaxLength.Add(DescriptionProperty,2000);       
			DictStringPropertyMaxLength.Add(CustomerInfoDbtableNameProperty,500); 
			DictStringPropertyMaxLength.Add(CustomerInfoCustomerIdDbfieldNameProperty,500); 
			DictStringPropertyMaxLength.Add(CustomerInfoEmailDbfieldNameProperty,500);                   
			DictStringPropertyMaxLength.Add(PaypalPaymentApiBaseUrlProperty,500); 
			DictStringPropertyMaxLength.Add(PaypalPaymentSbClientIdProperty,500); 
			DictStringPropertyMaxLength.Add(PaypalPaymentDefaultCurrencyCodeProperty,500);  
			DictStringPropertyMaxLength.Add(VisaPaymentApiBaseUrlProperty,500); 
			DictStringPropertyMaxLength.Add(VisaPaymentApiParam1Property,500); 
			DictStringPropertyMaxLength.Add(VisaPaymentApiParam2Property,500); 
			DictStringPropertyMaxLength.Add(VisaPaymentApiParam3Property,500); 
			DictStringPropertyMaxLength.Add(VisaPaymentApiParam4Property,500); 
			DictStringPropertyMaxLength.Add(VisaPaymentApiParam5Property,500);      
			DictStringPropertyMaxLength.Add(MasteSiteHostLayoutHtmlContentProperty,2147483647); 
			DictStringPropertyMaxLength.Add(MasteSiteCustNavigationJavaScripControlProperty,2147483647);    
			DictStringPropertyMaxLength.Add(SupplierInfoDbtableNameProperty,500); 
			DictStringPropertyMaxLength.Add(SupplierInfoIdDbfieldNameProperty,500); 
			DictStringPropertyMaxLength.Add(SupplierInfoEmailDbfieldNameProperty,500); 
			DictStringPropertyMaxLength.Add(SupplierInfoDataTransferIdProperty,500); 
			DictStringPropertyMaxLength.Add(SaveSupplierInfoPostActionIdProperty,500); 
			DictStringPropertyMaxLength.Add(SitePublishedBaseUrlProperty,500); 
			DictStringPropertyMaxLength.Add(SitePublishedLoginUrlProperty,500); 
			DictStringPropertyMaxLength.Add(StartPageProperty,500);  
        }

		protected override void OnInitialize()
        {
            base.OnInitialize();
            PropertyNeedToValidationList = new List<string>();
            PropertyNeedToValidationList.AddRange (MandatoryProperties);
            PropertyNeedToValidationList.AddRange(DictStringPropertyMaxLength.Keys);  
            OnInitialized();

        }
  

        public override ValidationResult ValidateDto()
        {
              ValidationResult aValidationResult =FirstLevelVlidationDtoFactory.ValidateDtoStringmaxLengthAndMandatory(this, MandatoryProperties, ForeignKeyProperties, DictStringPropertyMaxLength);
              CustomerValidateDto(aValidationResult);
              return aValidationResult;
        
        }
    
        public override ValidationResult ValidateProperty(string PropertyName)
        {
             ValidationResult aValidationResult = FirstLevelVlidationDtoFactory.ValidatePropertyStringmaxLengthAndMandatory( this,PropertyName, MandatoryProperties, ForeignKeyProperties, DictStringPropertyMaxLength);
             CustomerValidateProperty( PropertyName,  aValidationResult);
             return aValidationResult;
        }


        partial void OnInitialized();
        partial void CustomerValidateDto(ValidationResult aValidationResult);
        partial void CustomerValidateProperty(string PropertyName, ValidationResult aValidationResult);       
   
        #region  Entity Dto Properties 
    


        /// <summary> The Sort property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> Sort
        {
            get { return  GetValue<Nullable<System.Int32>>( SortProperty);}
            set { SetValue(SortProperty,value); }
        }

        /// <summary> The Name property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Name
        {
            get { return  GetValue<System.String>( NameProperty);}
            set { SetValue(NameProperty,value); }
        }

        /// <summary> The Description property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Description
        {
            get { return  GetValue<System.String>( DescriptionProperty);}
            set { SetValue(DescriptionProperty,value); }
        }

        /// <summary> The EmAppEsiteTheme property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> EmAppEsiteTheme
        {
            get { return  GetValue<Nullable<System.Int32>>( EmAppEsiteThemeProperty);}
            set { SetValue(EmAppEsiteThemeProperty,value); }
        }

        /// <summary> The LogoImageId1 property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> LogoImageId1
        {
            get { return  GetValue<Nullable<System.Int32>>( LogoImageId1Property);}
            set { SetValue(LogoImageId1Property,value); }
        }

        /// <summary> The LogoImageId2 property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> LogoImageId2
        {
            get { return  GetValue<Nullable<System.Int32>>( LogoImageId2Property);}
            set { SetValue(LogoImageId2Property,value); }
        }

        /// <summary> The IsAllowGuestCheckout property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsAllowGuestCheckout
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsAllowGuestCheckoutProperty);}
            set { SetValue(IsAllowGuestCheckoutProperty,value); }
        }

        /// <summary> The MyOrderListSearchId property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> MyOrderListSearchId
        {
            get { return  GetValue<Nullable<System.Int32>>( MyOrderListSearchIdProperty);}
            set { SetValue(MyOrderListSearchIdProperty,value); }
        }

        /// <summary> The CustomerInfoDataModelId property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> CustomerInfoDataModelId
        {
            get { return  GetValue<Nullable<System.Int32>>( CustomerInfoDataModelIdProperty);}
            set { SetValue(CustomerInfoDataModelIdProperty,value); }
        }

        /// <summary> The CustomerInfoDbtableName property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String CustomerInfoDbtableName
        {
            get { return  GetValue<System.String>( CustomerInfoDbtableNameProperty);}
            set { SetValue(CustomerInfoDbtableNameProperty,value); }
        }

        /// <summary> The CustomerInfoCustomerIdDbfieldName property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String CustomerInfoCustomerIdDbfieldName
        {
            get { return  GetValue<System.String>( CustomerInfoCustomerIdDbfieldNameProperty);}
            set { SetValue(CustomerInfoCustomerIdDbfieldNameProperty,value); }
        }

        /// <summary> The CustomerInfoEmailDbfieldName property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String CustomerInfoEmailDbfieldName
        {
            get { return  GetValue<System.String>( CustomerInfoEmailDbfieldNameProperty);}
            set { SetValue(CustomerInfoEmailDbfieldNameProperty,value); }
        }

        /// <summary> The CustomerInfoDataTransferId property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> CustomerInfoDataTransferId
        {
            get { return  GetValue<Nullable<System.Int32>>( CustomerInfoDataTransferIdProperty);}
            set { SetValue(CustomerInfoDataTransferIdProperty,value); }
        }

        /// <summary> The SaveCustomerInfoPostActionId property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SaveCustomerInfoPostActionId
        {
            get { return  GetValue<Nullable<System.Int32>>( SaveCustomerInfoPostActionIdProperty);}
            set { SetValue(SaveCustomerInfoPostActionIdProperty,value); }
        }

        /// <summary> The CustomerShippingAddressSearchId property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> CustomerShippingAddressSearchId
        {
            get { return  GetValue<Nullable<System.Int32>>( CustomerShippingAddressSearchIdProperty);}
            set { SetValue(CustomerShippingAddressSearchIdProperty,value); }
        }

        /// <summary> The CustomerShippingAddressDataModelId property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> CustomerShippingAddressDataModelId
        {
            get { return  GetValue<Nullable<System.Int32>>( CustomerShippingAddressDataModelIdProperty);}
            set { SetValue(CustomerShippingAddressDataModelIdProperty,value); }
        }

        /// <summary> The CustomerShippingAddressDataTransferId property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> CustomerShippingAddressDataTransferId
        {
            get { return  GetValue<Nullable<System.Int32>>( CustomerShippingAddressDataTransferIdProperty);}
            set { SetValue(CustomerShippingAddressDataTransferIdProperty,value); }
        }

        /// <summary> The SaveCustomerShippingAddressPostActionId property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SaveCustomerShippingAddressPostActionId
        {
            get { return  GetValue<Nullable<System.Int32>>( SaveCustomerShippingAddressPostActionIdProperty);}
            set { SetValue(SaveCustomerShippingAddressPostActionIdProperty,value); }
        }

        /// <summary> The CustomerOrderListSearchId property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> CustomerOrderListSearchId
        {
            get { return  GetValue<Nullable<System.Int32>>( CustomerOrderListSearchIdProperty);}
            set { SetValue(CustomerOrderListSearchIdProperty,value); }
        }

        /// <summary> The OrderDataModelId property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> OrderDataModelId
        {
            get { return  GetValue<Nullable<System.Int32>>( OrderDataModelIdProperty);}
            set { SetValue(OrderDataModelIdProperty,value); }
        }

        /// <summary> The InvoiceDataModelId property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> InvoiceDataModelId
        {
            get { return  GetValue<Nullable<System.Int32>>( InvoiceDataModelIdProperty);}
            set { SetValue(InvoiceDataModelIdProperty,value); }
        }

        /// <summary> The InvoiceReportId property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> InvoiceReportId
        {
            get { return  GetValue<Nullable<System.Int32>>( InvoiceReportIdProperty);}
            set { SetValue(InvoiceReportIdProperty,value); }
        }

        /// <summary> The OrderDataTransferId property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> OrderDataTransferId
        {
            get { return  GetValue<Nullable<System.Int32>>( OrderDataTransferIdProperty);}
            set { SetValue(OrderDataTransferIdProperty,value); }
        }

        /// <summary> The SaveOrderPostActionId property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SaveOrderPostActionId
        {
            get { return  GetValue<Nullable<System.Int32>>( SaveOrderPostActionIdProperty);}
            set { SetValue(SaveOrderPostActionIdProperty,value); }
        }

        /// <summary> The CustomerPaymentHistorySearchId property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> CustomerPaymentHistorySearchId
        {
            get { return  GetValue<Nullable<System.Int32>>( CustomerPaymentHistorySearchIdProperty);}
            set { SetValue(CustomerPaymentHistorySearchIdProperty,value); }
        }

        /// <summary> The CustomerPaymentHistoryDataModelId property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> CustomerPaymentHistoryDataModelId
        {
            get { return  GetValue<Nullable<System.Int32>>( CustomerPaymentHistoryDataModelIdProperty);}
            set { SetValue(CustomerPaymentHistoryDataModelIdProperty,value); }
        }

        /// <summary> The CustomerPaymentHistoryDataTransferId property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> CustomerPaymentHistoryDataTransferId
        {
            get { return  GetValue<Nullable<System.Int32>>( CustomerPaymentHistoryDataTransferIdProperty);}
            set { SetValue(CustomerPaymentHistoryDataTransferIdProperty,value); }
        }

        /// <summary> The PaymentSuccessfulPostActionActionId property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> PaymentSuccessfulPostActionActionId
        {
            get { return  GetValue<Nullable<System.Int32>>( PaymentSuccessfulPostActionActionIdProperty);}
            set { SetValue(PaymentSuccessfulPostActionActionIdProperty,value); }
        }

        /// <summary> The PaymentFailedPostActionActionId property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> PaymentFailedPostActionActionId
        {
            get { return  GetValue<Nullable<System.Int32>>( PaymentFailedPostActionActionIdProperty);}
            set { SetValue(PaymentFailedPostActionActionIdProperty,value); }
        }

        /// <summary> The EnablePaypalPayment property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> EnablePaypalPayment
        {
            get { return  GetValue<Nullable<System.Boolean>>( EnablePaypalPaymentProperty);}
            set { SetValue(EnablePaypalPaymentProperty,value); }
        }

        /// <summary> The PaypalPaymentApiBaseUrl property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String PaypalPaymentApiBaseUrl
        {
            get { return  GetValue<System.String>( PaypalPaymentApiBaseUrlProperty);}
            set { SetValue(PaypalPaymentApiBaseUrlProperty,value); }
        }

        /// <summary> The PaypalPaymentSbClientId property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String PaypalPaymentSbClientId
        {
            get { return  GetValue<System.String>( PaypalPaymentSbClientIdProperty);}
            set { SetValue(PaypalPaymentSbClientIdProperty,value); }
        }

        /// <summary> The PaypalPaymentDefaultCurrencyCode property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String PaypalPaymentDefaultCurrencyCode
        {
            get { return  GetValue<System.String>( PaypalPaymentDefaultCurrencyCodeProperty);}
            set { SetValue(PaypalPaymentDefaultCurrencyCodeProperty,value); }
        }

        /// <summary> The EnableVisaPayment property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> EnableVisaPayment
        {
            get { return  GetValue<Nullable<System.Boolean>>( EnableVisaPaymentProperty);}
            set { SetValue(EnableVisaPaymentProperty,value); }
        }

        /// <summary> The VisaPaymentApiBaseUrl property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String VisaPaymentApiBaseUrl
        {
            get { return  GetValue<System.String>( VisaPaymentApiBaseUrlProperty);}
            set { SetValue(VisaPaymentApiBaseUrlProperty,value); }
        }

        /// <summary> The VisaPaymentApiParam1 property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String VisaPaymentApiParam1
        {
            get { return  GetValue<System.String>( VisaPaymentApiParam1Property);}
            set { SetValue(VisaPaymentApiParam1Property,value); }
        }

        /// <summary> The VisaPaymentApiParam2 property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String VisaPaymentApiParam2
        {
            get { return  GetValue<System.String>( VisaPaymentApiParam2Property);}
            set { SetValue(VisaPaymentApiParam2Property,value); }
        }

        /// <summary> The VisaPaymentApiParam3 property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String VisaPaymentApiParam3
        {
            get { return  GetValue<System.String>( VisaPaymentApiParam3Property);}
            set { SetValue(VisaPaymentApiParam3Property,value); }
        }

        /// <summary> The VisaPaymentApiParam4 property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String VisaPaymentApiParam4
        {
            get { return  GetValue<System.String>( VisaPaymentApiParam4Property);}
            set { SetValue(VisaPaymentApiParam4Property,value); }
        }

        /// <summary> The VisaPaymentApiParam5 property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String VisaPaymentApiParam5
        {
            get { return  GetValue<System.String>( VisaPaymentApiParam5Property);}
            set { SetValue(VisaPaymentApiParam5Property,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedByCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByCompanyIdProperty);}
            set { SetValue(AppCreatedByCompanyIdProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The MasteSiteHostLayoutHtmlContent property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String MasteSiteHostLayoutHtmlContent
        {
            get { return  GetValue<System.String>( MasteSiteHostLayoutHtmlContentProperty);}
            set { SetValue(MasteSiteHostLayoutHtmlContentProperty,value); }
        }

        /// <summary> The MasteSiteCustNavigationJavaScripControl property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String MasteSiteCustNavigationJavaScripControl
        {
            get { return  GetValue<System.String>( MasteSiteCustNavigationJavaScripControlProperty);}
            set { SetValue(MasteSiteCustNavigationJavaScripControlProperty,value); }
        }

        /// <summary> The EmApplicationType property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> EmApplicationType
        {
            get { return  GetValue<Nullable<System.Int32>>( EmApplicationTypeProperty);}
            set { SetValue(EmApplicationTypeProperty,value); }
        }

        /// <summary> The SaasApplicationId property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SaasApplicationId
        {
            get { return  GetValue<Nullable<System.Int32>>( SaasApplicationIdProperty);}
            set { SetValue(SaasApplicationIdProperty,value); }
        }

        /// <summary> The SupplierInfoDataModelId property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SupplierInfoDataModelId
        {
            get { return  GetValue<Nullable<System.Int32>>( SupplierInfoDataModelIdProperty);}
            set { SetValue(SupplierInfoDataModelIdProperty,value); }
        }

        /// <summary> The SupplierInfoDbtableName property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String SupplierInfoDbtableName
        {
            get { return  GetValue<System.String>( SupplierInfoDbtableNameProperty);}
            set { SetValue(SupplierInfoDbtableNameProperty,value); }
        }

        /// <summary> The SupplierInfoIdDbfieldName property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String SupplierInfoIdDbfieldName
        {
            get { return  GetValue<System.String>( SupplierInfoIdDbfieldNameProperty);}
            set { SetValue(SupplierInfoIdDbfieldNameProperty,value); }
        }

        /// <summary> The SupplierInfoEmailDbfieldName property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String SupplierInfoEmailDbfieldName
        {
            get { return  GetValue<System.String>( SupplierInfoEmailDbfieldNameProperty);}
            set { SetValue(SupplierInfoEmailDbfieldNameProperty,value); }
        }

        /// <summary> The SupplierInfoDataTransferId property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String SupplierInfoDataTransferId
        {
            get { return  GetValue<System.String>( SupplierInfoDataTransferIdProperty);}
            set { SetValue(SupplierInfoDataTransferIdProperty,value); }
        }

        /// <summary> The SaveSupplierInfoPostActionId property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String SaveSupplierInfoPostActionId
        {
            get { return  GetValue<System.String>( SaveSupplierInfoPostActionIdProperty);}
            set { SetValue(SaveSupplierInfoPostActionIdProperty,value); }
        }

        /// <summary> The SitePublishedBaseUrl property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String SitePublishedBaseUrl
        {
            get { return  GetValue<System.String>( SitePublishedBaseUrlProperty);}
            set { SetValue(SitePublishedBaseUrlProperty,value); }
        }

        /// <summary> The SitePublishedLoginUrl property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String SitePublishedLoginUrl
        {
            get { return  GetValue<System.String>( SitePublishedLoginUrlProperty);}
            set { SetValue(SitePublishedLoginUrlProperty,value); }
        }

        /// <summary> The StartPage property of the Entity AppEsite</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String StartPage
        {
            get { return  GetValue<System.String>( StartPageProperty);}
            set { SetValue(StartPageProperty,value); }
        }
        
        #endregion

       
        
    }
}

