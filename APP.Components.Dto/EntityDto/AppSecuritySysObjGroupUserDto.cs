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
    /// DTO class for the entity 'AppSecuritySysObjGroupUser'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppSecuritySysObjGroupUserDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string GroupIdProperty = ObjectInfoHelper.GetName<AppSecuritySysObjGroupUserDto ,Nullable<System.Int32>>(o => o.GroupId);
        public static readonly string UserIdProperty = ObjectInfoHelper.GetName<AppSecuritySysObjGroupUserDto ,Nullable<System.Int32>>(o => o.UserId);
        public static readonly string OrganizationIdProperty = ObjectInfoHelper.GetName<AppSecuritySysObjGroupUserDto ,Nullable<System.Int32>>(o => o.OrganizationId);
        public static readonly string TransactionIdProperty = ObjectInfoHelper.GetName<AppSecuritySysObjGroupUserDto ,Nullable<System.Int32>>(o => o.TransactionId);
        public static readonly string TransactionUnitIdProperty = ObjectInfoHelper.GetName<AppSecuritySysObjGroupUserDto ,Nullable<System.Int32>>(o => o.TransactionUnitId);
        public static readonly string TransactionFieldIdProperty = ObjectInfoHelper.GetName<AppSecuritySysObjGroupUserDto ,Nullable<System.Int32>>(o => o.TransactionFieldId);
        public static readonly string SearchIdProperty = ObjectInfoHelper.GetName<AppSecuritySysObjGroupUserDto ,Nullable<System.Int32>>(o => o.SearchId);
        public static readonly string SearchViewIdProperty = ObjectInfoHelper.GetName<AppSecuritySysObjGroupUserDto ,Nullable<System.Int32>>(o => o.SearchViewId);
        public static readonly string RouteStateIdProperty = ObjectInfoHelper.GetName<AppSecuritySysObjGroupUserDto ,Nullable<System.Int32>>(o => o.RouteStateId);
        public static readonly string DesktopIdProperty = ObjectInfoHelper.GetName<AppSecuritySysObjGroupUserDto ,Nullable<System.Int32>>(o => o.DesktopId);
        public static readonly string TransactionUnitLinkedSearchIdProperty = ObjectInfoHelper.GetName<AppSecuritySysObjGroupUserDto ,Nullable<System.Int32>>(o => o.TransactionUnitLinkedSearchId);
        public static readonly string UserActionTransactionIdProperty = ObjectInfoHelper.GetName<AppSecuritySysObjGroupUserDto ,Nullable<System.Int32>>(o => o.UserActionTransactionId);
        public static readonly string UserActionTransactionCodeProperty = ObjectInfoHelper.GetName<AppSecuritySysObjGroupUserDto ,Nullable<System.Int32>>(o => o.UserActionTransactionCode);
        public static readonly string UserActionTransactionUnitIdProperty = ObjectInfoHelper.GetName<AppSecuritySysObjGroupUserDto ,Nullable<System.Int32>>(o => o.UserActionTransactionUnitId);
        public static readonly string UserActionTransactionUnitCodeProperty = ObjectInfoHelper.GetName<AppSecuritySysObjGroupUserDto ,Nullable<System.Int32>>(o => o.UserActionTransactionUnitCode);
        public static readonly string FormLinkTargetIdProperty = ObjectInfoHelper.GetName<AppSecuritySysObjGroupUserDto ,Nullable<System.Int32>>(o => o.FormLinkTargetId);
        public static readonly string ReportIdProperty = ObjectInfoHelper.GetName<AppSecuritySysObjGroupUserDto ,Nullable<System.Int32>>(o => o.ReportId);
        public static readonly string EmUserTypeProperty = ObjectInfoHelper.GetName<AppSecuritySysObjGroupUserDto ,Nullable<System.Int32>>(o => o.EmUserType);
        public static readonly string IsInVisibleProperty = ObjectInfoHelper.GetName<AppSecuritySysObjGroupUserDto ,Nullable<System.Boolean>>(o => o.IsInVisible);
        public static readonly string IsUnSaveAbleProperty = ObjectInfoHelper.GetName<AppSecuritySysObjGroupUserDto ,Nullable<System.Boolean>>(o => o.IsUnSaveAble);
        public static readonly string IsSpecialPermissionProperty = ObjectInfoHelper.GetName<AppSecuritySysObjGroupUserDto ,Nullable<System.Boolean>>(o => o.IsSpecialPermission);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppSecuritySysObjGroupUserDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppSecuritySysObjGroupUserDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppSecuritySysObjGroupUserDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppSecuritySysObjGroupUserDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppSecuritySysObjGroupUserDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
        public static readonly string IsIgnoreFilterByProperty = ObjectInfoHelper.GetName<AppSecuritySysObjGroupUserDto ,Nullable<System.Boolean>>(o => o.IsIgnoreFilterBy);
        public static readonly string IsDefaultProperty = ObjectInfoHelper.GetName<AppSecuritySysObjGroupUserDto ,Nullable<System.Boolean>>(o => o.IsDefault);
        public static readonly string IsNeedSpecailEditPrivilegeProperty = ObjectInfoHelper.GetName<AppSecuritySysObjGroupUserDto ,Nullable<System.Boolean>>(o => o.IsNeedSpecailEditPrivilege);
        public static readonly string CommandIdProperty = ObjectInfoHelper.GetName<AppSecuritySysObjGroupUserDto ,Nullable<System.Int32>>(o => o.CommandId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppSecuritySysObjGroupUserDto()
        {        
        }
		
		static AppSecuritySysObjGroupUserDto()
        {
                                           
			  
			ForeignKeyProperties.Add(GroupIdProperty);  
			ForeignKeyProperties.Add(UserIdProperty);  
			ForeignKeyProperties.Add(OrganizationIdProperty);  
			ForeignKeyProperties.Add(TransactionIdProperty);  
			ForeignKeyProperties.Add(TransactionUnitIdProperty);  
			ForeignKeyProperties.Add(TransactionFieldIdProperty);  
			ForeignKeyProperties.Add(SearchIdProperty);  
			ForeignKeyProperties.Add(SearchViewIdProperty);  
			ForeignKeyProperties.Add(RouteStateIdProperty);  
			ForeignKeyProperties.Add(DesktopIdProperty);  
			ForeignKeyProperties.Add(TransactionUnitLinkedSearchIdProperty);  
			ForeignKeyProperties.Add(UserActionTransactionIdProperty);   
			ForeignKeyProperties.Add(UserActionTransactionUnitIdProperty);   
			ForeignKeyProperties.Add(FormLinkTargetIdProperty);  
			ForeignKeyProperties.Add(ReportIdProperty);              		
                                             
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
    


        /// <summary> The GroupId property of the Entity AppSecuritySysObjGroupUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> GroupId
        {
            get { return  GetValue<Nullable<System.Int32>>( GroupIdProperty);}
            set { SetValue(GroupIdProperty,value); }
        }

        /// <summary> The UserId property of the Entity AppSecuritySysObjGroupUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> UserId
        {
            get { return  GetValue<Nullable<System.Int32>>( UserIdProperty);}
            set { SetValue(UserIdProperty,value); }
        }

        /// <summary> The OrganizationId property of the Entity AppSecuritySysObjGroupUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> OrganizationId
        {
            get { return  GetValue<Nullable<System.Int32>>( OrganizationIdProperty);}
            set { SetValue(OrganizationIdProperty,value); }
        }

        /// <summary> The TransactionId property of the Entity AppSecuritySysObjGroupUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionId
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionIdProperty);}
            set { SetValue(TransactionIdProperty,value); }
        }

        /// <summary> The TransactionUnitId property of the Entity AppSecuritySysObjGroupUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionUnitId
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionUnitIdProperty);}
            set { SetValue(TransactionUnitIdProperty,value); }
        }

        /// <summary> The TransactionFieldId property of the Entity AppSecuritySysObjGroupUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionFieldId
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionFieldIdProperty);}
            set { SetValue(TransactionFieldIdProperty,value); }
        }

        /// <summary> The SearchId property of the Entity AppSecuritySysObjGroupUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SearchId
        {
            get { return  GetValue<Nullable<System.Int32>>( SearchIdProperty);}
            set { SetValue(SearchIdProperty,value); }
        }

        /// <summary> The SearchViewId property of the Entity AppSecuritySysObjGroupUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SearchViewId
        {
            get { return  GetValue<Nullable<System.Int32>>( SearchViewIdProperty);}
            set { SetValue(SearchViewIdProperty,value); }
        }

        /// <summary> The RouteStateId property of the Entity AppSecuritySysObjGroupUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> RouteStateId
        {
            get { return  GetValue<Nullable<System.Int32>>( RouteStateIdProperty);}
            set { SetValue(RouteStateIdProperty,value); }
        }

        /// <summary> The DesktopId property of the Entity AppSecuritySysObjGroupUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DesktopId
        {
            get { return  GetValue<Nullable<System.Int32>>( DesktopIdProperty);}
            set { SetValue(DesktopIdProperty,value); }
        }

        /// <summary> The TransactionUnitLinkedSearchId property of the Entity AppSecuritySysObjGroupUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionUnitLinkedSearchId
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionUnitLinkedSearchIdProperty);}
            set { SetValue(TransactionUnitLinkedSearchIdProperty,value); }
        }

        /// <summary> The UserActionTransactionId property of the Entity AppSecuritySysObjGroupUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> UserActionTransactionId
        {
            get { return  GetValue<Nullable<System.Int32>>( UserActionTransactionIdProperty);}
            set { SetValue(UserActionTransactionIdProperty,value); }
        }

        /// <summary> The UserActionTransactionCode property of the Entity AppSecuritySysObjGroupUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> UserActionTransactionCode
        {
            get { return  GetValue<Nullable<System.Int32>>( UserActionTransactionCodeProperty);}
            set { SetValue(UserActionTransactionCodeProperty,value); }
        }

        /// <summary> The UserActionTransactionUnitId property of the Entity AppSecuritySysObjGroupUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> UserActionTransactionUnitId
        {
            get { return  GetValue<Nullable<System.Int32>>( UserActionTransactionUnitIdProperty);}
            set { SetValue(UserActionTransactionUnitIdProperty,value); }
        }

        /// <summary> The UserActionTransactionUnitCode property of the Entity AppSecuritySysObjGroupUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> UserActionTransactionUnitCode
        {
            get { return  GetValue<Nullable<System.Int32>>( UserActionTransactionUnitCodeProperty);}
            set { SetValue(UserActionTransactionUnitCodeProperty,value); }
        }

        /// <summary> The FormLinkTargetId property of the Entity AppSecuritySysObjGroupUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> FormLinkTargetId
        {
            get { return  GetValue<Nullable<System.Int32>>( FormLinkTargetIdProperty);}
            set { SetValue(FormLinkTargetIdProperty,value); }
        }

        /// <summary> The ReportId property of the Entity AppSecuritySysObjGroupUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ReportId
        {
            get { return  GetValue<Nullable<System.Int32>>( ReportIdProperty);}
            set { SetValue(ReportIdProperty,value); }
        }

        /// <summary> The EmUserType property of the Entity AppSecuritySysObjGroupUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> EmUserType
        {
            get { return  GetValue<Nullable<System.Int32>>( EmUserTypeProperty);}
            set { SetValue(EmUserTypeProperty,value); }
        }

        /// <summary> The IsInVisible property of the Entity AppSecuritySysObjGroupUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsInVisible
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsInVisibleProperty);}
            set { SetValue(IsInVisibleProperty,value); }
        }

        /// <summary> The IsUnSaveAble property of the Entity AppSecuritySysObjGroupUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsUnSaveAble
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsUnSaveAbleProperty);}
            set { SetValue(IsUnSaveAbleProperty,value); }
        }

        /// <summary> The IsSpecialPermission property of the Entity AppSecuritySysObjGroupUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsSpecialPermission
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsSpecialPermissionProperty);}
            set { SetValue(IsSpecialPermissionProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppSecuritySysObjGroupUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppSecuritySysObjGroupUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppSecuritySysObjGroupUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppSecuritySysObjGroupUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppSecuritySysObjGroupUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedByCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByCompanyIdProperty);}
            set { SetValue(AppCreatedByCompanyIdProperty,value); }
        }

        /// <summary> The IsIgnoreFilterBy property of the Entity AppSecuritySysObjGroupUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsIgnoreFilterBy
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsIgnoreFilterByProperty);}
            set { SetValue(IsIgnoreFilterByProperty,value); }
        }

        /// <summary> The IsDefault property of the Entity AppSecuritySysObjGroupUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsDefault
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsDefaultProperty);}
            set { SetValue(IsDefaultProperty,value); }
        }

        /// <summary> The IsNeedSpecailEditPrivilege property of the Entity AppSecuritySysObjGroupUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsNeedSpecailEditPrivilege
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsNeedSpecailEditPrivilegeProperty);}
            set { SetValue(IsNeedSpecailEditPrivilegeProperty,value); }
        }

        /// <summary> The CommandId property of the Entity AppSecuritySysObjGroupUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> CommandId
        {
            get { return  GetValue<Nullable<System.Int32>>( CommandIdProperty);}
            set { SetValue(CommandIdProperty,value); }
        }
        
        #endregion

       
        
    }
}

