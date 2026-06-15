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
    /// DTO class for the  Extend Relation Entity 'AppProjectPortfolioBoard'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppProjectPortfolioBoardExDto : AppProjectPortfolioBoardDto 
    {

     	#region Static Name Properties Declaration
    
 
            public static readonly string ForeignAppProjectOrWorkFlowProperty = ObjectInfoHelper.GetName<AppProjectPortfolioBoardExDto,  AppProjectOrWorkFlowExDto>(o=>o.ForeignAppProjectOrWorkFlowExDto);
            public static readonly string ForeignAppProjectPortfolioProperty = ObjectInfoHelper.GetName<AppProjectPortfolioBoardExDto,  AppProjectPortfolioExDto>(o=>o.ForeignAppProjectPortfolioExDto); 

        
        #endregion
	
	
        public AppProjectPortfolioBoardExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties




		
		

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppProjectOrWorkFlowEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppProjectOrWorkFlowExDto ForeignAppProjectOrWorkFlowExDto
        {
            get
            {
			    return  GetValue<AppProjectOrWorkFlowExDto>(ForeignAppProjectOrWorkFlowProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppProjectOrWorkFlowProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppProjectPortfolioEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppProjectPortfolioExDto ForeignAppProjectPortfolioExDto
        {
            get
            {
			    return  GetValue<AppProjectPortfolioExDto>(ForeignAppProjectPortfolioProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppProjectPortfolioProperty,value);
            }
        }	



        #endregion
        
    }
}

