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
    /// DTO class for the  Extend Relation Entity 'AppMessageDeleted'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppMessageDeletedExDto : AppMessageDeletedDto 
    {

     	#region Static Name Properties Declaration
    
 
            public static readonly string ForeignAppMessageProperty = ObjectInfoHelper.GetName<AppMessageDeletedExDto,  AppMessageExDto>(o=>o.ForeignAppMessageExDto); 

        
        #endregion
	
	
        public AppMessageDeletedExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties




		
		

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppMessageEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppMessageExDto ForeignAppMessageExDto
        {
            get
            {
			    return  GetValue<AppMessageExDto>(ForeignAppMessageProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppMessageProperty,value);
            }
        }	



        #endregion
        
    }
}

