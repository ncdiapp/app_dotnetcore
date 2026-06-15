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
    /// DTO class for the  Extend Relation Entity 'AppTransactionDataLoad'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppTransactionDataLoadExDto : AppTransactionDataLoadDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppProjectWorkFlowActionListProperty = ObjectInfoHelper.GetName<AppTransactionDataLoadExDto,  ObservableSet<AppProjectWorkFlowActionExDto>>(o=>o.AppProjectWorkFlowActionList);
            public static readonly string AppTranscationDataLoadFieldMappingListProperty = ObjectInfoHelper.GetName<AppTransactionDataLoadExDto,  ObservableSet<AppTranscationDataLoadFieldMappingExDto>>(o=>o.AppTranscationDataLoadFieldMappingList); 
            public static readonly string ForeignAppDataSetProperty = ObjectInfoHelper.GetName<AppTransactionDataLoadExDto,  AppDataSetExDto>(o=>o.ForeignAppDataSetExDto);
            public static readonly string ForeignAppTransactionProperty = ObjectInfoHelper.GetName<AppTransactionDataLoadExDto,  AppTransactionExDto>(o=>o.ForeignAppTransactionExDto);
            public static readonly string ForeignAppTransactionUnitProperty = ObjectInfoHelper.GetName<AppTransactionDataLoadExDto,  AppTransactionUnitExDto>(o=>o.ForeignAppTransactionUnitExDto); 

        
        #endregion
	
	
        public AppTransactionDataLoadExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppProjectWorkFlowActionList = new  ObservableSet<AppProjectWorkFlowActionExDto>();
            AppTranscationDataLoadFieldMappingList = new  ObservableSet<AppTranscationDataLoadFieldMappingExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



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

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppTranscationDataLoadFieldMappingEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppTranscationDataLoadFieldMappingExDto> AppTranscationDataLoadFieldMappingList
        {
            get
            {
			    return  GetValue<ObservableSet<AppTranscationDataLoadFieldMappingExDto>>(AppTranscationDataLoadFieldMappingListProperty);    
            }
            set
            {
				SetValue(AppTranscationDataLoadFieldMappingListProperty,value);
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

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppTransactionUnitEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppTransactionUnitExDto ForeignAppTransactionUnitExDto
        {
            get
            {
			    return  GetValue<AppTransactionUnitExDto>(ForeignAppTransactionUnitProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppTransactionUnitProperty,value);
            }
        }	



        #endregion
        
    }
}

