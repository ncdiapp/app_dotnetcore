 using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using APP.Framework;
using APP.Framework.Collections;
using System.Runtime.Serialization;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{    
    public partial class AppFormLayoutItemExDto : AppFormLayoutItemDto 
    {
        //[DataMember(EmitDefaultValue = false)]
        //public AppTransactionUnitExDto ForeignAppTransactionUnitExDto
        //{
        //    get;
        //    set;
        //}

        public const string canvasFormControlUnit = "canvasFormControlUnit";
        public const string gridFormControlUnit = "gridFormControlUnit ";


        public bool IsChildTranscationUnitGrid
        {
            get
            {
                return this.GridTransactionUnitId.HasValue;
            }

        }
        public string RuningTimeUnitLayoutPostionStyle
        {
            get;
            set;
        }

	//	[IgnoreDataMember]
		public  EmAppGrandChildEditMode  GrandChildEditMode 
		{
			get;
			set;
		}

		[IgnoreDataMember]
        public AppTransactionExDto PrintAppTransactionExDto
        {
            get;
            set;
        }

        public AppProjectWorkFlowActionDto BindToCommandAction
        {
            get;
            set;
        }

        public AppTransactionUnitLinkedSearchDto BindToLinkedSearch
        {
            get;
            set;
        }

        public bool IsShowGridAsListItems
        {
            get;
            set;
        }

        public bool IsShowDDLAsItemList
        {
            get;
            set;
        }

        public EmAppWebPageUiControlDisplayType? EmAppWebPageUiControlDisplayType
        {
            get;
            set;
        }

        [IgnoreDataMember]
        public List<int> NeedToAddTransactionFieldIdList
        {
            get;
            set;
        }

        [IgnoreDataMember]
        public List<int> NeedToAddGrandChildTransFieldIdList
        {
            get;
            set;
        }


       

        [IgnoreDataMember]
        public AppChildDataDto GridUnitRowDataDto { get; set; }

       

        [IgnoreDataMember]
        public object GridUnitCellValue { get; set; }
    }
}

