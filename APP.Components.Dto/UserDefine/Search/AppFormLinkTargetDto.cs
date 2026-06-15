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
    public partial class AppFormLinkTargetDto
    {

        public AppTransactionGroupExDto TransactionGroupExDto
        {
            get; set;

        }

        [DataMember(EmitDefaultValue = false)]
        public string UiId {
            get 
            {
                if (OtherSettingsDto != null && !string.IsNullOrWhiteSpace(OtherSettingsDto.UiId))
                {
                    return OtherSettingsDto.UiId;
                }                              

                return "";
            }
        }


        [DataMember(EmitDefaultValue = false)]
        public AppFormLinkTargetOtherSettingsDto OtherSettingsDto
        {
            get;
            set;
        }

        public int? GetLinkTargetActionCode()
        {
            if (this.ActionType.HasValue)
            {
                if (this.ActionType.Value == (int)EmAppLinkTargetActionType.CallExternalMethod)
                {
                    return (int)EmAppSysTransactionUnitActionCode.CallExternalMethodAction;
                }
                if (this.ActionType.Value == (int)EmAppLinkTargetActionType.Create)
                {
                    return (int)EmAppSysTransactionUnitActionCode.CreateAction;
                }
                if (this.ActionType.Value == (int)EmAppLinkTargetActionType.Delete)
                {
                    return (int)EmAppSysTransactionUnitActionCode.DeleteAction;
                }
                if (this.ActionType.Value == (int)EmAppLinkTargetActionType.Edit)
                {
                    return (int)EmAppSysTransactionUnitActionCode.EditAction;
                }
                if (this.ActionType.Value == (int)EmAppLinkTargetActionType.EditUserLogin)
                {
                    return (int)EmAppSysTransactionUnitActionCode.EditUserLoginAction;
                }
                if (this.ActionType.Value == (int)EmAppLinkTargetActionType.Preview)
                {
                    return (int)EmAppSysTransactionUnitActionCode.PreviewAction;
                }

            }

            return null;
        }
    }


    public partial class AppFormLinkTargetOtherSettingsDto
    {
        [DataMember(EmitDefaultValue = false)]
        public int? TemplateItemType { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public bool IsLinkToComsumeApiTransaction { get; set; }


        //[DataMember(EmitDefaultValue = false)]
        //public bool IsStandAloneCommandAction { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string,int> DictTransDbFiledSearchFieldMappingAfterApiCall { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string UiId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public EmAppLinkTargetApplyToRowRangeType? LinkTargetApplyToRowRangeType { get; set; }

    }
}

