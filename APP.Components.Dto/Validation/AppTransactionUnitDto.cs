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

    public partial class AppTransactionUnitDto : NotifyDataErrorDto
    {
        public static readonly string DefaultNewUnitName = "New Unit";


        partial void OnInitialized()
        {

        }


        partial void CustomerValidateDto(ValidationResult aValidationResult)
        {
            CustomerValidateExDto(aValidationResult);
        }

        protected virtual void CustomerValidateExDto(ValidationResult aValidationResult)
        {
        
            CustomerValidateProperty(AppTransactionUnitDto.UnitDisplayNameProperty, aValidationResult);
      
        }

        partial void CustomerValidateProperty(string PropertyName, ValidationResult aValidationResult)
        {
            if (PropertyName == AppTransactionUnitDto.DataBaseTableNameProperty)
            {
                if (string.IsNullOrEmpty(DataBaseTableName) || string.IsNullOrEmpty(DataBaseTableName.Trim()))
                {
                    aValidationResult.Items.Add(new ValidationItem(null, "App_AppTransactionUnit_DataBaseTableNameIsEmpty", ValidationItemType.Error, "", PropertyName));
                }
            }

            if (PropertyName == AppTransactionUnitDto.UnitDisplayNameProperty)
            {
                if (string.IsNullOrEmpty(UnitDisplayName) || string.IsNullOrEmpty(UnitDisplayName.Trim()))
                {
                    ValidationItem item = aValidationResult.AddItem(null, "App_AppTransactionUnit_TransactionUnitNameIsEmpty", ValidationItemType.Error, "Transaction Unit Name Is Empty.", PropertyName);
                }

                if (string.IsNullOrEmpty(DataBaseTableName) && UnitDisplayName == DefaultNewUnitName)
                {
                    ValidationItem item = aValidationResult.AddItem(null, "App_AppTransactionUnit_PleaseRenameTransactionUnitsFromTheDefaultName", ValidationItemType.Error, "Please Rename Transaction Units From The Default Name.", PropertyName);
                }
                
               


            }

           
        }

    }
}

