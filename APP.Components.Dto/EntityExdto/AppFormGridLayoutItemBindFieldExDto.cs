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
    /// DTO class for the  Extend Relation Entity 'AppFormGridLayoutItemBindField'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppFormGridLayoutItemBindFieldExDto : AppFormGridLayoutItemBindFieldDto 
    {

     	#region Static Name Properties Declaration
    
 
            public static readonly string ForeignAppFormLayoutItemProperty = ObjectInfoHelper.GetName<AppFormGridLayoutItemBindFieldExDto,  AppFormLayoutItemExDto>(o=>o.ForeignAppFormLayoutItemExDto);
            public static readonly string ForeignAppTransactionFieldProperty = ObjectInfoHelper.GetName<AppFormGridLayoutItemBindFieldExDto,  AppTransactionFieldExDto>(o=>o.ForeignAppTransactionFieldExDto);
            public static readonly string ForeignAppTransactionUnit_Property = ObjectInfoHelper.GetName<AppFormGridLayoutItemBindFieldExDto,  AppTransactionUnitExDto>(o=>o.ForeignAppTransactionUnit_ExDto);
            public static readonly string ForeignAppTransactionUnitProperty = ObjectInfoHelper.GetName<AppFormGridLayoutItemBindFieldExDto,  AppTransactionUnitExDto>(o=>o.ForeignAppTransactionUnitExDto); 

        
        #endregion
	
	
        public AppFormGridLayoutItemBindFieldExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties




		
		

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppFormLayoutItemEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppFormLayoutItemExDto ForeignAppFormLayoutItemExDto
        {
            get
            {
			    return  GetValue<AppFormLayoutItemExDto>(ForeignAppFormLayoutItemProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppFormLayoutItemProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppTransactionFieldEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppTransactionFieldExDto ForeignAppTransactionFieldExDto
        {
            get
            {
			    return  GetValue<AppTransactionFieldExDto>(ForeignAppTransactionFieldProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppTransactionFieldProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppTransactionUnitEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppTransactionUnitExDto ForeignAppTransactionUnit_ExDto
        {
            get
            {
			    return  GetValue<AppTransactionUnitExDto>(ForeignAppTransactionUnit_Property ) ;    
            }
            set
            {
				SetValue(ForeignAppTransactionUnit_Property,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppTransactionUnitEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppTransactionUnitExDto ForeignAppTransactionUnitExDto
        {
            get
            {
			    return  GetValue<AppTransactionUnitExDto>(ForeignAppTransactionUnitProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppTransactionUnitProperty,value);
            }
        }	



        #endregion
        
    }
}

