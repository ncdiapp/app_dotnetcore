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
    /// DTO class for the entity 'AppSecurityFormActionResource'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppSecurityFormActionResourceDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string FormActionIdProperty = ObjectInfoHelper.GetName<AppSecurityFormActionResourceDto ,System.Int32>(o => o.FormActionId);
        public static readonly string GroupIdProperty = ObjectInfoHelper.GetName<AppSecurityFormActionResourceDto ,Nullable<System.Int32>>(o => o.GroupId);
        public static readonly string UserIdProperty = ObjectInfoHelper.GetName<AppSecurityFormActionResourceDto ,Nullable<System.Int32>>(o => o.UserId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppSecurityFormActionResourceDto()
        {        
        }
		
		static AppSecurityFormActionResourceDto()
        {
              
			MandatoryProperties.Add(FormActionIdProperty);   
			  
			ForeignKeyProperties.Add(FormActionIdProperty);  
			ForeignKeyProperties.Add(GroupIdProperty);  
			ForeignKeyProperties.Add(UserIdProperty); 		
                  
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
    


        /// <summary> The FormActionId property of the Entity AppSecurityFormActionResource</summary>
        [DataMember(EmitDefaultValue=false)]
        public  System.Int32 FormActionId
        {
            get { return  GetValue<System.Int32>( FormActionIdProperty);}
            set { SetValue(FormActionIdProperty,value); }
        }

        /// <summary> The GroupId property of the Entity AppSecurityFormActionResource</summary>
        [DataMember(EmitDefaultValue=false)]
        public  Nullable<System.Int32> GroupId
        {
            get { return  GetValue<Nullable<System.Int32>>( GroupIdProperty);}
            set { SetValue(GroupIdProperty,value); }
        }

        /// <summary> The UserId property of the Entity AppSecurityFormActionResource</summary>
        [DataMember(EmitDefaultValue=false)]
        public  Nullable<System.Int32> UserId
        {
            get { return  GetValue<Nullable<System.Int32>>( UserIdProperty);}
            set { SetValue(UserIdProperty,value); }
        }
        
        #endregion

       
        
    }
}

