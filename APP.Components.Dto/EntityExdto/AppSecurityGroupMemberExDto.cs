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
    /// DTO class for the  Extend Relation Entity 'AppSecurityGroupMember'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppSecurityGroupMemberExDto : AppSecurityGroupMemberDto 
    {

     	#region Static Name Properties Declaration
    
 
            public static readonly string ForeignAppSecurityGroupProperty = ObjectInfoHelper.GetName<AppSecurityGroupMemberExDto,  AppSecurityGroupExDto>(o=>o.ForeignAppSecurityGroupExDto);
            public static readonly string ForeignAppSecurityUserProperty = ObjectInfoHelper.GetName<AppSecurityGroupMemberExDto,  AppSecurityUserExDto>(o=>o.ForeignAppSecurityUserExDto); 

        
        #endregion
	
	
        public AppSecurityGroupMemberExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties




		
		

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppSecurityGroupEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppSecurityGroupExDto ForeignAppSecurityGroupExDto
        {
            get
            {
			    return  GetValue<AppSecurityGroupExDto>(ForeignAppSecurityGroupProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppSecurityGroupProperty,value);
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

