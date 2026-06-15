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
  

    public partial class AppSecurityUserInvitationDto
    {
        partial void OnInitialized()
        {
            //MandatoryProperties.Remove(AppSearchDto.TypeProperty); 

            MandatoryProperties.Add(AppSecurityUserInvitationDto.InvitedUserEmailProperty);
            MandatoryProperties.Add(AppSecurityUserInvitationDto.EmUserTypeProperty);
            MandatoryProperties.Add(AppSecurityUserInvitationDto.InvitingBusinessPartnerIdProperty);
        }

        partial void CustomerValidateDto(ValidationResult aValidationResult)
        {
            CustomerValidateExDto(aValidationResult);
        }

        protected virtual void CustomerValidateExDto(ValidationResult aValidationResult)
        {
            //CustomerValidateProperty(AppSecurityUserInvitationDto.InvitedUserEmailProperty, aValidationResult);
            //CustomerValidateProperty(AppSecurityUserInvitationDto.EmUserTypeProperty, aValidationResult);
            //CustomerValidateProperty(AppSecurityUserInvitationDto.InvitingBusinessPartnerIdProperty, aValidationResult);

        }

        partial void CustomerValidateProperty(string PropertyName, ValidationResult aValidationResult)
        {
           
        }


    }
}