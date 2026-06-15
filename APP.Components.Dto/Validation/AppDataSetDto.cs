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
    public partial class AppDataSetDto : NotifyDataErrorDto
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
            CustomerValidateProperty(AppDataSetDto.NameProperty, aValidationResult);
            CustomerValidateProperty(AppDataSetDto.QueryTextProperty, aValidationResult);
        }

        partial void CustomerValidateProperty(string PropertyName, ValidationResult aValidationResult)
        {

            if (PropertyName == AppDataSetDto.NameProperty)
            {
                if (string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(Name.Trim()))
                {
                    aValidationResult.Items.Add(new ValidationItem(null, "App_DataSet_NameIsEmpty", ValidationItemType.Error, "Name is empty", PropertyName));
                }
            }

            if (PropertyName == AppDataSetDto.QueryTextProperty)
            {
                if (!BaseDataSetId.HasValue && 
                    !(UsageTypeId.HasValue && (UsageTypeId.Value == (int)EmAppDataSetUsageType.ErDiagram 
                    || UsageTypeId.Value == (int)EmAppDataSetUsageType.ExcelTableImportSetting 
                    || UsageTypeId.Value == (int)EmAppDataSetUsageType.ExcelEntityImportSetting
                    || UsageTypeId.Value == (int)EmAppDataSetUsageType.DbToDbTableImportSetting)))
                {
                    if (string.IsNullOrEmpty(QueryText) || string.IsNullOrEmpty(QueryText.Trim()))
                    {
                        aValidationResult.Items.Add(new ValidationItem(null, "App_DataSet_QueryTextIsEmpty", ValidationItemType.Error, "QueryText is empty", PropertyName));
                    }
                }
            }

        }



    }
}

