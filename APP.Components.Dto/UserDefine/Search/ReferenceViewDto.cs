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
    [DataContract(Namespace = ContractNamespaces.Dto)]
    [KnownType(typeof(ReferenceViewDefinitionDto))]
    public class ReferenceViewDto : ReferenceViewDefinitionDto
    {
        // key:EntityId  only for massupdate view
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, List<LookupItemDto>> DictEntityLookupItemDto
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public IList<ReferenceViewColumnDto> Columns
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public ObservableCollection<ReferenceViewColumnDto> PivotRows
        {
            get { return GetValue(() => PivotRows); }
            set { SetValue(() => PivotRows, value); }
        }

        [DataMember(EmitDefaultValue = false)]
        public ObservableCollection<ReferenceViewColumnDto> PivotColumns
        {
            get { return GetValue(() => PivotColumns); }
            set { SetValue(() => PivotColumns, value); }
        }

        [DataMember(EmitDefaultValue = false)]
        public ObservableCollection<ReferenceViewColumnDto> PivotAggregationFields
        {
            get { return GetValue(() => PivotAggregationFields); }
            set { SetValue(() => PivotAggregationFields, value); }
        }

        [DataMember(EmitDefaultValue = false)]
        public int? PivotDefaultAggregation
        {
            get;
            set;
        }


        //[IgnoreDataMember]
        //public IEnumerable<SearchResultRowDto> CurrentMassUpdateSearchResultRowDtoList
        //{
        //    get;
        //    set;
        //}

        //[DataMember(EmitDefaultValue = false)]
        //public System.Byte[] Uilayout
        //{
        //    get;
        //    set;
        //}



        [DataMember(EmitDefaultValue = false)]
        public List<AppFormLinkTargetDto> AppFormLinkTargetList
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<AppFormLinkTargetDto> FormGroupLinkTargetList
        {
            get
            {
                if (AppFormLinkTargetList != null)
                {
                    return AppFormLinkTargetList.Where(o =>  o.ActionType.HasValue && (
                        o.ActionType.Value == (int)EmAppLinkTargetActionType.Edit
                        || o.ActionType.Value == (int)EmAppLinkTargetActionType.EditOnPopup
                        || o.ActionType.Value == (int)EmAppLinkTargetActionType.Preview
                    )).OrderBy(o=>o.Sort).ToList();
                }

                return null;
            }
        }



        [DataMember(EmitDefaultValue = false)]
        public List<AppViewLinkedSeaechOrUrlDto> AppViewLinkedSeaechOrUrlDtoList
        {
            get;
            set;
        }



        // only for client
        // only apply for Grid delete "MainReferenceId|RowID"
        // need to change to "MainReferenceId|GuID"
        private List<KeyValuePair<int, Guid>> _DeletedReferenceIdRowIdList;

        [IgnoreDataMember]
        public List<KeyValuePair<int, Guid>> DeletedReferenceIdRowIdList
        {
            get
            {
                if (_DeletedReferenceIdRowIdList == null)
                {
                    _DeletedReferenceIdRowIdList = new List<KeyValuePair<int, Guid>>();
                }

                return _DeletedReferenceIdRowIdList;
            }
        }

        [IgnoreDataMember]
        public object ContainerControl
        {
            get;
            set;
        }

        [IgnoreDataMember]
        public bool IsForceMultipleSelectedRows
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<string> GroupByFieldList
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? TransRootIdColumnId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? FolderIdColumnId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? FileCodeColumnId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<LookupItemDto> SchedulerViewGroupByResources
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? Options
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? CollapseGroupsToLevel
        {
            get;
            set;
        }


        [IgnoreDataMember]
        public int? EsiteUiDisplayType
        {
            get;
            set;
        }

        [IgnoreDataMember]
        public string EsiteSliderInitFunctionParameters
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<ReferenceViewDto> ChildReferenceViewDtoList
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? SearchId
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public AppSearchViewOtherSettingsDto OtherSettingsDto
        {
            get;
            set;
        }
    }
}
