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
	public partial class MassUpdateSaveDto
	{
        [DataMember(EmitDefaultValue = false)]
        public bool IsListEditSimpleMassUpdate
        {
            get; set;
        }

        [DataMember]
		public int?  SearchViewId
		{
			get;
			set;
		}	

		/// <summary> Key: ViewColumnID: </summary>
		[DataMember(EmitDefaultValue = false)]
		public List<StaticSearchResultRowJsonDto> ModifiedSearchResult
		{
			get; set;
		}

        /// <summary> Key: ViewColumnID: </summary>
		[DataMember(EmitDefaultValue = false)]
        public List<StaticSearchResultRowJsonDto> DeletedSearchResult
        {
            get; set;
        }

		/// <summary> Key: ViewColumnID: </summary>
		[DataMember(EmitDefaultValue = false)]
		public   AppListDataDto MassUpdateAppListDataDto
		{
			get; set;
		}




		
	}


}