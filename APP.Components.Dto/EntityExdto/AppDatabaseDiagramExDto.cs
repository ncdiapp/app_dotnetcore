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
    /// DTO class for the  Extend Relation Entity 'AppDatabaseDiagram'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppDatabaseDiagramExDto : AppDatabaseDiagramDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppDatabaseDiagramItemListProperty = ObjectInfoHelper.GetName<AppDatabaseDiagramExDto,  ObservableSet<AppDatabaseDiagramItemExDto>>(o=>o.AppDatabaseDiagramItemList); 
 

        
        #endregion
	
	
        public AppDatabaseDiagramExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppDatabaseDiagramItemList = new  ObservableSet<AppDatabaseDiagramItemExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppDatabaseDiagramItemEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppDatabaseDiagramItemExDto> AppDatabaseDiagramItemList
        {
            get
            {
			    return  GetValue<ObservableSet<AppDatabaseDiagramItemExDto>>(AppDatabaseDiagramItemListProperty);    
            }
            set
            {
				SetValue(AppDatabaseDiagramItemListProperty,value);
            }
        }

		
		
	



        #endregion
        
    }
}

