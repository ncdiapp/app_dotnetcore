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
    /// DTO class for the entity 'AppProjectPerspectiveTask'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppProjectPerspectiveTaskDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string PerspectiveSectionIdProperty = ObjectInfoHelper.GetName<AppProjectPerspectiveTaskDto ,Nullable<System.Int32>>(o => o.PerspectiveSectionId);
        public static readonly string ProjectWorkFlowTaskIdProperty = ObjectInfoHelper.GetName<AppProjectPerspectiveTaskDto ,Nullable<System.Int32>>(o => o.ProjectWorkFlowTaskId);
        public static readonly string DisplayOrderProperty = ObjectInfoHelper.GetName<AppProjectPerspectiveTaskDto ,Nullable<System.Int32>>(o => o.DisplayOrder);
        public static readonly string AddtionTaskNotesProperty = ObjectInfoHelper.GetName<AppProjectPerspectiveTaskDto ,System.String>(o => o.AddtionTaskNotes);
        public static readonly string DescriptionProperty = ObjectInfoHelper.GetName<AppProjectPerspectiveTaskDto ,System.String>(o => o.Description);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppProjectPerspectiveTaskDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppProjectPerspectiveTaskDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppProjectPerspectiveTaskDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppProjectPerspectiveTaskDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppProjectPerspectiveTaskDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppProjectPerspectiveTaskDto()
        {        
        }
		
		static AppProjectPerspectiveTaskDto()
        {
                       
			  
			ForeignKeyProperties.Add(PerspectiveSectionIdProperty);  
			ForeignKeyProperties.Add(ProjectWorkFlowTaskIdProperty);         		
                 
			DictStringPropertyMaxLength.Add(AddtionTaskNotesProperty,100); 
			DictStringPropertyMaxLength.Add(DescriptionProperty,500);       
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
    


        /// <summary> The PerspectiveSectionId property of the Entity AppProjectPerspectiveTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> PerspectiveSectionId
        {
            get { return  GetValue<Nullable<System.Int32>>( PerspectiveSectionIdProperty);}
            set { SetValue(PerspectiveSectionIdProperty,value); }
        }

        /// <summary> The ProjectWorkFlowTaskId property of the Entity AppProjectPerspectiveTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ProjectWorkFlowTaskId
        {
            get { return  GetValue<Nullable<System.Int32>>( ProjectWorkFlowTaskIdProperty);}
            set { SetValue(ProjectWorkFlowTaskIdProperty,value); }
        }

        /// <summary> The DisplayOrder property of the Entity AppProjectPerspectiveTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DisplayOrder
        {
            get { return  GetValue<Nullable<System.Int32>>( DisplayOrderProperty);}
            set { SetValue(DisplayOrderProperty,value); }
        }

        /// <summary> The AddtionTaskNotes property of the Entity AppProjectPerspectiveTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String AddtionTaskNotes
        {
            get { return  GetValue<System.String>( AddtionTaskNotesProperty);}
            set { SetValue(AddtionTaskNotesProperty,value); }
        }

        /// <summary> The Description property of the Entity AppProjectPerspectiveTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Description
        {
            get { return  GetValue<System.String>( DescriptionProperty);}
            set { SetValue(DescriptionProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppProjectPerspectiveTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppProjectPerspectiveTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppProjectPerspectiveTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppProjectPerspectiveTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppProjectPerspectiveTask</summary>
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

