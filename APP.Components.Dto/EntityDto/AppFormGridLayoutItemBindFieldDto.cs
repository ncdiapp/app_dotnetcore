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
    /// DTO class for the entity 'AppFormGridLayoutItemBindField'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppFormGridLayoutItemBindFieldDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string FormLayoutIdProperty = ObjectInfoHelper.GetName<AppFormGridLayoutItemBindFieldDto ,Nullable<System.Int32>>(o => o.FormLayoutId);
        public static readonly string TransactionFieldProperty = ObjectInfoHelper.GetName<AppFormGridLayoutItemBindFieldDto ,Nullable<System.Int32>>(o => o.TransactionField);
        public static readonly string AliasNameProperty = ObjectInfoHelper.GetName<AppFormGridLayoutItemBindFieldDto ,System.String>(o => o.AliasName);
        public static readonly string WidthProperty = ObjectInfoHelper.GetName<AppFormGridLayoutItemBindFieldDto ,Nullable<System.Int32>>(o => o.Width);
        public static readonly string HeightProperty = ObjectInfoHelper.GetName<AppFormGridLayoutItemBindFieldDto ,Nullable<System.Int32>>(o => o.Height);
        public static readonly string VisibleProperty = ObjectInfoHelper.GetName<AppFormGridLayoutItemBindFieldDto ,Nullable<System.Boolean>>(o => o.Visible);
        public static readonly string ChildTransactionUnitIdProperty = ObjectInfoHelper.GetName<AppFormGridLayoutItemBindFieldDto ,Nullable<System.Int32>>(o => o.ChildTransactionUnitId);
        public static readonly string GrandChildTransactionUnitIdProperty = ObjectInfoHelper.GetName<AppFormGridLayoutItemBindFieldDto ,Nullable<System.Int32>>(o => o.GrandChildTransactionUnitId);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppFormGridLayoutItemBindFieldDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppFormGridLayoutItemBindFieldDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppFormGridLayoutItemBindFieldDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppFormGridLayoutItemBindFieldDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppFormGridLayoutItemBindFieldDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppFormGridLayoutItemBindFieldDto()
        {        
        }
		
		static AppFormGridLayoutItemBindFieldDto()
        {
                          
			  
			ForeignKeyProperties.Add(FormLayoutIdProperty);  
			ForeignKeyProperties.Add(TransactionFieldProperty);      
			ForeignKeyProperties.Add(ChildTransactionUnitIdProperty);  
			ForeignKeyProperties.Add(GrandChildTransactionUnitIdProperty);      		
                
			DictStringPropertyMaxLength.Add(AliasNameProperty,300);            
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
    


        /// <summary> The FormLayoutId property of the Entity AppFormGridLayoutItemBindField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> FormLayoutId
        {
            get { return  GetValue<Nullable<System.Int32>>( FormLayoutIdProperty);}
            set { SetValue(FormLayoutIdProperty,value); }
        }

        /// <summary> The TransactionField property of the Entity AppFormGridLayoutItemBindField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionField
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionFieldProperty);}
            set { SetValue(TransactionFieldProperty,value); }
        }

        /// <summary> The AliasName property of the Entity AppFormGridLayoutItemBindField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String AliasName
        {
            get { return  GetValue<System.String>( AliasNameProperty);}
            set { SetValue(AliasNameProperty,value); }
        }

        /// <summary> The Width property of the Entity AppFormGridLayoutItemBindField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> Width
        {
            get { return  GetValue<Nullable<System.Int32>>( WidthProperty);}
            set { SetValue(WidthProperty,value); }
        }

        /// <summary> The Height property of the Entity AppFormGridLayoutItemBindField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> Height
        {
            get { return  GetValue<Nullable<System.Int32>>( HeightProperty);}
            set { SetValue(HeightProperty,value); }
        }

        /// <summary> The Visible property of the Entity AppFormGridLayoutItemBindField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> Visible
        {
            get { return  GetValue<Nullable<System.Boolean>>( VisibleProperty);}
            set { SetValue(VisibleProperty,value); }
        }

        /// <summary> The ChildTransactionUnitId property of the Entity AppFormGridLayoutItemBindField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ChildTransactionUnitId
        {
            get { return  GetValue<Nullable<System.Int32>>( ChildTransactionUnitIdProperty);}
            set { SetValue(ChildTransactionUnitIdProperty,value); }
        }

        /// <summary> The GrandChildTransactionUnitId property of the Entity AppFormGridLayoutItemBindField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> GrandChildTransactionUnitId
        {
            get { return  GetValue<Nullable<System.Int32>>( GrandChildTransactionUnitIdProperty);}
            set { SetValue(GrandChildTransactionUnitIdProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppFormGridLayoutItemBindField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppFormGridLayoutItemBindField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppFormGridLayoutItemBindField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppFormGridLayoutItemBindField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppFormGridLayoutItemBindField</summary>
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

