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
    /// DTO class for the entity 'AppEstoreCatelogTreeLevel'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppEstoreCatelogTreeLevelDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string EstoreIdProperty = ObjectInfoHelper.GetName<AppEstoreCatelogTreeLevelDto ,Nullable<System.Int32>>(o => o.EstoreId);
        public static readonly string TreeNodeLevelProperty = ObjectInfoHelper.GetName<AppEstoreCatelogTreeLevelDto ,Nullable<System.Int32>>(o => o.TreeNodeLevel);
        public static readonly string TreeNodeIdviewFieldIdProperty = ObjectInfoHelper.GetName<AppEstoreCatelogTreeLevelDto ,Nullable<System.Int32>>(o => o.TreeNodeIdviewFieldId);
        public static readonly string TreeNodeDisplay1ViewFieldIdProperty = ObjectInfoHelper.GetName<AppEstoreCatelogTreeLevelDto ,Nullable<System.Int32>>(o => o.TreeNodeDisplay1ViewFieldId);
        public static readonly string TreeNodeDisplay2ViewFieldIdProperty = ObjectInfoHelper.GetName<AppEstoreCatelogTreeLevelDto ,Nullable<System.Int32>>(o => o.TreeNodeDisplay2ViewFieldId);
        public static readonly string TreeNodeImageIdviewFieldIdProperty = ObjectInfoHelper.GetName<AppEstoreCatelogTreeLevelDto ,Nullable<System.Int32>>(o => o.TreeNodeImageIdviewFieldId);
        public static readonly string PassToProductSearchFieldIdProperty = ObjectInfoHelper.GetName<AppEstoreCatelogTreeLevelDto ,Nullable<System.Int32>>(o => o.PassToProductSearchFieldId);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppEstoreCatelogTreeLevelDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppEstoreCatelogTreeLevelDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppEstoreCatelogTreeLevelDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppEstoreCatelogTreeLevelDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppEstoreCatelogTreeLevelDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppEstoreCatelogTreeLevelDto()
        {        
        }
		
		static AppEstoreCatelogTreeLevelDto()
        {
                         
			  
			ForeignKeyProperties.Add(EstoreIdProperty);            		
                           
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
    


        /// <summary> The EstoreId property of the Entity AppEstoreCatelogTreeLevel</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> EstoreId
        {
            get { return  GetValue<Nullable<System.Int32>>( EstoreIdProperty);}
            set { SetValue(EstoreIdProperty,value); }
        }

        /// <summary> The TreeNodeLevel property of the Entity AppEstoreCatelogTreeLevel</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TreeNodeLevel
        {
            get { return  GetValue<Nullable<System.Int32>>( TreeNodeLevelProperty);}
            set { SetValue(TreeNodeLevelProperty,value); }
        }

        /// <summary> The TreeNodeIdviewFieldId property of the Entity AppEstoreCatelogTreeLevel</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TreeNodeIdviewFieldId
        {
            get { return  GetValue<Nullable<System.Int32>>( TreeNodeIdviewFieldIdProperty);}
            set { SetValue(TreeNodeIdviewFieldIdProperty,value); }
        }

        /// <summary> The TreeNodeDisplay1ViewFieldId property of the Entity AppEstoreCatelogTreeLevel</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TreeNodeDisplay1ViewFieldId
        {
            get { return  GetValue<Nullable<System.Int32>>( TreeNodeDisplay1ViewFieldIdProperty);}
            set { SetValue(TreeNodeDisplay1ViewFieldIdProperty,value); }
        }

        /// <summary> The TreeNodeDisplay2ViewFieldId property of the Entity AppEstoreCatelogTreeLevel</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TreeNodeDisplay2ViewFieldId
        {
            get { return  GetValue<Nullable<System.Int32>>( TreeNodeDisplay2ViewFieldIdProperty);}
            set { SetValue(TreeNodeDisplay2ViewFieldIdProperty,value); }
        }

        /// <summary> The TreeNodeImageIdviewFieldId property of the Entity AppEstoreCatelogTreeLevel</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TreeNodeImageIdviewFieldId
        {
            get { return  GetValue<Nullable<System.Int32>>( TreeNodeImageIdviewFieldIdProperty);}
            set { SetValue(TreeNodeImageIdviewFieldIdProperty,value); }
        }

        /// <summary> The PassToProductSearchFieldId property of the Entity AppEstoreCatelogTreeLevel</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> PassToProductSearchFieldId
        {
            get { return  GetValue<Nullable<System.Int32>>( PassToProductSearchFieldIdProperty);}
            set { SetValue(PassToProductSearchFieldIdProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppEstoreCatelogTreeLevel</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppEstoreCatelogTreeLevel</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppEstoreCatelogTreeLevel</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppEstoreCatelogTreeLevel</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppEstoreCatelogTreeLevel</summary>
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

