using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Framework;
using APP.Framework.Validation;
using APP.Components.Dto;
using System.Collections.ObjectModel;

namespace APP.Components.EntityDto
{
    public partial class AppDesktopItemDto
    {
        [DataMember(EmitDefaultValue = false)]
        public AppListMenuDto LinkToListMenu
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsDesktopItemHidden
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string CurrentHostId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string ParentHostId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? TotalColumns
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? RowHeight
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? MinWidth
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? ViewType
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
        public string AynalysisViewFieldX
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public string AynalysisViewFieldY
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string AynalysisViewFieldNodeId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string AynalysisViewFieldParentNodeId
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public List<AppSearchViewFieldExDto> AppSearchViewFieldList
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public List<ReferenceViewColumnDto> ClusterViewItemColumns
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<AppViewLinkedSeaechOrUrlDto> AppViewLinkedSeaechOrUrlDtoList
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<AppFormLinkTargetDto> AppFormLinkTargetList
        {
            get;
            set;
        }

        //[DataMember(EmitDefaultValue = false)]
        //public List<ReferenceViewColumnDto> PivotRows
        //{
        //    get
        //    {
        //        if (ClusterViewItemColumns != null)
        //        {
        //            return ClusterViewItemColumns.Where(o => o.IsPivotRowField.HasValue && o.IsPivotRowField.Value).OrderBy(o => o.Sort).ToList();
        //        }

        //        return new List<ReferenceViewColumnDto>();
        //    }
        //}

        //[DataMember(EmitDefaultValue = false)]
        //public List<ReferenceViewColumnDto> PivotColumns
        //{
        //    get
        //    {
        //        if (ClusterViewItemColumns != null)
        //        {
        //            return ClusterViewItemColumns.Where(o => o.IsPivotColumnField.HasValue && o.IsPivotColumnField.Value).OrderBy(o => o.Sort).ToList();
        //        }

        //        return new List<ReferenceViewColumnDto>();
        //    }
        //}

        //[DataMember(EmitDefaultValue = false)]
        //public List<ReferenceViewColumnDto> PivotAggregationFields
        //{
        //    get
        //    {
        //        if (ClusterViewItemColumns != null)
        //        {
        //            return ClusterViewItemColumns.Where(o => o.IsPivotAggregationField.HasValue && o.IsPivotAggregationField.Value).OrderBy(o => o.Sort).ToList();
        //        }

        //        return new List<ReferenceViewColumnDto>();
        //    }
        //}

        //[DataMember(EmitDefaultValue = false)]
        //public int? PivotDefaultAggregation
        //{
        //    get;
        //    set;
        //}


        [DataMember(EmitDefaultValue = false)]
        public string UiId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? MasterClusterViewId
        {
            get;
            set;
        }



    }
}

