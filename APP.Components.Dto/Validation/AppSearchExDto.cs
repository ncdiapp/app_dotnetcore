 using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using APP.Framework;
using APP.Framework.Collections;
using System.Runtime.Serialization;
using APP.Components.Dto;
using APP.Framework.Validation;

namespace APP.Components.EntityDto
{
   
    public partial class AppSearchExDto : AppSearchDto 
    {

        protected override void CustomerValidateExDto(ValidationResult aValidationResult)
        {
            base.CustomerValidateExDto(aValidationResult);
            ValidateSearchField(aValidationResult);   
        }

        public void ValidateSearchField(ValidationResult aValidationResult)
        {

            foreach (var aSearchField in AppSearchFieldList)
            {
                ValidationResult aSearchFieldValidationResult = aSearchField.ValidateDto();
                if (aSearchFieldValidationResult.HasItems)
                {
                    aValidationResult.Merge(aSearchFieldValidationResult);
                }

                //if (!aSearchField.ControlType.HasValue)
                //{
                //    aValidationResult.AddItem(null, "App_SearchField_ControlTypeIsEmpty", ValidationItemType.Error, "Search Field Control Type Is Empty.");
                //}
            }
        }
       
    }
}

