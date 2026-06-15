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
    /// DTO class for the entity 'AppDatabaseDiagramItem'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppDatabaseDiagramItemDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string DiagramIdProperty = ObjectInfoHelper.GetName<AppDatabaseDiagramItemDto ,Nullable<System.Int32>>(o => o.DiagramId);
        public static readonly string NameProperty = ObjectInfoHelper.GetName<AppDatabaseDiagramItemDto ,System.String>(o => o.Name);
        public static readonly string DescriptionProperty = ObjectInfoHelper.GetName<AppDatabaseDiagramItemDto ,System.String>(o => o.Description);
        public static readonly string ItemTypeProperty = ObjectInfoHelper.GetName<AppDatabaseDiagramItemDto ,Nullable<System.Int32>>(o => o.ItemType);
        public static readonly string PositionXProperty = ObjectInfoHelper.GetName<AppDatabaseDiagramItemDto ,Nullable<System.Int32>>(o => o.PositionX);
        public static readonly string PositionYProperty = ObjectInfoHelper.GetName<AppDatabaseDiagramItemDto ,Nullable<System.Int32>>(o => o.PositionY);
        public static readonly string HeightProperty = ObjectInfoHelper.GetName<AppDatabaseDiagramItemDto ,Nullable<System.Int32>>(o => o.Height);
        public static readonly string WidthProperty = ObjectInfoHelper.GetName<AppDatabaseDiagramItemDto ,Nullable<System.Int32>>(o => o.Width);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppDatabaseDiagramItemDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppDatabaseDiagramItemDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppDatabaseDiagramItemDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppDatabaseDiagramItemDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppDatabaseDiagramItemDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppDatabaseDiagramItemDto()
        {        
        }
		
		static AppDatabaseDiagramItemDto()
        {
                          
			  
			ForeignKeyProperties.Add(DiagramIdProperty);             		
               
			DictStringPropertyMaxLength.Add(NameProperty,500); 
			DictStringPropertyMaxLength.Add(DescriptionProperty,2000);            
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
    


        /// <summary> The DiagramId property of the Entity AppDatabaseDiagramItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DiagramId
        {
            get { return  GetValue<Nullable<System.Int32>>( DiagramIdProperty);}
            set { SetValue(DiagramIdProperty,value); }
        }

        /// <summary> The Name property of the Entity AppDatabaseDiagramItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Name
        {
            get { return  GetValue<System.String>( NameProperty);}
            set { SetValue(NameProperty,value); }
        }

        /// <summary> The Description property of the Entity AppDatabaseDiagramItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Description
        {
            get { return  GetValue<System.String>( DescriptionProperty);}
            set { SetValue(DescriptionProperty,value); }
        }

        /// <summary> The ItemType property of the Entity AppDatabaseDiagramItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ItemType
        {
            get { return  GetValue<Nullable<System.Int32>>( ItemTypeProperty);}
            set { SetValue(ItemTypeProperty,value); }
        }

        /// <summary> The PositionX property of the Entity AppDatabaseDiagramItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> PositionX
        {
            get { return  GetValue<Nullable<System.Int32>>( PositionXProperty);}
            set { SetValue(PositionXProperty,value); }
        }

        /// <summary> The PositionY property of the Entity AppDatabaseDiagramItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> PositionY
        {
            get { return  GetValue<Nullable<System.Int32>>( PositionYProperty);}
            set { SetValue(PositionYProperty,value); }
        }

        /// <summary> The Height property of the Entity AppDatabaseDiagramItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> Height
        {
            get { return  GetValue<Nullable<System.Int32>>( HeightProperty);}
            set { SetValue(HeightProperty,value); }
        }

        /// <summary> The Width property of the Entity AppDatabaseDiagramItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> Width
        {
            get { return  GetValue<Nullable<System.Int32>>( WidthProperty);}
            set { SetValue(WidthProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppDatabaseDiagramItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppDatabaseDiagramItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppDatabaseDiagramItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppDatabaseDiagramItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppDatabaseDiagramItem</summary>
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

