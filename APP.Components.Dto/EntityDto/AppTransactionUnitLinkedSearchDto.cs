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
    /// DTO class for the entity 'AppTransactionUnitLinkedSearch'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppTransactionUnitLinkedSearchDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string TransactionUnitIdProperty = ObjectInfoHelper.GetName<AppTransactionUnitLinkedSearchDto ,System.Int32>(o => o.TransactionUnitId);
        public static readonly string SearchIdProperty = ObjectInfoHelper.GetName<AppTransactionUnitLinkedSearchDto ,Nullable<System.Int32>>(o => o.SearchId);
        public static readonly string SearchSaveIdProperty = ObjectInfoHelper.GetName<AppTransactionUnitLinkedSearchDto ,Nullable<System.Int32>>(o => o.SearchSaveId);
        public static readonly string SearchViewIdProperty = ObjectInfoHelper.GetName<AppTransactionUnitLinkedSearchDto ,System.Int32>(o => o.SearchViewId);
        public static readonly string NameProperty = ObjectInfoHelper.GetName<AppTransactionUnitLinkedSearchDto ,System.String>(o => o.Name);
        public static readonly string ActionProperty = ObjectInfoHelper.GetName<AppTransactionUnitLinkedSearchDto ,Nullable<System.Int32>>(o => o.Action);
        public static readonly string IsSingleSelectedRowProperty = ObjectInfoHelper.GetName<AppTransactionUnitLinkedSearchDto ,Nullable<System.Boolean>>(o => o.IsSingleSelectedRow);
        public static readonly string DescriptionProperty = ObjectInfoHelper.GetName<AppTransactionUnitLinkedSearchDto ,System.String>(o => o.Description);
        public static readonly string UsageTypeProperty = ObjectInfoHelper.GetName<AppTransactionUnitLinkedSearchDto ,Nullable<System.Int32>>(o => o.UsageType);
        public static readonly string GroupNameProperty = ObjectInfoHelper.GetName<AppTransactionUnitLinkedSearchDto ,System.String>(o => o.GroupName);
        public static readonly string IsNeedPreValidationProperty = ObjectInfoHelper.GetName<AppTransactionUnitLinkedSearchDto ,Nullable<System.Boolean>>(o => o.IsNeedPreValidation);
        public static readonly string IsNeedPostValidationProperty = ObjectInfoHelper.GetName<AppTransactionUnitLinkedSearchDto ,Nullable<System.Boolean>>(o => o.IsNeedPostValidation);
        public static readonly string CallbackRestResourceUriProperty = ObjectInfoHelper.GetName<AppTransactionUnitLinkedSearchDto ,System.String>(o => o.CallbackRestResourceUri);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppTransactionUnitLinkedSearchDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppTransactionUnitLinkedSearchDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppTransactionUnitLinkedSearchDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppTransactionUnitLinkedSearchDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppTransactionUnitLinkedSearchDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
        public static readonly string TargetTransactionIdProperty = ObjectInfoHelper.GetName<AppTransactionUnitLinkedSearchDto ,Nullable<System.Int32>>(o => o.TargetTransactionId);
        public static readonly string ConditionTransFieldIdProperty = ObjectInfoHelper.GetName<AppTransactionUnitLinkedSearchDto ,Nullable<System.Int32>>(o => o.ConditionTransFieldId);
        public static readonly string CallBackCommandIdProperty = ObjectInfoHelper.GetName<AppTransactionUnitLinkedSearchDto ,Nullable<System.Int32>>(o => o.CallBackCommandId);
        public static readonly string SortProperty = ObjectInfoHelper.GetName<AppTransactionUnitLinkedSearchDto ,Nullable<System.Int32>>(o => o.Sort);
        public static readonly string IsPopupProperty = ObjectInfoHelper.GetName<AppTransactionUnitLinkedSearchDto ,Nullable<System.Boolean>>(o => o.IsPopup);
        public static readonly string PopupWidthProperty = ObjectInfoHelper.GetName<AppTransactionUnitLinkedSearchDto ,Nullable<System.Int32>>(o => o.PopupWidth);
        public static readonly string PopupHeightProperty = ObjectInfoHelper.GetName<AppTransactionUnitLinkedSearchDto ,Nullable<System.Int32>>(o => o.PopupHeight);
        public static readonly string IconNameProperty = ObjectInfoHelper.GetName<AppTransactionUnitLinkedSearchDto ,System.String>(o => o.IconName);
        public static readonly string OtherSettingsProperty = ObjectInfoHelper.GetName<AppTransactionUnitLinkedSearchDto ,System.String>(o => o.OtherSettings);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppTransactionUnitLinkedSearchDto()
        {        
        }
		
		static AppTransactionUnitLinkedSearchDto()
        {
              
			MandatoryProperties.Add(TransactionUnitIdProperty);    
			MandatoryProperties.Add(SearchViewIdProperty);                        
			  
			ForeignKeyProperties.Add(TransactionUnitIdProperty);  
			ForeignKeyProperties.Add(SearchIdProperty);  
			ForeignKeyProperties.Add(SearchSaveIdProperty);  
			ForeignKeyProperties.Add(SearchViewIdProperty);                
			ForeignKeyProperties.Add(TargetTransactionIdProperty);         		
                  
			DictStringPropertyMaxLength.Add(NameProperty,100);   
			DictStringPropertyMaxLength.Add(DescriptionProperty,500);  
			DictStringPropertyMaxLength.Add(GroupNameProperty,100);   
			DictStringPropertyMaxLength.Add(CallbackRestResourceUriProperty,1000);             
			DictStringPropertyMaxLength.Add(IconNameProperty,500); 
			DictStringPropertyMaxLength.Add(OtherSettingsProperty,2147483647);  
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
    


        /// <summary> The TransactionUnitId property of the Entity AppTransactionUnitLinkedSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Int32 TransactionUnitId
        {
            get { return  GetValue<System.Int32>( TransactionUnitIdProperty);}
            set { SetValue(TransactionUnitIdProperty,value); }
        }

        /// <summary> The SearchId property of the Entity AppTransactionUnitLinkedSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SearchId
        {
            get { return  GetValue<Nullable<System.Int32>>( SearchIdProperty);}
            set { SetValue(SearchIdProperty,value); }
        }

        /// <summary> The SearchSaveId property of the Entity AppTransactionUnitLinkedSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SearchSaveId
        {
            get { return  GetValue<Nullable<System.Int32>>( SearchSaveIdProperty);}
            set { SetValue(SearchSaveIdProperty,value); }
        }

        /// <summary> The SearchViewId property of the Entity AppTransactionUnitLinkedSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Int32 SearchViewId
        {
            get { return  GetValue<System.Int32>( SearchViewIdProperty);}
            set { SetValue(SearchViewIdProperty,value); }
        }

        /// <summary> The Name property of the Entity AppTransactionUnitLinkedSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Name
        {
            get { return  GetValue<System.String>( NameProperty);}
            set { SetValue(NameProperty,value); }
        }

        /// <summary> The Action property of the Entity AppTransactionUnitLinkedSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> Action
        {
            get { return  GetValue<Nullable<System.Int32>>( ActionProperty);}
            set { SetValue(ActionProperty,value); }
        }

        /// <summary> The IsSingleSelectedRow property of the Entity AppTransactionUnitLinkedSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsSingleSelectedRow
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsSingleSelectedRowProperty);}
            set { SetValue(IsSingleSelectedRowProperty,value); }
        }

        /// <summary> The Description property of the Entity AppTransactionUnitLinkedSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Description
        {
            get { return  GetValue<System.String>( DescriptionProperty);}
            set { SetValue(DescriptionProperty,value); }
        }

        /// <summary> The UsageType property of the Entity AppTransactionUnitLinkedSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> UsageType
        {
            get { return  GetValue<Nullable<System.Int32>>( UsageTypeProperty);}
            set { SetValue(UsageTypeProperty,value); }
        }

        /// <summary> The GroupName property of the Entity AppTransactionUnitLinkedSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String GroupName
        {
            get { return  GetValue<System.String>( GroupNameProperty);}
            set { SetValue(GroupNameProperty,value); }
        }

        /// <summary> The IsNeedPreValidation property of the Entity AppTransactionUnitLinkedSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsNeedPreValidation
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsNeedPreValidationProperty);}
            set { SetValue(IsNeedPreValidationProperty,value); }
        }

        /// <summary> The IsNeedPostValidation property of the Entity AppTransactionUnitLinkedSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsNeedPostValidation
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsNeedPostValidationProperty);}
            set { SetValue(IsNeedPostValidationProperty,value); }
        }

        /// <summary> The CallbackRestResourceUri property of the Entity AppTransactionUnitLinkedSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String CallbackRestResourceUri
        {
            get { return  GetValue<System.String>( CallbackRestResourceUriProperty);}
            set { SetValue(CallbackRestResourceUriProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppTransactionUnitLinkedSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppTransactionUnitLinkedSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppTransactionUnitLinkedSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppTransactionUnitLinkedSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppTransactionUnitLinkedSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedByCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByCompanyIdProperty);}
            set { SetValue(AppCreatedByCompanyIdProperty,value); }
        }

        /// <summary> The TargetTransactionId property of the Entity AppTransactionUnitLinkedSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TargetTransactionId
        {
            get { return  GetValue<Nullable<System.Int32>>( TargetTransactionIdProperty);}
            set { SetValue(TargetTransactionIdProperty,value); }
        }

        /// <summary> The ConditionTransFieldId property of the Entity AppTransactionUnitLinkedSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ConditionTransFieldId
        {
            get { return  GetValue<Nullable<System.Int32>>( ConditionTransFieldIdProperty);}
            set { SetValue(ConditionTransFieldIdProperty,value); }
        }

        /// <summary> The CallBackCommandId property of the Entity AppTransactionUnitLinkedSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> CallBackCommandId
        {
            get { return  GetValue<Nullable<System.Int32>>( CallBackCommandIdProperty);}
            set { SetValue(CallBackCommandIdProperty,value); }
        }

        /// <summary> The Sort property of the Entity AppTransactionUnitLinkedSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> Sort
        {
            get { return  GetValue<Nullable<System.Int32>>( SortProperty);}
            set { SetValue(SortProperty,value); }
        }

        /// <summary> The IsPopup property of the Entity AppTransactionUnitLinkedSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsPopup
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsPopupProperty);}
            set { SetValue(IsPopupProperty,value); }
        }

        /// <summary> The PopupWidth property of the Entity AppTransactionUnitLinkedSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> PopupWidth
        {
            get { return  GetValue<Nullable<System.Int32>>( PopupWidthProperty);}
            set { SetValue(PopupWidthProperty,value); }
        }

        /// <summary> The PopupHeight property of the Entity AppTransactionUnitLinkedSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> PopupHeight
        {
            get { return  GetValue<Nullable<System.Int32>>( PopupHeightProperty);}
            set { SetValue(PopupHeightProperty,value); }
        }

        /// <summary> The IconName property of the Entity AppTransactionUnitLinkedSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String IconName
        {
            get { return  GetValue<System.String>( IconNameProperty);}
            set { SetValue(IconNameProperty,value); }
        }

        /// <summary> The OtherSettings property of the Entity AppTransactionUnitLinkedSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String OtherSettings
        {
            get { return  GetValue<System.String>( OtherSettingsProperty);}
            set { SetValue(OtherSettingsProperty,value); }
        }
        
        #endregion

       
        
    }
}

