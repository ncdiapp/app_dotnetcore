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
    /// DTO class for the  Extend Relation Entity 'AppEntityInfo'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppEntityInfoExDto : AppEntityInfoDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppEntityEnumValueListProperty = ObjectInfoHelper.GetName<AppEntityInfoExDto,  ObservableSet<AppEntityEnumValueExDto>>(o=>o.AppEntityEnumValueList);
            public static readonly string AppEntitySimpleListValueListProperty = ObjectInfoHelper.GetName<AppEntityInfoExDto,  ObservableSet<AppEntitySimpleListValueExDto>>(o=>o.AppEntitySimpleListValueList);
            public static readonly string AppSearchFieldListProperty = ObjectInfoHelper.GetName<AppEntityInfoExDto,  ObservableSet<AppSearchFieldExDto>>(o=>o.AppSearchFieldList);
            public static readonly string AppSearchViewFieldListProperty = ObjectInfoHelper.GetName<AppEntityInfoExDto,  ObservableSet<AppSearchViewFieldExDto>>(o=>o.AppSearchViewFieldList);
            public static readonly string AppTransactionListProperty = ObjectInfoHelper.GetName<AppEntityInfoExDto,  ObservableSet<AppTransactionExDto>>(o=>o.AppTransactionList);
            public static readonly string AppTransactionFieldListProperty = ObjectInfoHelper.GetName<AppEntityInfoExDto,  ObservableSet<AppTransactionFieldExDto>>(o=>o.AppTransactionFieldList); 
            public static readonly string ForeignAppDataSourceRegisterProperty = ObjectInfoHelper.GetName<AppEntityInfoExDto,  AppDataSourceRegisterExDto>(o=>o.ForeignAppDataSourceRegisterExDto);
            public static readonly string ForeignAppListMenuProperty = ObjectInfoHelper.GetName<AppEntityInfoExDto,  AppListMenuExDto>(o=>o.ForeignAppListMenuExDto); 

        
        #endregion
	
	
        public AppEntityInfoExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppEntityEnumValueList = new  ObservableSet<AppEntityEnumValueExDto>();
            AppEntitySimpleListValueList = new  ObservableSet<AppEntitySimpleListValueExDto>();
            AppSearchFieldList = new  ObservableSet<AppSearchFieldExDto>();
            AppSearchViewFieldList = new  ObservableSet<AppSearchViewFieldExDto>();
            AppTransactionList = new  ObservableSet<AppTransactionExDto>();
            AppTransactionFieldList = new  ObservableSet<AppTransactionFieldExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppEntityEnumValueEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppEntityEnumValueExDto> AppEntityEnumValueList
        {
            get
            {
			    return  GetValue<ObservableSet<AppEntityEnumValueExDto>>(AppEntityEnumValueListProperty);    
            }
            set
            {
				SetValue(AppEntityEnumValueListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppEntitySimpleListValueEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppEntitySimpleListValueExDto> AppEntitySimpleListValueList
        {
            get
            {
			    return  GetValue<ObservableSet<AppEntitySimpleListValueExDto>>(AppEntitySimpleListValueListProperty);    
            }
            set
            {
				SetValue(AppEntitySimpleListValueListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppSearchFieldEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppSearchFieldExDto> AppSearchFieldList
        {
            get
            {
			    return  GetValue<ObservableSet<AppSearchFieldExDto>>(AppSearchFieldListProperty);    
            }
            set
            {
				SetValue(AppSearchFieldListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppSearchViewFieldEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppSearchViewFieldExDto> AppSearchViewFieldList
        {
            get
            {
			    return  GetValue<ObservableSet<AppSearchViewFieldExDto>>(AppSearchViewFieldListProperty);    
            }
            set
            {
				SetValue(AppSearchViewFieldListProperty,value);
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

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppTransactionFieldEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppTransactionFieldExDto> AppTransactionFieldList
        {
            get
            {
			    return  GetValue<ObservableSet<AppTransactionFieldExDto>>(AppTransactionFieldListProperty);    
            }
            set
            {
				SetValue(AppTransactionFieldListProperty,value);
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

