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
    /// DTO class for the  Extend Relation Entity 'AppProjectPerspectiveView'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppProjectPerspectiveViewExDto : AppProjectPerspectiveViewDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppProjectPerspectiveTaskListProperty = ObjectInfoHelper.GetName<AppProjectPerspectiveViewExDto,  ObservableSet<AppProjectPerspectiveTaskExDto>>(o=>o.AppProjectPerspectiveTaskList); 
 

        
        #endregion
	
	
        public AppProjectPerspectiveViewExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppProjectPerspectiveTaskList = new  ObservableSet<AppProjectPerspectiveTaskExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppProjectPerspectiveTaskEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppProjectPerspectiveTaskExDto> AppProjectPerspectiveTaskList
        {
            get
            {
			    return  GetValue<ObservableSet<AppProjectPerspectiveTaskExDto>>(AppProjectPerspectiveTaskListProperty);    
            }
            set
            {
				SetValue(AppProjectPerspectiveTaskListProperty,value);
            }
        }

		
		
	



        #endregion
        
    }
}

