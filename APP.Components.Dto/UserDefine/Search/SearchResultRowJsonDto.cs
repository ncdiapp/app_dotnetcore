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
    public partial class StaticSearchResultRowJsonDto 
    {
        public StaticSearchResultRowJsonDto()
        {
            IsChanged = false;
          
            ChangedColumnIds = new List<int>();
            DictLinkTargetParameterValue = new Dictionary<int, object>();
            DictSketchOrFileDisplayCode = new Dictionary<int, string>();
            DictThumbnailUrl = new Dictionary<int, string>();
            DictImageUrl = new Dictionary<int, string>();

            DictViewColumnIDKeyValue = new Dictionary<int, object>();
        }

        [DataMember(EmitDefaultValue = false)]
        public string Display
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public bool IsSelected
        {
            get;
            set;
        }



        [DataMember(EmitDefaultValue = false)]
        public int? SelectedRowLinkTargetId
        {
            get;
            set;
        }



        //// only for massupdate
        [DataMember(EmitDefaultValue = false)]
        public List<int> ChangedColumnIds
        {
            get;
            set;
        }




        [DataMember(EmitDefaultValue = false)]
        public bool IsChanged
        {
            get;
            set;
        }




		[DataMember(EmitDefaultValue = false)]
		public bool IsNew
		{
			get;
			set;
		}

		// only for mass update
		[DataMember(EmitDefaultValue = false)]
        public Guid? RowValueGuId
        {
            get;
            set;
        }

	

		[IgnoreDataMember]
		public object this[int index]
		{
			get
			{
				if (DictViewColumnIDKeyValue.ContainsKey(index))
				{
					return DictViewColumnIDKeyValue[index];
				}
				else
				{
					return null;
				}

			}
		}


		/// <summary> Key: ViewColumnID: </summary>
		[DataMember(EmitDefaultValue = false)]
        public Dictionary<int, object> DictLinkTargetParameterValue
        {
            get;set;
        }

   
        /// <summary> Key: ViewColumnID: </summary>
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, string> DictSketchOrFileDisplayCode
        {
          get;set;
        }

        /// <summary> Key: ViewColumnID — static resource URL for Image column thumbnails (e.g. /api/resources/Company_1/Image/thumbnail/guid). </summary>
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, string> DictThumbnailUrl
        {
            get;
            set;
        }

        /// <summary> Key: ViewColumnID — static resource URL for Image column regular images (e.g. /api/resources/Company_1/Image/regular/guid). Used by Card View. </summary>
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, string> DictImageUrl
        {
            get;
            set;
        }


        /// <summary> Key: ViewColumnID: </summary>
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, object> DictViewColumnIDKeyValue
        {
            get;
            set;
        }
      

        public override string ToString()
        {
            return Display;
        }
                

        public object CurrentColumn
        {
            get;
            set;
        }

        public bool DisableUIUpdate
        {
            get;
            set;
        }



        [DataMember(EmitDefaultValue = false)]
        public string EventName
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string EventBody
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public DateTime? EventStartDate
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public DateTime? EventEndDate
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string EventStartDateString
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string EventEndDateString
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public DateTime? EventActualStartDate
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public DateTime? EventActualEndDate
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string EventActualStartDateString
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string EventActualEndDateString
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public double EventCompletePercentage
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public int? EventType
        {
            get;
            set;
        }
        [DataMember(EmitDefaultValue = false)]
        public string EventTypeString
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public int? EventCompletStage
        {
            get;
            set;
        }

      
        [DataMember(EmitDefaultValue = false)]
        public string EventCompletStageString
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? EventStatus
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string EventStatusString
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? EventUserId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string EventTypeDisplay
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string EventUserDisplay
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string EventDescription1
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string EventDescription2
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public object EventGroupById
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? EventDateId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? EventTransactionId
        {
            get;
            set;
        }
        
        [DataMember(EmitDefaultValue = false)]
        public object EventTransactionRId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string EventColorId
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

        //AppSearchViewEntity aAppSearchViewEntity = AppSearchViewConfigBL.RetrieveOneAppSearchViewEntity(massUpdateSaveDto.SearchViewId);

        [DataMember(EmitDefaultValue = false)]
        public BaseAppCatalogueTreeDto BaseAppCatalogueTreeDto
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public AppEshopCatalogCardDto EshopCatalogCardDto
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public AppEshopCatalogCardDetailDto EshopCatalogCardDetailDto
        {
            get; set;
        }


        [DataMember(EmitDefaultValue = false)]
        public IEnumerable<StaticSearchResultRowJsonDto> Children
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, List<StaticSearchResultRowJsonDto>> DictViewIdAndChildRowList
        {
            get;
            set;
        }

        public Dictionary<string, List<StaticSearchResultRowJsonDto>> DictUdKeyAndChildRowList
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, StaticSearchResultRowJsonDto> DictClusterChildViewUiIdAndResultRowJsonDto
        {
            get;
            set;
        }


        //[DataMember(EmitDefaultValue = false)]
        //public Dictionary<int, List<string>> DictFormulaTypeAndWarningMessage
        //{
        //    get;
        //    set;
        //}

        //[DataMember(EmitDefaultValue = false)]
        //public Dictionary<int, int> DictTransFieldIdAndWarningHighlightStyleId
        //{
        //    get;
        //    set;
        //}

    }

    public class NTuple<T> : IEquatable<NTuple<T>>
    {
        public NTuple(IEnumerable<T> values)
        {
            Values = values.ToArray();
        }

        public readonly T[] Values;

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            if (obj == null)
                return false;
            return Equals(obj as NTuple<T>);
        }

        public bool Equals(NTuple<T> other)
        {
            if (ReferenceEquals(this, other))
                return true;
            if (other == null)
                return false;
            var length = Values.Length;
            if (length != other.Values.Length)
                return false;
            for (var i = 0; i < length; ++i)
                if (!Equals(Values[i], other.Values[i]))
                    return false;
            return true;
        }

        public override int GetHashCode()
        {
            var hc = 17;
            foreach (var value in Values)
                hc = hc * 37 + (!ReferenceEquals(value, null) ? value.GetHashCode() : 0);
            return hc;
        }
    }
}