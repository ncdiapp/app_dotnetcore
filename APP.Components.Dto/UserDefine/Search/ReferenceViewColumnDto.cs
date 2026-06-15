using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class ReferenceViewColumnDto : ObservableObject
    {
        public static readonly string IsGroupByProperty = ObjectInfoHelper.GetName<AppSearchViewFieldDto, Nullable<System.Boolean>>(o => o.IsGroupBy);
        public static readonly string GroupByLevelProperty = ObjectInfoHelper.GetName<AppSearchViewFieldDto, Nullable<System.Int32>>(o => o.GroupByLevel);
        public static readonly string AggregationFunctionTypeProperty = ObjectInfoHelper.GetName<AppSearchViewFieldDto, Nullable<System.Int32>>(o => o.AggregationFunctionType);


        [DataMember]
        public int Id
        {
            get;
            set;
        }

        [DataMember]
        public string Name
        {
            get;
            set;
        }

        [DataMember]
        public string DisplayName
        {
            get;
            set;
        }

        // only for Massupdate view
        [DataMember]
        public IEnumerable<LookupItemDto> DataSource
        {
            get;
            set;
        }

        // only for Massupdate view
        [DataMember]
        public int ControlType
        {
            get;
            set;
        }

        // only for Massupdate view
        [DataMember]
        public int? EntityId
        {
            get;
            set;
        }

        // only for Massupdate view  always is true
        [DataMember]
        public bool IsUpdatable
        {
            get;
            set;
        }


        // only for Massupdate view  always is true
        [DataMember]
        public bool IsMassUpdateReadonly
        {
            get;
            set;
        }


        //

        [DataMember(EmitDefaultValue = false)]
        public int? Nbdecimal
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public EmAppViewColumnType ColumnType
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsVisible
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public virtual int? Sort
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string DataType
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool? IsMapToChartX
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool? IsMapToChartY
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public bool? IsCalulationField
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? ChartYmappingOrder
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? TreeLevel
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public bool IsTreeNodeId
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public bool IsTreeNodeDisplay
        {
            get;
            set;
        }

        // Card View Member
        [DataMember(EmitDefaultValue = false)]
        public int LabelRowIndex
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int LabelColumnIndex
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int ValueRowIndex
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int ValueColumnIndex
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string ValueWidth
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string ValueHeight
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int ValueRow
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int ValueColSpan
        {
            get;
            set;
        }

        [DataMember]
        public int ValueRowSpan
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int LabelColSpan
        {
            get;
            set;
        }

        [DataMember]
        public int LabelRowSpan
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool LabelIsVisible
        {
            get;
            set;
        }

        //// for built-table search

        [DataMember(EmitDefaultValue = false)]
        public string SysTableFiledPath
        {
            get;
            set;
        }


        /// <summary> The IsGroupBy property of the Entity PdmReferenceViewColumn</summary>
        [DataMember(EmitDefaultValue = false)]
        public Nullable<System.Boolean> IsGroupBy
        {
            get { return GetValue<Nullable<System.Boolean>>(IsGroupByProperty); }
            set { SetValue(IsGroupByProperty, value); }
        }

        /// <summary> The GroupByLevel property of the Entity PdmReferenceViewColumn</summary>
        [DataMember(EmitDefaultValue = false)]
        public Nullable<System.Int32> GroupByLevel
        {
            get { return GetValue<Nullable<System.Int32>>(GroupByLevelProperty); }
            set { SetValue(GroupByLevelProperty, value); }
        }

        /// <summary> The AggregationFunctionType property of the Entity PdmReferenceViewColumn</summary>
        [DataMember(EmitDefaultValue = false)]
        public Nullable<System.Int32> AggregationFunctionType
        {
            get { return GetValue<Nullable<System.Int32>>(AggregationFunctionTypeProperty); }
            set { SetValue(AggregationFunctionTypeProperty, value); }
        }



        [IgnoreDataMember]
        public object UpdateToValue
        {
            get { return GetValue(() => UpdateToValue); }
            set { SetValue(() => UpdateToValue, value); }
        }

        [IgnoreDataMember]
        public object ConditionValue
        {
            get { return GetValue(() => ConditionValue); }
            set { SetValue(() => ConditionValue, value); }
        }

        [IgnoreDataMember]
        public object FindValue
        {
            get { return GetValue(() => FindValue); }
            set { SetValue(() => FindValue, value); }
        }

        [IgnoreDataMember]
        public object ReplaceValue
        {
            get { return GetValue(() => ReplaceValue); }
            set { SetValue(() => ReplaceValue, value); }
        }

        [IgnoreDataMember]
        public object ReplaceSketchCode
        {
            get { return GetValue(() => ReplaceSketchCode); }
            set { SetValue(() => ReplaceSketchCode, value); }
        }



        [DataMember(EmitDefaultValue = false)]
        public bool? IsTransRootId
        {
            get;
            set;

        }

        [DataMember(EmitDefaultValue = false)]
        public bool? IsFileFoderId
        {
            get;
            set;
        }



        [DataMember(EmitDefaultValue = false)]
        public bool? IsPivotRowField
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool? IsPivotColumnField
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool? IsPivotAggregationField
        {
            get;
            set;
        }

        //[DataMember(EmitDefaultValue = false)]
        //public int? AggregationFunctionType
        //{
        //    get;
        //    set;
        //}


        [DataMember(EmitDefaultValue = false)]
        public int? Width
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public virtual int? RowNumber
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public virtual int? ColumnNumber
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? MappingSearchFieldId
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public bool IsUserDefined1
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsUserDefined2
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsUserDefined3
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsUserDefined4
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public int? EmInternalCodeRegistration
        {
            get;
            set;
        }

        public EmAppWebPageUiControlDisplayType? EmAppWebPageUiControlDisplayType
        {
            get;
            set;
        }

        public bool IsNeedToShowCardFieldLabel
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsEshopPriceColumn
        {
            get;
            set;
        }        
    }

    // [DataContract(Namespace = ContractNamespaces.Dto)]
    //public class EntityInfoDto
    //{
    //    [DataMember]
    //    public int Value
    //    {
    //        get;
    //        set;
    //    }
    //    [DataMember]
    //    public string Display
    //    {
    //        get;
    //        set;
    //    }

    //    [DataMember]
    //    public string ImageUrl
    //    {
    //        get;
    //        set;
    //    }
    //}
}