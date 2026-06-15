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
    /// DTO class for the  Extend Relation Entity 'AppApplicationAssetsItem'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppApplicationAssetsItemExDto : AppApplicationAssetsItemDto 
    {

     	#region Static Name Properties Declaration
    
 
            public static readonly string ForeignAppDesktopProperty = ObjectInfoHelper.GetName<AppApplicationAssetsItemExDto,  AppDesktopExDto>(o=>o.ForeignAppDesktopExDto);
            public static readonly string ForeignAppFormProperty = ObjectInfoHelper.GetName<AppApplicationAssetsItemExDto,  AppFormExDto>(o=>o.ForeignAppFormExDto);
            public static readonly string ForeignAppListMenuProperty = ObjectInfoHelper.GetName<AppApplicationAssetsItemExDto,  AppListMenuExDto>(o=>o.ForeignAppListMenuExDto);
            public static readonly string ForeignAppProjectOrWorkFlowProperty = ObjectInfoHelper.GetName<AppApplicationAssetsItemExDto,  AppProjectOrWorkFlowExDto>(o=>o.ForeignAppProjectOrWorkFlowExDto);
            public static readonly string ForeignAppReportProperty = ObjectInfoHelper.GetName<AppApplicationAssetsItemExDto,  AppReportExDto>(o=>o.ForeignAppReportExDto);
            public static readonly string ForeignAppSearchProperty = ObjectInfoHelper.GetName<AppApplicationAssetsItemExDto,  AppSearchExDto>(o=>o.ForeignAppSearchExDto);
            public static readonly string ForeignAppTransactionProperty = ObjectInfoHelper.GetName<AppApplicationAssetsItemExDto,  AppTransactionExDto>(o=>o.ForeignAppTransactionExDto); 

        
        #endregion
	
	
        public AppApplicationAssetsItemExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties




		
		

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppDesktopEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppDesktopExDto ForeignAppDesktopExDto
        {
            get
            {
			    return  GetValue<AppDesktopExDto>(ForeignAppDesktopProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppDesktopProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppFormEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppFormExDto ForeignAppFormExDto
        {
            get
            {
			    return  GetValue<AppFormExDto>(ForeignAppFormProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppFormProperty,value);
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

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppProjectOrWorkFlowEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppProjectOrWorkFlowExDto ForeignAppProjectOrWorkFlowExDto
        {
            get
            {
			    return  GetValue<AppProjectOrWorkFlowExDto>(ForeignAppProjectOrWorkFlowProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppProjectOrWorkFlowProperty,value);
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

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppSearchEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppSearchExDto ForeignAppSearchExDto
        {
            get
            {
			    return  GetValue<AppSearchExDto>(ForeignAppSearchProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppSearchProperty,value);
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

