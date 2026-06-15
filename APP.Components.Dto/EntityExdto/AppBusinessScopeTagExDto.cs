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
    /// DTO class for the  Extend Relation Entity 'AppBusinessScopeTag'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppBusinessScopeTagExDto : AppBusinessScopeTagDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppProjectTaskTagListProperty = ObjectInfoHelper.GetName<AppBusinessScopeTagExDto,  ObservableSet<AppProjectTaskTagExDto>>(o=>o.AppProjectTaskTagList); 
 

        
        #endregion
	
	
        public AppBusinessScopeTagExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppProjectTaskTagList = new  ObservableSet<AppProjectTaskTagExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppProjectTaskTagEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppProjectTaskTagExDto> AppProjectTaskTagList
        {
            get
            {
			    return  GetValue<ObservableSet<AppProjectTaskTagExDto>>(AppProjectTaskTagListProperty);    
            }
            set
            {
				SetValue(AppProjectTaskTagListProperty,value);
            }
        }

		
		
	



        #endregion
        
    }
}

