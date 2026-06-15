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
    public partial class AppViewLinkedSeaechOrUrlDto
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
            CustomerValidateProperty(AppViewLinkedSeaechOrUrlDto.LinkTargetSearchIdProperty, aValidationResult);
            CustomerValidateProperty(AppViewLinkedSeaechOrUrlDto.LinkTargetUrlOrRouteCodeProperty, aValidationResult);
        }

        partial void CustomerValidateProperty(string PropertyName, ValidationResult aValidationResult)
        {
            if (PropertyName == AppViewLinkedSeaechOrUrlDto.LinkTargetSearchIdProperty)
            {
                if (!LinkTargetSearchId.HasValue)
                {
                    //aValidationResult.Items.Add(new ValidationItem(null, "App_Transaction_TransactionNameIsEmpty", ValidationItemType.Error, "Transaction Name Is Empty", PropertyName));

                }
            }
        }
    }
}

