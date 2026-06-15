
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
    public class SearchDto : SearchDefinitionDto
    {
        //[DataMember(EmitDefaultValue = false)]
        //public bool SearchOnLoad
        //{
        //    get;
        //    set;
        //}

        [DataMember(EmitDefaultValue = false)]
        public IEnumerable<SearchCriteriaDto> Criterias
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int CriteriasRowCount
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public ReferenceViewDefinitionDto ReferenceViewDefinitionDto
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public ReferenceViewDto DefaultView
        {
            get { return GetValue(() => DefaultView); }
            set { SetValue(() => DefaultView, value); }
        }

        [DataMember(EmitDefaultValue = false)]
        public ReferenceViewDto DefaultMassUpdateView
        {
            get { return GetValue(() => DefaultMassUpdateView); }
            set { SetValue(() => DefaultMassUpdateView, value); }
        }

        [DataMember(EmitDefaultValue = false)]
        public int? SearchTemplateSavedID
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public bool IsAutoExcute
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? WhereUsedSearchId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public SearchDto EmbeddedChildSearchDto
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public bool IsDefault
        {
            get;
            set;
        }

        // to simple search result 

        [DataMember(EmitDefaultValue = false)]
        public List<int> SelectReferenceIds
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string QuickSearchValueText
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public int? TopNbResult
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, string> DictFolderIdFolderDisplay
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? CurrentUserId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public AppMasterDetailDto LinkedSeachFormData
        {
            get;
            set;
        }



        [DataMember]
        public List<int> IsChangedNeedTTriggerExecutionSearchCriteriaIds
        {
            get;
            set;
        }

        // for Html5 Search


        [DataMember]
        public List<int> IsChangedNeedToCascadingSearchCriteriaIds
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, List<LookupItemDto>> DictCascadingChildFieldIdAndDataSource
        {
            get;
            set;
        }


        [DataMember]
        public int? CurrentCascadingTriggerSearchCriteriaId
        {
            get;
            set;
        }

        [DataMember]
        public bool? IsHideAllToolsBar
        {
            get;
            set;
        }

        [DataMember]
        public bool IsShowSearchTitleLabel
        {
            get;
            set;
        }


        
        public DateTime? BaseDate
        {
            get;
            set;
        }

        [DataMember]
        public string CurrentMapPositionText
        {
            get;
            set;
        }


        [DataMember]
        public double? CurrentMapPositionLat
        {
            get;
            set;
        }

        [DataMember]
        public double? CurrentMapPositionLng
        {
            get;
            set;
        }

        [DataMember]
        public int? CurrentEsiteId
        {
            get;
            set;
        }

        [DataMember]
        public int? EsiteMenuCategoryId
        {
            get;
            set;
        }
        

        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, List<LookupItemDto>> DictFilterOptionLevelAndLookupList
        {
            get;
            set;
        }

        [DataMember]
        public int? DefaultAppProvideApiId
        {
            get;
            set;
        }



        [DataMember]
        public int? TopDataLimit
        {
            get;
            set;
        }

        
        public bool? IsForPublicAcesss
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public int? BatchExecuteLinkTargetId
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public List<LookupItemDto> LinkToTransactions
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public List<LookupItemDto> LinkToCommands
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public bool IsWorkflowLogSearch
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? WorkflowTransactionId
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public string WorkflowTransactionRId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string WorkflowLogBatchNumber
        {
            get;
            set;
        }
    }
}