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
    /// DTO class for the entity 'AppUserMessgeFollowup'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppUserMessgeFollowupDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string UserIdProperty = ObjectInfoHelper.GetName<AppUserMessgeFollowupDto ,Nullable<System.Int32>>(o => o.UserId);
        public static readonly string TransactionIdProperty = ObjectInfoHelper.GetName<AppUserMessgeFollowupDto ,Nullable<System.Int32>>(o => o.TransactionId);
        public static readonly string TransactionRootValueIdProperty = ObjectInfoHelper.GetName<AppUserMessgeFollowupDto ,System.String>(o => o.TransactionRootValueId);
        public static readonly string ProjectActivityIdProperty = ObjectInfoHelper.GetName<AppUserMessgeFollowupDto ,Nullable<System.Int32>>(o => o.ProjectActivityId);
        public static readonly string ProjectTeamIdProperty = ObjectInfoHelper.GetName<AppUserMessgeFollowupDto ,Nullable<System.Int32>>(o => o.ProjectTeamId);
        public static readonly string ProjectIdProperty = ObjectInfoHelper.GetName<AppUserMessgeFollowupDto ,Nullable<System.Int32>>(o => o.ProjectId);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppUserMessgeFollowupDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppUserMessgeFollowupDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppUserMessgeFollowupDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppUserMessgeFollowupDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppUserMessgeFollowupDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
        public static readonly string RoleIdProperty = ObjectInfoHelper.GetName<AppUserMessgeFollowupDto ,Nullable<System.Int32>>(o => o.RoleId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppUserMessgeFollowupDto()
        {        
        }
		
		static AppUserMessgeFollowupDto()
        {
                         
			             		
                
			DictStringPropertyMaxLength.Add(TransactionRootValueIdProperty,200);           
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
    


        /// <summary> The UserId property of the Entity AppUserMessgeFollowup</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> UserId
        {
            get { return  GetValue<Nullable<System.Int32>>( UserIdProperty);}
            set { SetValue(UserIdProperty,value); }
        }

        /// <summary> The TransactionId property of the Entity AppUserMessgeFollowup</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionId
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionIdProperty);}
            set { SetValue(TransactionIdProperty,value); }
        }

        /// <summary> The TransactionRootValueId property of the Entity AppUserMessgeFollowup</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String TransactionRootValueId
        {
            get { return  GetValue<System.String>( TransactionRootValueIdProperty);}
            set { SetValue(TransactionRootValueIdProperty,value); }
        }

        /// <summary> The ProjectActivityId property of the Entity AppUserMessgeFollowup</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ProjectActivityId
        {
            get { return  GetValue<Nullable<System.Int32>>( ProjectActivityIdProperty);}
            set { SetValue(ProjectActivityIdProperty,value); }
        }

        /// <summary> The ProjectTeamId property of the Entity AppUserMessgeFollowup</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ProjectTeamId
        {
            get { return  GetValue<Nullable<System.Int32>>( ProjectTeamIdProperty);}
            set { SetValue(ProjectTeamIdProperty,value); }
        }

        /// <summary> The ProjectId property of the Entity AppUserMessgeFollowup</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ProjectId
        {
            get { return  GetValue<Nullable<System.Int32>>( ProjectIdProperty);}
            set { SetValue(ProjectIdProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppUserMessgeFollowup</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppUserMessgeFollowup</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppUserMessgeFollowup</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppUserMessgeFollowup</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppUserMessgeFollowup</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedByCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByCompanyIdProperty);}
            set { SetValue(AppCreatedByCompanyIdProperty,value); }
        }

        /// <summary> The RoleId property of the Entity AppUserMessgeFollowup</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> RoleId
        {
            get { return  GetValue<Nullable<System.Int32>>( RoleIdProperty);}
            set { SetValue(RoleIdProperty,value); }
        }
        
        #endregion

       
        
    }
}

