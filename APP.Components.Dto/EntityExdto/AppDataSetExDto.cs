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
    /// DTO class for the  Extend Relation Entity 'AppDataSet'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppDataSetExDto : AppDataSetDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppDataSet_ListProperty = ObjectInfoHelper.GetName<AppDataSetExDto,  ObservableSet<AppDataSetExDto>>(o=>o.AppDataSet_List);
            public static readonly string AppDataSetParameterListProperty = ObjectInfoHelper.GetName<AppDataSetExDto,  ObservableSet<AppDataSetParameterExDto>>(o=>o.AppDataSetParameterList);
            public static readonly string AppDateSetDataExtractViewListProperty = ObjectInfoHelper.GetName<AppDataSetExDto,  ObservableSet<AppDateSetDataExtractViewExDto>>(o=>o.AppDateSetDataExtractViewList);
            public static readonly string AppProjectWorkFlowActionListProperty = ObjectInfoHelper.GetName<AppDataSetExDto,  ObservableSet<AppProjectWorkFlowActionExDto>>(o=>o.AppProjectWorkFlowActionList);
            public static readonly string AppSearchListProperty = ObjectInfoHelper.GetName<AppDataSetExDto,  ObservableSet<AppSearchExDto>>(o=>o.AppSearchList);
            public static readonly string AppSearchViewListProperty = ObjectInfoHelper.GetName<AppDataSetExDto,  ObservableSet<AppSearchViewExDto>>(o=>o.AppSearchViewList);
            public static readonly string AppTransactionDataLoadListProperty = ObjectInfoHelper.GetName<AppDataSetExDto,  ObservableSet<AppTransactionDataLoadExDto>>(o=>o.AppTransactionDataLoadList); 
            public static readonly string ForeignAppDataSetProperty = ObjectInfoHelper.GetName<AppDataSetExDto,  AppDataSetExDto>(o=>o.ForeignAppDataSetExDto);
            public static readonly string ForeignAppDataSourceRegisterProperty = ObjectInfoHelper.GetName<AppDataSetExDto,  AppDataSourceRegisterExDto>(o=>o.ForeignAppDataSourceRegisterExDto);
            public static readonly string ForeignAppListMenuProperty = ObjectInfoHelper.GetName<AppDataSetExDto,  AppListMenuExDto>(o=>o.ForeignAppListMenuExDto);
            public static readonly string ForeignAppWebApiConfigProperty = ObjectInfoHelper.GetName<AppDataSetExDto,  AppWebApiConfigExDto>(o=>o.ForeignAppWebApiConfigExDto); 

        
        #endregion
	
	
        public AppDataSetExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppDataSet_List = new  ObservableSet<AppDataSetExDto>();
            AppDataSetParameterList = new  ObservableSet<AppDataSetParameterExDto>();
            AppDateSetDataExtractViewList = new  ObservableSet<AppDateSetDataExtractViewExDto>();
            AppProjectWorkFlowActionList = new  ObservableSet<AppProjectWorkFlowActionExDto>();
            AppSearchList = new  ObservableSet<AppSearchExDto>();
            AppSearchViewList = new  ObservableSet<AppSearchViewExDto>();
            AppTransactionDataLoadList = new  ObservableSet<AppTransactionDataLoadExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppDataSetEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppDataSetExDto> AppDataSet_List
        {
            get
            {
			    return  GetValue<ObservableSet<AppDataSetExDto>>(AppDataSet_ListProperty);    
            }
            set
            {
				SetValue(AppDataSet_ListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppDataSetParameterEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppDataSetParameterExDto> AppDataSetParameterList
        {
            get
            {
			    return  GetValue<ObservableSet<AppDataSetParameterExDto>>(AppDataSetParameterListProperty);    
            }
            set
            {
				SetValue(AppDataSetParameterListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppDateSetDataExtractViewEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppDateSetDataExtractViewExDto> AppDateSetDataExtractViewList
        {
            get
            {
			    return  GetValue<ObservableSet<AppDateSetDataExtractViewExDto>>(AppDateSetDataExtractViewListProperty);    
            }
            set
            {
				SetValue(AppDateSetDataExtractViewListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppProjectWorkFlowActionEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppProjectWorkFlowActionExDto> AppProjectWorkFlowActionList
        {
            get
            {
			    return  GetValue<ObservableSet<AppProjectWorkFlowActionExDto>>(AppProjectWorkFlowActionListProperty);    
            }
            set
            {
				SetValue(AppProjectWorkFlowActionListProperty,value);
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

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppSearchViewEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppSearchViewExDto> AppSearchViewList
        {
            get
            {
			    return  GetValue<ObservableSet<AppSearchViewExDto>>(AppSearchViewListProperty);    
            }
            set
            {
				SetValue(AppSearchViewListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppTransactionDataLoadEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppTransactionDataLoadExDto> AppTransactionDataLoadList
        {
            get
            {
			    return  GetValue<ObservableSet<AppTransactionDataLoadExDto>>(AppTransactionDataLoadListProperty);    
            }
            set
            {
				SetValue(AppTransactionDataLoadListProperty,value);
            }
        }

		
		

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppDataSetEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppDataSetExDto ForeignAppDataSetExDto
        {
            get
            {
			    return  GetValue<AppDataSetExDto>(ForeignAppDataSetProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppDataSetProperty,value);
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

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppWebApiConfigEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppWebApiConfigExDto ForeignAppWebApiConfigExDto
        {
            get
            {
			    return  GetValue<AppWebApiConfigExDto>(ForeignAppWebApiConfigProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppWebApiConfigProperty,value);
            }
        }	



        #endregion
        
    }
}

