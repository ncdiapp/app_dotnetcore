using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Framework;
using APP.Framework.Validation;
using APP.Components.Dto;
using APP.Framework.Globalization;

namespace APP.Components.EntityDto
{


    public static class FirstLevelVlidationDtoFactory 
    {



        public static readonly string plm_Entity_Property_Length_Greater = "plm_Entity_Property_Length_Greater";
        public static readonly string plm_Entity_Property__Mandatory = "plm_Entity_Property__Mandatory";
        public static readonly string plm_Entity_Property_MandatoryFormatString = "plm_Entity_Property_MandatoryFormatString";
        

        public static ValidationResult ValidateDtoStringmaxLengthAndMandatory(EditableObject aEditableObject,  List<string> aMandatoryProperties , List<string> aForeignKeyProperties, Dictionary<string, int> aDictStringPropertyMaxLength )

        {
            ValidationResult aValidationResult = new ValidationResult();
            ValidationDtoStringLengthResult(aEditableObject, aValidationResult, aDictStringPropertyMaxLength);
            ValidationDtoMandatoryPropertiesResult(aEditableObject, aValidationResult,aMandatoryProperties , aForeignKeyProperties);
            return aValidationResult;
       

        }

        public static ValidationResult ValidatePropertyStringmaxLengthAndMandatory(EditableObject aEditableObject, string propertyName, List<string> aMandatoryProperties, List<string> aForeignKeyProperties, Dictionary<string, int> aDictStringPropertyMaxLength)
        {
            ValidationResult aValidationResult = new ValidationResult();
            if(aDictStringPropertyMaxLength.ContainsKey(propertyName) )
            {
                ValidatePropertyLength(aEditableObject, aValidationResult, aDictStringPropertyMaxLength[propertyName], propertyName);
            }

            if (aMandatoryProperties.Contains(propertyName))
            {
                ValidateMandatoryProperty(aEditableObject, aValidationResult, aForeignKeyProperties, propertyName);
 
            }
            
            return aValidationResult;
         
        }


        private static void ValidationDtoStringLengthResult(EditableObject aEditableObject, ValidationResult aValidationResult, Dictionary<string, int> aDictStringPropertyMaxLength)
        {
            foreach (var stringProp in aDictStringPropertyMaxLength.Keys)
            {
                ValidatePropertyLength(aEditableObject, aValidationResult, aDictStringPropertyMaxLength[stringProp], stringProp);

            }

        }

        private static void ValidationDtoMandatoryPropertiesResult(EditableObject aEditableObject, ValidationResult aValidationResult, List<string> aMandatoryProperties, List<string> aForeignKeyProperties)
        {
            foreach (var mProp in aMandatoryProperties)
            {
                ValidateMandatoryProperty(aEditableObject, aValidationResult, aForeignKeyProperties, mProp);


            }
        }

        private static void ValidatePropertyLength(EditableObject aEditableObject, ValidationResult aValidationResult, int maxLength, string propertyName)
        {
            var stringValue = aEditableObject.GetValue<string>(propertyName);
            if (!string.IsNullOrEmpty(stringValue) && stringValue.Length > maxLength)
            {

                string messageFormat = "{0} greater than {1}";
                ValidationItem item = aValidationResult.AddItem(null, plm_Entity_Property_Length_Greater, ValidationItemType.Error, messageFormat, propertyName);
                item.MessageParameters.Add(propertyName);
                item.MessageParameters.Add(maxLength);                
            }
        }
      

        private static void ValidateMandatoryProperty(EditableObject aEditableObject, ValidationResult aValidationResult, List<string> aForeignKeyProperties, string propertyName)
        {
            // Skip the new Validation for Foreign Entity if it is new object
            if (!(aEditableObject.IsNew && aForeignKeyProperties.Contains(propertyName)))
            {
                var mValue = aEditableObject.GetValue<object>(propertyName);

                if (mValue == null || (mValue is string && (string.IsNullOrEmpty(mValue.ToString()))))
                {

                  //  string message = StringLocalizer.Localize(plm_Entity_Property__Mandatory, mProp + " is empty  ");
                  //  aValidationResult.AddItem(null, plm_Entity_Property__Mandatory, ValidationItemType.Error, message, mProp);


                    string defaultMesssage = "{0} is empty  ";
                    ValidationItem item = aValidationResult.AddItem(null, plm_Entity_Property_MandatoryFormatString, ValidationItemType.Error, defaultMesssage, propertyName);
                    item.MessageParameters.Add(propertyName);
                }

            }
        }





    
    }
}