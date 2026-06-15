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
    public partial class AppSearchViewDto : NotifyDataErrorDto 
    {


        partial void OnInitialized()
        {           

            MandatoryProperties.Remove(AppSearchViewDto.NoSecurityProperty);
            MandatoryProperties.Remove(AppSearchViewDto.OptionsProperty);
            MandatoryProperties.Remove(AppSearchViewDto.ViewTypeProperty);
            MandatoryProperties.Remove(AppSearchViewDto.GridOutputModeProperty);
            MandatoryProperties.Remove(AppSearchViewDto.ColumnCountProperty);
            MandatoryProperties.Remove(AppSearchViewDto.RowPerPageProperty);
        }


        partial void CustomerValidateDto(ValidationResult aValidationResult)
        {
            CustomerValidateExDto(aValidationResult);
        }

        protected virtual void CustomerValidateExDto(ValidationResult aValidationResult)
        {
            CustomerValidateProperty(AppSearchViewDto.DataSetIdProperty, aValidationResult);
        }

        partial void CustomerValidateProperty(string PropertyName, ValidationResult aValidationResult)
        {
            if (PropertyName == AppSearchViewDto.DataSetIdProperty)
            {
                if (!DataSetId.HasValue)
                {
                    aValidationResult.Items.Add(new ValidationItem(null, "App_SearchView_DataSetIsEmpty", ValidationItemType.Error, "", PropertyName));
                }
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsDefaultView
		{
			get;set;
		}

        [DataMember(EmitDefaultValue = false)]
        public bool IsDraft
        {
            get; set;
        }

    }
}

