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
    /// DTO class for the  Extend Relation Entity 'AppWebApiParamsHeaderSettig'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppWebApiParamsHeaderSettigExDto : AppWebApiParamsHeaderSettigDto 
    {

     	#region Static Name Properties Declaration
    
 
            public static readonly string ForeignAppWebApiConfigProperty = ObjectInfoHelper.GetName<AppWebApiParamsHeaderSettigExDto,  AppWebApiConfigExDto>(o=>o.ForeignAppWebApiConfigExDto); 

        
        #endregion
	
	
        public AppWebApiParamsHeaderSettigExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties




		
		

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppWebApiConfigEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppWebApiConfigExDto ForeignAppWebApiConfigExDto
        {
            get
            {
			    return  GetValue<AppWebApiConfigExDto>(ForeignAppWebApiConfigProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppWebApiConfigProperty,value);
            }
        }	



        #endregion
        
    }
}

