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
    /// DTO class for the  Extend Relation Entity 'AppWorkFlowStep'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppWorkFlowStepExDto : AppWorkFlowStepDto 
    {

     	#region Static Name Properties Declaration
    
 
            public static readonly string ForeignAppWorkflowProperty = ObjectInfoHelper.GetName<AppWorkFlowStepExDto,  AppWorkflowExDto>(o=>o.ForeignAppWorkflowExDto);
            public static readonly string ForeignAppWorkflowLibActionProperty = ObjectInfoHelper.GetName<AppWorkFlowStepExDto,  AppWorkflowLibActionExDto>(o=>o.ForeignAppWorkflowLibActionExDto); 

        
        #endregion
	
	
        public AppWorkFlowStepExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties




		
		

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppWorkflowEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppWorkflowExDto ForeignAppWorkflowExDto
        {
            get
            {
			    return  GetValue<AppWorkflowExDto>(ForeignAppWorkflowProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppWorkflowProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppWorkflowLibActionEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppWorkflowLibActionExDto ForeignAppWorkflowLibActionExDto
        {
            get
            {
			    return  GetValue<AppWorkflowLibActionExDto>(ForeignAppWorkflowLibActionProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppWorkflowLibActionProperty,value);
            }
        }	



        #endregion
        
    }
}

