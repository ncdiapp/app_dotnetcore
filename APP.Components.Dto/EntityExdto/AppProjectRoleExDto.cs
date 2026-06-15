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
    /// DTO class for the  Extend Relation Entity 'AppProjectRole'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppProjectRoleExDto : AppProjectRoleDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppProjectRolePrivilegeListProperty = ObjectInfoHelper.GetName<AppProjectRoleExDto,  ObservableSet<AppProjectRolePrivilegeExDto>>(o=>o.AppProjectRolePrivilegeList);
            public static readonly string AppProjectTeamMemberRoleListProperty = ObjectInfoHelper.GetName<AppProjectRoleExDto,  ObservableSet<AppProjectTeamMemberRoleExDto>>(o=>o.AppProjectTeamMemberRoleList); 
 

        
        #endregion
	
	
        public AppProjectRoleExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppProjectRolePrivilegeList = new  ObservableSet<AppProjectRolePrivilegeExDto>();
            AppProjectTeamMemberRoleList = new  ObservableSet<AppProjectTeamMemberRoleExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppProjectRolePrivilegeEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppProjectRolePrivilegeExDto> AppProjectRolePrivilegeList
        {
            get
            {
			    return  GetValue<ObservableSet<AppProjectRolePrivilegeExDto>>(AppProjectRolePrivilegeListProperty);    
            }
            set
            {
				SetValue(AppProjectRolePrivilegeListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppProjectTeamMemberRoleEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppProjectTeamMemberRoleExDto> AppProjectTeamMemberRoleList
        {
            get
            {
			    return  GetValue<ObservableSet<AppProjectTeamMemberRoleExDto>>(AppProjectTeamMemberRoleListProperty);    
            }
            set
            {
				SetValue(AppProjectTeamMemberRoleListProperty,value);
            }
        }

		
		
	



        #endregion
        
    }
}

