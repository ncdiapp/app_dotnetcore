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
    /// DTO class for the  Extend Relation Entity 'AppDesktop'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppDesktopExDto : AppDesktopDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppApplicationAssetsItemListProperty = ObjectInfoHelper.GetName<AppDesktopExDto,  ObservableSet<AppApplicationAssetsItemExDto>>(o=>o.AppApplicationAssetsItemList);
            public static readonly string AppDesktopItemListProperty = ObjectInfoHelper.GetName<AppDesktopExDto,  ObservableSet<AppDesktopItemExDto>>(o=>o.AppDesktopItemList);
            public static readonly string AppSecuritySysObjGroupUserListProperty = ObjectInfoHelper.GetName<AppDesktopExDto,  ObservableSet<AppSecuritySysObjGroupUserExDto>>(o=>o.AppSecuritySysObjGroupUserList); 
            public static readonly string ForeignAppListMenuProperty = ObjectInfoHelper.GetName<AppDesktopExDto,  AppListMenuExDto>(o=>o.ForeignAppListMenuExDto); 

        
        #endregion
	
	
        public AppDesktopExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppApplicationAssetsItemList = new  ObservableSet<AppApplicationAssetsItemExDto>();
            AppDesktopItemList = new  ObservableSet<AppDesktopItemExDto>();
            AppSecuritySysObjGroupUserList = new  ObservableSet<AppSecuritySysObjGroupUserExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppApplicationAssetsItemEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppApplicationAssetsItemExDto> AppApplicationAssetsItemList
        {
            get
            {
			    return  GetValue<ObservableSet<AppApplicationAssetsItemExDto>>(AppApplicationAssetsItemListProperty);    
            }
            set
            {
				SetValue(AppApplicationAssetsItemListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppDesktopItemEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppDesktopItemExDto> AppDesktopItemList
        {
            get
            {
			    return  GetValue<ObservableSet<AppDesktopItemExDto>>(AppDesktopItemListProperty);    
            }
            set
            {
				SetValue(AppDesktopItemListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppSecuritySysObjGroupUserEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppSecuritySysObjGroupUserExDto> AppSecuritySysObjGroupUserList
        {
            get
            {
			    return  GetValue<ObservableSet<AppSecuritySysObjGroupUserExDto>>(AppSecuritySysObjGroupUserListProperty);    
            }
            set
            {
				SetValue(AppSecuritySysObjGroupUserListProperty,value);
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

