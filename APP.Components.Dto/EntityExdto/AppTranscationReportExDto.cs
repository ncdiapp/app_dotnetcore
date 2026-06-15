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
    /// DTO class for the  Extend Relation Entity 'AppTranscationReport'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppTranscationReportExDto : AppTranscationReportDto 
    {

     	#region Static Name Properties Declaration
    
 
            public static readonly string ForeignAppReportProperty = ObjectInfoHelper.GetName<AppTranscationReportExDto,  AppReportExDto>(o=>o.ForeignAppReportExDto);
            public static readonly string ForeignAppTransactionProperty = ObjectInfoHelper.GetName<AppTranscationReportExDto,  AppTransactionExDto>(o=>o.ForeignAppTransactionExDto); 

        
        #endregion
	
	
        public AppTranscationReportExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties




		
		

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppReportEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppReportExDto ForeignAppReportExDto
        {
            get
            {
			    return  GetValue<AppReportExDto>(ForeignAppReportProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppReportProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppTransactionEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppTransactionExDto ForeignAppTransactionExDto
        {
            get
            {
			    return  GetValue<AppTransactionExDto>(ForeignAppTransactionProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppTransactionProperty,value);
            }
        }	



        #endregion
        
    }
}

