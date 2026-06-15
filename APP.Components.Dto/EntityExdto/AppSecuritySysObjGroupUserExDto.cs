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
    /// DTO class for the  Extend Relation Entity 'AppSecuritySysObjGroupUser'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppSecuritySysObjGroupUserExDto : AppSecuritySysObjGroupUserDto 
    {

     	#region Static Name Properties Declaration
    
 
            public static readonly string ForeignAppComOrganizationProperty = ObjectInfoHelper.GetName<AppSecuritySysObjGroupUserExDto,  AppComOrganizationExDto>(o=>o.ForeignAppComOrganizationExDto);
            public static readonly string ForeignAppDesktopProperty = ObjectInfoHelper.GetName<AppSecuritySysObjGroupUserExDto,  AppDesktopExDto>(o=>o.ForeignAppDesktopExDto);
            public static readonly string ForeignAppFormLinkTargetProperty = ObjectInfoHelper.GetName<AppSecuritySysObjGroupUserExDto,  AppFormLinkTargetExDto>(o=>o.ForeignAppFormLinkTargetExDto);
            public static readonly string ForeignAppReportProperty = ObjectInfoHelper.GetName<AppSecuritySysObjGroupUserExDto,  AppReportExDto>(o=>o.ForeignAppReportExDto);
            public static readonly string ForeignAppRouteStateProperty = ObjectInfoHelper.GetName<AppSecuritySysObjGroupUserExDto,  AppRouteStateExDto>(o=>o.ForeignAppRouteStateExDto);
            public static readonly string ForeignAppSearchProperty = ObjectInfoHelper.GetName<AppSecuritySysObjGroupUserExDto,  AppSearchExDto>(o=>o.ForeignAppSearchExDto);
            public static readonly string ForeignAppSearchViewProperty = ObjectInfoHelper.GetName<AppSecuritySysObjGroupUserExDto,  AppSearchViewExDto>(o=>o.ForeignAppSearchViewExDto);
            public static readonly string ForeignAppSecurityGroupProperty = ObjectInfoHelper.GetName<AppSecuritySysObjGroupUserExDto,  AppSecurityGroupExDto>(o=>o.ForeignAppSecurityGroupExDto);
            public static readonly string ForeignAppSecurityUserProperty = ObjectInfoHelper.GetName<AppSecuritySysObjGroupUserExDto,  AppSecurityUserExDto>(o=>o.ForeignAppSecurityUserExDto);
            public static readonly string ForeignAppTransaction_Property = ObjectInfoHelper.GetName<AppSecuritySysObjGroupUserExDto,  AppTransactionExDto>(o=>o.ForeignAppTransaction_ExDto);
            public static readonly string ForeignAppTransactionProperty = ObjectInfoHelper.GetName<AppSecuritySysObjGroupUserExDto,  AppTransactionExDto>(o=>o.ForeignAppTransactionExDto);
            public static readonly string ForeignAppTransactionFieldProperty = ObjectInfoHelper.GetName<AppSecuritySysObjGroupUserExDto,  AppTransactionFieldExDto>(o=>o.ForeignAppTransactionFieldExDto);
            public static readonly string ForeignAppTransactionUnit_Property = ObjectInfoHelper.GetName<AppSecuritySysObjGroupUserExDto,  AppTransactionUnitExDto>(o=>o.ForeignAppTransactionUnit_ExDto);
            public static readonly string ForeignAppTransactionUnitProperty = ObjectInfoHelper.GetName<AppSecuritySysObjGroupUserExDto,  AppTransactionUnitExDto>(o=>o.ForeignAppTransactionUnitExDto);
            public static readonly string ForeignAppTransactionUnitLinkedSearchProperty = ObjectInfoHelper.GetName<AppSecuritySysObjGroupUserExDto,  AppTransactionUnitLinkedSearchExDto>(o=>o.ForeignAppTransactionUnitLinkedSearchExDto); 

        
        #endregion
	
	
        public AppSecuritySysObjGroupUserExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties




		
		

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppComOrganizationEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppComOrganizationExDto ForeignAppComOrganizationExDto
        {
            get
            {
			    return  GetValue<AppComOrganizationExDto>(ForeignAppComOrganizationProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppComOrganizationProperty,value);
            }
        }

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

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppFormLinkTargetEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppFormLinkTargetExDto ForeignAppFormLinkTargetExDto
        {
            get
            {
			    return  GetValue<AppFormLinkTargetExDto>(ForeignAppFormLinkTargetProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppFormLinkTargetProperty,value);
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

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppRouteStateEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppRouteStateExDto ForeignAppRouteStateExDto
        {
            get
            {
			    return  GetValue<AppRouteStateExDto>(ForeignAppRouteStateProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppRouteStateProperty,value);
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

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppSecurityGroupEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppSecurityGroupExDto ForeignAppSecurityGroupExDto
        {
            get
            {
			    return  GetValue<AppSecurityGroupExDto>(ForeignAppSecurityGroupProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppSecurityGroupProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppSecurityUserEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppSecurityUserExDto ForeignAppSecurityUserExDto
        {
            get
            {
			    return  GetValue<AppSecurityUserExDto>(ForeignAppSecurityUserProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppSecurityUserProperty,value);
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
        public  AppTransactionUnitExDto ForeignAppTransactionUnit_ExDto
        {
            get
            {
			    return  GetValue<AppTransactionUnitExDto>(ForeignAppTransactionUnit_Property ) ;    
            }
            set
            {
				SetValue(ForeignAppTransactionUnit_Property,value);
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

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppTransactionUnitLinkedSearchEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppTransactionUnitLinkedSearchExDto ForeignAppTransactionUnitLinkedSearchExDto
        {
            get
            {
			    return  GetValue<AppTransactionUnitLinkedSearchExDto>(ForeignAppTransactionUnitLinkedSearchProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppTransactionUnitLinkedSearchProperty,value);
            }
        }	



        #endregion
        
    }
}

