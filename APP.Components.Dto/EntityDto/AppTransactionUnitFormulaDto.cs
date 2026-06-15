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
    /// DTO class for the entity 'AppTransactionUnitFormula'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppTransactionUnitFormulaDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string TransactionUnitIdProperty = ObjectInfoHelper.GetName<AppTransactionUnitFormulaDto ,Nullable<System.Int32>>(o => o.TransactionUnitId);
        public static readonly string CaculationFlowSortProperty = ObjectInfoHelper.GetName<AppTransactionUnitFormulaDto ,Nullable<System.Int32>>(o => o.CaculationFlowSort);
        public static readonly string FormulaExpressionProperty = ObjectInfoHelper.GetName<AppTransactionUnitFormulaDto ,System.String>(o => o.FormulaExpression);
        public static readonly string WarningMessageProperty = ObjectInfoHelper.GetName<AppTransactionUnitFormulaDto ,System.String>(o => o.WarningMessage);
        public static readonly string FunctionTypeProperty = ObjectInfoHelper.GetName<AppTransactionUnitFormulaDto ,Nullable<System.Int32>>(o => o.FunctionType);
        public static readonly string OperationTypeProperty = ObjectInfoHelper.GetName<AppTransactionUnitFormulaDto ,Nullable<System.Int32>>(o => o.OperationType);
        public static readonly string ConditionFieldIdProperty = ObjectInfoHelper.GetName<AppTransactionUnitFormulaDto ,Nullable<System.Int32>>(o => o.ConditionFieldId);
        public static readonly string SwitchTrueFalseTypeProperty = ObjectInfoHelper.GetName<AppTransactionUnitFormulaDto ,Nullable<System.Boolean>>(o => o.SwitchTrueFalseType);
        public static readonly string ChildTransactionUnitIdProperty = ObjectInfoHelper.GetName<AppTransactionUnitFormulaDto ,Nullable<System.Int32>>(o => o.ChildTransactionUnitId);
        public static readonly string SystemTimeStampProperty = ObjectInfoHelper.GetName<AppTransactionUnitFormulaDto ,System.Byte[]>(o => o.SystemTimeStamp);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppTransactionUnitFormulaDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppTransactionUnitFormulaDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppTransactionUnitFormulaDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppTransactionUnitFormulaDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppTransactionUnitFormulaDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
        public static readonly string WarningHighlightTransFieldIdProperty = ObjectInfoHelper.GetName<AppTransactionUnitFormulaDto ,Nullable<System.Int32>>(o => o.WarningHighlightTransFieldId);
        public static readonly string WarningHighlightStyleIdProperty = ObjectInfoHelper.GetName<AppTransactionUnitFormulaDto ,Nullable<System.Int32>>(o => o.WarningHighlightStyleId);
        public static readonly string FormulaNameProperty = ObjectInfoHelper.GetName<AppTransactionUnitFormulaDto ,System.String>(o => o.FormulaName);
        public static readonly string ApplyToScopeProperty = ObjectInfoHelper.GetName<AppTransactionUnitFormulaDto ,Nullable<System.Int32>>(o => o.ApplyToScope);
        public static readonly string SearchViewIdProperty = ObjectInfoHelper.GetName<AppTransactionUnitFormulaDto ,Nullable<System.Int32>>(o => o.SearchViewId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppTransactionUnitFormulaDto()
        {        
        }
		
		static AppTransactionUnitFormulaDto()
        {
                
			MandatoryProperties.Add(FormulaExpressionProperty);                  
			  
			ForeignKeyProperties.Add(TransactionUnitIdProperty);         
			ForeignKeyProperties.Add(ChildTransactionUnitIdProperty);            		
                
			DictStringPropertyMaxLength.Add(FormulaExpressionProperty,4000); 
			DictStringPropertyMaxLength.Add(WarningMessageProperty,4000);              
			DictStringPropertyMaxLength.Add(FormulaNameProperty,500);    
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
    


        /// <summary> The TransactionUnitId property of the Entity AppTransactionUnitFormula</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionUnitId
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionUnitIdProperty);}
            set { SetValue(TransactionUnitIdProperty,value); }
        }

        /// <summary> The CaculationFlowSort property of the Entity AppTransactionUnitFormula</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> CaculationFlowSort
        {
            get { return  GetValue<Nullable<System.Int32>>( CaculationFlowSortProperty);}
            set { SetValue(CaculationFlowSortProperty,value); }
        }

        /// <summary> The FormulaExpression property of the Entity AppTransactionUnitFormula</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String FormulaExpression
        {
            get { return  GetValue<System.String>( FormulaExpressionProperty);}
            set { SetValue(FormulaExpressionProperty,value); }
        }

        /// <summary> The WarningMessage property of the Entity AppTransactionUnitFormula</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String WarningMessage
        {
            get { return  GetValue<System.String>( WarningMessageProperty);}
            set { SetValue(WarningMessageProperty,value); }
        }

        /// <summary> The FunctionType property of the Entity AppTransactionUnitFormula</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> FunctionType
        {
            get { return  GetValue<Nullable<System.Int32>>( FunctionTypeProperty);}
            set { SetValue(FunctionTypeProperty,value); }
        }

        /// <summary> The OperationType property of the Entity AppTransactionUnitFormula</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> OperationType
        {
            get { return  GetValue<Nullable<System.Int32>>( OperationTypeProperty);}
            set { SetValue(OperationTypeProperty,value); }
        }

        /// <summary> The ConditionFieldId property of the Entity AppTransactionUnitFormula</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ConditionFieldId
        {
            get { return  GetValue<Nullable<System.Int32>>( ConditionFieldIdProperty);}
            set { SetValue(ConditionFieldIdProperty,value); }
        }

        /// <summary> The SwitchTrueFalseType property of the Entity AppTransactionUnitFormula</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> SwitchTrueFalseType
        {
            get { return  GetValue<Nullable<System.Boolean>>( SwitchTrueFalseTypeProperty);}
            set { SetValue(SwitchTrueFalseTypeProperty,value); }
        }

        /// <summary> The ChildTransactionUnitId property of the Entity AppTransactionUnitFormula</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ChildTransactionUnitId
        {
            get { return  GetValue<Nullable<System.Int32>>( ChildTransactionUnitIdProperty);}
            set { SetValue(ChildTransactionUnitIdProperty,value); }
        }

        /// <summary> The SystemTimeStamp property of the Entity AppTransactionUnitFormula</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Byte[] SystemTimeStamp
        {
            get { return  GetValue<System.Byte[]>( SystemTimeStampProperty);}
            set { SetValue(SystemTimeStampProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppTransactionUnitFormula</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppTransactionUnitFormula</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppTransactionUnitFormula</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppTransactionUnitFormula</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppTransactionUnitFormula</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedByCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByCompanyIdProperty);}
            set { SetValue(AppCreatedByCompanyIdProperty,value); }
        }

        /// <summary> The WarningHighlightTransFieldId property of the Entity AppTransactionUnitFormula</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> WarningHighlightTransFieldId
        {
            get { return  GetValue<Nullable<System.Int32>>( WarningHighlightTransFieldIdProperty);}
            set { SetValue(WarningHighlightTransFieldIdProperty,value); }
        }

        /// <summary> The WarningHighlightStyleId property of the Entity AppTransactionUnitFormula</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> WarningHighlightStyleId
        {
            get { return  GetValue<Nullable<System.Int32>>( WarningHighlightStyleIdProperty);}
            set { SetValue(WarningHighlightStyleIdProperty,value); }
        }

        /// <summary> The FormulaName property of the Entity AppTransactionUnitFormula</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String FormulaName
        {
            get { return  GetValue<System.String>( FormulaNameProperty);}
            set { SetValue(FormulaNameProperty,value); }
        }

        /// <summary> The ApplyToScope property of the Entity AppTransactionUnitFormula</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ApplyToScope
        {
            get { return  GetValue<Nullable<System.Int32>>( ApplyToScopeProperty);}
            set { SetValue(ApplyToScopeProperty,value); }
        }

        /// <summary> The SearchViewId property of the Entity AppTransactionUnitFormula</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SearchViewId
        {
            get { return  GetValue<Nullable<System.Int32>>( SearchViewIdProperty);}
            set { SetValue(SearchViewIdProperty,value); }
        }
        
        #endregion

       
        
    }
}

