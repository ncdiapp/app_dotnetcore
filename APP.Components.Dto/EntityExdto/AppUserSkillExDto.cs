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
    /// DTO class for the  Extend Relation Entity 'AppUserSkill'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppUserSkillExDto : AppUserSkillDto 
    {

     	#region Static Name Properties Declaration
    
 
            public static readonly string ForeignAppSecurityUserProperty = ObjectInfoHelper.GetName<AppUserSkillExDto,  AppSecurityUserExDto>(o=>o.ForeignAppSecurityUserExDto);
            public static readonly string ForeignAppUserSkillListProperty = ObjectInfoHelper.GetName<AppUserSkillExDto,  AppUserSkillListExDto>(o=>o.ForeignAppUserSkillListExDto); 

        
        #endregion
	
	
        public AppUserSkillExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties




		
		

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

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppUserSkillListEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppUserSkillListExDto ForeignAppUserSkillListExDto
        {
            get
            {
			    return  GetValue<AppUserSkillListExDto>(ForeignAppUserSkillListProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppUserSkillListProperty,value);
            }
        }	



        #endregion
        
    }
}

