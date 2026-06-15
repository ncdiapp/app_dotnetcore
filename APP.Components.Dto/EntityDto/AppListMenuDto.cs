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
    /// DTO class for the entity 'AppListMenu'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppListMenuDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string ParentIdProperty = ObjectInfoHelper.GetName<AppListMenuDto ,Nullable<System.Int32>>(o => o.ParentId);
        public static readonly string NameProperty = ObjectInfoHelper.GetName<AppListMenuDto ,System.String>(o => o.Name);
        public static readonly string DescriptionProperty = ObjectInfoHelper.GetName<AppListMenuDto ,System.String>(o => o.Description);
        public static readonly string IconNameProperty = ObjectInfoHelper.GetName<AppListMenuDto ,System.String>(o => o.IconName);
        public static readonly string RouteCodeProperty = ObjectInfoHelper.GetName<AppListMenuDto ,System.String>(o => o.RouteCode);
        public static readonly string LinkProperty = ObjectInfoHelper.GetName<AppListMenuDto ,System.String>(o => o.Link);
        public static readonly string SortProperty = ObjectInfoHelper.GetName<AppListMenuDto ,Nullable<System.Int32>>(o => o.Sort);
        public static readonly string LinkTypeProperty = ObjectInfoHelper.GetName<AppListMenuDto ,System.Int32>(o => o.LinkType);
        public static readonly string IsSharedbyMutipleCompanyProperty = ObjectInfoHelper.GetName<AppListMenuDto ,Nullable<System.Boolean>>(o => o.IsSharedbyMutipleCompany);
        public static readonly string EmDeviceMenuShowModeProperty = ObjectInfoHelper.GetName<AppListMenuDto ,Nullable<System.Int32>>(o => o.EmDeviceMenuShowMode);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppListMenuDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppListMenuDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppListMenuDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppListMenuDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppListMenuDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
        public static readonly string GlobalGuidProperty = ObjectInfoHelper.GetName<AppListMenuDto ,Nullable<System.Guid>>(o => o.GlobalGuid);
        public static readonly string ModuleRegisterIdProperty = ObjectInfoHelper.GetName<AppListMenuDto ,Nullable<System.Int32>>(o => o.ModuleRegisterId);
        public static readonly string DisplayModeMenuOrTabProperty = ObjectInfoHelper.GetName<AppListMenuDto ,Nullable<System.Int32>>(o => o.DisplayModeMenuOrTab);
        public static readonly string IconName2Property = ObjectInfoHelper.GetName<AppListMenuDto ,System.String>(o => o.IconName2);
        public static readonly string EsiteIdProperty = ObjectInfoHelper.GetName<AppListMenuDto ,Nullable<System.Int32>>(o => o.EsiteId);
        public static readonly string EmAppMenuItemCategoryProperty = ObjectInfoHelper.GetName<AppListMenuDto ,Nullable<System.Int32>>(o => o.EmAppMenuItemCategory);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppListMenuDto()
        {        
        }
		
		static AppListMenuDto()
        {
               
			MandatoryProperties.Add(NameProperty);       
			MandatoryProperties.Add(LinkTypeProperty);              
			  
			ForeignKeyProperties.Add(ParentIdProperty);               
			ForeignKeyProperties.Add(AppCreatedByCompanyIdProperty);       		
               
			DictStringPropertyMaxLength.Add(NameProperty,100); 
			DictStringPropertyMaxLength.Add(DescriptionProperty,2000); 
			DictStringPropertyMaxLength.Add(IconNameProperty,50); 
			DictStringPropertyMaxLength.Add(RouteCodeProperty,50); 
			DictStringPropertyMaxLength.Add(LinkProperty,500);             
			DictStringPropertyMaxLength.Add(IconName2Property,200);    
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
    


        /// <summary> The ParentId property of the Entity AppListMenu</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ParentId
        {
            get { return  GetValue<Nullable<System.Int32>>( ParentIdProperty);}
            set { SetValue(ParentIdProperty,value); }
        }

        /// <summary> The Name property of the Entity AppListMenu</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Name
        {
            get { return  GetValue<System.String>( NameProperty);}
            set { SetValue(NameProperty,value); }
        }

        /// <summary> The Description property of the Entity AppListMenu</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Description
        {
            get { return  GetValue<System.String>( DescriptionProperty);}
            set { SetValue(DescriptionProperty,value); }
        }

        /// <summary> The IconName property of the Entity AppListMenu</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String IconName
        {
            get { return  GetValue<System.String>( IconNameProperty);}
            set { SetValue(IconNameProperty,value); }
        }

        /// <summary> The RouteCode property of the Entity AppListMenu</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String RouteCode
        {
            get { return  GetValue<System.String>( RouteCodeProperty);}
            set { SetValue(RouteCodeProperty,value); }
        }

        /// <summary> The Link property of the Entity AppListMenu</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Link
        {
            get { return  GetValue<System.String>( LinkProperty);}
            set { SetValue(LinkProperty,value); }
        }

        /// <summary> The Sort property of the Entity AppListMenu</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> Sort
        {
            get { return  GetValue<Nullable<System.Int32>>( SortProperty);}
            set { SetValue(SortProperty,value); }
        }

        /// <summary> The LinkType property of the Entity AppListMenu</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Int32 LinkType
        {
            get { return  GetValue<System.Int32>( LinkTypeProperty);}
            set { SetValue(LinkTypeProperty,value); }
        }

        /// <summary> The IsSharedbyMutipleCompany property of the Entity AppListMenu</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsSharedbyMutipleCompany
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsSharedbyMutipleCompanyProperty);}
            set { SetValue(IsSharedbyMutipleCompanyProperty,value); }
        }

        /// <summary> The EmDeviceMenuShowMode property of the Entity AppListMenu</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> EmDeviceMenuShowMode
        {
            get { return  GetValue<Nullable<System.Int32>>( EmDeviceMenuShowModeProperty);}
            set { SetValue(EmDeviceMenuShowModeProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppListMenu</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppListMenu</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppListMenu</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppListMenu</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppListMenu</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedByCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByCompanyIdProperty);}
            set { SetValue(AppCreatedByCompanyIdProperty,value); }
        }

        /// <summary> The GlobalGuid property of the Entity AppListMenu</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Guid> GlobalGuid
        {
            get { return  GetValue<Nullable<System.Guid>>( GlobalGuidProperty);}
            set { SetValue(GlobalGuidProperty,value); }
        }

        /// <summary> The ModuleRegisterId property of the Entity AppListMenu</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ModuleRegisterId
        {
            get { return  GetValue<Nullable<System.Int32>>( ModuleRegisterIdProperty);}
            set { SetValue(ModuleRegisterIdProperty,value); }
        }

        /// <summary> The DisplayModeMenuOrTab property of the Entity AppListMenu</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DisplayModeMenuOrTab
        {
            get { return  GetValue<Nullable<System.Int32>>( DisplayModeMenuOrTabProperty);}
            set { SetValue(DisplayModeMenuOrTabProperty,value); }
        }

        /// <summary> The IconName2 property of the Entity AppListMenu</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String IconName2
        {
            get { return  GetValue<System.String>( IconName2Property);}
            set { SetValue(IconName2Property,value); }
        }

        /// <summary> The EsiteId property of the Entity AppListMenu</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> EsiteId
        {
            get { return  GetValue<Nullable<System.Int32>>( EsiteIdProperty);}
            set { SetValue(EsiteIdProperty,value); }
        }

        /// <summary> The EmAppMenuItemCategory property of the Entity AppListMenu</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> EmAppMenuItemCategory
        {
            get { return  GetValue<Nullable<System.Int32>>( EmAppMenuItemCategoryProperty);}
            set { SetValue(EmAppMenuItemCategoryProperty,value); }
        }
        
        #endregion

       
        
    }
}

