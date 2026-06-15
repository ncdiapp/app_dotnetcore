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
    /// DTO class for the  Extend Relation Entity 'AppProjectPerspectiveTask'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppProjectPerspectiveTaskExDto : AppProjectPerspectiveTaskDto 
    {

     	#region Static Name Properties Declaration
    
 
            public static readonly string ForeignAppProjectPerspectiveViewProperty = ObjectInfoHelper.GetName<AppProjectPerspectiveTaskExDto,  AppProjectPerspectiveViewExDto>(o=>o.ForeignAppProjectPerspectiveViewExDto);
            public static readonly string ForeignAppProjectWorkFlowTaskProperty = ObjectInfoHelper.GetName<AppProjectPerspectiveTaskExDto,  AppProjectWorkFlowTaskExDto>(o=>o.ForeignAppProjectWorkFlowTaskExDto); 

        
        #endregion
	
	
        public AppProjectPerspectiveTaskExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties




		
		

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppProjectPerspectiveViewEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppProjectPerspectiveViewExDto ForeignAppProjectPerspectiveViewExDto
        {
            get
            {
			    return  GetValue<AppProjectPerspectiveViewExDto>(ForeignAppProjectPerspectiveViewProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppProjectPerspectiveViewProperty,value);
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

