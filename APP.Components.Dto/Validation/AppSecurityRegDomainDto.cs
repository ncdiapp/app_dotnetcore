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
    public partial class AppSecurityRegDomainDto
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
            CustomerValidateProperty(AppSecurityRegDomainDto.DomainTypeProperty, aValidationResult);           
        }

        partial void CustomerValidateProperty(string PropertyName, ValidationResult aValidationResult)
        {
            if (PropertyName == AppSecurityRegDomainDto.DomainTypeProperty)
            {
                if (!DomainType.HasValue)
                {
                    aValidationResult.Items.Add(new ValidationItem(null, "App_SecurityRegDomain_DomainTypeIsEmpty", ValidationItemType.Error, "", PropertyName));
                }
            }
        }

    }
}

