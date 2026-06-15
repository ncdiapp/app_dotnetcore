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
    /// DTO class for the  Extend Relation Entity 'AppProjectTaskTag'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppProjectTaskTagExDto : AppProjectTaskTagDto 
    {

     	#region Static Name Properties Declaration
    
 
            public static readonly string ForeignAppBusinessScopeTagProperty = ObjectInfoHelper.GetName<AppProjectTaskTagExDto,  AppBusinessScopeTagExDto>(o=>o.ForeignAppBusinessScopeTagExDto);
            public static readonly string ForeignAppProjectWorkFlowTaskProperty = ObjectInfoHelper.GetName<AppProjectTaskTagExDto,  AppProjectWorkFlowTaskExDto>(o=>o.ForeignAppProjectWorkFlowTaskExDto); 

        
        #endregion
	
	
        public AppProjectTaskTagExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties




		
		

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppBusinessScopeTagEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppBusinessScopeTagExDto ForeignAppBusinessScopeTagExDto
        {
            get
            {
			    return  GetValue<AppBusinessScopeTagExDto>(ForeignAppBusinessScopeTagProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppBusinessScopeTagProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppProjectWorkFlowTaskEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppProjectWorkFlowTaskExDto ForeignAppProjectWorkFlowTaskExDto
        {
            get
            {
			    return  GetValue<AppProjectWorkFlowTaskExDto>(ForeignAppProjectWorkFlowTaskProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppProjectWorkFlowTaskProperty,value);
            }
        }	



        #endregion
        
    }
}

