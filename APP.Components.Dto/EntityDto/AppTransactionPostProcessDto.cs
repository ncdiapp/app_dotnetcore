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
    /// DTO class for the entity 'AppTransactionPostProcess'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppTransactionPostProcessDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string NameProperty = ObjectInfoHelper.GetName<AppTransactionPostProcessDto ,System.String>(o => o.Name);
        public static readonly string TransactionIdProperty = ObjectInfoHelper.GetName<AppTransactionPostProcessDto ,Nullable<System.Int32>>(o => o.TransactionId);
        public static readonly string ProcessFlowProperty = ObjectInfoHelper.GetName<AppTransactionPostProcessDto ,Nullable<System.Int32>>(o => o.ProcessFlow);
        public static readonly string PostStoreProcedureNameProperty = ObjectInfoHelper.GetName<AppTransactionPostProcessDto ,System.String>(o => o.PostStoreProcedureName);
        public static readonly string ExternalCommandProperty = ObjectInfoHelper.GetName<AppTransactionPostProcessDto ,System.String>(o => o.ExternalCommand);
        public static readonly string InternalMethodProperty = ObjectInfoHelper.GetName<AppTransactionPostProcessDto ,System.String>(o => o.InternalMethod);
        public static readonly string RootUnitIdProperty = ObjectInfoHelper.GetName<AppTransactionPostProcessDto ,Nullable<System.Int32>>(o => o.RootUnitId);
        public static readonly string ParameterOptionsProperty = ObjectInfoHelper.GetName<AppTransactionPostProcessDto ,System.String>(o => o.ParameterOptions);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppTransactionPostProcessDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppTransactionPostProcessDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppTransactionPostProcessDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppTransactionPostProcessDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppTransactionPostProcessDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppTransactionPostProcessDto()
        {        
        }
		
		static AppTransactionPostProcessDto()
        {
                          
			   
			ForeignKeyProperties.Add(TransactionIdProperty);      
			ForeignKeyProperties.Add(RootUnitIdProperty);       		
              
			DictStringPropertyMaxLength.Add(NameProperty,200);   
			DictStringPropertyMaxLength.Add(PostStoreProcedureNameProperty,500); 
			DictStringPropertyMaxLength.Add(ExternalCommandProperty,500); 
			DictStringPropertyMaxLength.Add(InternalMethodProperty,500);  
			DictStringPropertyMaxLength.Add(ParameterOptionsProperty,500);       
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
    


        /// <summary> The Name property of the Entity AppTransactionPostProcess</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Name
        {
            get { return  GetValue<System.String>( NameProperty);}
            set { SetValue(NameProperty,value); }
        }

        /// <summary> The TransactionId property of the Entity AppTransactionPostProcess</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionId
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionIdProperty);}
            set { SetValue(TransactionIdProperty,value); }
        }

        /// <summary> The ProcessFlow property of the Entity AppTransactionPostProcess</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ProcessFlow
        {
            get { return  GetValue<Nullable<System.Int32>>( ProcessFlowProperty);}
            set { SetValue(ProcessFlowProperty,value); }
        }

        /// <summary> The PostStoreProcedureName property of the Entity AppTransactionPostProcess</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String PostStoreProcedureName
        {
            get { return  GetValue<System.String>( PostStoreProcedureNameProperty);}
            set { SetValue(PostStoreProcedureNameProperty,value); }
        }

        /// <summary> The ExternalCommand property of the Entity AppTransactionPostProcess</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ExternalCommand
        {
            get { return  GetValue<System.String>( ExternalCommandProperty);}
            set { SetValue(ExternalCommandProperty,value); }
        }

        /// <summary> The InternalMethod property of the Entity AppTransactionPostProcess</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String InternalMethod
        {
            get { return  GetValue<System.String>( InternalMethodProperty);}
            set { SetValue(InternalMethodProperty,value); }
        }

        /// <summary> The RootUnitId property of the Entity AppTransactionPostProcess</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> RootUnitId
        {
            get { return  GetValue<Nullable<System.Int32>>( RootUnitIdProperty);}
            set { SetValue(RootUnitIdProperty,value); }
        }

        /// <summary> The ParameterOptions property of the Entity AppTransactionPostProcess</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ParameterOptions
        {
            get { return  GetValue<System.String>( ParameterOptionsProperty);}
            set { SetValue(ParameterOptionsProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppTransactionPostProcess</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppTransactionPostProcess</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppTransactionPostProcess</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppTransactionPostProcess</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppTransactionPostProcess</summary>
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

