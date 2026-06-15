using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework;
using APP.Framework.Collections;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    public partial class AppFormDomAttributeDto
    {
        public int? DefaultNbColumns { get; set; }

        public int? ColSpanValue { get; set; }

        public int? HeightValue { get; set; }

        public bool IsUnlimitedHeight { get; set; }

        public string StyleClass { get; set; }

        public string StyleString { get; set; }

        public bool IsHideLabel { get; set; }

        public string BackgroundColor { get; set; }

        public string TextColor { get; set; }

        public int? LabelWidth { get; set; }

        public string DisplayName { get; set; }

        public int? NbDecimal { get; set; }

        public string DefaultValue { get; set; }

        // Include Control Type, and LayoutType like section, add button ...
        public int? WidgetDisplayType { get; set; }

        public bool IsBindingToDataField { get; set; }

        //public bool IsDataGridHost { get; set; }

        public EmAppDataType? DataType { get; set; }

        // public EmAppFromLayoutItemBindingType? BindingType { get; set; }

        public int? TranscationUnitLevel { get; set; }

        public int? ColumnWidth { get; set; }

        public int? CommandActionId { get; set; }

        public int? LinkedSearchId { get; set; }

        public bool IsShowSearchCriterias { get; set; }

        public bool IsCollapsible { get; set; }

        public bool IsDefaultCollapsed { get; set; }

        public bool IsTab { get; set; }

        public string VisibleExpression { get; set; }

        public string InlineStyle { get; set; }

        public bool IsDisplayGridAsCardList { get; set; }

        public bool IsDisplayAsSlider { get; set; }

        public string HtmlContent { get; set; }

        public int? EntityId { get; set; }
    }
}