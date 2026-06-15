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
    /// DTO class for the  Extend Relation Entity 'AppSecurityRegDomainListMenu'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppSecurityRegDomainListMenuExDto : AppSecurityRegDomainListMenuDto 
    {

     	#region Static Name Properties Declaration
    
 
            public static readonly string ForeignAppComOrganizationProperty = ObjectInfoHelper.GetName<AppSecurityRegDomainListMenuExDto,  AppComOrganizationExDto>(o=>o.ForeignAppComOrganizationExDto);
            public static readonly string ForeignAppListMenuProperty = ObjectInfoHelper.GetName<AppSecurityRegDomainListMenuExDto,  AppListMenuExDto>(o=>o.ForeignAppListMenuExDto);
            public static readonly string ForeignAppSecurityRegDomainProperty = ObjectInfoHelper.GetName<AppSecurityRegDomainListMenuExDto,  AppSecurityRegDomainExDto>(o=>o.ForeignAppSecurityRegDomainExDto); 

        
        #endregion
	
	
        public AppSecurityRegDomainListMenuExDto()
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

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppSecurityRegDomainEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppSecurityRegDomainExDto ForeignAppSecurityRegDomainExDto
        {
            get
            {
			    return  GetValue<AppSecurityRegDomainExDto>(ForeignAppSecurityRegDomainProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppSecurityRegDomainProperty,value);
            }
        }	



        #endregion
        
    }
}

