using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Framework;
using APP.Framework.Validation;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    /// <summary>
    /// DTO class for the entity 'AppOrganizationLevel'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppOrganizationLevelDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string ClassificationLevelProperty = ObjectInfoHelper.GetName<AppOrganizationLevelDto ,Nullable<System.Int32>>(o => o.ClassificationLevel);
        public static readonly string CodeNumProperty = ObjectInfoHelper.GetName<AppOrganizationLevelDto ,System.String>(o => o.CodeNum);
        public static readonly string LevelNameProperty = ObjectInfoHelper.GetName<AppOrganizationLevelDto ,System.String>(o => o.LevelName);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppOrganizationLevelDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppOrganizationLevelDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppOrganizationLevelDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppOrganizationLevelDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppOrganizationLevelDto()
        {        
        }
		
		static AppOrganizationLevelDto()
        {
                    
			        		
               
			DictStringPropertyMaxLength.Add(CodeNumProperty,10); 
			DictStringPropertyMaxLength.Add(LevelNameProperty,50);      
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
    


        /// <summary> The ClassificationLevel property of the Entity AppOrganizationLevel</summary>
        [DataMember(EmitDefaultValue=false)]
        public  Nullable<System.Int32> ClassificationLevel
        {
            get { return  GetValue<Nullable<System.Int32>>( ClassificationLevelProperty);}
            set { SetValue(ClassificationLevelProperty,value); }
        }

        /// <summary> The CodeNum property of the Entity AppOrganizationLevel</summary>
        [DataMember(EmitDefaultValue=false)]
        public  System.String CodeNum
        {
            get { return  GetValue<System.String>( CodeNumProperty);}
            set { SetValue(CodeNumProperty,value); }
        }

        /// <summary> The LevelName property of the Entity AppOrganizationLevel</summary>
        [DataMember(EmitDefaultValue=false)]
        public  System.String LevelName
        {
            get { return  GetValue<System.String>( LevelNameProperty);}
            set { SetValue(LevelNameProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppOrganizationLevel</summary>
        [DataMember(EmitDefaultValue=false)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppOrganizationLevel</summary>
        [DataMember(EmitDefaultValue=false)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppOrganizationLevel</summary>
        [DataMember(EmitDefaultValue=false)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppOrganizationLevel</summary>
        [DataMember(EmitDefaultValue=false)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }
        
        #endregion

       
        
    }
}

