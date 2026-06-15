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
    /// DTO class for the entity 'AppTransAuditTrailLog'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppTransAuditTrailLogDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string TransactionIdProperty = ObjectInfoHelper.GetName<AppTransAuditTrailLogDto ,Nullable<System.Int32>>(o => o.TransactionId);
        public static readonly string RootValueIdProperty = ObjectInfoHelper.GetName<AppTransAuditTrailLogDto ,Nullable<System.Int32>>(o => o.RootValueId);
        public static readonly string TransactionFieldIdProperty = ObjectInfoHelper.GetName<AppTransAuditTrailLogDto ,Nullable<System.Int32>>(o => o.TransactionFieldId);
        public static readonly string RowIdentityNameProperty = ObjectInfoHelper.GetName<AppTransAuditTrailLogDto ,System.String>(o => o.RowIdentityName);
        public static readonly string TraiLogActionProperty = ObjectInfoHelper.GetName<AppTransAuditTrailLogDto ,System.String>(o => o.TraiLogAction);
        public static readonly string ModifiedValueBeforProperty = ObjectInfoHelper.GetName<AppTransAuditTrailLogDto ,System.String>(o => o.ModifiedValueBefor);
        public static readonly string ModifiedValueAfterProperty = ObjectInfoHelper.GetName<AppTransAuditTrailLogDto ,System.String>(o => o.ModifiedValueAfter);
        public static readonly string BatchNoIdProperty = ObjectInfoHelper.GetName<AppTransAuditTrailLogDto ,Nullable<System.Int64>>(o => o.BatchNoId);
        public static readonly string UnitIdProperty = ObjectInfoHelper.GetName<AppTransAuditTrailLogDto ,Nullable<System.Int32>>(o => o.UnitId);
        public static readonly string ChildUnitRowValueIdProperty = ObjectInfoHelper.GetName<AppTransAuditTrailLogDto ,System.String>(o => o.ChildUnitRowValueId);
        public static readonly string GrandChildUnitRowValueIdProperty = ObjectInfoHelper.GetName<AppTransAuditTrailLogDto ,System.String>(o => o.GrandChildUnitRowValueId);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppTransAuditTrailLogDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppTransAuditTrailLogDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppTransAuditTrailLogDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppTransAuditTrailLogDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppTransAuditTrailLogDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppTransAuditTrailLogDto()
        {        
        }
		
		static AppTransAuditTrailLogDto()
        {
                             
			                 		
                 
			DictStringPropertyMaxLength.Add(RowIdentityNameProperty,500); 
			DictStringPropertyMaxLength.Add(TraiLogActionProperty,100); 
			DictStringPropertyMaxLength.Add(ModifiedValueBeforProperty,4000); 
			DictStringPropertyMaxLength.Add(ModifiedValueAfterProperty,4000);   
			DictStringPropertyMaxLength.Add(ChildUnitRowValueIdProperty,50); 
			DictStringPropertyMaxLength.Add(GrandChildUnitRowValueIdProperty,50);       
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
    


        /// <summary> The TransactionId property of the Entity AppTransAuditTrailLog</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionId
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionIdProperty);}
            set { SetValue(TransactionIdProperty,value); }
        }

        /// <summary> The RootValueId property of the Entity AppTransAuditTrailLog</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> RootValueId
        {
            get { return  GetValue<Nullable<System.Int32>>( RootValueIdProperty);}
            set { SetValue(RootValueIdProperty,value); }
        }

        /// <summary> The TransactionFieldId property of the Entity AppTransAuditTrailLog</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionFieldId
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionFieldIdProperty);}
            set { SetValue(TransactionFieldIdProperty,value); }
        }

        /// <summary> The RowIdentityName property of the Entity AppTransAuditTrailLog</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String RowIdentityName
        {
            get { return  GetValue<System.String>( RowIdentityNameProperty);}
            set { SetValue(RowIdentityNameProperty,value); }
        }

        /// <summary> The TraiLogAction property of the Entity AppTransAuditTrailLog</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String TraiLogAction
        {
            get { return  GetValue<System.String>( TraiLogActionProperty);}
            set { SetValue(TraiLogActionProperty,value); }
        }

        /// <summary> The ModifiedValueBefor property of the Entity AppTransAuditTrailLog</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ModifiedValueBefor
        {
            get { return  GetValue<System.String>( ModifiedValueBeforProperty);}
            set { SetValue(ModifiedValueBeforProperty,value); }
        }

        /// <summary> The ModifiedValueAfter property of the Entity AppTransAuditTrailLog</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ModifiedValueAfter
        {
            get { return  GetValue<System.String>( ModifiedValueAfterProperty);}
            set { SetValue(ModifiedValueAfterProperty,value); }
        }

        /// <summary> The BatchNoId property of the Entity AppTransAuditTrailLog</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int64> BatchNoId
        {
            get { return  GetValue<Nullable<System.Int64>>( BatchNoIdProperty);}
            set { SetValue(BatchNoIdProperty,value); }
        }

        /// <summary> The UnitId property of the Entity AppTransAuditTrailLog</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> UnitId
        {
            get { return  GetValue<Nullable<System.Int32>>( UnitIdProperty);}
            set { SetValue(UnitIdProperty,value); }
        }

        /// <summary> The ChildUnitRowValueId property of the Entity AppTransAuditTrailLog</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ChildUnitRowValueId
        {
            get { return  GetValue<System.String>( ChildUnitRowValueIdProperty);}
            set { SetValue(ChildUnitRowValueIdProperty,value); }
        }

        /// <summary> The GrandChildUnitRowValueId property of the Entity AppTransAuditTrailLog</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String GrandChildUnitRowValueId
        {
            get { return  GetValue<System.String>( GrandChildUnitRowValueIdProperty);}
            set { SetValue(GrandChildUnitRowValueIdProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppTransAuditTrailLog</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppTransAuditTrailLog</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppTransAuditTrailLog</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppTransAuditTrailLog</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppTransAuditTrailLog</summary>
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

