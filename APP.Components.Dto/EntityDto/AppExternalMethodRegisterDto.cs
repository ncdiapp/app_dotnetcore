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
    /// DTO class for the entity 'AppExternalMethodRegister'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppExternalMethodRegisterDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string MethodDisplayNameProperty = ObjectInfoHelper.GetName<AppExternalMethodRegisterDto ,System.String>(o => o.MethodDisplayName);
        public static readonly string AssemblyNameProperty = ObjectInfoHelper.GetName<AppExternalMethodRegisterDto ,System.String>(o => o.AssemblyName);
        public static readonly string TypeNameProperty = ObjectInfoHelper.GetName<AppExternalMethodRegisterDto ,System.String>(o => o.TypeName);
        public static readonly string MethodNameProperty = ObjectInfoHelper.GetName<AppExternalMethodRegisterDto ,System.String>(o => o.MethodName);
        public static readonly string InputParameterListProperty = ObjectInfoHelper.GetName<AppExternalMethodRegisterDto ,System.String>(o => o.InputParameterList);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppExternalMethodRegisterDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppExternalMethodRegisterDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppExternalMethodRegisterDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppExternalMethodRegisterDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppExternalMethodRegisterDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppExternalMethodRegisterDto()
        {        
        }
		
		static AppExternalMethodRegisterDto()
        {
                       
			           		
              
			DictStringPropertyMaxLength.Add(MethodDisplayNameProperty,100); 
			DictStringPropertyMaxLength.Add(AssemblyNameProperty,500); 
			DictStringPropertyMaxLength.Add(TypeNameProperty,500); 
			DictStringPropertyMaxLength.Add(MethodNameProperty,500); 
			DictStringPropertyMaxLength.Add(InputParameterListProperty,500);       
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
    


        /// <summary> The MethodDisplayName property of the Entity AppExternalMethodRegister</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String MethodDisplayName
        {
            get { return  GetValue<System.String>( MethodDisplayNameProperty);}
            set { SetValue(MethodDisplayNameProperty,value); }
        }

        /// <summary> The AssemblyName property of the Entity AppExternalMethodRegister</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String AssemblyName
        {
            get { return  GetValue<System.String>( AssemblyNameProperty);}
            set { SetValue(AssemblyNameProperty,value); }
        }

        /// <summary> The TypeName property of the Entity AppExternalMethodRegister</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String TypeName
        {
            get { return  GetValue<System.String>( TypeNameProperty);}
            set { SetValue(TypeNameProperty,value); }
        }

        /// <summary> The MethodName property of the Entity AppExternalMethodRegister</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String MethodName
        {
            get { return  GetValue<System.String>( MethodNameProperty);}
            set { SetValue(MethodNameProperty,value); }
        }

        /// <summary> The InputParameterList property of the Entity AppExternalMethodRegister</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String InputParameterList
        {
            get { return  GetValue<System.String>( InputParameterListProperty);}
            set { SetValue(InputParameterListProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppExternalMethodRegister</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppExternalMethodRegister</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppExternalMethodRegister</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppExternalMethodRegister</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppExternalMethodRegister</summary>
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

