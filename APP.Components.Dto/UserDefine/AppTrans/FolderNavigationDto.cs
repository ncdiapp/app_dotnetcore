using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Framework;
using APP.Framework.Validation;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    /// <summary>
    /// DTO class for the entity 'AppSefolder'.
    /// </summary>


    public partial class FolderNavigationDto
	{

        [DataMember(EmitDefaultValue = false)]
        public AppSefolderDto[] HairarchyFolderRootList
		{
            get;
            set;
        }

		[DataMember(EmitDefaultValue = false)]
		public List<AppSearchViewDto> ViewList
		{
			get;
			set;
		}

		[DataMember(EmitDefaultValue = false)]
		public IEnumerable<StaticSearchResultRowJsonDto> SearchResultList
		{
			get;
			set;
		}

        [DataMember(EmitDefaultValue = false)]
        public int? TransBusinessType
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? TransMgtRootFolderId
        {
            get;
            set;
        }

    }
}

