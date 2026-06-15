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
    /// DTO class for the entity 'AppItemBase'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppItemBaseDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string CodeProperty = ObjectInfoHelper.GetName<AppItemBaseDto ,System.String>(o => o.Code);
        public static readonly string Desc1Property = ObjectInfoHelper.GetName<AppItemBaseDto ,System.String>(o => o.Desc1);
        public static readonly string Desc2Property = ObjectInfoHelper.GetName<AppItemBaseDto ,System.String>(o => o.Desc2);
        public static readonly string ParentIdProperty = ObjectInfoHelper.GetName<AppItemBaseDto ,Nullable<System.Int32>>(o => o.ParentId);
        public static readonly string CopyFromIdProperty = ObjectInfoHelper.GetName<AppItemBaseDto ,Nullable<System.Int32>>(o => o.CopyFromId);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppItemBaseDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppItemBaseDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppItemBaseDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppItemBaseDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string CustomerIdProperty = ObjectInfoHelper.GetName<AppItemBaseDto ,Nullable<System.Int32>>(o => o.CustomerId);
        public static readonly string SupplierIdProperty = ObjectInfoHelper.GetName<AppItemBaseDto ,Nullable<System.Int32>>(o => o.SupplierId);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppItemBaseDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppItemBaseDto()
        {        
        }
		
		static AppItemBaseDto()
        {
              
			MandatoryProperties.Add(CodeProperty);            
			             		
              
			DictStringPropertyMaxLength.Add(CodeProperty,50); 
			DictStringPropertyMaxLength.Add(Desc1Property,200); 
			DictStringPropertyMaxLength.Add(Desc2Property,500);           
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
    


        /// <summary> The Code property of the Entity AppItemBase</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Code
        {
            get { return  GetValue<System.String>( CodeProperty);}
            set { SetValue(CodeProperty,value); }
        }

        /// <summary> The Desc1 property of the Entity AppItemBase</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Desc1
        {
            get { return  GetValue<System.String>( Desc1Property);}
            set { SetValue(Desc1Property,value); }
        }

        /// <summary> The Desc2 property of the Entity AppItemBase</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Desc2
        {
            get { return  GetValue<System.String>( Desc2Property);}
            set { SetValue(Desc2Property,value); }
        }

        /// <summary> The ParentId property of the Entity AppItemBase</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ParentId
        {
            get { return  GetValue<Nullable<System.Int32>>( ParentIdProperty);}
            set { SetValue(ParentIdProperty,value); }
        }

        /// <summary> The CopyFromId property of the Entity AppItemBase</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> CopyFromId
        {
            get { return  GetValue<Nullable<System.Int32>>( CopyFromIdProperty);}
            set { SetValue(CopyFromIdProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppItemBase</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppItemBase</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppItemBase</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppItemBase</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The CustomerId property of the Entity AppItemBase</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> CustomerId
        {
            get { return  GetValue<Nullable<System.Int32>>( CustomerIdProperty);}
            set { SetValue(CustomerIdProperty,value); }
        }

        /// <summary> The SupplierId property of the Entity AppItemBase</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SupplierId
        {
            get { return  GetValue<Nullable<System.Int32>>( SupplierIdProperty);}
            set { SetValue(SupplierIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppItemBase</summary>
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

