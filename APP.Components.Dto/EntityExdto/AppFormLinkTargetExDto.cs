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
    /// DTO class for the  Extend Relation Entity 'AppFormLinkTarget'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppFormLinkTargetExDto : AppFormLinkTargetDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppSecuritySysObjGroupUserListProperty = ObjectInfoHelper.GetName<AppFormLinkTargetExDto,  ObservableSet<AppSecuritySysObjGroupUserExDto>>(o=>o.AppSecuritySysObjGroupUserList); 
            public static readonly string ForeignAppSearchProperty = ObjectInfoHelper.GetName<AppFormLinkTargetExDto,  AppSearchExDto>(o=>o.ForeignAppSearchExDto);
            public static readonly string ForeignAppSearchField__Property = ObjectInfoHelper.GetName<AppFormLinkTargetExDto,  AppSearchFieldExDto>(o=>o.ForeignAppSearchField__ExDto);
            public static readonly string ForeignAppSearchField_Property = ObjectInfoHelper.GetName<AppFormLinkTargetExDto,  AppSearchFieldExDto>(o=>o.ForeignAppSearchField_ExDto);
            public static readonly string ForeignAppSearchFieldProperty = ObjectInfoHelper.GetName<AppFormLinkTargetExDto,  AppSearchFieldExDto>(o=>o.ForeignAppSearchFieldExDto);
            public static readonly string ForeignAppSearchViewProperty = ObjectInfoHelper.GetName<AppFormLinkTargetExDto,  AppSearchViewExDto>(o=>o.ForeignAppSearchViewExDto);
            public static readonly string ForeignAppSearchViewField___Property = ObjectInfoHelper.GetName<AppFormLinkTargetExDto,  AppSearchViewFieldExDto>(o=>o.ForeignAppSearchViewField___ExDto);
            public static readonly string ForeignAppSearchViewField____Property = ObjectInfoHelper.GetName<AppFormLinkTargetExDto,  AppSearchViewFieldExDto>(o=>o.ForeignAppSearchViewField____ExDto);
            public static readonly string ForeignAppSearchViewField__Property = ObjectInfoHelper.GetName<AppFormLinkTargetExDto,  AppSearchViewFieldExDto>(o=>o.ForeignAppSearchViewField__ExDto);
            public static readonly string ForeignAppSearchViewFieldProperty = ObjectInfoHelper.GetName<AppFormLinkTargetExDto,  AppSearchViewFieldExDto>(o=>o.ForeignAppSearchViewFieldExDto);
            public static readonly string ForeignAppSearchViewField_Property = ObjectInfoHelper.GetName<AppFormLinkTargetExDto,  AppSearchViewFieldExDto>(o=>o.ForeignAppSearchViewField_ExDto);
            public static readonly string ForeignAppTransactionProperty = ObjectInfoHelper.GetName<AppFormLinkTargetExDto,  AppTransactionExDto>(o=>o.ForeignAppTransactionExDto);
            public static readonly string ForeignAppTransactionDataTransferSettingProperty = ObjectInfoHelper.GetName<AppFormLinkTargetExDto,  AppTransactionDataTransferSettingExDto>(o=>o.ForeignAppTransactionDataTransferSettingExDto);
            public static readonly string ForeignAppTransactionUnitProperty = ObjectInfoHelper.GetName<AppFormLinkTargetExDto,  AppTransactionUnitExDto>(o=>o.ForeignAppTransactionUnitExDto); 

        
        #endregion
	
	
        public AppFormLinkTargetExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppSecuritySysObjGroupUserList = new  ObservableSet<AppSecuritySysObjGroupUserExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



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

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppSearchFieldEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppSearchFieldExDto ForeignAppSearchField__ExDto
        {
            get
            {
			    return  GetValue<AppSearchFieldExDto>(ForeignAppSearchField__Property ) ;    
            }
            set
            {
				SetValue(ForeignAppSearchField__Property,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppSearchFieldEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppSearchFieldExDto ForeignAppSearchField_ExDto
        {
            get
            {
			    return  GetValue<AppSearchFieldExDto>(ForeignAppSearchField_Property ) ;    
            }
            set
            {
				SetValue(ForeignAppSearchField_Property,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppSearchFieldEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppSearchFieldExDto ForeignAppSearchFieldExDto
        {
            get
            {
			    return  GetValue<AppSearchFieldExDto>(ForeignAppSearchFieldProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppSearchFieldProperty,value);
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

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppSearchViewFieldEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppSearchViewFieldExDto ForeignAppSearchViewField___ExDto
        {
            get
            {
			    return  GetValue<AppSearchViewFieldExDto>(ForeignAppSearchViewField___Property ) ;    
            }
            set
            {
				SetValue(ForeignAppSearchViewField___Property,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppSearchViewFieldEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppSearchViewFieldExDto ForeignAppSearchViewField____ExDto
        {
            get
            {
			    return  GetValue<AppSearchViewFieldExDto>(ForeignAppSearchViewField____Property ) ;    
            }
            set
            {
				SetValue(ForeignAppSearchViewField____Property,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppSearchViewFieldEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppSearchViewFieldExDto ForeignAppSearchViewField__ExDto
        {
            get
            {
			    return  GetValue<AppSearchViewFieldExDto>(ForeignAppSearchViewField__Property ) ;    
            }
            set
            {
				SetValue(ForeignAppSearchViewField__Property,value);
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

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppSearchViewFieldEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppSearchViewFieldExDto ForeignAppSearchViewField_ExDto
        {
            get
            {
			    return  GetValue<AppSearchViewFieldExDto>(ForeignAppSearchViewField_Property ) ;    
            }
            set
            {
				SetValue(ForeignAppSearchViewField_Property,value);
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

