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
    /// DTO class for the  Extend Relation Entity 'AppDataSourceRegister'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppDataSourceRegisterExDto : AppDataSourceRegisterDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppDataSetListProperty = ObjectInfoHelper.GetName<AppDataSourceRegisterExDto,  ObservableSet<AppDataSetExDto>>(o=>o.AppDataSetList);
            public static readonly string AppEntityInfoListProperty = ObjectInfoHelper.GetName<AppDataSourceRegisterExDto,  ObservableSet<AppEntityInfoExDto>>(o=>o.AppEntityInfoList);
            public static readonly string AppReportListProperty = ObjectInfoHelper.GetName<AppDataSourceRegisterExDto,  ObservableSet<AppReportExDto>>(o=>o.AppReportList);
            public static readonly string AppTransactionListProperty = ObjectInfoHelper.GetName<AppDataSourceRegisterExDto,  ObservableSet<AppTransactionExDto>>(o=>o.AppTransactionList); 
            public static readonly string ForeignAppCompanyProperty = ObjectInfoHelper.GetName<AppDataSourceRegisterExDto,  AppCompanyExDto>(o=>o.ForeignAppCompanyExDto); 

        
        #endregion
	
	
        public AppDataSourceRegisterExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppDataSetList = new  ObservableSet<AppDataSetExDto>();
            AppEntityInfoList = new  ObservableSet<AppEntityInfoExDto>();
            AppReportList = new  ObservableSet<AppReportExDto>();
            AppTransactionList = new  ObservableSet<AppTransactionExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppDataSetEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppDataSetExDto> AppDataSetList
        {
            get
            {
			    return  GetValue<ObservableSet<AppDataSetExDto>>(AppDataSetListProperty);    
            }
            set
            {
				SetValue(AppDataSetListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppEntityInfoEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppEntityInfoExDto> AppEntityInfoList
        {
            get
            {
			    return  GetValue<ObservableSet<AppEntityInfoExDto>>(AppEntityInfoListProperty);    
            }
            set
            {
				SetValue(AppEntityInfoListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppReportEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppReportExDto> AppReportList
        {
            get
            {
			    return  GetValue<ObservableSet<AppReportExDto>>(AppReportListProperty);    
            }
            set
            {
				SetValue(AppReportListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppTransactionEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppTransactionExDto> AppTransactionList
        {
            get
            {
			    return  GetValue<ObservableSet<AppTransactionExDto>>(AppTransactionListProperty);    
            }
            set
            {
				SetValue(AppTransactionListProperty,value);
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

