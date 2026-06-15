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
    /// DTO class for the  Extend Relation Entity 'AppBusinessPartnerInviteUser'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppBusinessPartnerInviteUserExDto : AppBusinessPartnerInviteUserDto 
    {

     	#region Static Name Properties Declaration
    
 
            public static readonly string ForeignAppBusinessPartnerProperty = ObjectInfoHelper.GetName<AppBusinessPartnerInviteUserExDto,  AppBusinessPartnerExDto>(o=>o.ForeignAppBusinessPartnerExDto);
            public static readonly string ForeignAppCompanyProperty = ObjectInfoHelper.GetName<AppBusinessPartnerInviteUserExDto,  AppCompanyExDto>(o=>o.ForeignAppCompanyExDto);
            public static readonly string ForeignAppSecurityUserProperty = ObjectInfoHelper.GetName<AppBusinessPartnerInviteUserExDto,  AppSecurityUserExDto>(o=>o.ForeignAppSecurityUserExDto); 

        
        #endregion
	
	
        public AppBusinessPartnerInviteUserExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties




		
		

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppBusinessPartnerEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppBusinessPartnerExDto ForeignAppBusinessPartnerExDto
        {
            get
            {
			    return  GetValue<AppBusinessPartnerExDto>(ForeignAppBusinessPartnerProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppBusinessPartnerProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppCompanyEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppCompanyExDto ForeignAppCompanyExDto
        {
            get
            {
			    return  GetValue<AppCompanyExDto>(ForeignAppCompanyProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppCompanyProperty,value);
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



        #endregion
        
    }
}

