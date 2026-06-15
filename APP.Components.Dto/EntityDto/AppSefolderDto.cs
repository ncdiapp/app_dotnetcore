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
    /// DTO class for the entity 'AppSefolder'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppSefolderDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string FolderTypeProperty = ObjectInfoHelper.GetName<AppSefolderDto ,Nullable<System.Int32>>(o => o.FolderType);
        public static readonly string NameProperty = ObjectInfoHelper.GetName<AppSefolderDto ,System.String>(o => o.Name);
        public static readonly string DescriptionProperty = ObjectInfoHelper.GetName<AppSefolderDto ,System.String>(o => o.Description);
        public static readonly string ParentIdProperty = ObjectInfoHelper.GetName<AppSefolderDto ,Nullable<System.Int32>>(o => o.ParentId);
        public static readonly string IsSystemFolderProperty = ObjectInfoHelper.GetName<AppSefolderDto ,Nullable<System.Boolean>>(o => o.IsSystemFolder);
        public static readonly string DefaultViewIdProperty = ObjectInfoHelper.GetName<AppSefolderDto ,Nullable<System.Int32>>(o => o.DefaultViewId);
        public static readonly string TransactionIdProperty = ObjectInfoHelper.GetName<AppSefolderDto ,Nullable<System.Int32>>(o => o.TransactionId);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppSefolderDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppSefolderDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppSefolderDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppSefolderDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppSefolderDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
        public static readonly string OtherSettingsProperty = ObjectInfoHelper.GetName<AppSefolderDto ,System.String>(o => o.OtherSettings);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppSefolderDto()
        {        
        }
		
		static AppSefolderDto()
        {
               
			MandatoryProperties.Add(NameProperty);            
			        
			ForeignKeyProperties.Add(TransactionIdProperty);       		
               
			DictStringPropertyMaxLength.Add(NameProperty,200); 
			DictStringPropertyMaxLength.Add(DescriptionProperty,500);          
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
    


        /// <summary> The FolderType property of the Entity AppSefolder</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> FolderType
        {
            get { return  GetValue<Nullable<System.Int32>>( FolderTypeProperty);}
            set { SetValue(FolderTypeProperty,value); }
        }

        /// <summary> The Name property of the Entity AppSefolder</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Name
        {
            get { return  GetValue<System.String>( NameProperty);}
            set { SetValue(NameProperty,value); }
        }

        /// <summary> The Description property of the Entity AppSefolder</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Description
        {
            get { return  GetValue<System.String>( DescriptionProperty);}
            set { SetValue(DescriptionProperty,value); }
        }

        /// <summary> The ParentId property of the Entity AppSefolder</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ParentId
        {
            get { return  GetValue<Nullable<System.Int32>>( ParentIdProperty);}
            set { SetValue(ParentIdProperty,value); }
        }

        /// <summary> The IsSystemFolder property of the Entity AppSefolder</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsSystemFolder
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsSystemFolderProperty);}
            set { SetValue(IsSystemFolderProperty,value); }
        }

        /// <summary> The DefaultViewId property of the Entity AppSefolder</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DefaultViewId
        {
            get { return  GetValue<Nullable<System.Int32>>( DefaultViewIdProperty);}
            set { SetValue(DefaultViewIdProperty,value); }
        }

        /// <summary> The TransactionId property of the Entity AppSefolder</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionId
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionIdProperty);}
            set { SetValue(TransactionIdProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppSefolder</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppSefolder</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppSefolder</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppSefolder</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppSefolder</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedByCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByCompanyIdProperty);}
            set { SetValue(AppCreatedByCompanyIdProperty,value); }
        }

        /// <summary> The OtherSettings property of the Entity AppSefolder</summary>
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

