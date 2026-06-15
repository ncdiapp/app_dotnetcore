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
    /// DTO class for the entity 'AppSecurityEntityAction'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppSecurityEntityActionDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string ActionCodeProperty = ObjectInfoHelper.GetName<AppSecurityEntityActionDto ,System.String>(o => o.ActionCode);
        public static readonly string DescriptionProperty = ObjectInfoHelper.GetName<AppSecurityEntityActionDto ,System.String>(o => o.Description);
        public static readonly string NoSecurityControlProperty = ObjectInfoHelper.GetName<AppSecurityEntityActionDto ,System.Boolean>(o => o.NoSecurityControl);
        public static readonly string TransactionIdProperty = ObjectInfoHelper.GetName<AppSecurityEntityActionDto ,Nullable<System.Int32>>(o => o.TransactionId);
        public static readonly string RouteStateIdProperty = ObjectInfoHelper.GetName<AppSecurityEntityActionDto ,Nullable<System.Int32>>(o => o.RouteStateId);
        public static readonly string IsSharedbyMutipleCompanyProperty = ObjectInfoHelper.GetName<AppSecurityEntityActionDto ,Nullable<System.Boolean>>(o => o.IsSharedbyMutipleCompany);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppSecurityEntityActionDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppSecurityEntityActionDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppSecurityEntityActionDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppSecurityEntityActionDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppSecurityEntityActionDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppSecurityEntityActionDto()
        {        
        }
		
		static AppSecurityEntityActionDto()
        {
              
			MandatoryProperties.Add(ActionCodeProperty);   
			MandatoryProperties.Add(NoSecurityControlProperty);         
			      
			ForeignKeyProperties.Add(RouteStateIdProperty);       		
              
			DictStringPropertyMaxLength.Add(ActionCodeProperty,50); 
			DictStringPropertyMaxLength.Add(DescriptionProperty,150);           
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
    


        /// <summary> The ActionCode property of the Entity AppSecurityEntityAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ActionCode
        {
            get { return  GetValue<System.String>( ActionCodeProperty);}
            set { SetValue(ActionCodeProperty,value); }
        }

        /// <summary> The Description property of the Entity AppSecurityEntityAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Description
        {
            get { return  GetValue<System.String>( DescriptionProperty);}
            set { SetValue(DescriptionProperty,value); }
        }

        /// <summary> The NoSecurityControl property of the Entity AppSecurityEntityAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Boolean NoSecurityControl
        {
            get { return  GetValue<System.Boolean>( NoSecurityControlProperty);}
            set { SetValue(NoSecurityControlProperty,value); }
        }

        /// <summary> The TransactionId property of the Entity AppSecurityEntityAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionId
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionIdProperty);}
            set { SetValue(TransactionIdProperty,value); }
        }

        /// <summary> The RouteStateId property of the Entity AppSecurityEntityAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> RouteStateId
        {
            get { return  GetValue<Nullable<System.Int32>>( RouteStateIdProperty);}
            set { SetValue(RouteStateIdProperty,value); }
        }

        /// <summary> The IsSharedbyMutipleCompany property of the Entity AppSecurityEntityAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsSharedbyMutipleCompany
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsSharedbyMutipleCompanyProperty);}
            set { SetValue(IsSharedbyMutipleCompanyProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppSecurityEntityAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppSecurityEntityAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppSecurityEntityAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppSecurityEntityAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppSecurityEntityAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedByCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByCompanyIdProperty);}
            set { SetValue(AppCreatedByCompanyIdProperty,value); }
        }
        
        #endregion

       
        
    }
}

