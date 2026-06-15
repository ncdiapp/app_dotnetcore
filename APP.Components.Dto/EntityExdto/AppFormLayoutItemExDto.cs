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
    /// DTO class for the  Extend Relation Entity 'AppFormLayoutItem'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppFormLayoutItemExDto : AppFormLayoutItemDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppFormGridLayoutItemBindFieldListProperty = ObjectInfoHelper.GetName<AppFormLayoutItemExDto,  ObservableSet<AppFormGridLayoutItemBindFieldExDto>>(o=>o.AppFormGridLayoutItemBindFieldList);
            public static readonly string AppFormLayoutItem_ListProperty = ObjectInfoHelper.GetName<AppFormLayoutItemExDto,  ObservableSet<AppFormLayoutItemExDto>>(o=>o.AppFormLayoutItem_List);
            public static readonly string AppTransactionField_ListProperty = ObjectInfoHelper.GetName<AppFormLayoutItemExDto,  ObservableSet<AppTransactionFieldExDto>>(o=>o.AppTransactionField_List); 
            public static readonly string ForeignAppFormProperty = ObjectInfoHelper.GetName<AppFormLayoutItemExDto,  AppFormExDto>(o=>o.ForeignAppFormExDto);
            public static readonly string ForeignAppFormLayoutItemProperty = ObjectInfoHelper.GetName<AppFormLayoutItemExDto,  AppFormLayoutItemExDto>(o=>o.ForeignAppFormLayoutItemExDto);
            public static readonly string ForeignAppSearchViewFieldProperty = ObjectInfoHelper.GetName<AppFormLayoutItemExDto,  AppSearchViewFieldExDto>(o=>o.ForeignAppSearchViewFieldExDto);
            public static readonly string ForeignAppTransactionFieldProperty = ObjectInfoHelper.GetName<AppFormLayoutItemExDto,  AppTransactionFieldExDto>(o=>o.ForeignAppTransactionFieldExDto);
            public static readonly string ForeignAppTransactionUnitProperty = ObjectInfoHelper.GetName<AppFormLayoutItemExDto,  AppTransactionUnitExDto>(o=>o.ForeignAppTransactionUnitExDto); 

        
        #endregion
	
	
        public AppFormLayoutItemExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppFormGridLayoutItemBindFieldList = new  ObservableSet<AppFormGridLayoutItemBindFieldExDto>();
            AppFormLayoutItem_List = new  ObservableSet<AppFormLayoutItemExDto>();
            AppTransactionField_List = new  ObservableSet<AppTransactionFieldExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppFormGridLayoutItemBindFieldEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppFormGridLayoutItemBindFieldExDto> AppFormGridLayoutItemBindFieldList
        {
            get
            {
			    return  GetValue<ObservableSet<AppFormGridLayoutItemBindFieldExDto>>(AppFormGridLayoutItemBindFieldListProperty);    
            }
            set
            {
				SetValue(AppFormGridLayoutItemBindFieldListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppFormLayoutItemEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppFormLayoutItemExDto> AppFormLayoutItem_List
        {
            get
            {
			    return  GetValue<ObservableSet<AppFormLayoutItemExDto>>(AppFormLayoutItem_ListProperty);    
            }
            set
            {
				SetValue(AppFormLayoutItem_ListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppTransactionFieldEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppTransactionFieldExDto> AppTransactionField_List
        {
            get
            {
			    return  GetValue<ObservableSet<AppTransactionFieldExDto>>(AppTransactionField_ListProperty);    
            }
            set
            {
				SetValue(AppTransactionField_ListProperty,value);
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

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppFormLayoutItemEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppFormLayoutItemExDto ForeignAppFormLayoutItemExDto
        {
            get
            {
			    return  GetValue<AppFormLayoutItemExDto>(ForeignAppFormLayoutItemProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppFormLayoutItemProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppSearchViewFieldEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppSearchViewFieldExDto ForeignAppSearchViewFieldExDto
        {
            get
            {
			    return  GetValue<AppSearchViewFieldExDto>(ForeignAppSearchViewFieldProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppSearchViewFieldProperty,value);
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

