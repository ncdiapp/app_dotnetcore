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
    
    public partial class AppTransactionUnitDeleteFlowDto : NotifyDataErrorDto 
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
            CustomerValidateProperty(AppSearchDto.DataSetIdProperty, aValidationResult);
        }

        partial void CustomerValidateProperty(string PropertyName, ValidationResult aValidationResult)
        {
            if (PropertyName == AppTransactionUnitDeleteFlowDto.RelativeTableNameProperty)
            {
                if (string.IsNullOrEmpty(RelativeTableName) && string.IsNullOrEmpty(StoredProcedureName))
                {
                    aValidationResult.Items.Add(new ValidationItem(null, "aaaaaaa", ValidationItemType.Error, "", PropertyName));
                }
            }
        }
        
        
    }
}

