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
    /// DTO class for the  Extend Relation Entity 'AppBusinessPartner'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppBusinessPartnerExDto : AppBusinessPartnerDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppBusinessPartnerInviteUserListProperty = ObjectInfoHelper.GetName<AppBusinessPartnerExDto,  ObservableSet<AppBusinessPartnerInviteUserExDto>>(o=>o.AppBusinessPartnerInviteUserList); 
            public static readonly string ForeignAppCompanyProperty = ObjectInfoHelper.GetName<AppBusinessPartnerExDto,  AppCompanyExDto>(o=>o.ForeignAppCompanyExDto); 

        
        #endregion
	
	
        public AppBusinessPartnerExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppBusinessPartnerInviteUserList = new  ObservableSet<AppBusinessPartnerInviteUserExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppBusinessPartnerInviteUserEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppBusinessPartnerInviteUserExDto> AppBusinessPartnerInviteUserList
        {
            get
            {
			    return  GetValue<ObservableSet<AppBusinessPartnerInviteUserExDto>>(AppBusinessPartnerInviteUserListProperty);    
            }
            set
            {
				SetValue(AppBusinessPartnerInviteUserListProperty,value);
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



        #endregion
        
    }
}

