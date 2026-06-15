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
    /// DTO class for the  Extend Relation Entity 'AppCompanyOrderModule'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppCompanyOrderModuleExDto : AppCompanyOrderModuleDto 
    {

     	#region Static Name Properties Declaration
    
 
            public static readonly string ForeignAppCompanyProperty = ObjectInfoHelper.GetName<AppCompanyOrderModuleExDto,  AppCompanyExDto>(o=>o.ForeignAppCompanyExDto);
            public static readonly string ForeignAppModuleLibRegisterProperty = ObjectInfoHelper.GetName<AppCompanyOrderModuleExDto,  AppModuleLibRegisterExDto>(o=>o.ForeignAppModuleLibRegisterExDto); 

        
        #endregion
	
	
        public AppCompanyOrderModuleExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties




		
		

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

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppModuleLibRegisterEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppModuleLibRegisterExDto ForeignAppModuleLibRegisterExDto
        {
            get
            {
			    return  GetValue<AppModuleLibRegisterExDto>(ForeignAppModuleLibRegisterProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppModuleLibRegisterProperty,value);
            }
        }	



        #endregion
        
    }
}

