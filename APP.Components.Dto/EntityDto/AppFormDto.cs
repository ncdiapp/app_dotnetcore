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
    /// DTO class for the entity 'AppForm'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppFormDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string NameProperty = ObjectInfoHelper.GetName<AppFormDto ,System.String>(o => o.Name);
        public static readonly string DescriptionProperty = ObjectInfoHelper.GetName<AppFormDto ,System.String>(o => o.Description);
        public static readonly string LayoutTypeProperty = ObjectInfoHelper.GetName<AppFormDto ,Nullable<System.Int32>>(o => o.LayoutType);
        public static readonly string FormScopeProperty = ObjectInfoHelper.GetName<AppFormDto ,Nullable<System.Int32>>(o => o.FormScope);
        public static readonly string SearchViewIdProperty = ObjectInfoHelper.GetName<AppFormDto ,Nullable<System.Int32>>(o => o.SearchViewId);
        public static readonly string SystemDefineRouteStateProperty = ObjectInfoHelper.GetName<AppFormDto ,System.String>(o => o.SystemDefineRouteState);
        public static readonly string RouteParamter1Property = ObjectInfoHelper.GetName<AppFormDto ,System.String>(o => o.RouteParamter1);
        public static readonly string RouteParamter2Property = ObjectInfoHelper.GetName<AppFormDto ,System.String>(o => o.RouteParamter2);
        public static readonly string RouteParamter3Property = ObjectInfoHelper.GetName<AppFormDto ,System.String>(o => o.RouteParamter3);
        public static readonly string DefaultWidthProperty = ObjectInfoHelper.GetName<AppFormDto ,System.String>(o => o.DefaultWidth);
        public static readonly string DefaultHightProperty = ObjectInfoHelper.GetName<AppFormDto ,System.String>(o => o.DefaultHight);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppFormDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppFormDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppFormDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppFormDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppFormDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
        public static readonly string SaasApplicationIdProperty = ObjectInfoHelper.GetName<AppFormDto ,Nullable<System.Int32>>(o => o.SaasApplicationId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppFormDto()
        {        
        }
		
		static AppFormDto()
        {
                              
			      
			ForeignKeyProperties.Add(SearchViewIdProperty);             
			ForeignKeyProperties.Add(SaasApplicationIdProperty); 		
              
			DictStringPropertyMaxLength.Add(NameProperty,200); 
			DictStringPropertyMaxLength.Add(DescriptionProperty,1000);    
			DictStringPropertyMaxLength.Add(SystemDefineRouteStateProperty,500); 
			DictStringPropertyMaxLength.Add(RouteParamter1Property,100); 
			DictStringPropertyMaxLength.Add(RouteParamter2Property,100); 
			DictStringPropertyMaxLength.Add(RouteParamter3Property,100); 
			DictStringPropertyMaxLength.Add(DefaultWidthProperty,50); 
			DictStringPropertyMaxLength.Add(DefaultHightProperty,50);        
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
    


        /// <summary> The Name property of the Entity AppForm</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Name
        {
            get { return  GetValue<System.String>( NameProperty);}
            set { SetValue(NameProperty,value); }
        }

        /// <summary> The Description property of the Entity AppForm</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Description
        {
            get { return  GetValue<System.String>( DescriptionProperty);}
            set { SetValue(DescriptionProperty,value); }
        }

        /// <summary> The LayoutType property of the Entity AppForm</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> LayoutType
        {
            get { return  GetValue<Nullable<System.Int32>>( LayoutTypeProperty);}
            set { SetValue(LayoutTypeProperty,value); }
        }

        /// <summary> The FormScope property of the Entity AppForm</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> FormScope
        {
            get { return  GetValue<Nullable<System.Int32>>( FormScopeProperty);}
            set { SetValue(FormScopeProperty,value); }
        }

        /// <summary> The SearchViewId property of the Entity AppForm</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SearchViewId
        {
            get { return  GetValue<Nullable<System.Int32>>( SearchViewIdProperty);}
            set { SetValue(SearchViewIdProperty,value); }
        }

        /// <summary> The SystemDefineRouteState property of the Entity AppForm</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String SystemDefineRouteState
        {
            get { return  GetValue<System.String>( SystemDefineRouteStateProperty);}
            set { SetValue(SystemDefineRouteStateProperty,value); }
        }

        /// <summary> The RouteParamter1 property of the Entity AppForm</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String RouteParamter1
        {
            get { return  GetValue<System.String>( RouteParamter1Property);}
            set { SetValue(RouteParamter1Property,value); }
        }

        /// <summary> The RouteParamter2 property of the Entity AppForm</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String RouteParamter2
        {
            get { return  GetValue<System.String>( RouteParamter2Property);}
            set { SetValue(RouteParamter2Property,value); }
        }

        /// <summary> The RouteParamter3 property of the Entity AppForm</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String RouteParamter3
        {
            get { return  GetValue<System.String>( RouteParamter3Property);}
            set { SetValue(RouteParamter3Property,value); }
        }

        /// <summary> The DefaultWidth property of the Entity AppForm</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String DefaultWidth
        {
            get { return  GetValue<System.String>( DefaultWidthProperty);}
            set { SetValue(DefaultWidthProperty,value); }
        }

        /// <summary> The DefaultHight property of the Entity AppForm</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String DefaultHight
        {
            get { return  GetValue<System.String>( DefaultHightProperty);}
            set { SetValue(DefaultHightProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppForm</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppForm</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppForm</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppForm</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppForm</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedByCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByCompanyIdProperty);}
            set { SetValue(AppCreatedByCompanyIdProperty,value); }
        }

        /// <summary> The SaasApplicationId property of the Entity AppForm</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SaasApplicationId
        {
            get { return  GetValue<Nullable<System.Int32>>( SaasApplicationIdProperty);}
            set { SetValue(SaasApplicationIdProperty,value); }
        }
        
        #endregion

       
        
    }
}

