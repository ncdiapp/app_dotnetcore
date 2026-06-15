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
    /// DTO class for the entity 'AppTransactionGroup'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppTransactionGroupDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string AssotmentnavigationIdProperty = ObjectInfoHelper.GetName<AppTransactionGroupDto ,Nullable<System.Int32>>(o => o.AssotmentnavigationId);
        public static readonly string GroupNameProperty = ObjectInfoHelper.GetName<AppTransactionGroupDto ,System.String>(o => o.GroupName);
        public static readonly string DescriptionProperty = ObjectInfoHelper.GetName<AppTransactionGroupDto ,System.String>(o => o.Description);
        public static readonly string IsDefaultGroupProperty = ObjectInfoHelper.GetName<AppTransactionGroupDto ,Nullable<System.Boolean>>(o => o.IsDefaultGroup);
        public static readonly string GroupSortOrderProperty = ObjectInfoHelper.GetName<AppTransactionGroupDto ,Nullable<System.Int32>>(o => o.GroupSortOrder);
        public static readonly string EmBuseinssScopeProperty = ObjectInfoHelper.GetName<AppTransactionGroupDto ,Nullable<System.Int32>>(o => o.EmBuseinssScope);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppTransactionGroupDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppTransactionGroupDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppTransactionGroupDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppTransactionGroupDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppTransactionGroupDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
        public static readonly string SaasApplicationIdProperty = ObjectInfoHelper.GetName<AppTransactionGroupDto ,Nullable<System.Int32>>(o => o.SaasApplicationId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppTransactionGroupDto()
        {        
        }
		
		static AppTransactionGroupDto()
        {
                         
			  
			ForeignKeyProperties.Add(AssotmentnavigationIdProperty);            		
               
			DictStringPropertyMaxLength.Add(GroupNameProperty,100); 
			DictStringPropertyMaxLength.Add(DescriptionProperty,500);           
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
    


        /// <summary> The AssotmentnavigationId property of the Entity AppTransactionGroup</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AssotmentnavigationId
        {
            get { return  GetValue<Nullable<System.Int32>>( AssotmentnavigationIdProperty);}
            set { SetValue(AssotmentnavigationIdProperty,value); }
        }

        /// <summary> The GroupName property of the Entity AppTransactionGroup</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String GroupName
        {
            get { return  GetValue<System.String>( GroupNameProperty);}
            set { SetValue(GroupNameProperty,value); }
        }

        /// <summary> The Description property of the Entity AppTransactionGroup</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Description
        {
            get { return  GetValue<System.String>( DescriptionProperty);}
            set { SetValue(DescriptionProperty,value); }
        }

        /// <summary> The IsDefaultGroup property of the Entity AppTransactionGroup</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsDefaultGroup
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsDefaultGroupProperty);}
            set { SetValue(IsDefaultGroupProperty,value); }
        }

        /// <summary> The GroupSortOrder property of the Entity AppTransactionGroup</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> GroupSortOrder
        {
            get { return  GetValue<Nullable<System.Int32>>( GroupSortOrderProperty);}
            set { SetValue(GroupSortOrderProperty,value); }
        }

        /// <summary> The EmBuseinssScope property of the Entity AppTransactionGroup</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> EmBuseinssScope
        {
            get { return  GetValue<Nullable<System.Int32>>( EmBuseinssScopeProperty);}
            set { SetValue(EmBuseinssScopeProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppTransactionGroup</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppTransactionGroup</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppTransactionGroup</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppTransactionGroup</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppTransactionGroup</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedByCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByCompanyIdProperty);}
            set { SetValue(AppCreatedByCompanyIdProperty,value); }
        }

        /// <summary> The SaasApplicationId property of the Entity AppTransactionGroup</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SaasApplicationId
        {
            get { return  GetValue<Nullable<System.Int32>>( SaasApplicationIdProperty);}
            set { SetValue(SaasApplicationIdProperty,value); }
        }
        
        #endregion

       
        
    }
}

