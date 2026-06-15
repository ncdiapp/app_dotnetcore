using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework;
using APP.Framework.Collections;
using APP.Framework.Validation;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    public partial class AppTransactionExDto
    {


	

		protected override void CustomerValidateExDto(ValidationResult aValidationResult)
        {
            base.CustomerValidateExDto(aValidationResult);
            ValidateAppTransactionUnitList(aValidationResult);
        }

        public void ValidateAppTransactionUnitList(ValidationResult aValidationResult)
        {
            AppTransactionUnitExDto rootUnitDto = AppTransactionUnitList.FirstOrDefault();
            if (rootUnitDto != null)
            {
                rootUnitDto.Level = 1;
                ValidateAppTransactionUnit(rootUnitDto, aValidationResult);

                foreach (var childDto in rootUnitDto.Children)
                {
                    childDto.Level = 2;
                    ValidateAppTransactionUnit(childDto, aValidationResult);

                    foreach (var grandChildDto in childDto.Children)
                    {
                        grandChildDto.Level = 3;
                        ValidateAppTransactionUnit(grandChildDto, aValidationResult);
                    }
                }
            }
        }

        public void ValidateAppTransactionUnit(AppTransactionUnitExDto aUnit, ValidationResult aValidationResult)
        {
            aValidationResult.Merge(aUnit.ValidateDto());




            //ValidationResult  aUnitValidationResult = aUnit.ValidateDto();
            //if (aUnitValidationResult.HasErrors)
            //{               

            //    ValidationItem item = aValidationResult.AddItem(null, "App_Transaction_ErrorFoundOnTransactionUnit", ValidationItemType.Error, "Error Found On Transaction Unit: {0}", aUnit.DataBaseTableName);
            //    item.MessageParameters.Add(aUnit.DataBaseTableName);
            //    aValidationResult.Merge(aUnitValidationResult);




            //}

            //string defaultMesssage = "{0} is empty  ";
            //ValidationItem item = aValidationResult.AddItem(null, "plm_Entity_Property_MandatoryFormatString", ValidationItemType.Error, defaultMesssage, propertyName);

            //foreach (var aSearchField in AppSearchFieldList)
            //{
            //    ValidationResult aSearchFieldValidationResult = aSearchField.ValidateDto();
            //    if (aSearchFieldValidationResult.HasItems)
            //    {
            //        aValidationResult.Merge(aSearchFieldValidationResult);
            //    }


            //}
        }


    }
}