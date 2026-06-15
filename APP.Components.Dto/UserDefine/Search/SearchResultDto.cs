using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework;
using APP.Framework.Collections;
using APP.Components.Dto;
using System.ComponentModel;

namespace APP.Components.EntityDto
{
	[DataContract(Namespace = ContractNamespaces.Dto)]
	public partial class SearchResultDto
	{


		[DataMember]
		public IEnumerable<StaticSearchResultRowJsonDto> SearchResultRowList
		{
			get; 
			set;
		}

        [DataMember]
        public SearchResultDto ChildSearchResultDto
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, List<StaticSearchResultRowJsonDto>> DictDateIdAndResultRowList
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, List<StaticSearchResultRowJsonDto>> DictTransformedResultSet
        {
            get;
            set;
        }
        


        [DataMember]
		public int  DisplayViewType
		{
			get;
			set;
		}

		[DataMember]
		public DateTime? StartDateTime
		{
			get;
			set;
		}

		[DataMember]
		public DateTime? EndDateTime
		{
			get;
			set;
		}

        [DataMember]
        public DateTime? BaseDate
        {
            get;
            set;
        }


        [DataMember]
        public List<DateTime> WeekStartDateList
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


        /// <summary> Key: ViewColumnID: </summary>
        [DataMember(EmitDefaultValue = false)]
        public AppListDataDto MassUpdateAppListDataDto
        {
            get; set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, List<AppProjectWorkFlowActionExDto>> DcitTransactionIdAndCommandList
        {
            get; set;
        }

        [DataMember(EmitDefaultValue = false)]
        public double? PositionLat
        {
            get; set;
        }

        [DataMember(EmitDefaultValue = false)]
        public double? PositionLng
        {
            get; set;
        }

        [DataMember(EmitDefaultValue = false)]
        public AppEshopCatalogViewDto EshopCatalogViewDto
        {
            get; set;
        }

        [DataMember(EmitDefaultValue = false)]
        public StaticSearchResultRowJsonDto DefaultSelectedRowDto
        {
            get;
            set;
        }


        public int? ApiOperationId
        {
            get; set;
        }

        public string ApiResponseJsonData
        {
            get; set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? LinkTargetId
        {
            get;
            set;
        }
      
    }


}