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
    /// DTO class for the  Extend Relation Entity 'AppProjectTeamMember'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppProjectTeamMemberExDto : AppProjectTeamMemberDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppProjectTeamMemberRoleListProperty = ObjectInfoHelper.GetName<AppProjectTeamMemberExDto,  ObservableSet<AppProjectTeamMemberRoleExDto>>(o=>o.AppProjectTeamMemberRoleList); 
            public static readonly string ForeignAppProjectOrWorkFlowProperty = ObjectInfoHelper.GetName<AppProjectTeamMemberExDto,  AppProjectOrWorkFlowExDto>(o=>o.ForeignAppProjectOrWorkFlowExDto);
            public static readonly string ForeignAppProjectTeamProperty = ObjectInfoHelper.GetName<AppProjectTeamMemberExDto,  AppProjectTeamExDto>(o=>o.ForeignAppProjectTeamExDto);
            public static readonly string ForeignAppSecurityUserProperty = ObjectInfoHelper.GetName<AppProjectTeamMemberExDto,  AppSecurityUserExDto>(o=>o.ForeignAppSecurityUserExDto); 

        
        #endregion
	
	
        public AppProjectTeamMemberExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppProjectTeamMemberRoleList = new  ObservableSet<AppProjectTeamMemberRoleExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



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

		
		

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppProjectOrWorkFlowEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppProjectOrWorkFlowExDto ForeignAppProjectOrWorkFlowExDto
        {
            get
            {
			    return  GetValue<AppProjectOrWorkFlowExDto>(ForeignAppProjectOrWorkFlowProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppProjectOrWorkFlowProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppProjectTeamEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppProjectTeamExDto ForeignAppProjectTeamExDto
        {
            get
            {
			    return  GetValue<AppProjectTeamExDto>(ForeignAppProjectTeamProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppProjectTeamProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppSecurityUserEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppSecurityUserExDto ForeignAppSecurityUserExDto
        {
            get
            {
			    return  GetValue<AppSecurityUserExDto>(ForeignAppSecurityUserProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppSecurityUserProperty,value);
            }
        }	



        #endregion
        
    }
}

