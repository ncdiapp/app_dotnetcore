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
    
    public partial class AppSearchFieldDto : NotifyDataErrorDto 
    {
        partial void OnInitialized()
        {
            MandatoryProperties.Remove(AppSearchFieldDto.IsReadOnlyProperty);
            MandatoryProperties.Remove(AppSearchFieldDto.IsAutoPopulateProperty);
            //SetUpDefaultValue();
        }


        partial void CustomerValidateDto(ValidationResult aValidationResult)
        {
            CustomerValidateExDto(aValidationResult);
        }

        protected virtual void CustomerValidateExDto(ValidationResult aValidationResult)
        {
            CustomerValidateProperty(AppSearchFieldDto.SysTableFiledPathProperty, aValidationResult);
            CustomerValidateProperty(AppSearchFieldDto.ControlTypeProperty, aValidationResult);
            CustomerValidateProperty(AppSearchFieldDto.EntityIdProperty, aValidationResult);
        }

        partial void CustomerValidateProperty(string PropertyName, ValidationResult aValidationResult)
        {

            if (PropertyName == AppSearchFieldDto.SysTableFiledPathProperty)
            {
                //if (string.IsNullOrEmpty(SysTableFiledPath) || string.IsNullOrEmpty(SysTableFiledPath.Trim()))
                //{
                //    aValidationResult.Items.Add(new ValidationItem(null, "App_SearchField_SysTableFiledPathIsEmpty", ValidationItemType.Error, "", PropertyName));
                //}
            }


            if (PropertyName == AppSearchFieldDto.ControlTypeProperty)
            {
                if (!ControlType.HasValue)
                {
                    aValidationResult.Items.Add(new ValidationItem(null, "App_SearchField_ControlTypeIsEmpty", ValidationItemType.Error, "", PropertyName));
                }
            }

            if (PropertyName == AppSearchFieldDto.EntityIdProperty)
            {
                if (ControlType.HasValue && (ControlType.Value == (int)EmAppControlType.DDL || ControlType.Value == (int)EmAppControlType.AutoComplete || ControlType.Value == (int)EmAppControlType.SearchAbleDDL) && !EntityId.HasValue)
                {
                    aValidationResult.Items.Add(new ValidationItem(null, "App_SearchField_DDLFieldEntityIdIsEmpty", ValidationItemType.Error, "", PropertyName));
                }
            }
        }

        //private void SetUpDefaultValue()
        //{
       
        //}
      
        
    }
}

