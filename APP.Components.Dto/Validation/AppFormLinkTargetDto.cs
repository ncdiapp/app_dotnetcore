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
    public partial class AppFormLinkTargetDto : NotifyDataErrorDto
    {
        partial void OnInitialized()
        {

        }


        partial void CustomerValidateDto(ValidationResult aValidationResult)
        {
            CustomerValidateExDto(aValidationResult);
        }

        protected virtual void CustomerValidateExDto(ValidationResult aValidationResult)
        {
            CustomerValidateProperty(AppFormLinkTargetDto.NavigationActionNameProperty, aValidationResult);
            CustomerValidateProperty(AppFormLinkTargetDto.ActionTypeProperty, aValidationResult);
            CustomerValidateProperty(AppFormLinkTargetDto.LinkTargetTransactionIdProperty, aValidationResult);
            CustomerValidateProperty(AppFormLinkTargetDto.SourceColumn1Property, aValidationResult);
            CustomerValidateProperty(AppFormLinkTargetDto.TargetColumn1Property, aValidationResult);
        }

        partial void CustomerValidateProperty(string PropertyName, ValidationResult aValidationResult)
        {
            if (LinkTargetUsageType.HasValue && (
                LinkTargetUsageType.Value == (int)EmAppLinkTargetUsageType.SearchViewLinkToForm
                || LinkTargetUsageType.Value == (int)EmAppLinkTargetUsageType.SearchViewLinkToFormGroup
                || LinkTargetUsageType.Value == (int)EmAppLinkTargetUsageType.TransactionUnitLinkToForm
                ))
            {
                if (PropertyName == AppFormLinkTargetDto.NavigationActionNameProperty)
                {
                    if (string.IsNullOrEmpty(NavigationActionName) || string.IsNullOrEmpty(NavigationActionName.Trim()))
                    {
                        aValidationResult.Items.Add(new ValidationItem(null, "App_FormTarget_ActionNameIsEmpty", ValidationItemType.Error, "Menu Display Name is empty", PropertyName));
                    }
                }

                if (PropertyName == AppFormLinkTargetDto.ActionTypeProperty)
                {
                    if (!ActionType.HasValue)
                    {
                        aValidationResult.Items.Add(new ValidationItem(null, "App_FormTarget_ActionTypeIsEmpty", ValidationItemType.Error, "Action Type is empty", PropertyName));
                    }
                }

            }
            //if (PropertyName == AppFormLinkTargetDto.LinkTargetTransactionIdProperty)
            //{
            //    if (!LinkTargetTransactionId.HasValue && ActionType.HasValue &&
            //        (ActionType.Value == (int)EmAppLinkTargetActionType.Create ||
            //        ActionType.Value == (int)EmAppLinkTargetActionType.Edit ||
            //        ActionType.Value == (int)EmAppLinkTargetActionType.Delete ||
            //         ActionType.Value == (int)EmAppLinkTargetActionType.Preview))
            //    {
            //        aValidationResult.Items.Add(new ValidationItem(null, "App_DataSet_LinkTargetTransactionIdIsEmpty", ValidationItemType.Error, "", PropertyName));
            //    }
            //}

            if (PropertyName == AppFormLinkTargetDto.SourceColumn1Property)
            {
                //if ((string.IsNullOrEmpty(SourceColumn1) || string.IsNullOrEmpty(SourceColumn1.Trim()))
                //    && ActionType.HasValue &&
                //    (
                //    ActionType.Value == (int)EmAppLinkTargetActionType.Edit ||
                //     ActionType.Value == (int)EmAppLinkTargetActionType.EditOnPopup ||
                //    ActionType.Value == (int)EmAppLinkTargetActionType.Delete ||
                //     ActionType.Value == (int)EmAppLinkTargetActionType.Preview))
                //{
                //    aValidationResult.Items.Add(new ValidationItem(null, "App_DataSet_SourceColumn1IsEmpty", ValidationItemType.Error, "Source 1 is empty", PropertyName));
                //}
            }

            //if (PropertyName == AppFormLinkTargetDto.TargetColumn1Property)
            //{
            //    if ((string.IsNullOrEmpty(TargetColumn1) || string.IsNullOrEmpty(TargetColumn1.Trim()))
            //        && ActionType.HasValue &&
            //        (ActionType.Value == (int)EmAppLinkTargetActionType.Create ||
            //        ActionType.Value == (int)EmAppLinkTargetActionType.Edit ||
            //        ActionType.Value == (int)EmAppLinkTargetActionType.Delete))
            //    {
            //        aValidationResult.Items.Add(new ValidationItem(null, "App_DataSet_TargetColumn1IsEmpty", ValidationItemType.Error, "", PropertyName));
            //    }
            //}
        }

    }
}

