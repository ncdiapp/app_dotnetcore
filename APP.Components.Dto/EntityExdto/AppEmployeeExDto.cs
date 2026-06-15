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
    /// DTO class for the  Extend Relation Entity 'AppEmployee'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppEmployeeExDto : AppEmployeeDto 
    {

     	#region Static Name Properties Declaration
    
 
 

        
        #endregion
	
	
        public AppEmployeeExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties




		
		
	


        /// <summary> Gets / sets related entity of type 'AppSecurityUserEntity' which has to be set using a fetch action earlier. If no related entity
        /// is set for this property, null is returned. This property is not visible in databound grids.</summary>
        
        [DataMember(EmitDefaultValue=false)]
        public virtual AppSecurityUserExDto AppSecurityUser
        {
            get
            {
			    return  GetValue(() => AppSecurityUser);
            }
            set
            {
				SetValue(() =>  AppSecurityUser,value);
            }
        }

        #endregion
        
    }
}

