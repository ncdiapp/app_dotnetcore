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
    /// DTO class for the entity 'AppSecurityFormAction'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppSecurityFormActionDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string FormIdProperty = ObjectInfoHelper.GetName<AppSecurityFormActionDto ,Nullable<System.Int32>>(o => o.FormId);
        public static readonly string NameProperty = ObjectInfoHelper.GetName<AppSecurityFormActionDto ,System.String>(o => o.Name);
        public static readonly string DescriptionProperty = ObjectInfoHelper.GetName<AppSecurityFormActionDto ,System.String>(o => o.Description);
        public static readonly string ActionTypeProperty = ObjectInfoHelper.GetName<AppSecurityFormActionDto ,Nullable<System.Int32>>(o => o.ActionType);
        public static readonly string IsNeedSecurityControlProperty = ObjectInfoHelper.GetName<AppSecurityFormActionDto ,Nullable<System.Boolean>>(o => o.IsNeedSecurityControl);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppSecurityFormActionDto()
        {        
        }
		
		static AppSecurityFormActionDto()
        {
                  
			  
			ForeignKeyProperties.Add(FormIdProperty);     		
               
			DictStringPropertyMaxLength.Add(NameProperty,500); 
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
    


        /// <summary> The FormId property of the Entity AppSecurityFormAction</summary>
        [DataMember(EmitDefaultValue=false)]
        public  Nullable<System.Int32> FormId
        {
            get { return  GetValue<Nullable<System.Int32>>( FormIdProperty);}
            set { SetValue(FormIdProperty,value); }
        }

        /// <summary> The Name property of the Entity AppSecurityFormAction</summary>
        [DataMember(EmitDefaultValue=false)]
        public  System.String Name
        {
            get { return  GetValue<System.String>( NameProperty);}
            set { SetValue(NameProperty,value); }
        }

        /// <summary> The Description property of the Entity AppSecurityFormAction</summary>
        [DataMember(EmitDefaultValue=false)]
        public  System.String Description
        {
            get { return  GetValue<System.String>( DescriptionProperty);}
            set { SetValue(DescriptionProperty,value); }
        }

        /// <summary> The ActionType property of the Entity AppSecurityFormAction</summary>
        [DataMember(EmitDefaultValue=false)]
        public  Nullable<System.Int32> ActionType
        {
            get { return  GetValue<Nullable<System.Int32>>( ActionTypeProperty);}
            set { SetValue(ActionTypeProperty,value); }
        }

        /// <summary> The IsNeedSecurityControl property of the Entity AppSecurityFormAction</summary>
        [DataMember(EmitDefaultValue=false)]
        public  Nullable<System.Boolean> IsNeedSecurityControl
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsNeedSecurityControlProperty);}
            set { SetValue(IsNeedSecurityControlProperty,value); }
        }
        
        #endregion

       
        
    }
}

