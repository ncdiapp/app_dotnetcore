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
    public partial class AppSearchViewFieldDto : NotifyDataErrorDto 
    {

        partial void OnInitialized()
        {

            MandatoryProperties.Remove(AppSearchViewFieldDto.IsVisibleProperty);
            //SetUpDefaultValue();
        }


        partial void CustomerValidateDto(ValidationResult aValidationResult)
        {
            CustomerValidateExDto(aValidationResult);
        }

        protected virtual void CustomerValidateExDto(ValidationResult aValidationResult)
        {
            CustomerValidateProperty(AppSearchViewFieldDto.SysTableFiledPathProperty, aValidationResult);
            CustomerValidateProperty(AppSearchViewFieldDto.ControlTypeProperty, aValidationResult);
            CustomerValidateProperty(AppSearchViewFieldDto.EntityIdProperty, aValidationResult);
        }

        partial void CustomerValidateProperty(string PropertyName, ValidationResult aValidationResult)
        {
            if (PropertyName == AppSearchViewFieldDto.SysTableFiledPathProperty)
            {
                if (string.IsNullOrEmpty(SysTableFiledPath) || string.IsNullOrEmpty(SysTableFiledPath.Trim()))
                {
                    string errorMsg = "Field name is empty";

                    if (!string.IsNullOrWhiteSpace(DisplayText))
                    {
                        errorMsg += ": "+ DisplayText;
                    }
                    aValidationResult.Items.Add(new ValidationItem(null, "App_SearchViewField_SysTableFiledPathIsEmpty", ValidationItemType.Error, errorMsg, PropertyName));
                }
            }

            if (PropertyName == AppSearchViewFieldDto.ControlTypeProperty)
            {
                if (!ControlType.HasValue)
                {
                    string errorMsg = "Control type is empty";

                    if (!string.IsNullOrWhiteSpace(SysTableFiledPath))
                    {
                        errorMsg += ": " + SysTableFiledPath;
                    }

                    aValidationResult.Items.Add(new ValidationItem(null, "App_SearchViewField_ControlTypeIsEmpty", ValidationItemType.Error, errorMsg, PropertyName));
                }
            }

            if (PropertyName == AppSearchViewFieldDto.EntityIdProperty)
            {
                if (ControlType.HasValue && (ControlType.Value == (int)EmAppControlType.DDL || ControlType.Value == (int)EmAppControlType.AutoComplete || ControlType.Value == (int)EmAppControlType.SearchAbleDDL) && !EntityId.HasValue)
                {
                    string errorMsg = "DDL entity is empty";

                    if (!string.IsNullOrWhiteSpace(SysTableFiledPath))
                    {
                        errorMsg += ": " + SysTableFiledPath;
                    }

                    aValidationResult.Items.Add(new ValidationItem(null, "App_SearchViewField_DDLFieldEntityIdIsEmpty", ValidationItemType.Error, errorMsg, PropertyName));
                }
            }
        }

        //private void SetUpDefaultValue()
        //{
        
        //}
    }
}

