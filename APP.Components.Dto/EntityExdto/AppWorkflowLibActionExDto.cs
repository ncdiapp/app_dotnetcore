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
    /// DTO class for the  Extend Relation Entity 'AppWorkflowLibAction'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppWorkflowLibActionExDto : AppWorkflowLibActionDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppWorkFlowStepListProperty = ObjectInfoHelper.GetName<AppWorkflowLibActionExDto,  ObservableSet<AppWorkFlowStepExDto>>(o=>o.AppWorkFlowStepList); 
 

        
        #endregion
	
	
        public AppWorkflowLibActionExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppWorkFlowStepList = new  ObservableSet<AppWorkFlowStepExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppWorkFlowStepEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppWorkFlowStepExDto> AppWorkFlowStepList
        {
            get
            {
			    return  GetValue<ObservableSet<AppWorkFlowStepExDto>>(AppWorkFlowStepListProperty);    
            }
            set
            {
				SetValue(AppWorkFlowStepListProperty,value);
            }
        }

		
		
	



        #endregion
        
    }
}

