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
    /// DTO class for the entity 'AppMessageUserReceived'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppMessageUserReceivedDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string MessageIdProperty = ObjectInfoHelper.GetName<AppMessageUserReceivedDto ,System.Int32>(o => o.MessageId);
        public static readonly string ReadDateProperty = ObjectInfoHelper.GetName<AppMessageUserReceivedDto ,Nullable<System.DateTime>>(o => o.ReadDate);
        public static readonly string ReceivedEmailProperty = ObjectInfoHelper.GetName<AppMessageUserReceivedDto ,System.String>(o => o.ReceivedEmail);
        public static readonly string IsSentByEmailServerProperty = ObjectInfoHelper.GetName<AppMessageUserReceivedDto ,Nullable<System.Boolean>>(o => o.IsSentByEmailServer);
        public static readonly string ReceivedByIdProperty = ObjectInfoHelper.GetName<AppMessageUserReceivedDto ,Nullable<System.Int32>>(o => o.ReceivedById);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppMessageUserReceivedDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppMessageUserReceivedDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppMessageUserReceivedDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppMessageUserReceivedDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppMessageUserReceivedDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppMessageUserReceivedDto()
        {        
        }
		
		static AppMessageUserReceivedDto()
        {
              
			MandatoryProperties.Add(MessageIdProperty);   
			MandatoryProperties.Add(ReceivedEmailProperty);        
			  
			ForeignKeyProperties.Add(MessageIdProperty);          		
                
			DictStringPropertyMaxLength.Add(ReceivedEmailProperty,200);         
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
    


        /// <summary> The MessageId property of the Entity AppMessageUserReceived</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Int32 MessageId
        {
            get { return  GetValue<System.Int32>( MessageIdProperty);}
            set { SetValue(MessageIdProperty,value); }
        }

        /// <summary> The ReadDate property of the Entity AppMessageUserReceived</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> ReadDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( ReadDateProperty);}
            set { SetValue(ReadDateProperty,value); }
        }

        /// <summary> The ReceivedEmail property of the Entity AppMessageUserReceived</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ReceivedEmail
        {
            get { return  GetValue<System.String>( ReceivedEmailProperty);}
            set { SetValue(ReceivedEmailProperty,value); }
        }

        /// <summary> The IsSentByEmailServer property of the Entity AppMessageUserReceived</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsSentByEmailServer
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsSentByEmailServerProperty);}
            set { SetValue(IsSentByEmailServerProperty,value); }
        }

        /// <summary> The ReceivedById property of the Entity AppMessageUserReceived</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ReceivedById
        {
            get { return  GetValue<Nullable<System.Int32>>( ReceivedByIdProperty);}
            set { SetValue(ReceivedByIdProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppMessageUserReceived</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppMessageUserReceived</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppMessageUserReceived</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppMessageUserReceived</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppMessageUserReceived</summary>
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

