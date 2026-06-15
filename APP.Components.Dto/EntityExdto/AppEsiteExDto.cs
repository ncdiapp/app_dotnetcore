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
    /// DTO class for the  Extend Relation Entity 'AppEsite'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppEsiteExDto : AppEsiteDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppEsiteCatalogueListProperty = ObjectInfoHelper.GetName<AppEsiteExDto,  ObservableSet<AppEsiteCatalogueExDto>>(o=>o.AppEsiteCatalogueList);
            public static readonly string AppEsitePagesListProperty = ObjectInfoHelper.GetName<AppEsiteExDto,  ObservableSet<AppEsitePagesExDto>>(o=>o.AppEsitePagesList); 
            public static readonly string ForeignAppListMenuProperty = ObjectInfoHelper.GetName<AppEsiteExDto,  AppListMenuExDto>(o=>o.ForeignAppListMenuExDto); 

        
        #endregion
	
	
        public AppEsiteExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppEsiteCatalogueList = new  ObservableSet<AppEsiteCatalogueExDto>();
            AppEsitePagesList = new  ObservableSet<AppEsitePagesExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppEsiteCatalogueEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppEsiteCatalogueExDto> AppEsiteCatalogueList
        {
            get
            {
			    return  GetValue<ObservableSet<AppEsiteCatalogueExDto>>(AppEsiteCatalogueListProperty);    
            }
            set
            {
				SetValue(AppEsiteCatalogueListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppEsitePagesEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppEsitePagesExDto> AppEsitePagesList
        {
            get
            {
			    return  GetValue<ObservableSet<AppEsitePagesExDto>>(AppEsitePagesListProperty);    
            }
            set
            {
				SetValue(AppEsitePagesListProperty,value);
            }
        }

		
		

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppListMenuEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppListMenuExDto ForeignAppListMenuExDto
        {
            get
            {
			    return  GetValue<AppListMenuExDto>(ForeignAppListMenuProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppListMenuProperty,value);
            }
        }	



        #endregion
        
    }
}

