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
    /// DTO class for the  Extend Relation Entity 'AppListMenu'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppListMenuExDto : AppListMenuDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppApplicationAssetsItemListProperty = ObjectInfoHelper.GetName<AppListMenuExDto,  ObservableSet<AppApplicationAssetsItemExDto>>(o=>o.AppApplicationAssetsItemList);
            public static readonly string AppDataSetListProperty = ObjectInfoHelper.GetName<AppListMenuExDto,  ObservableSet<AppDataSetExDto>>(o=>o.AppDataSetList);
            public static readonly string AppDesktopListProperty = ObjectInfoHelper.GetName<AppListMenuExDto,  ObservableSet<AppDesktopExDto>>(o=>o.AppDesktopList);
            public static readonly string AppEntityInfoListProperty = ObjectInfoHelper.GetName<AppListMenuExDto,  ObservableSet<AppEntityInfoExDto>>(o=>o.AppEntityInfoList);
            public static readonly string AppEsiteListProperty = ObjectInfoHelper.GetName<AppListMenuExDto,  ObservableSet<AppEsiteExDto>>(o=>o.AppEsiteList);
            public static readonly string AppFormListProperty = ObjectInfoHelper.GetName<AppListMenuExDto,  ObservableSet<AppFormExDto>>(o=>o.AppFormList);
            public static readonly string AppListMenu_ListProperty = ObjectInfoHelper.GetName<AppListMenuExDto,  ObservableSet<AppListMenuExDto>>(o=>o.AppListMenu_List);
            public static readonly string AppProjectOrWorkFlowListProperty = ObjectInfoHelper.GetName<AppListMenuExDto,  ObservableSet<AppProjectOrWorkFlowExDto>>(o=>o.AppProjectOrWorkFlowList);
            public static readonly string AppReportListProperty = ObjectInfoHelper.GetName<AppListMenuExDto,  ObservableSet<AppReportExDto>>(o=>o.AppReportList);
            public static readonly string AppSearchListProperty = ObjectInfoHelper.GetName<AppListMenuExDto,  ObservableSet<AppSearchExDto>>(o=>o.AppSearchList);
            public static readonly string AppSecurityRegDomainListMenuListProperty = ObjectInfoHelper.GetName<AppListMenuExDto,  ObservableSet<AppSecurityRegDomainListMenuExDto>>(o=>o.AppSecurityRegDomainListMenuList);
            public static readonly string AppSecurityUserListMenuListProperty = ObjectInfoHelper.GetName<AppListMenuExDto,  ObservableSet<AppSecurityUserListMenuExDto>>(o=>o.AppSecurityUserListMenuList);
            public static readonly string AppSysLabelLanguageListProperty = ObjectInfoHelper.GetName<AppListMenuExDto,  ObservableSet<AppSysLabelLanguageExDto>>(o=>o.AppSysLabelLanguageList);
            public static readonly string AppTransactionListProperty = ObjectInfoHelper.GetName<AppListMenuExDto,  ObservableSet<AppTransactionExDto>>(o=>o.AppTransactionList);
            public static readonly string AppVersionEditionModuleListProperty = ObjectInfoHelper.GetName<AppListMenuExDto,  ObservableSet<AppVersionEditionModuleExDto>>(o=>o.AppVersionEditionModuleList); 
            public static readonly string ForeignAppCompanyProperty = ObjectInfoHelper.GetName<AppListMenuExDto,  AppCompanyExDto>(o=>o.ForeignAppCompanyExDto);
            public static readonly string ForeignAppListMenuProperty = ObjectInfoHelper.GetName<AppListMenuExDto,  AppListMenuExDto>(o=>o.ForeignAppListMenuExDto); 

        
        #endregion
	
	
        public AppListMenuExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppApplicationAssetsItemList = new  ObservableSet<AppApplicationAssetsItemExDto>();
            AppDataSetList = new  ObservableSet<AppDataSetExDto>();
            AppDesktopList = new  ObservableSet<AppDesktopExDto>();
            AppEntityInfoList = new  ObservableSet<AppEntityInfoExDto>();
            AppEsiteList = new  ObservableSet<AppEsiteExDto>();
            AppFormList = new  ObservableSet<AppFormExDto>();
            AppListMenu_List = new  ObservableSet<AppListMenuExDto>();
            AppProjectOrWorkFlowList = new  ObservableSet<AppProjectOrWorkFlowExDto>();
            AppReportList = new  ObservableSet<AppReportExDto>();
            AppSearchList = new  ObservableSet<AppSearchExDto>();
            AppSecurityRegDomainListMenuList = new  ObservableSet<AppSecurityRegDomainListMenuExDto>();
            AppSecurityUserListMenuList = new  ObservableSet<AppSecurityUserListMenuExDto>();
            AppSysLabelLanguageList = new  ObservableSet<AppSysLabelLanguageExDto>();
            AppTransactionList = new  ObservableSet<AppTransactionExDto>();
            AppVersionEditionModuleList = new  ObservableSet<AppVersionEditionModuleExDto>(); 
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

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppDataSetEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppDataSetExDto> AppDataSetList
        {
            get
            {
			    return  GetValue<ObservableSet<AppDataSetExDto>>(AppDataSetListProperty);    
            }
            set
            {
				SetValue(AppDataSetListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppDesktopEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppDesktopExDto> AppDesktopList
        {
            get
            {
			    return  GetValue<ObservableSet<AppDesktopExDto>>(AppDesktopListProperty);    
            }
            set
            {
				SetValue(AppDesktopListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppEntityInfoEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppEntityInfoExDto> AppEntityInfoList
        {
            get
            {
			    return  GetValue<ObservableSet<AppEntityInfoExDto>>(AppEntityInfoListProperty);    
            }
            set
            {
				SetValue(AppEntityInfoListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppEsiteEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppEsiteExDto> AppEsiteList
        {
            get
            {
			    return  GetValue<ObservableSet<AppEsiteExDto>>(AppEsiteListProperty);    
            }
            set
            {
				SetValue(AppEsiteListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppFormEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppFormExDto> AppFormList
        {
            get
            {
			    return  GetValue<ObservableSet<AppFormExDto>>(AppFormListProperty);    
            }
            set
            {
				SetValue(AppFormListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppListMenuEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppListMenuExDto> AppListMenu_List
        {
            get
            {
			    return  GetValue<ObservableSet<AppListMenuExDto>>(AppListMenu_ListProperty);    
            }
            set
            {
				SetValue(AppListMenu_ListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppProjectOrWorkFlowEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppProjectOrWorkFlowExDto> AppProjectOrWorkFlowList
        {
            get
            {
			    return  GetValue<ObservableSet<AppProjectOrWorkFlowExDto>>(AppProjectOrWorkFlowListProperty);    
            }
            set
            {
				SetValue(AppProjectOrWorkFlowListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppReportEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppReportExDto> AppReportList
        {
            get
            {
			    return  GetValue<ObservableSet<AppReportExDto>>(AppReportListProperty);    
            }
            set
            {
				SetValue(AppReportListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppSearchEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppSearchExDto> AppSearchList
        {
            get
            {
			    return  GetValue<ObservableSet<AppSearchExDto>>(AppSearchListProperty);    
            }
            set
            {
				SetValue(AppSearchListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppSecurityRegDomainListMenuEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppSecurityRegDomainListMenuExDto> AppSecurityRegDomainListMenuList
        {
            get
            {
			    return  GetValue<ObservableSet<AppSecurityRegDomainListMenuExDto>>(AppSecurityRegDomainListMenuListProperty);    
            }
            set
            {
				SetValue(AppSecurityRegDomainListMenuListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppSecurityUserListMenuEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppSecurityUserListMenuExDto> AppSecurityUserListMenuList
        {
            get
            {
			    return  GetValue<ObservableSet<AppSecurityUserListMenuExDto>>(AppSecurityUserListMenuListProperty);    
            }
            set
            {
				SetValue(AppSecurityUserListMenuListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppSysLabelLanguageEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppSysLabelLanguageExDto> AppSysLabelLanguageList
        {
            get
            {
			    return  GetValue<ObservableSet<AppSysLabelLanguageExDto>>(AppSysLabelLanguageListProperty);    
            }
            set
            {
				SetValue(AppSysLabelLanguageListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppTransactionEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppTransactionExDto> AppTransactionList
        {
            get
            {
			    return  GetValue<ObservableSet<AppTransactionExDto>>(AppTransactionListProperty);    
            }
            set
            {
				SetValue(AppTransactionListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppVersionEditionModuleEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppVersionEditionModuleExDto> AppVersionEditionModuleList
        {
            get
            {
			    return  GetValue<ObservableSet<AppVersionEditionModuleExDto>>(AppVersionEditionModuleListProperty);    
            }
            set
            {
				SetValue(AppVersionEditionModuleListProperty,value);
            }
        }

		
		

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppCompanyEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppCompanyExDto ForeignAppCompanyExDto
        {
            get
            {
			    return  GetValue<AppCompanyExDto>(ForeignAppCompanyProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppCompanyProperty,value);
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

