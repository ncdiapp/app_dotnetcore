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
   
    public partial class ScopeMessageGroupDto
    {     

        [DataMember(EmitDefaultValue = false)]
        public List<AppMessageDto> MessageDtoList
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsFollowUpUserFromTransaction
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<AppUserMessgeFollowupDto> FollowupDtoList
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<int> FollowupUserIdList
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, string> DictAttachmentFileIdAndDisplay
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? GroupByType
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


    }


}

