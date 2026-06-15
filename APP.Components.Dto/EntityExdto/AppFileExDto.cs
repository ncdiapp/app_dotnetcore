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
    /// DTO class for the  Extend Relation Entity 'AppFile'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppFileExDto : AppFileDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppCurrentUserFavouriteFolderOrFileListProperty = ObjectInfoHelper.GetName<AppFileExDto,  ObservableSet<AppCurrentUserFavouriteFolderOrFileExDto>>(o=>o.AppCurrentUserFavouriteFolderOrFileList);
            public static readonly string AppFileOrFolderShareToOtherListProperty = ObjectInfoHelper.GetName<AppFileExDto,  ObservableSet<AppFileOrFolderShareToOtherExDto>>(o=>o.AppFileOrFolderShareToOtherList); 
            public static readonly string ForeignAppSefolderProperty = ObjectInfoHelper.GetName<AppFileExDto,  AppSefolderExDto>(o=>o.ForeignAppSefolderExDto); 

        
        #endregion
	
	
        public AppFileExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppCurrentUserFavouriteFolderOrFileList = new  ObservableSet<AppCurrentUserFavouriteFolderOrFileExDto>();
            AppFileOrFolderShareToOtherList = new  ObservableSet<AppFileOrFolderShareToOtherExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppCurrentUserFavouriteFolderOrFileEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppCurrentUserFavouriteFolderOrFileExDto> AppCurrentUserFavouriteFolderOrFileList
        {
            get
            {
			    return  GetValue<ObservableSet<AppCurrentUserFavouriteFolderOrFileExDto>>(AppCurrentUserFavouriteFolderOrFileListProperty);    
            }
            set
            {
				SetValue(AppCurrentUserFavouriteFolderOrFileListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppFileOrFolderShareToOtherEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppFileOrFolderShareToOtherExDto> AppFileOrFolderShareToOtherList
        {
            get
            {
			    return  GetValue<ObservableSet<AppFileOrFolderShareToOtherExDto>>(AppFileOrFolderShareToOtherListProperty);    
            }
            set
            {
				SetValue(AppFileOrFolderShareToOtherListProperty,value);
            }
        }

		
		

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppSefolderEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppSefolderExDto ForeignAppSefolderExDto
        {
            get
            {
			    return  GetValue<AppSefolderExDto>(ForeignAppSefolderProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppSefolderProperty,value);
            }
        }	



        #endregion
        
    }
}

