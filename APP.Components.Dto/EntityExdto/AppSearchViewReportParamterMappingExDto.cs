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
    /// DTO class for the  Extend Relation Entity 'AppSearchViewReportParamterMapping'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppSearchViewReportParamterMappingExDto : AppSearchViewReportParamterMappingDto 
    {

     	#region Static Name Properties Declaration
    
 
            public static readonly string ForeignAppSearchViewFieldProperty = ObjectInfoHelper.GetName<AppSearchViewReportParamterMappingExDto,  AppSearchViewFieldExDto>(o=>o.ForeignAppSearchViewFieldExDto);
            public static readonly string ForeignAppSearchViewReportProperty = ObjectInfoHelper.GetName<AppSearchViewReportParamterMappingExDto,  AppSearchViewReportExDto>(o=>o.ForeignAppSearchViewReportExDto); 

        
        #endregion
	
	
        public AppSearchViewReportParamterMappingExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties




		
		

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppSearchViewFieldEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppSearchViewFieldExDto ForeignAppSearchViewFieldExDto
        {
            get
            {
			    return  GetValue<AppSearchViewFieldExDto>(ForeignAppSearchViewFieldProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppSearchViewFieldProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppSearchViewReportEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppSearchViewReportExDto ForeignAppSearchViewReportExDto
        {
            get
            {
			    return  GetValue<AppSearchViewReportExDto>(ForeignAppSearchViewReportProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppSearchViewReportProperty,value);
            }
        }	



        #endregion
        
    }
}

