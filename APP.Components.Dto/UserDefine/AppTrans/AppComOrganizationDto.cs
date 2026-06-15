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
	/// DTO class for the entity 'AppComOrganizationDto'.
	/// </summary>


	public partial class AppComOrganizationDto
	{

        [DataMember(EmitDefaultValue = false)]
        public AppComOrganizationDto[] Children
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int CountContent
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public bool IsFolderReadonly
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public string PathName
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public string PathDescription
        {
            get;
            set;
        }

        //[DataMember(EmitDefaultValue = false)]
        //public bool IsCompanyOrganizationPlaceholder
        //{
        //    get;
        //    set;
        //}

    }
}

