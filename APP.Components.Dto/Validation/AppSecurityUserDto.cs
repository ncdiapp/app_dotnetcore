using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework;
using APP.Framework.Collections;
using APP.Framework.Validation;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
  

    public partial class AppSecurityUserDto
    {
        partial void OnInitialized()
        {
            //MandatoryProperties.Remove(AppSearchDto.TypeProperty);
            MandatoryProperties.Remove(AppSecurityUserDto.IsDeletedProperty);

        }


        partial void CustomerValidateDto(ValidationResult aValidationResult)
        {
            CustomerValidateExDto(aValidationResult);
        }

        protected virtual void CustomerValidateExDto(ValidationResult aValidationResult)
        {
            CustomerValidateProperty(AppSecurityUserDto.DomainIdProperty, aValidationResult);
            CustomerValidateProperty(AppSecurityUserDto.ConfirmPasswordProperty, aValidationResult);

        }

        partial void CustomerValidateProperty(string PropertyName, ValidationResult aValidationResult)
        {
            if (PropertyName == AppSecurityUserDto.DomainIdProperty)
            {
                if (DomainId == 0)
                {
                    aValidationResult.Items.Add(new ValidationItem(null, "App_SecurityUser_DomainIdIsEmpty", ValidationItemType.Error, "Domain Type is empty.", PropertyName));
                }
            }

            if (PropertyName == AppSecurityUserDto.ConfirmPasswordProperty)
            {
                if (ConfirmPassword != Password)
                {
                    aValidationResult.Items.Add(new ValidationItem(null, "App_SecurityUser_PasswordAndConfirmPasswordDoNotMatch", ValidationItemType.Error, "Password and Confirm Password are not the same", PropertyName));
                }
            }
        }


    }
}