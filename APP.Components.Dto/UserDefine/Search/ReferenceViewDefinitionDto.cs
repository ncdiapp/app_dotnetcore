using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework;
using APP.Framework.Collections;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class ReferenceViewDefinitionDto : EditableObject
    {
        [DataMember(EmitDefaultValue = false)]
        public string Description
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string Display
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string UiId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsClusterChildView
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int ViewType
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? ChartType
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsMassUpdate
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsAllowAddRow
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsAllowDeleteRow
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsAllowAdvancedUpdate
        {
            get;
            set;
        }
        

        [DataMember(EmitDefaultValue = false)]
        public int? MassUpdateViewType
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? MassUpdateTransactionId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? BlqueryId
        {
            get;
            set;
        }


        //[DataMember(EmitDefaultValue = false)]
        //public bool IsNeedToUseSearchReferenceIDAsFilter
        //{
        //    get;
        //    set;
        //}

        [DataMember(EmitDefaultValue = false)]
        public int? ReferenceIDViewFilterColumnId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int RefTxtype
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Nullable<System.Int32> ItemWidth
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Nullable<System.Int32> ItemHight
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsCallinFromRangplanning
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? FreezeColumnCount
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? WhereUsedDefaultViewId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string PivotOrChartSetting
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public string AppRestResourceUri
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public string AppRestResourceUriDisplay
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public int? CanlendarDefaultViewMode
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsEnableCalendarMonthView
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsEnableCalendarWeekView
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsEnableCalendarDayView
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsEnableCalendarNavigator
        {
            get;
            set;
        }



        [DataMember(EmitDefaultValue = false)]
        public int? GridOutputMode
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public EmAppWebPageSearchViewDisplayType? EmAppWebPageSearchViewDisplayType
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<KeyValuePair<int, int?>> NeedToAddViewFieldIdAndTypeList
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public bool NeedToAddPlaceHolderColumnAtLast
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? HierachyParentViewId
        {
            get;
            set;
        }
    }
}