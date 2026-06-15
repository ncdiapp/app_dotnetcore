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
    /// DTO class for the  Extend Relation Entity 'AppSecurityLoginAuditor'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppSecurityLoginAuditorExDto : AppSecurityLoginAuditorDto 
    {

     	#region Static Name Properties Declaration
    
 
 

        
        #endregion
	
	
        public AppSecurityLoginAuditorExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties




		
		
	



        #endregion
        
    }
}

