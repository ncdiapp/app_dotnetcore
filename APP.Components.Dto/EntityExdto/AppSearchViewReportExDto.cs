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
    /// DTO class for the  Extend Relation Entity 'AppSearchViewReport'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppSearchViewReportExDto : AppSearchViewReportDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppSearchViewReportParamterMappingListProperty = ObjectInfoHelper.GetName<AppSearchViewReportExDto,  ObservableSet<AppSearchViewReportParamterMappingExDto>>(o=>o.AppSearchViewReportParamterMappingList); 
            public static readonly string ForeignAppReportProperty = ObjectInfoHelper.GetName<AppSearchViewReportExDto,  AppReportExDto>(o=>o.ForeignAppReportExDto);
            public static readonly string ForeignAppSearchViewProperty = ObjectInfoHelper.GetName<AppSearchViewReportExDto,  AppSearchViewExDto>(o=>o.ForeignAppSearchViewExDto); 

        
        #endregion
	
	
        public AppSearchViewReportExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppSearchViewReportParamterMappingList = new  ObservableSet<AppSearchViewReportParamterMappingExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppSearchViewReportParamterMappingEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppSearchViewReportParamterMappingExDto> AppSearchViewReportParamterMappingList
        {
            get
            {
			    return  GetValue<ObservableSet<AppSearchViewReportParamterMappingExDto>>(AppSearchViewReportParamterMappingListProperty);    
            }
            set
            {
				SetValue(AppSearchViewReportParamterMappingListProperty,value);
            }
        }

		
		

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppReportEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppReportExDto ForeignAppReportExDto
        {
            get
            {
			    return  GetValue<AppReportExDto>(ForeignAppReportProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppReportProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppSearchViewEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppSearchViewExDto ForeignAppSearchViewExDto
        {
            get
            {
			    return  GetValue<AppSearchViewExDto>(ForeignAppSearchViewProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppSearchViewProperty,value);
            }
        }	



        #endregion
        
    }
}

