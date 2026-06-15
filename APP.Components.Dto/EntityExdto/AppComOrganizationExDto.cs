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
    /// DTO class for the  Extend Relation Entity 'AppComOrganization'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppComOrganizationExDto : AppComOrganizationDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppSecurityGroupListProperty = ObjectInfoHelper.GetName<AppComOrganizationExDto,  ObservableSet<AppSecurityGroupExDto>>(o=>o.AppSecurityGroupList);
            public static readonly string AppSecurityRegDomainListMenuListProperty = ObjectInfoHelper.GetName<AppComOrganizationExDto,  ObservableSet<AppSecurityRegDomainListMenuExDto>>(o=>o.AppSecurityRegDomainListMenuList);
            public static readonly string AppSecuritySysObjGroupUserListProperty = ObjectInfoHelper.GetName<AppComOrganizationExDto,  ObservableSet<AppSecuritySysObjGroupUserExDto>>(o=>o.AppSecuritySysObjGroupUserList);
            public static readonly string AppSecurityUserListProperty = ObjectInfoHelper.GetName<AppComOrganizationExDto,  ObservableSet<AppSecurityUserExDto>>(o=>o.AppSecurityUserList); 
            public static readonly string ForeignAppCompanyProperty = ObjectInfoHelper.GetName<AppComOrganizationExDto,  AppCompanyExDto>(o=>o.ForeignAppCompanyExDto); 

        
        #endregion
	
	
        public AppComOrganizationExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppSecurityGroupList = new  ObservableSet<AppSecurityGroupExDto>();
            AppSecurityRegDomainListMenuList = new  ObservableSet<AppSecurityRegDomainListMenuExDto>();
            AppSecuritySysObjGroupUserList = new  ObservableSet<AppSecuritySysObjGroupUserExDto>();
            AppSecurityUserList = new  ObservableSet<AppSecurityUserExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



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

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppSecurityRegDomainListMenuEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppSecurityRegDomainListMenuExDto> AppSecurityRegDomainListMenuList
        {
            get
            {
			    return  GetValue<ObservableSet<AppSecurityRegDomainListMenuExDto>>(AppSecurityRegDomainListMenuListProperty);    
            }
            set
            {
				SetValue(AppSecurityRegDomainListMenuListProperty,value);
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

