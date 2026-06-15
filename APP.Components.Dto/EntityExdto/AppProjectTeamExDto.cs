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
    /// DTO class for the  Extend Relation Entity 'AppProjectTeam'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppProjectTeamExDto : AppProjectTeamDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppProjectOrWorkFlowListProperty = ObjectInfoHelper.GetName<AppProjectTeamExDto,  ObservableSet<AppProjectOrWorkFlowExDto>>(o=>o.AppProjectOrWorkFlowList);
            public static readonly string AppProjectTeamMemberListProperty = ObjectInfoHelper.GetName<AppProjectTeamExDto,  ObservableSet<AppProjectTeamMemberExDto>>(o=>o.AppProjectTeamMemberList); 
 

        
        #endregion
	
	
        public AppProjectTeamExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppProjectOrWorkFlowList = new  ObservableSet<AppProjectOrWorkFlowExDto>();
            AppProjectTeamMemberList = new  ObservableSet<AppProjectTeamMemberExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



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

		
		
	



        #endregion
        
    }
}

