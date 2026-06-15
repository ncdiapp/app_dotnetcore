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
    /// DTO class for the entity 'AppProjectTeam'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppProjectTeamDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string TeamNameProperty = ObjectInfoHelper.GetName<AppProjectTeamDto ,System.String>(o => o.TeamName);
        public static readonly string DescriptionProperty = ObjectInfoHelper.GetName<AppProjectTeamDto ,System.String>(o => o.Description);
        public static readonly string IsPrefinedTeamProperty = ObjectInfoHelper.GetName<AppProjectTeamDto ,Nullable<System.Boolean>>(o => o.IsPrefinedTeam);
        public static readonly string EmPrivacyProperty = ObjectInfoHelper.GetName<AppProjectTeamDto ,Nullable<System.Int32>>(o => o.EmPrivacy);
        public static readonly string ParticipatedDmainIdProperty = ObjectInfoHelper.GetName<AppProjectTeamDto ,System.String>(o => o.ParticipatedDmainId);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppProjectTeamDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppProjectTeamDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppProjectTeamDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppProjectTeamDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppProjectTeamDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppProjectTeamDto()
        {        
        }
		
		static AppProjectTeamDto()
        {
                       
			           		
              
			DictStringPropertyMaxLength.Add(TeamNameProperty,100); 
			DictStringPropertyMaxLength.Add(DescriptionProperty,500);   
			DictStringPropertyMaxLength.Add(ParticipatedDmainIdProperty,500);       
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
    


        /// <summary> The TeamName property of the Entity AppProjectTeam</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String TeamName
        {
            get { return  GetValue<System.String>( TeamNameProperty);}
            set { SetValue(TeamNameProperty,value); }
        }

        /// <summary> The Description property of the Entity AppProjectTeam</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Description
        {
            get { return  GetValue<System.String>( DescriptionProperty);}
            set { SetValue(DescriptionProperty,value); }
        }

        /// <summary> The IsPrefinedTeam property of the Entity AppProjectTeam</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsPrefinedTeam
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsPrefinedTeamProperty);}
            set { SetValue(IsPrefinedTeamProperty,value); }
        }

        /// <summary> The EmPrivacy property of the Entity AppProjectTeam</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> EmPrivacy
        {
            get { return  GetValue<Nullable<System.Int32>>( EmPrivacyProperty);}
            set { SetValue(EmPrivacyProperty,value); }
        }

        /// <summary> The ParticipatedDmainId property of the Entity AppProjectTeam</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ParticipatedDmainId
        {
            get { return  GetValue<System.String>( ParticipatedDmainIdProperty);}
            set { SetValue(ParticipatedDmainIdProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppProjectTeam</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppProjectTeam</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppProjectTeam</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppProjectTeam</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppProjectTeam</summary>
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

