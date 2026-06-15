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
    /// DTO class for the  Extend Relation Entity 'AppForm'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppFormExDto : AppFormDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppApplicationAssetsItemListProperty = ObjectInfoHelper.GetName<AppFormExDto,  ObservableSet<AppApplicationAssetsItemExDto>>(o=>o.AppApplicationAssetsItemList);
            public static readonly string AppTransactionGroupItemListProperty = ObjectInfoHelper.GetName<AppFormExDto,  ObservableSet<AppFormGroupItemExDto>>(o=>o.AppTransactionGroupItemList);
            public static readonly string AppFormLayoutItemListProperty = ObjectInfoHelper.GetName<AppFormExDto,  ObservableSet<AppFormLayoutItemExDto>>(o=>o.AppFormLayoutItemList);
            public static readonly string AppTransaction_ListProperty = ObjectInfoHelper.GetName<AppFormExDto,  ObservableSet<AppTransactionExDto>>(o=>o.AppTransaction_List);
            public static readonly string AppTransactionListProperty = ObjectInfoHelper.GetName<AppFormExDto,  ObservableSet<AppTransactionExDto>>(o=>o.AppTransactionList); 
            public static readonly string ForeignAppListMenuProperty = ObjectInfoHelper.GetName<AppFormExDto,  AppListMenuExDto>(o=>o.ForeignAppListMenuExDto);
            public static readonly string ForeignAppSearchViewProperty = ObjectInfoHelper.GetName<AppFormExDto,  AppSearchViewExDto>(o=>o.ForeignAppSearchViewExDto); 

        
        #endregion
	
	
        public AppFormExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppApplicationAssetsItemList = new  ObservableSet<AppApplicationAssetsItemExDto>();
            AppTransactionGroupItemList = new  ObservableSet<AppFormGroupItemExDto>();
            AppFormLayoutItemList = new  ObservableSet<AppFormLayoutItemExDto>();
            AppTransaction_List = new  ObservableSet<AppTransactionExDto>();
            AppTransactionList = new  ObservableSet<AppTransactionExDto>(); 
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

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppFormGroupItemEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppFormGroupItemExDto> AppTransactionGroupItemList
        {
            get
            {
			    return  GetValue<ObservableSet<AppFormGroupItemExDto>>(AppTransactionGroupItemListProperty);    
            }
            set
            {
				SetValue(AppTransactionGroupItemListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppFormLayoutItemEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppFormLayoutItemExDto> AppFormLayoutItemList
        {
            get
            {
			    return  GetValue<ObservableSet<AppFormLayoutItemExDto>>(AppFormLayoutItemListProperty);    
            }
            set
            {
				SetValue(AppFormLayoutItemListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppTransactionEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppTransactionExDto> AppTransaction_List
        {
            get
            {
			    return  GetValue<ObservableSet<AppTransactionExDto>>(AppTransaction_ListProperty);    
            }
            set
            {
				SetValue(AppTransaction_ListProperty,value);
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



        #endregion
        
    }
}

