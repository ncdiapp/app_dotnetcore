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
    /// DTO class for the  Extend Relation Entity 'AppProjectWorkFlowAction'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppProjectWorkFlowActionExDto : AppProjectWorkFlowActionDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppTransactionField___ListProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionExDto,  ObservableSet<AppTransactionFieldExDto>>(o=>o.AppTransactionField___List); 
            public static readonly string ForeignAppDataSetProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionExDto,  AppDataSetExDto>(o=>o.ForeignAppDataSetExDto);
            public static readonly string ForeignAppMessageProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionExDto,  AppMessageExDto>(o=>o.ForeignAppMessageExDto);
            public static readonly string ForeignAppProjectOrWorkFlowProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionExDto,  AppProjectOrWorkFlowExDto>(o=>o.ForeignAppProjectOrWorkFlowExDto);
            public static readonly string ForeignAppProjectWorkFlowConditionProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionExDto,  AppProjectWorkFlowConditionExDto>(o=>o.ForeignAppProjectWorkFlowConditionExDto);
            public static readonly string ForeignAppProjectWorkFlowTaskProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionExDto,  AppProjectWorkFlowTaskExDto>(o=>o.ForeignAppProjectWorkFlowTaskExDto);
            public static readonly string ForeignAppSearchViewProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionExDto,  AppSearchViewExDto>(o=>o.ForeignAppSearchViewExDto);
            public static readonly string ForeignAppTransaction_Property = ObjectInfoHelper.GetName<AppProjectWorkFlowActionExDto,  AppTransactionExDto>(o=>o.ForeignAppTransaction_ExDto);
            public static readonly string ForeignAppTransactionProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionExDto,  AppTransactionExDto>(o=>o.ForeignAppTransactionExDto);
            public static readonly string ForeignAppTransactionDataLoadProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionExDto,  AppTransactionDataLoadExDto>(o=>o.ForeignAppTransactionDataLoadExDto);
            public static readonly string ForeignAppTransactionDataTransferSettingProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionExDto,  AppTransactionDataTransferSettingExDto>(o=>o.ForeignAppTransactionDataTransferSettingExDto);
            public static readonly string ForeignAppTransactionFieldProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionExDto,  AppTransactionFieldExDto>(o=>o.ForeignAppTransactionFieldExDto);
            public static readonly string ForeignAppTransactionField__Property = ObjectInfoHelper.GetName<AppProjectWorkFlowActionExDto,  AppTransactionFieldExDto>(o=>o.ForeignAppTransactionField__ExDto);
            public static readonly string ForeignAppTransactionField_Property = ObjectInfoHelper.GetName<AppProjectWorkFlowActionExDto,  AppTransactionFieldExDto>(o=>o.ForeignAppTransactionField_ExDto); 

        
        #endregion
	
	
        public AppProjectWorkFlowActionExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppTransactionField___List = new  ObservableSet<AppTransactionFieldExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppTransactionFieldEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppTransactionFieldExDto> AppTransactionField___List
        {
            get
            {
			    return  GetValue<ObservableSet<AppTransactionFieldExDto>>(AppTransactionField___ListProperty);    
            }
            set
            {
				SetValue(AppTransactionField___ListProperty,value);
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

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppMessageEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppMessageExDto ForeignAppMessageExDto
        {
            get
            {
			    return  GetValue<AppMessageExDto>(ForeignAppMessageProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppMessageProperty,value);
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

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppProjectWorkFlowConditionEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppProjectWorkFlowConditionExDto ForeignAppProjectWorkFlowConditionExDto
        {
            get
            {
			    return  GetValue<AppProjectWorkFlowConditionExDto>(ForeignAppProjectWorkFlowConditionProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppProjectWorkFlowConditionProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppProjectWorkFlowTaskEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppProjectWorkFlowTaskExDto ForeignAppProjectWorkFlowTaskExDto
        {
            get
            {
			    return  GetValue<AppProjectWorkFlowTaskExDto>(ForeignAppProjectWorkFlowTaskProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppProjectWorkFlowTaskProperty,value);
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

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppTransactionEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppTransactionExDto ForeignAppTransaction_ExDto
        {
            get
            {
			    return  GetValue<AppTransactionExDto>(ForeignAppTransaction_Property ) ;    
            }
            set
            {
				SetValue(ForeignAppTransaction_Property,value);
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

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppTransactionDataLoadEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppTransactionDataLoadExDto ForeignAppTransactionDataLoadExDto
        {
            get
            {
			    return  GetValue<AppTransactionDataLoadExDto>(ForeignAppTransactionDataLoadProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppTransactionDataLoadProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppTransactionDataTransferSettingEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppTransactionDataTransferSettingExDto ForeignAppTransactionDataTransferSettingExDto
        {
            get
            {
			    return  GetValue<AppTransactionDataTransferSettingExDto>(ForeignAppTransactionDataTransferSettingProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppTransactionDataTransferSettingProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppTransactionFieldEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppTransactionFieldExDto ForeignAppTransactionFieldExDto
        {
            get
            {
			    return  GetValue<AppTransactionFieldExDto>(ForeignAppTransactionFieldProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppTransactionFieldProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppTransactionFieldEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppTransactionFieldExDto ForeignAppTransactionField__ExDto
        {
            get
            {
			    return  GetValue<AppTransactionFieldExDto>(ForeignAppTransactionField__Property ) ;    
            }
            set
            {
				SetValue(ForeignAppTransactionField__Property,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppTransactionFieldEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppTransactionFieldExDto ForeignAppTransactionField_ExDto
        {
            get
            {
			    return  GetValue<AppTransactionFieldExDto>(ForeignAppTransactionField_Property ) ;    
            }
            set
            {
				SetValue(ForeignAppTransactionField_Property,value);
            }
        }	



        #endregion
        
    }
}

