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
    /// DTO class for the  Extend Relation Entity 'AppUserSkillList'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppUserSkillListExDto : AppUserSkillListDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppUserSkillListProperty = ObjectInfoHelper.GetName<AppUserSkillListExDto,  ObservableSet<AppUserSkillExDto>>(o=>o.AppUserSkillList); 
 

        
        #endregion
	
	
        public AppUserSkillListExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppUserSkillList = new  ObservableSet<AppUserSkillExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppUserSkillEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppUserSkillExDto> AppUserSkillList
        {
            get
            {
			    return  GetValue<ObservableSet<AppUserSkillExDto>>(AppUserSkillListProperty);    
            }
            set
            {
				SetValue(AppUserSkillListProperty,value);
            }
        }

		
		
	



        #endregion
        
    }
}

