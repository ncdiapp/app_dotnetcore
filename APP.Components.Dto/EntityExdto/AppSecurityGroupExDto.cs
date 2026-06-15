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
    /// DTO class for the  Extend Relation Entity 'AppSecurityGroup'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppSecurityGroupExDto : AppSecurityGroupDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppFileOrFolderShareToOtherListProperty = ObjectInfoHelper.GetName<AppSecurityGroupExDto,  ObservableSet<AppFileOrFolderShareToOtherExDto>>(o=>o.AppFileOrFolderShareToOtherList);
            public static readonly string AppMessageListProperty = ObjectInfoHelper.GetName<AppSecurityGroupExDto,  ObservableSet<AppMessageExDto>>(o=>o.AppMessageList);
            public static readonly string AppSearchSavedListProperty = ObjectInfoHelper.GetName<AppSecurityGroupExDto,  ObservableSet<AppSearchSavedExDto>>(o=>o.AppSearchSavedList);
            public static readonly string AppSecurityGroupMemberListProperty = ObjectInfoHelper.GetName<AppSecurityGroupExDto,  ObservableSet<AppSecurityGroupMemberExDto>>(o=>o.AppSecurityGroupMemberList);
            public static readonly string AppSecuritySysObjGroupUserListProperty = ObjectInfoHelper.GetName<AppSecurityGroupExDto,  ObservableSet<AppSecuritySysObjGroupUserExDto>>(o=>o.AppSecuritySysObjGroupUserList);
            public static readonly string AppSecurityUserListMenuListProperty = ObjectInfoHelper.GetName<AppSecurityGroupExDto,  ObservableSet<AppSecurityUserListMenuExDto>>(o=>o.AppSecurityUserListMenuList);
            public static readonly string AppSecurityUserRolePrevilegeListProperty = ObjectInfoHelper.GetName<AppSecurityGroupExDto,  ObservableSet<AppSecurityUserRolePrevilegeExDto>>(o=>o.AppSecurityUserRolePrevilegeList);
            public static readonly string AppSefolderResourceListProperty = ObjectInfoHelper.GetName<AppSecurityGroupExDto,  ObservableSet<AppSefolderResourceExDto>>(o=>o.AppSefolderResourceList); 
            public static readonly string ForeignAppComOrganizationProperty = ObjectInfoHelper.GetName<AppSecurityGroupExDto,  AppComOrganizationExDto>(o=>o.ForeignAppComOrganizationExDto);
            public static readonly string ForeignAppCompanyProperty = ObjectInfoHelper.GetName<AppSecurityGroupExDto,  AppCompanyExDto>(o=>o.ForeignAppCompanyExDto); 

        
        #endregion
	
	
        public AppSecurityGroupExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppFileOrFolderShareToOtherList = new  ObservableSet<AppFileOrFolderShareToOtherExDto>();
            AppMessageList = new  ObservableSet<AppMessageExDto>();
            AppSearchSavedList = new  ObservableSet<AppSearchSavedExDto>();
            AppSecurityGroupMemberList = new  ObservableSet<AppSecurityGroupMemberExDto>();
            AppSecuritySysObjGroupUserList = new  ObservableSet<AppSecuritySysObjGroupUserExDto>();
            AppSecurityUserListMenuList = new  ObservableSet<AppSecurityUserListMenuExDto>();
            AppSecurityUserRolePrevilegeList = new  ObservableSet<AppSecurityUserRolePrevilegeExDto>();
            AppSefolderResourceList = new  ObservableSet<AppSefolderResourceExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



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

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppMessageEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppMessageExDto> AppMessageList
        {
            get
            {
			    return  GetValue<ObservableSet<AppMessageExDto>>(AppMessageListProperty);    
            }
            set
            {
				SetValue(AppMessageListProperty,value);
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



        #endregion
        
    }
}

