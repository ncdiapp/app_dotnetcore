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
    /// DTO class for the  Extend Relation Entity 'AppProjectTeamMemberRole'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppProjectTeamMemberRoleExDto : AppProjectTeamMemberRoleDto 
    {

     	#region Static Name Properties Declaration
    
 
            public static readonly string ForeignAppProjectRoleProperty = ObjectInfoHelper.GetName<AppProjectTeamMemberRoleExDto,  AppProjectRoleExDto>(o=>o.ForeignAppProjectRoleExDto);
            public static readonly string ForeignAppProjectTeamMemberProperty = ObjectInfoHelper.GetName<AppProjectTeamMemberRoleExDto,  AppProjectTeamMemberExDto>(o=>o.ForeignAppProjectTeamMemberExDto); 

        
        #endregion
	
	
        public AppProjectTeamMemberRoleExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties




		
		

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppProjectRoleEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppProjectRoleExDto ForeignAppProjectRoleExDto
        {
            get
            {
			    return  GetValue<AppProjectRoleExDto>(ForeignAppProjectRoleProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppProjectRoleProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppProjectTeamMemberEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppProjectTeamMemberExDto ForeignAppProjectTeamMemberExDto
        {
            get
            {
			    return  GetValue<AppProjectTeamMemberExDto>(ForeignAppProjectTeamMemberProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppProjectTeamMemberProperty,value);
            }
        }	



        #endregion
        
    }
}

