using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

using APP.Framework;
using APP.Framework.Collections;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    /// <summary>
    /// DTO class for the  Extend Relation Entity 'AppSecurityUser'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppSecurityUserExDto : AppSecurityUserDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppBusinessPartnerInviteUserListProperty = ObjectInfoHelper.GetName<AppSecurityUserExDto,  ObservableSet<AppBusinessPartnerInviteUserExDto>>(o=>o.AppBusinessPartnerInviteUserList);
            public static readonly string AppCalendarListProperty = ObjectInfoHelper.GetName<AppSecurityUserExDto,  ObservableSet<AppCalendarExDto>>(o=>o.AppCalendarList);
            public static readonly string AppCurrentUserFavouriteFolderOrFileListProperty = ObjectInfoHelper.GetName<AppSecurityUserExDto,  ObservableSet<AppCurrentUserFavouriteFolderOrFileExDto>>(o=>o.AppCurrentUserFavouriteFolderOrFileList);
            public static readonly string AppFileOrFolderShareToOtherListProperty = ObjectInfoHelper.GetName<AppSecurityUserExDto,  ObservableSet<AppFileOrFolderShareToOtherExDto>>(o=>o.AppFileOrFolderShareToOtherList);
            public static readonly string AppProjectOrWorkFlowListProperty = ObjectInfoHelper.GetName<AppSecurityUserExDto,  ObservableSet<AppProjectOrWorkFlowExDto>>(o=>o.AppProjectOrWorkFlowList);
            public static readonly string AppProjectTeamMemberListProperty = ObjectInfoHelper.GetName<AppSecurityUserExDto,  ObservableSet<AppProjectTeamMemberExDto>>(o=>o.AppProjectTeamMemberList);
            public static readonly string AppSearchSavedListProperty = ObjectInfoHelper.GetName<AppSecurityUserExDto,  ObservableSet<AppSearchSavedExDto>>(o=>o.AppSearchSavedList);
            public static readonly string AppSecurityGroupMemberListProperty = ObjectInfoHelper.GetName<AppSecurityUserExDto,  ObservableSet<AppSecurityGroupMemberExDto>>(o=>o.AppSecurityGroupMemberList);
            public static readonly string AppSecuritySysObjGroupUserListProperty = ObjectInfoHelper.GetName<AppSecurityUserExDto,  ObservableSet<AppSecuritySysObjGroupUserExDto>>(o=>o.AppSecuritySysObjGroupUserList);
            public static readonly string AppSecurityUserContactListProperty = ObjectInfoHelper.GetName<AppSecurityUserExDto,  ObservableSet<AppSecurityUserContactExDto>>(o=>o.AppSecurityUserContactList);
            public static readonly string AppSecurityUserInvitationListProperty = ObjectInfoHelper.GetName<AppSecurityUserExDto,  ObservableSet<AppSecurityUserInvitationExDto>>(o=>o.AppSecurityUserInvitationList);
            public static readonly string AppSecurityUserListMenuListProperty = ObjectInfoHelper.GetName<AppSecurityUserExDto,  ObservableSet<AppSecurityUserListMenuExDto>>(o=>o.AppSecurityUserListMenuList);
            public static readonly string AppSecurityUserRolePrevilegeListProperty = ObjectInfoHelper.GetName<AppSecurityUserExDto,  ObservableSet<AppSecurityUserRolePrevilegeExDto>>(o=>o.AppSecurityUserRolePrevilegeList);
            public static readonly string AppSecurityUserSessionListProperty = ObjectInfoHelper.GetName<AppSecurityUserExDto,  ObservableSet<AppSecurityUserSessionExDto>>(o=>o.AppSecurityUserSessionList);
            public static readonly string AppSefolderResourceListProperty = ObjectInfoHelper.GetName<AppSecurityUserExDto,  ObservableSet<AppSefolderResourceExDto>>(o=>o.AppSefolderResourceList);
            public static readonly string AppUserSkillListProperty = ObjectInfoHelper.GetName<AppSecurityUserExDto,  ObservableSet<AppUserSkillExDto>>(o=>o.AppUserSkillList); 
            public static readonly string ForeignAppCalendar_Property = ObjectInfoHelper.GetName<AppSecurityUserExDto,  AppCalendarExDto>(o=>o.ForeignAppCalendar_ExDto);
            public static readonly string ForeignAppComOrganizationProperty = ObjectInfoHelper.GetName<AppSecurityUserExDto,  AppComOrganizationExDto>(o=>o.ForeignAppComOrganizationExDto);
            public static readonly string ForeignAppCompanyProperty = ObjectInfoHelper.GetName<AppSecurityUserExDto,  AppCompanyExDto>(o=>o.ForeignAppCompanyExDto);
            public static readonly string ForeignAppSecurityRegDomainProperty = ObjectInfoHelper.GetName<AppSecurityUserExDto,  AppSecurityRegDomainExDto>(o=>o.ForeignAppSecurityRegDomainExDto);
            public static readonly string ForeignAppSefolderProperty = ObjectInfoHelper.GetName<AppSecurityUserExDto,  AppSefolderExDto>(o=>o.ForeignAppSefolderExDto); 

        
        #endregion
	
	
        public AppSecurityUserExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppBusinessPartnerInviteUserList = new  ObservableSet<AppBusinessPartnerInviteUserExDto>();
            AppCalendarList = new  ObservableSet<AppCalendarExDto>();
            AppCurrentUserFavouriteFolderOrFileList = new  ObservableSet<AppCurrentUserFavouriteFolderOrFileExDto>();
            AppFileOrFolderShareToOtherList = new  ObservableSet<AppFileOrFolderShareToOtherExDto>();
            AppProjectOrWorkFlowList = new  ObservableSet<AppProjectOrWorkFlowExDto>();
            AppProjectTeamMemberList = new  ObservableSet<AppProjectTeamMemberExDto>();
            AppSearchSavedList = new  ObservableSet<AppSearchSavedExDto>();
            AppSecurityGroupMemberList = new  ObservableSet<AppSecurityGroupMemberExDto>();
            AppSecuritySysObjGroupUserList = new  ObservableSet<AppSecuritySysObjGroupUserExDto>();
            AppSecurityUserContactList = new  ObservableSet<AppSecurityUserContactExDto>();
            AppSecurityUserInvitationList = new  ObservableSet<AppSecurityUserInvitationExDto>();
            AppSecurityUserListMenuList = new  ObservableSet<AppSecurityUserListMenuExDto>();
            AppSecurityUserRolePrevilegeList = new  ObservableSet<AppSecurityUserRolePrevilegeExDto>();
            AppSecurityUserSessionList = new  ObservableSet<AppSecurityUserSessionExDto>();
            AppSefolderResourceList = new  ObservableSet<AppSefolderResourceExDto>();
            AppUserSkillList = new  ObservableSet<AppUserSkillExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppBusinessPartnerInviteUserEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppBusinessPartnerInviteUserExDto> AppBusinessPartnerInviteUserList
        {
            get
            {
			    return  GetValue<ObservableSet<AppBusinessPartnerInviteUserExDto>>(AppBusinessPartnerInviteUserListProperty);    
            }
            set
            {
				SetValue(AppBusinessPartnerInviteUserListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppCalendarEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppCalendarExDto> AppCalendarList
        {
            get
            {
			    return  GetValue<ObservableSet<AppCalendarExDto>>(AppCalendarListProperty);    
            }
            set
            {
				SetValue(AppCalendarListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppCurrentUserFavouriteFolderOrFileEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppCurrentUserFavouriteFolderOrFileExDto> AppCurrentUserFavouriteFolderOrFileList
        {
            get
            {
			    return  GetValue<ObservableSet<AppCurrentUserFavouriteFolderOrFileExDto>>(AppCurrentUserFavouriteFolderOrFileListProperty);    
            }
            set
            {
				SetValue(AppCurrentUserFavouriteFolderOrFileListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppFileOrFolderShareToOtherEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppFileOrFolderShareToOtherExDto> AppFileOrFolderShareToOtherList
        {
            get
            {
			    return  GetValue<ObservableSet<AppFileOrFolderShareToOtherExDto>>(AppFileOrFolderShareToOtherListProperty);    
            }
            set
            {
				SetValue(AppFileOrFolderShareToOtherListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppProjectOrWorkFlowEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppProjectOrWorkFlowExDto> AppProjectOrWorkFlowList
        {
            get
            {
			    return  GetValue<ObservableSet<AppProjectOrWorkFlowExDto>>(AppProjectOrWorkFlowListProperty);    
            }
            set
            {
				SetValue(AppProjectOrWorkFlowListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppProjectTeamMemberEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppProjectTeamMemberExDto> AppProjectTeamMemberList
        {
            get
            {
			    return  GetValue<ObservableSet<AppProjectTeamMemberExDto>>(AppProjectTeamMemberListProperty);    
            }
            set
            {
				SetValue(AppProjectTeamMemberListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppSearchSavedEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppSearchSavedExDto> AppSearchSavedList
        {
            get
            {
			    return  GetValue<ObservableSet<AppSearchSavedExDto>>(AppSearchSavedListProperty);    
            }
            set
            {
				SetValue(AppSearchSavedListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppSecurityGroupMemberEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppSecurityGroupMemberExDto> AppSecurityGroupMemberList
        {
            get
            {
			    return  GetValue<ObservableSet<AppSecurityGroupMemberExDto>>(AppSecurityGroupMemberListProperty);    
            }
            set
            {
				SetValue(AppSecurityGroupMemberListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppSecuritySysObjGroupUserEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppSecuritySysObjGroupUserExDto> AppSecuritySysObjGroupUserList
        {
            get
            {
			    return  GetValue<ObservableSet<AppSecuritySysObjGroupUserExDto>>(AppSecuritySysObjGroupUserListProperty);    
            }
            set
            {
				SetValue(AppSecuritySysObjGroupUserListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppSecurityUserContactEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppSecurityUserContactExDto> AppSecurityUserContactList
        {
            get
            {
			    return  GetValue<ObservableSet<AppSecurityUserContactExDto>>(AppSecurityUserContactListProperty);    
            }
            set
            {
				SetValue(AppSecurityUserContactListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppSecurityUserInvitationEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppSecurityUserInvitationExDto> AppSecurityUserInvitationList
        {
            get
            {
			    return  GetValue<ObservableSet<AppSecurityUserInvitationExDto>>(AppSecurityUserInvitationListProperty);    
            }
            set
            {
				SetValue(AppSecurityUserInvitationListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppSecurityUserListMenuEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppSecurityUserListMenuExDto> AppSecurityUserListMenuList
        {
            get
            {
			    return  GetValue<ObservableSet<AppSecurityUserListMenuExDto>>(AppSecurityUserListMenuListProperty);    
            }
            set
            {
				SetValue(AppSecurityUserListMenuListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppSecurityUserRolePrevilegeEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppSecurityUserRolePrevilegeExDto> AppSecurityUserRolePrevilegeList
        {
            get
            {
			    return  GetValue<ObservableSet<AppSecurityUserRolePrevilegeExDto>>(AppSecurityUserRolePrevilegeListProperty);    
            }
            set
            {
				SetValue(AppSecurityUserRolePrevilegeListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppSecurityUserSessionEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppSecurityUserSessionExDto> AppSecurityUserSessionList
        {
            get
            {
			    return  GetValue<ObservableSet<AppSecurityUserSessionExDto>>(AppSecurityUserSessionListProperty);    
            }
            set
            {
				SetValue(AppSecurityUserSessionListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppSefolderResourceEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppSefolderResourceExDto> AppSefolderResourceList
        {
            get
            {
			    return  GetValue<ObservableSet<AppSefolderResourceExDto>>(AppSefolderResourceListProperty);    
            }
            set
            {
				SetValue(AppSefolderResourceListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppUserSkillEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppUserSkillExDto> AppUserSkillList
        {
            get
            {
			    return  GetValue<ObservableSet<AppUserSkillExDto>>(AppUserSkillListProperty);    
            }
            set
            {
				SetValue(AppUserSkillListProperty,value);
            }
        }

		
		

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppCalendarEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppCalendarExDto ForeignAppCalendar_ExDto
        {
            get
            {
			    return  GetValue<AppCalendarExDto>(ForeignAppCalendar_Property ) ;    
            }
            set
            {
				SetValue(ForeignAppCalendar_Property,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppComOrganizationEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppComOrganizationExDto ForeignAppComOrganizationExDto
        {
            get
            {
			    return  GetValue<AppComOrganizationExDto>(ForeignAppComOrganizationProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppComOrganizationProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppCompanyEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppCompanyExDto ForeignAppCompanyExDto
        {
            get
            {
			    return  GetValue<AppCompanyExDto>(ForeignAppCompanyProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppCompanyProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppSecurityRegDomainEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppSecurityRegDomainExDto ForeignAppSecurityRegDomainExDto
        {
            get
            {
			    return  GetValue<AppSecurityRegDomainExDto>(ForeignAppSecurityRegDomainProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppSecurityRegDomainProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppSefolderEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppSefolderExDto ForeignAppSefolderExDto
        {
            get
            {
			    return  GetValue<AppSefolderExDto>(ForeignAppSefolderProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppSefolderProperty,value);
            }
        }	


        /// <summary> Gets / sets related entity of type 'AppEmployeeEntity' which has to be set using a fetch action earlier. If no related entity
        /// is set for this property, null is returned. This property is not visible in databound grids.</summary>
        
        [DataMember(EmitDefaultValue=false)]
        public virtual AppEmployeeExDto AppEmployee
        {
            get
            {
			    return  GetValue(() => AppEmployee);
            }
            set
            {
				SetValue(() =>  AppEmployee,value);
            }
        }

        #endregion
        
    }
}

