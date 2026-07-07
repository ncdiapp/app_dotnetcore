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
    /// DTO class for the  Extend Relation Entity 'AppReport'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppReportExDto : AppReportDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppApplicationAssetsItemListProperty = ObjectInfoHelper.GetName<AppReportExDto,  ObservableSet<AppApplicationAssetsItemExDto>>(o=>o.AppApplicationAssetsItemList);
            public static readonly string AppSearchViewReportListProperty = ObjectInfoHelper.GetName<AppReportExDto,  ObservableSet<AppSearchViewReportExDto>>(o=>o.AppSearchViewReportList);
            public static readonly string AppSecuritySysObjGroupUserListProperty = ObjectInfoHelper.GetName<AppReportExDto,  ObservableSet<AppSecuritySysObjGroupUserExDto>>(o=>o.AppSecuritySysObjGroupUserList);
            public static readonly string AppTranscationReportListProperty = ObjectInfoHelper.GetName<AppReportExDto,  ObservableSet<AppTranscationReportExDto>>(o=>o.AppTranscationReportList); 
            public static readonly string ForeignAppDataSourceRegisterProperty = ObjectInfoHelper.GetName<AppReportExDto,  AppDataSourceRegisterExDto>(o=>o.ForeignAppDataSourceRegisterExDto);
            public static readonly string ForeignAppListMenuProperty = ObjectInfoHelper.GetName<AppReportExDto,  AppListMenuExDto>(o=>o.ForeignAppListMenuExDto); 

        
        #endregion
	
	
        public AppReportExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppApplicationAssetsItemList = new  ObservableSet<AppApplicationAssetsItemExDto>();
            AppSearchViewReportList = new  ObservableSet<AppSearchViewReportExDto>();
            AppSecuritySysObjGroupUserList = new  ObservableSet<AppSecuritySysObjGroupUserExDto>();
            AppTranscationReportList = new  ObservableSet<AppTranscationReportExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Report Template (AppReportTemplate table — not a LLBLGen entity)

        public static readonly string ReportTemplateProperty =
            ObjectInfoHelper.GetName<AppReportExDto, AppReportTemplateDto>(o => o.ReportTemplate);

        [DataMember(EmitDefaultValue = false)]
        public AppReportTemplateDto ReportTemplate
        {
            get { return GetValue<AppReportTemplateDto>(ReportTemplateProperty); }
            set { SetValue(ReportTemplateProperty, value); }
        }

        #endregion

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

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppSearchViewReportEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppSearchViewReportExDto> AppSearchViewReportList
        {
            get
            {
			    return  GetValue<ObservableSet<AppSearchViewReportExDto>>(AppSearchViewReportListProperty);    
            }
            set
            {
				SetValue(AppSearchViewReportListProperty,value);
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

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppTranscationReportEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppTranscationReportExDto> AppTranscationReportList
        {
            get
            {
			    return  GetValue<ObservableSet<AppTranscationReportExDto>>(AppTranscationReportListProperty);    
            }
            set
            {
				SetValue(AppTranscationReportListProperty,value);
            }
        }

		
		

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppDataSourceRegisterEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppDataSourceRegisterExDto ForeignAppDataSourceRegisterExDto
        {
            get
            {
			    return  GetValue<AppDataSourceRegisterExDto>(ForeignAppDataSourceRegisterProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppDataSourceRegisterProperty,value);
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

