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
    /// DTO class for the entity 'AppLogTrack'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppLogTrackDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string CatalogueProperty = ObjectInfoHelper.GetName<AppLogTrackDto ,System.String>(o => o.Catalogue);
        public static readonly string DescriptionProperty = ObjectInfoHelper.GetName<AppLogTrackDto ,System.String>(o => o.Description);
        public static readonly string StatusCodeProperty = ObjectInfoHelper.GetName<AppLogTrackDto ,System.String>(o => o.StatusCode);
        public static readonly string MessageProperty = ObjectInfoHelper.GetName<AppLogTrackDto ,System.String>(o => o.Message);
        public static readonly string OtherInfoProperty = ObjectInfoHelper.GetName<AppLogTrackDto ,System.String>(o => o.OtherInfo);
        public static readonly string LogDateProperty = ObjectInfoHelper.GetName<AppLogTrackDto ,Nullable<System.DateTime>>(o => o.LogDate);
        public static readonly string CommandIdProperty = ObjectInfoHelper.GetName<AppLogTrackDto ,Nullable<System.Int32>>(o => o.CommandId);
        public static readonly string TransactionIdProperty = ObjectInfoHelper.GetName<AppLogTrackDto ,Nullable<System.Int32>>(o => o.TransactionId);
        public static readonly string TransactionRidProperty = ObjectInfoHelper.GetName<AppLogTrackDto ,System.String>(o => o.TransactionRid);
        public static readonly string BatchNumberProperty = ObjectInfoHelper.GetName<AppLogTrackDto ,System.String>(o => o.BatchNumber);
        public static readonly string TransactionNameProperty = ObjectInfoHelper.GetName<AppLogTrackDto ,System.String>(o => o.TransactionName);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppLogTrackDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppLogTrackDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppLogTrackDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppLogTrackDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppLogTrackDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppLogTrackDto()
        {        
        }
		
		static AppLogTrackDto()
        {
                             
			                 		
              
			DictStringPropertyMaxLength.Add(CatalogueProperty,4000); 
			DictStringPropertyMaxLength.Add(DescriptionProperty,4000); 
			DictStringPropertyMaxLength.Add(StatusCodeProperty,4000); 
			DictStringPropertyMaxLength.Add(MessageProperty,2147483647); 
			DictStringPropertyMaxLength.Add(OtherInfoProperty,2147483647);    
			DictStringPropertyMaxLength.Add(TransactionRidProperty,4000); 
			DictStringPropertyMaxLength.Add(BatchNumberProperty,2000); 
			DictStringPropertyMaxLength.Add(TransactionNameProperty,2000);       
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
    


        /// <summary> The Catalogue property of the Entity AppLogTrack</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Catalogue
        {
            get { return  GetValue<System.String>( CatalogueProperty);}
            set { SetValue(CatalogueProperty,value); }
        }

        /// <summary> The Description property of the Entity AppLogTrack</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Description
        {
            get { return  GetValue<System.String>( DescriptionProperty);}
            set { SetValue(DescriptionProperty,value); }
        }

        /// <summary> The StatusCode property of the Entity AppLogTrack</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String StatusCode
        {
            get { return  GetValue<System.String>( StatusCodeProperty);}
            set { SetValue(StatusCodeProperty,value); }
        }

        /// <summary> The Message property of the Entity AppLogTrack</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Message
        {
            get { return  GetValue<System.String>( MessageProperty);}
            set { SetValue(MessageProperty,value); }
        }

        /// <summary> The OtherInfo property of the Entity AppLogTrack</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String OtherInfo
        {
            get { return  GetValue<System.String>( OtherInfoProperty);}
            set { SetValue(OtherInfoProperty,value); }
        }

        /// <summary> The LogDate property of the Entity AppLogTrack</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> LogDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( LogDateProperty);}
            set { SetValue(LogDateProperty,value); }
        }

        /// <summary> The CommandId property of the Entity AppLogTrack</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> CommandId
        {
            get { return  GetValue<Nullable<System.Int32>>( CommandIdProperty);}
            set { SetValue(CommandIdProperty,value); }
        }

        /// <summary> The TransactionId property of the Entity AppLogTrack</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionId
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionIdProperty);}
            set { SetValue(TransactionIdProperty,value); }
        }

        /// <summary> The TransactionRid property of the Entity AppLogTrack</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String TransactionRid
        {
            get { return  GetValue<System.String>( TransactionRidProperty);}
            set { SetValue(TransactionRidProperty,value); }
        }

        /// <summary> The BatchNumber property of the Entity AppLogTrack</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String BatchNumber
        {
            get { return  GetValue<System.String>( BatchNumberProperty);}
            set { SetValue(BatchNumberProperty,value); }
        }

        /// <summary> The TransactionName property of the Entity AppLogTrack</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String TransactionName
        {
            get { return  GetValue<System.String>( TransactionNameProperty);}
            set { SetValue(TransactionNameProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppLogTrack</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppLogTrack</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppLogTrack</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedByCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByCompanyIdProperty);}
            set { SetValue(AppCreatedByCompanyIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppLogTrack</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppLogTrack</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }
        
        #endregion

       
        
    }
}

