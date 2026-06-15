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
    /// DTO class for the  Extend Relation Entity 'AppCompany'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppCompanyExDto : AppCompanyDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppBusinessPartnerListProperty = ObjectInfoHelper.GetName<AppCompanyExDto,  ObservableSet<AppBusinessPartnerExDto>>(o=>o.AppBusinessPartnerList);
            public static readonly string AppBusinessPartnerInviteUserListProperty = ObjectInfoHelper.GetName<AppCompanyExDto,  ObservableSet<AppBusinessPartnerInviteUserExDto>>(o=>o.AppBusinessPartnerInviteUserList);
            public static readonly string AppComOrganizationListProperty = ObjectInfoHelper.GetName<AppCompanyExDto,  ObservableSet<AppComOrganizationExDto>>(o=>o.AppComOrganizationList);
            public static readonly string AppComOrgLevelListProperty = ObjectInfoHelper.GetName<AppCompanyExDto,  ObservableSet<AppComOrgLevelExDto>>(o=>o.AppComOrgLevelList);
            public static readonly string AppCompany_ListProperty = ObjectInfoHelper.GetName<AppCompanyExDto,  ObservableSet<AppCompanyExDto>>(o=>o.AppCompany_List);
            public static readonly string AppCompanyOrderModuleListProperty = ObjectInfoHelper.GetName<AppCompanyExDto,  ObservableSet<AppCompanyOrderModuleExDto>>(o=>o.AppCompanyOrderModuleList);
            public static readonly string AppCompanyUserTypeRegisterListProperty = ObjectInfoHelper.GetName<AppCompanyExDto,  ObservableSet<AppCompanyUserTypeRegisterExDto>>(o=>o.AppCompanyUserTypeRegisterList);
            public static readonly string AppDataSourceRegisterListProperty = ObjectInfoHelper.GetName<AppCompanyExDto,  ObservableSet<AppDataSourceRegisterExDto>>(o=>o.AppDataSourceRegisterList);
            public static readonly string AppListMenuListProperty = ObjectInfoHelper.GetName<AppCompanyExDto,  ObservableSet<AppListMenuExDto>>(o=>o.AppListMenuList);
            public static readonly string AppSecurityGroupListProperty = ObjectInfoHelper.GetName<AppCompanyExDto,  ObservableSet<AppSecurityGroupExDto>>(o=>o.AppSecurityGroupList);
            public static readonly string AppSecurityUserListProperty = ObjectInfoHelper.GetName<AppCompanyExDto,  ObservableSet<AppSecurityUserExDto>>(o=>o.AppSecurityUserList); 
            public static readonly string ForeignAppCompanyProperty = ObjectInfoHelper.GetName<AppCompanyExDto,  AppCompanyExDto>(o=>o.ForeignAppCompanyExDto); 

        
        #endregion
	
	
        public AppCompanyExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppBusinessPartnerList = new  ObservableSet<AppBusinessPartnerExDto>();
            AppBusinessPartnerInviteUserList = new  ObservableSet<AppBusinessPartnerInviteUserExDto>();
            AppComOrganizationList = new  ObservableSet<AppComOrganizationExDto>();
            AppComOrgLevelList = new  ObservableSet<AppComOrgLevelExDto>();
            AppCompany_List = new  ObservableSet<AppCompanyExDto>();
            AppCompanyOrderModuleList = new  ObservableSet<AppCompanyOrderModuleExDto>();
            AppCompanyUserTypeRegisterList = new  ObservableSet<AppCompanyUserTypeRegisterExDto>();
            AppDataSourceRegisterList = new  ObservableSet<AppDataSourceRegisterExDto>();
            AppListMenuList = new  ObservableSet<AppListMenuExDto>();
            AppSecurityGroupList = new  ObservableSet<AppSecurityGroupExDto>();
            AppSecurityUserList = new  ObservableSet<AppSecurityUserExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppBusinessPartnerEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppBusinessPartnerExDto> AppBusinessPartnerList
        {
            get
            {
			    return  GetValue<ObservableSet<AppBusinessPartnerExDto>>(AppBusinessPartnerListProperty);    
            }
            set
            {
				SetValue(AppBusinessPartnerListProperty,value);
            }
        }

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

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppComOrganizationEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppComOrganizationExDto> AppComOrganizationList
        {
            get
            {
			    return  GetValue<ObservableSet<AppComOrganizationExDto>>(AppComOrganizationListProperty);    
            }
            set
            {
				SetValue(AppComOrganizationListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppComOrgLevelEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppComOrgLevelExDto> AppComOrgLevelList
        {
            get
            {
			    return  GetValue<ObservableSet<AppComOrgLevelExDto>>(AppComOrgLevelListProperty);    
            }
            set
            {
				SetValue(AppComOrgLevelListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppCompanyEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppCompanyExDto> AppCompany_List
        {
            get
            {
			    return  GetValue<ObservableSet<AppCompanyExDto>>(AppCompany_ListProperty);    
            }
            set
            {
				SetValue(AppCompany_ListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppCompanyOrderModuleEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppCompanyOrderModuleExDto> AppCompanyOrderModuleList
        {
            get
            {
			    return  GetValue<ObservableSet<AppCompanyOrderModuleExDto>>(AppCompanyOrderModuleListProperty);    
            }
            set
            {
				SetValue(AppCompanyOrderModuleListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppCompanyUserTypeRegisterEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppCompanyUserTypeRegisterExDto> AppCompanyUserTypeRegisterList
        {
            get
            {
			    return  GetValue<ObservableSet<AppCompanyUserTypeRegisterExDto>>(AppCompanyUserTypeRegisterListProperty);    
            }
            set
            {
				SetValue(AppCompanyUserTypeRegisterListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppDataSourceRegisterEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppDataSourceRegisterExDto> AppDataSourceRegisterList
        {
            get
            {
			    return  GetValue<ObservableSet<AppDataSourceRegisterExDto>>(AppDataSourceRegisterListProperty);    
            }
            set
            {
				SetValue(AppDataSourceRegisterListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppListMenuEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppListMenuExDto> AppListMenuList
        {
            get
            {
			    return  GetValue<ObservableSet<AppListMenuExDto>>(AppListMenuListProperty);    
            }
            set
            {
				SetValue(AppListMenuListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppSecurityGroupEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppSecurityGroupExDto> AppSecurityGroupList
        {
            get
            {
			    return  GetValue<ObservableSet<AppSecurityGroupExDto>>(AppSecurityGroupListProperty);    
            }
            set
            {
				SetValue(AppSecurityGroupListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppSecurityUserEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppSecurityUserExDto> AppSecurityUserList
        {
            get
            {
			    return  GetValue<ObservableSet<AppSecurityUserExDto>>(AppSecurityUserListProperty);    
            }
            set
            {
				SetValue(AppSecurityUserListProperty,value);
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



        #endregion
        
    }
}

