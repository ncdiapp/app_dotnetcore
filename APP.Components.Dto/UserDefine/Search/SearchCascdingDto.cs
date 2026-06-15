
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
	public class SearchCascdingDto
	{


		[DataMember(EmitDefaultValue = false)]
		public int SearchId

		{
			get;
			set;
		}

		// alwasy int?  Entity 
		[DataMember(EmitDefaultValue = false)]
		public object CurrentChangedValue

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

		// Return Value
		[DataMember(EmitDefaultValue = false)]
		public Dictionary<int, List<LookupItemDto>> DictReturnCascadingChildFieldIdAndDataSource
		{
			get;
			set;
		}

		[DataMember(EmitDefaultValue = false)]
		public 	Dictionary<int, object> DictReturnCriteriaIdValue

		{
			get;
			set;
		}

	}
}