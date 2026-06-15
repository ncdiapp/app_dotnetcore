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
    /// DTO class for the entity 'AppFile'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppFileDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string FileCodeProperty = ObjectInfoHelper.GetName<AppFileDto ,System.String>(o => o.FileCode);
        public static readonly string DescriptionProperty = ObjectInfoHelper.GetName<AppFileDto ,System.String>(o => o.Description);
        public static readonly string FolderIdProperty = ObjectInfoHelper.GetName<AppFileDto ,Nullable<System.Int32>>(o => o.FolderId);
        public static readonly string FileTypeProperty = ObjectInfoHelper.GetName<AppFileDto ,Nullable<System.Int32>>(o => o.FileType);
        public static readonly string ExtensionProperty = ObjectInfoHelper.GetName<AppFileDto ,System.String>(o => o.Extension);
        public static readonly string OriginalFilePathProperty = ObjectInfoHelper.GetName<AppFileDto ,System.String>(o => o.OriginalFilePath);
        public static readonly string ThumbnailFilePathProperty = ObjectInfoHelper.GetName<AppFileDto ,System.String>(o => o.ThumbnailFilePath);
        public static readonly string RegularImageFilepathProperty = ObjectInfoHelper.GetName<AppFileDto ,System.String>(o => o.RegularImageFilepath);
        public static readonly string FileContentProperty = ObjectInfoHelper.GetName<AppFileDto ,System.Byte[]>(o => o.FileContent);
        public static readonly string CommentsProperty = ObjectInfoHelper.GetName<AppFileDto ,System.String>(o => o.Comments);
        public static readonly string InitialFileIdProperty = ObjectInfoHelper.GetName<AppFileDto ,Nullable<System.Int32>>(o => o.InitialFileId);
        public static readonly string CheckoutByIdProperty = ObjectInfoHelper.GetName<AppFileDto ,Nullable<System.Int32>>(o => o.CheckoutById);
        public static readonly string CheckoutDateProperty = ObjectInfoHelper.GetName<AppFileDto ,Nullable<System.DateTime>>(o => o.CheckoutDate);
        public static readonly string ClientLastWriteTickProperty = ObjectInfoHelper.GetName<AppFileDto ,Nullable<System.Int64>>(o => o.ClientLastWriteTick);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppFileDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppFileDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppFileDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppFileDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppFileDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppFileDto()
        {        
        }
		
		static AppFileDto()
        {
                                
			    
			ForeignKeyProperties.Add(FolderIdProperty);                 		
              
			DictStringPropertyMaxLength.Add(FileCodeProperty,200); 
			DictStringPropertyMaxLength.Add(DescriptionProperty,100);   
			DictStringPropertyMaxLength.Add(ExtensionProperty,50); 
			DictStringPropertyMaxLength.Add(OriginalFilePathProperty,100); 
			DictStringPropertyMaxLength.Add(ThumbnailFilePathProperty,100); 
			DictStringPropertyMaxLength.Add(RegularImageFilepathProperty,100);  
			DictStringPropertyMaxLength.Add(CommentsProperty,4000);           
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
    


        /// <summary> The FileCode property of the Entity AppFile</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String FileCode
        {
            get { return  GetValue<System.String>( FileCodeProperty);}
            set { SetValue(FileCodeProperty,value); }
        }

        /// <summary> The Description property of the Entity AppFile</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Description
        {
            get { return  GetValue<System.String>( DescriptionProperty);}
            set { SetValue(DescriptionProperty,value); }
        }

        /// <summary> The FolderId property of the Entity AppFile</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> FolderId
        {
            get { return  GetValue<Nullable<System.Int32>>( FolderIdProperty);}
            set { SetValue(FolderIdProperty,value); }
        }

        /// <summary> The FileType property of the Entity AppFile</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> FileType
        {
            get { return  GetValue<Nullable<System.Int32>>( FileTypeProperty);}
            set { SetValue(FileTypeProperty,value); }
        }

        /// <summary> The Extension property of the Entity AppFile</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Extension
        {
            get { return  GetValue<System.String>( ExtensionProperty);}
            set { SetValue(ExtensionProperty,value); }
        }

        /// <summary> The OriginalFilePath property of the Entity AppFile</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String OriginalFilePath
        {
            get { return  GetValue<System.String>( OriginalFilePathProperty);}
            set { SetValue(OriginalFilePathProperty,value); }
        }

        /// <summary> The ThumbnailFilePath property of the Entity AppFile</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ThumbnailFilePath
        {
            get { return  GetValue<System.String>( ThumbnailFilePathProperty);}
            set { SetValue(ThumbnailFilePathProperty,value); }
        }

        /// <summary> The RegularImageFilepath property of the Entity AppFile</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String RegularImageFilepath
        {
            get { return  GetValue<System.String>( RegularImageFilepathProperty);}
            set { SetValue(RegularImageFilepathProperty,value); }
        }

        /// <summary> The FileContent property of the Entity AppFile</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Byte[] FileContent
        {
            get { return  GetValue<System.Byte[]>( FileContentProperty);}
            set { SetValue(FileContentProperty,value); }
        }

        /// <summary> The Comments property of the Entity AppFile</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Comments
        {
            get { return  GetValue<System.String>( CommentsProperty);}
            set { SetValue(CommentsProperty,value); }
        }

        /// <summary> The InitialFileId property of the Entity AppFile</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> InitialFileId
        {
            get { return  GetValue<Nullable<System.Int32>>( InitialFileIdProperty);}
            set { SetValue(InitialFileIdProperty,value); }
        }

        /// <summary> The CheckoutById property of the Entity AppFile</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> CheckoutById
        {
            get { return  GetValue<Nullable<System.Int32>>( CheckoutByIdProperty);}
            set { SetValue(CheckoutByIdProperty,value); }
        }

        /// <summary> The CheckoutDate property of the Entity AppFile</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> CheckoutDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( CheckoutDateProperty);}
            set { SetValue(CheckoutDateProperty,value); }
        }

        /// <summary> The ClientLastWriteTick property of the Entity AppFile</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int64> ClientLastWriteTick
        {
            get { return  GetValue<Nullable<System.Int64>>( ClientLastWriteTickProperty);}
            set { SetValue(ClientLastWriteTickProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppFile</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppFile</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppFile</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppFile</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppFile</summary>
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

