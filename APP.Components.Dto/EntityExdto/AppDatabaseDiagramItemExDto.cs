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
    /// DTO class for the  Extend Relation Entity 'AppDatabaseDiagramItem'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppDatabaseDiagramItemExDto : AppDatabaseDiagramItemDto 
    {

     	#region Static Name Properties Declaration
    
 
            public static readonly string ForeignAppDatabaseDiagramProperty = ObjectInfoHelper.GetName<AppDatabaseDiagramItemExDto,  AppDatabaseDiagramExDto>(o=>o.ForeignAppDatabaseDiagramExDto); 

        
        #endregion
	
	
        public AppDatabaseDiagramItemExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties




		
		

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppDatabaseDiagramEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppDatabaseDiagramExDto ForeignAppDatabaseDiagramExDto
        {
            get
            {
			    return  GetValue<AppDatabaseDiagramExDto>(ForeignAppDatabaseDiagramProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppDatabaseDiagramProperty,value);
            }
        }	



        #endregion
        
    }
}

