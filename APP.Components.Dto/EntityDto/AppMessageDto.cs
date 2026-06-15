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
    /// DTO class for the entity 'AppMessage'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppMessageDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string SubjectProperty = ObjectInfoHelper.GetName<AppMessageDto ,System.String>(o => o.Subject);
        public static readonly string MessageProperty = ObjectInfoHelper.GetName<AppMessageDto ,System.String>(o => o.Message);
        public static readonly string ReplyMsgToIdProperty = ObjectInfoHelper.GetName<AppMessageDto ,Nullable<System.Int32>>(o => o.ReplyMsgToId);
        public static readonly string FromEmailProperty = ObjectInfoHelper.GetName<AppMessageDto ,System.String>(o => o.FromEmail);
        public static readonly string ToListProperty = ObjectInfoHelper.GetName<AppMessageDto ,System.String>(o => o.ToList);
        public static readonly string CclistProperty = ObjectInfoHelper.GetName<AppMessageDto ,System.String>(o => o.Cclist);
        public static readonly string BcclistProperty = ObjectInfoHelper.GetName<AppMessageDto ,System.String>(o => o.Bcclist);
        public static readonly string MessagePostTypeProperty = ObjectInfoHelper.GetName<AppMessageDto ,Nullable<System.Int32>>(o => o.MessagePostType);
        public static readonly string MessgaeScopeTypeProperty = ObjectInfoHelper.GetName<AppMessageDto ,Nullable<System.Int32>>(o => o.MessgaeScopeType);
        public static readonly string IsDraftProperty = ObjectInfoHelper.GetName<AppMessageDto ,Nullable<System.Boolean>>(o => o.IsDraft);
        public static readonly string IsPredefinedTemplateProperty = ObjectInfoHelper.GetName<AppMessageDto ,Nullable<System.Boolean>>(o => o.IsPredefinedTemplate);
        public static readonly string TransactionIdProperty = ObjectInfoHelper.GetName<AppMessageDto ,Nullable<System.Int32>>(o => o.TransactionId);
        public static readonly string TransactionRootValueIdProperty = ObjectInfoHelper.GetName<AppMessageDto ,System.String>(o => o.TransactionRootValueId);
        public static readonly string ProjectActivityIdProperty = ObjectInfoHelper.GetName<AppMessageDto ,Nullable<System.Int32>>(o => o.ProjectActivityId);
        public static readonly string ProjectTeamIdProperty = ObjectInfoHelper.GetName<AppMessageDto ,Nullable<System.Int32>>(o => o.ProjectTeamId);
        public static readonly string ProjectIdProperty = ObjectInfoHelper.GetName<AppMessageDto ,Nullable<System.Int32>>(o => o.ProjectId);
        public static readonly string AttachmentFileTokenProperty = ObjectInfoHelper.GetName<AppMessageDto ,System.String>(o => o.AttachmentFileToken);
        public static readonly string MsgUniqueIdProperty = ObjectInfoHelper.GetName<AppMessageDto ,System.String>(o => o.MsgUniqueId);
        public static readonly string ReminderMinutesProperty = ObjectInfoHelper.GetName<AppMessageDto ,Nullable<System.Int32>>(o => o.ReminderMinutes);
        public static readonly string IsEnableReminderProperty = ObjectInfoHelper.GetName<AppMessageDto ,Nullable<System.Boolean>>(o => o.IsEnableReminder);
        public static readonly string ReminderTargetDateProperty = ObjectInfoHelper.GetName<AppMessageDto ,Nullable<System.DateTime>>(o => o.ReminderTargetDate);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppMessageDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppMessageDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppMessageDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppMessageDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppMessageDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
        public static readonly string TransactionGroupIdProperty = ObjectInfoHelper.GetName<AppMessageDto ,Nullable<System.Int32>>(o => o.TransactionGroupId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppMessageDto()
        {        
        }
		
		static AppMessageDto()
        {
              
			MandatoryProperties.Add(SubjectProperty);     
			MandatoryProperties.Add(ToListProperty);                       
			               
			ForeignKeyProperties.Add(ProjectActivityIdProperty);  
			ForeignKeyProperties.Add(ProjectTeamIdProperty);  
			ForeignKeyProperties.Add(ProjectIdProperty);            		
              
			DictStringPropertyMaxLength.Add(SubjectProperty,500); 
			DictStringPropertyMaxLength.Add(MessageProperty,2147483647);  
			DictStringPropertyMaxLength.Add(FromEmailProperty,200); 
			DictStringPropertyMaxLength.Add(ToListProperty,2147483647); 
			DictStringPropertyMaxLength.Add(CclistProperty,2147483647); 
			DictStringPropertyMaxLength.Add(BcclistProperty,2147483647);      
			DictStringPropertyMaxLength.Add(TransactionRootValueIdProperty,200);    
			DictStringPropertyMaxLength.Add(AttachmentFileTokenProperty,4000); 
			DictStringPropertyMaxLength.Add(MsgUniqueIdProperty,500);           
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
    


        /// <summary> The Subject property of the Entity AppMessage</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Subject
        {
            get { return  GetValue<System.String>( SubjectProperty);}
            set { SetValue(SubjectProperty,value); }
        }

        /// <summary> The Message property of the Entity AppMessage</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Message
        {
            get { return  GetValue<System.String>( MessageProperty);}
            set { SetValue(MessageProperty,value); }
        }

        /// <summary> The ReplyMsgToId property of the Entity AppMessage</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ReplyMsgToId
        {
            get { return  GetValue<Nullable<System.Int32>>( ReplyMsgToIdProperty);}
            set { SetValue(ReplyMsgToIdProperty,value); }
        }

        /// <summary> The FromEmail property of the Entity AppMessage</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String FromEmail
        {
            get { return  GetValue<System.String>( FromEmailProperty);}
            set { SetValue(FromEmailProperty,value); }
        }

        /// <summary> The ToList property of the Entity AppMessage</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ToList
        {
            get { return  GetValue<System.String>( ToListProperty);}
            set { SetValue(ToListProperty,value); }
        }

        /// <summary> The Cclist property of the Entity AppMessage</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Cclist
        {
            get { return  GetValue<System.String>( CclistProperty);}
            set { SetValue(CclistProperty,value); }
        }

        /// <summary> The Bcclist property of the Entity AppMessage</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Bcclist
        {
            get { return  GetValue<System.String>( BcclistProperty);}
            set { SetValue(BcclistProperty,value); }
        }

        /// <summary> The MessagePostType property of the Entity AppMessage</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> MessagePostType
        {
            get { return  GetValue<Nullable<System.Int32>>( MessagePostTypeProperty);}
            set { SetValue(MessagePostTypeProperty,value); }
        }

        /// <summary> The MessgaeScopeType property of the Entity AppMessage</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> MessgaeScopeType
        {
            get { return  GetValue<Nullable<System.Int32>>( MessgaeScopeTypeProperty);}
            set { SetValue(MessgaeScopeTypeProperty,value); }
        }

        /// <summary> The IsDraft property of the Entity AppMessage</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsDraft
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsDraftProperty);}
            set { SetValue(IsDraftProperty,value); }
        }

        /// <summary> The IsPredefinedTemplate property of the Entity AppMessage</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsPredefinedTemplate
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsPredefinedTemplateProperty);}
            set { SetValue(IsPredefinedTemplateProperty,value); }
        }

        /// <summary> The TransactionId property of the Entity AppMessage</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionId
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionIdProperty);}
            set { SetValue(TransactionIdProperty,value); }
        }

        /// <summary> The TransactionRootValueId property of the Entity AppMessage</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String TransactionRootValueId
        {
            get { return  GetValue<System.String>( TransactionRootValueIdProperty);}
            set { SetValue(TransactionRootValueIdProperty,value); }
        }

        /// <summary> The ProjectActivityId property of the Entity AppMessage</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ProjectActivityId
        {
            get { return  GetValue<Nullable<System.Int32>>( ProjectActivityIdProperty);}
            set { SetValue(ProjectActivityIdProperty,value); }
        }

        /// <summary> The ProjectTeamId property of the Entity AppMessage</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ProjectTeamId
        {
            get { return  GetValue<Nullable<System.Int32>>( ProjectTeamIdProperty);}
            set { SetValue(ProjectTeamIdProperty,value); }
        }

        /// <summary> The ProjectId property of the Entity AppMessage</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ProjectId
        {
            get { return  GetValue<Nullable<System.Int32>>( ProjectIdProperty);}
            set { SetValue(ProjectIdProperty,value); }
        }

        /// <summary> The AttachmentFileToken property of the Entity AppMessage</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String AttachmentFileToken
        {
            get { return  GetValue<System.String>( AttachmentFileTokenProperty);}
            set { SetValue(AttachmentFileTokenProperty,value); }
        }

        /// <summary> The MsgUniqueId property of the Entity AppMessage</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String MsgUniqueId
        {
            get { return  GetValue<System.String>( MsgUniqueIdProperty);}
            set { SetValue(MsgUniqueIdProperty,value); }
        }

        /// <summary> The ReminderMinutes property of the Entity AppMessage</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ReminderMinutes
        {
            get { return  GetValue<Nullable<System.Int32>>( ReminderMinutesProperty);}
            set { SetValue(ReminderMinutesProperty,value); }
        }

        /// <summary> The IsEnableReminder property of the Entity AppMessage</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsEnableReminder
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsEnableReminderProperty);}
            set { SetValue(IsEnableReminderProperty,value); }
        }

        /// <summary> The ReminderTargetDate property of the Entity AppMessage</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> ReminderTargetDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( ReminderTargetDateProperty);}
            set { SetValue(ReminderTargetDateProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppMessage</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppMessage</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppMessage</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppMessage</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppMessage</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedByCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByCompanyIdProperty);}
            set { SetValue(AppCreatedByCompanyIdProperty,value); }
        }

        /// <summary> The TransactionGroupId property of the Entity AppMessage</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionGroupId
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionGroupIdProperty);}
            set { SetValue(TransactionGroupIdProperty,value); }
        }
        
        #endregion

       
        
    }
}

