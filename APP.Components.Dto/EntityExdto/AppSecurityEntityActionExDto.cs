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
    /// DTO class for the  Extend Relation Entity 'AppSecurityEntityAction'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppSecurityEntityActionExDto : AppSecurityEntityActionDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppSecurityUserRolePrevilegeListProperty = ObjectInfoHelper.GetName<AppSecurityEntityActionExDto,  ObservableSet<AppSecurityUserRolePrevilegeExDto>>(o=>o.AppSecurityUserRolePrevilegeList); 
            public static readonly string ForeignAppRouteStateProperty = ObjectInfoHelper.GetName<AppSecurityEntityActionExDto,  AppRouteStateExDto>(o=>o.ForeignAppRouteStateExDto); 

        
        #endregion
	
	
        public AppSecurityEntityActionExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppSecurityUserRolePrevilegeList = new  ObservableSet<AppSecurityUserRolePrevilegeExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppSecurityUserRolePrevilegeEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppSecurityUserRolePrevilegeExDto> AppSecurityUserRolePrevilegeList
        {
            get
            {
			    return  GetValue<ObservableSet<AppSecurityUserRolePrevilegeExDto>>(AppSecurityUserRolePrevilegeListProperty);    
            }
            set
            {
				SetValue(AppSecurityUserRolePrevilegeListProperty,value);
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



        #endregion
        
    }
}

