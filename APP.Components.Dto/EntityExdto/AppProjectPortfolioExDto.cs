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
    /// DTO class for the  Extend Relation Entity 'AppProjectPortfolio'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppProjectPortfolioExDto : AppProjectPortfolioDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppProjectPortfolioBoardListProperty = ObjectInfoHelper.GetName<AppProjectPortfolioExDto,  ObservableSet<AppProjectPortfolioBoardExDto>>(o=>o.AppProjectPortfolioBoardList); 
 

        
        #endregion
	
	
        public AppProjectPortfolioExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppProjectPortfolioBoardList = new  ObservableSet<AppProjectPortfolioBoardExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppProjectPortfolioBoardEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppProjectPortfolioBoardExDto> AppProjectPortfolioBoardList
        {
            get
            {
			    return  GetValue<ObservableSet<AppProjectPortfolioBoardExDto>>(AppProjectPortfolioBoardListProperty);    
            }
            set
            {
				SetValue(AppProjectPortfolioBoardListProperty,value);
            }
        }

		
		
	



        #endregion
        
    }
}

