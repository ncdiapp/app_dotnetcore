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
    public partial class AppDesktopDto
    {
        [DataMember(EmitDefaultValue = false)]
        public bool IsDesktopReadOnly
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public AppDesktopOtherSettingsDto OtherSettingsDto
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public List<AppListMenuDto> UserBookmarkList
        {
            get;
            set;
        }

        //[DataMember(EmitDefaultValue = false)]
        //public int? SaasApplicationId { get; set; }


    }


    public partial class AppDesktopOtherSettingsDto
    {
        [DataMember(EmitDefaultValue = false)]
        public List<AppDesktopItemExDto> FlexLayoutItems
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public int? DefaultNumberOfColumns
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public bool IsUserDesktop
        {
            get;
            set;
        }

        //[DataMember(EmitDefaultValue = false)]
        //public int? UserDesktopUserId
        //{
        //    get;
        //    set;
        //}
    }
}

