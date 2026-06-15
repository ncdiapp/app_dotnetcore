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
    /// DTO class for the entity 'AppProjectWorkFlowCondition'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppProjectWorkFlowConditionDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string ProjectWorkFlowTaskIdProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowConditionDto ,Nullable<System.Int32>>(o => o.ProjectWorkFlowTaskId);
        public static readonly string ProjectIdProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowConditionDto ,Nullable<System.Int32>>(o => o.ProjectId);
        public static readonly string NameProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowConditionDto ,System.String>(o => o.Name);
        public static readonly string DescriptionProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowConditionDto ,System.String>(o => o.Description);
        public static readonly string FormulaExpressionProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowConditionDto ,System.String>(o => o.FormulaExpression);
        public static readonly string MonitorChildUnitIdProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowConditionDto ,Nullable<System.Int32>>(o => o.MonitorChildUnitId);
        public static readonly string ConditionTransactionFieldIdProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowConditionDto ,Nullable<System.Int32>>(o => o.ConditionTransactionFieldId);
        public static readonly string ConditionTypeIdProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowConditionDto ,Nullable<System.Int32>>(o => o.ConditionTypeId);
        public static readonly string ConditionPredictValueProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowConditionDto ,System.String>(o => o.ConditionPredictValue);
        public static readonly string RowIdentityProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowConditionDto ,System.Guid>(o => o.RowIdentity);
        public static readonly string TriggerFlowOrderProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowConditionDto ,Nullable<System.Int32>>(o => o.TriggerFlowOrder);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowConditionDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowConditionDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowConditionDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowConditionDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowConditionDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
        public static readonly string ConditionGuidProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowConditionDto ,Nullable<System.Guid>>(o => o.ConditionGuid);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppProjectWorkFlowConditionDto()
        {        
        }
		
		static AppProjectWorkFlowConditionDto()
        {
                       
			MandatoryProperties.Add(RowIdentityProperty);        
			  
			ForeignKeyProperties.Add(ProjectWorkFlowTaskIdProperty);  
			ForeignKeyProperties.Add(ProjectIdProperty);     
			ForeignKeyProperties.Add(MonitorChildUnitIdProperty);  
			ForeignKeyProperties.Add(ConditionTransactionFieldIdProperty);           		
                
			DictStringPropertyMaxLength.Add(NameProperty,50); 
			DictStringPropertyMaxLength.Add(DescriptionProperty,500); 
			DictStringPropertyMaxLength.Add(FormulaExpressionProperty,4000);    
			DictStringPropertyMaxLength.Add(ConditionPredictValueProperty,500);          
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
    


        /// <summary> The ProjectWorkFlowTaskId property of the Entity AppProjectWorkFlowCondition</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ProjectWorkFlowTaskId
        {
            get { return  GetValue<Nullable<System.Int32>>( ProjectWorkFlowTaskIdProperty);}
            set { SetValue(ProjectWorkFlowTaskIdProperty,value); }
        }

        /// <summary> The ProjectId property of the Entity AppProjectWorkFlowCondition</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ProjectId
        {
            get { return  GetValue<Nullable<System.Int32>>( ProjectIdProperty);}
            set { SetValue(ProjectIdProperty,value); }
        }

        /// <summary> The Name property of the Entity AppProjectWorkFlowCondition</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Name
        {
            get { return  GetValue<System.String>( NameProperty);}
            set { SetValue(NameProperty,value); }
        }

        /// <summary> The Description property of the Entity AppProjectWorkFlowCondition</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Description
        {
            get { return  GetValue<System.String>( DescriptionProperty);}
            set { SetValue(DescriptionProperty,value); }
        }

        /// <summary> The FormulaExpression property of the Entity AppProjectWorkFlowCondition</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String FormulaExpression
        {
            get { return  GetValue<System.String>( FormulaExpressionProperty);}
            set { SetValue(FormulaExpressionProperty,value); }
        }

        /// <summary> The MonitorChildUnitId property of the Entity AppProjectWorkFlowCondition</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> MonitorChildUnitId
        {
            get { return  GetValue<Nullable<System.Int32>>( MonitorChildUnitIdProperty);}
            set { SetValue(MonitorChildUnitIdProperty,value); }
        }

        /// <summary> The ConditionTransactionFieldId property of the Entity AppProjectWorkFlowCondition</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ConditionTransactionFieldId
        {
            get { return  GetValue<Nullable<System.Int32>>( ConditionTransactionFieldIdProperty);}
            set { SetValue(ConditionTransactionFieldIdProperty,value); }
        }

        /// <summary> The ConditionTypeId property of the Entity AppProjectWorkFlowCondition</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ConditionTypeId
        {
            get { return  GetValue<Nullable<System.Int32>>( ConditionTypeIdProperty);}
            set { SetValue(ConditionTypeIdProperty,value); }
        }

        /// <summary> The ConditionPredictValue property of the Entity AppProjectWorkFlowCondition</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ConditionPredictValue
        {
            get { return  GetValue<System.String>( ConditionPredictValueProperty);}
            set { SetValue(ConditionPredictValueProperty,value); }
        }

        /// <summary> The RowIdentity property of the Entity AppProjectWorkFlowCondition</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Guid RowIdentity
        {
            get { return  GetValue<System.Guid>( RowIdentityProperty);}
            set { SetValue(RowIdentityProperty,value); }
        }

        /// <summary> The TriggerFlowOrder property of the Entity AppProjectWorkFlowCondition</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TriggerFlowOrder
        {
            get { return  GetValue<Nullable<System.Int32>>( TriggerFlowOrderProperty);}
            set { SetValue(TriggerFlowOrderProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppProjectWorkFlowCondition</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppProjectWorkFlowCondition</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppProjectWorkFlowCondition</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppProjectWorkFlowCondition</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppProjectWorkFlowCondition</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedByCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByCompanyIdProperty);}
            set { SetValue(AppCreatedByCompanyIdProperty,value); }
        }

        /// <summary> The ConditionGuid property of the Entity AppProjectWorkFlowCondition</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Guid> ConditionGuid
        {
            get { return  GetValue<Nullable<System.Guid>>( ConditionGuidProperty);}
            set { SetValue(ConditionGuidProperty,value); }
        }
        
        #endregion

       
        
    }
}

