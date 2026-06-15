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
    /// DTO class for the  Extend Relation Entity 'AppSecurityRegDomain'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppSecurityRegDomainExDto : AppSecurityRegDomainDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppSecurityRegDomainListMenuListProperty = ObjectInfoHelper.GetName<AppSecurityRegDomainExDto,  ObservableSet<AppSecurityRegDomainListMenuExDto>>(o=>o.AppSecurityRegDomainListMenuList);
            public static readonly string AppSecurityUserListProperty = ObjectInfoHelper.GetName<AppSecurityRegDomainExDto,  ObservableSet<AppSecurityUserExDto>>(o=>o.AppSecurityUserList); 
 

        
        #endregion
	
	
        public AppSecurityRegDomainExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppSecurityRegDomainListMenuList = new  ObservableSet<AppSecurityRegDomainListMenuExDto>();
            AppSecurityUserList = new  ObservableSet<AppSecurityUserExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



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

		
		
	



        #endregion
        
    }
}

