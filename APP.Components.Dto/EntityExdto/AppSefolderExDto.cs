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
    /// DTO class for the  Extend Relation Entity 'AppSefolder'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppSefolderExDto : AppSefolderDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppCurrentUserFavouriteFolderOrFileListProperty = ObjectInfoHelper.GetName<AppSefolderExDto,  ObservableSet<AppCurrentUserFavouriteFolderOrFileExDto>>(o=>o.AppCurrentUserFavouriteFolderOrFileList);
            public static readonly string AppFileListProperty = ObjectInfoHelper.GetName<AppSefolderExDto,  ObservableSet<AppFileExDto>>(o=>o.AppFileList);
            public static readonly string AppFileOrFolderShareToOtherListProperty = ObjectInfoHelper.GetName<AppSefolderExDto,  ObservableSet<AppFileOrFolderShareToOtherExDto>>(o=>o.AppFileOrFolderShareToOtherList);
            public static readonly string AppSecurityUserListProperty = ObjectInfoHelper.GetName<AppSefolderExDto,  ObservableSet<AppSecurityUserExDto>>(o=>o.AppSecurityUserList);
            public static readonly string AppSefolderResourceListProperty = ObjectInfoHelper.GetName<AppSefolderExDto,  ObservableSet<AppSefolderResourceExDto>>(o=>o.AppSefolderResourceList); 
            public static readonly string ForeignAppTransactionProperty = ObjectInfoHelper.GetName<AppSefolderExDto,  AppTransactionExDto>(o=>o.ForeignAppTransactionExDto); 

        
        #endregion
	
	
        public AppSefolderExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppCurrentUserFavouriteFolderOrFileList = new  ObservableSet<AppCurrentUserFavouriteFolderOrFileExDto>();
            AppFileList = new  ObservableSet<AppFileExDto>();
            AppFileOrFolderShareToOtherList = new  ObservableSet<AppFileOrFolderShareToOtherExDto>();
            AppSecurityUserList = new  ObservableSet<AppSecurityUserExDto>();
            AppSefolderResourceList = new  ObservableSet<AppSefolderResourceExDto>(); 
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

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppFileEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppFileExDto> AppFileList
        {
            get
            {
			    return  GetValue<ObservableSet<AppFileExDto>>(AppFileListProperty);    
            }
            set
            {
				SetValue(AppFileListProperty,value);
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

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppSecurityUserEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppSecurityUserExDto> AppSecurityUserList
        {
            get
            {
			    return  GetValue<ObservableSet<AppSecurityUserExDto>>(AppSecurityUserListProperty);    
            }
            set
            {
				SetValue(AppSecurityUserListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppSefolderResourceEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppSefolderResourceExDto> AppSefolderResourceList
        {
            get
            {
			    return  GetValue<ObservableSet<AppSefolderResourceExDto>>(AppSefolderResourceListProperty);    
            }
            set
            {
				SetValue(AppSefolderResourceListProperty,value);
            }
        }

		
		

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppTransactionEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppTransactionExDto ForeignAppTransactionExDto
        {
            get
            {
			    return  GetValue<AppTransactionExDto>(ForeignAppTransactionProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppTransactionProperty,value);
            }
        }	



        #endregion
        
    }
}

