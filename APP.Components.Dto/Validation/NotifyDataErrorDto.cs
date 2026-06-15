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

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class NotifyDataErrorDto : EditableObject
    {

        protected override void OnInitialize()
        {
            base.OnInitialize();
            IsEnableNotifyPropertyValidationResult = true;

        }

        public virtual ValidationResult ValidateProperty(string PropertyName)
        {
            return null;

        }


        //private bool _IsEnableNotifyPropertyValidationResult = true;
        public bool IsEnableNotifyPropertyValidationResult
        {
            get;
            set;
        }

        //
        private List<String> _PropertyNeedToValidationList;
        public List<String> PropertyNeedToValidationList
        {
            get { return _PropertyNeedToValidationList; }
            set { _PropertyNeedToValidationList = value; }
        }

        private ValidationResult _PropertyValidationResult;
        public ValidationResult PropertyValidationResult
        {
            get { return _PropertyValidationResult; }
            set { _PropertyValidationResult = value; }
        }

        public virtual ValidationResult ValidateDto()
        {
            return null;

        }


        public void NotifyALlPropertyValidationResultChange()
        {

            PropertyValidationResult = ValidateDto();
            if (PropertyValidationResult.HasErrors)
            {
                foreach (var propertyName in PropertyNeedToValidationList.Distinct())
                {
                    NotifyPropertyValidationResultChange(propertyName);

                }
            }



        }


        public void NotifyOnePropertyValidationResultChange(string propertyName)
        {

            var result = ValidateProperty(propertyName);


            // no error
            if (result == null || !result.HasErrors)
            {
                if (PropertyValidationResult != null)
                {
                    var oldResult = PropertyValidationResult.FindItemByProperty(propertyName).ToList();
                    if (oldResult.Count() > 0)
                    {
                        foreach (var item in oldResult)
                        {
                            PropertyValidationResult.Items.Remove(item);
                        }

                        // remove the error 
                        NotifyPropertyValidationResultChange(propertyName);
                    }


                }
            }
            // there are some errors
            else if (result != null && result.HasErrors)
            {
                if (PropertyValidationResult != null)
                {
                    var oldResult = PropertyValidationResult.FindItemByProperty(propertyName).ToList();
                    if (oldResult.Count() > 0)
                    {
                        foreach (var item in oldResult)
                        {
                            PropertyValidationResult.Items.Remove(item);
                        }
                    }

                    foreach (var item in result.Items)
                    {
                        PropertyValidationResult.Items.Add(item);
                    }                  
                }
                else
                {
                    PropertyValidationResult = result;

                }

                NotifyPropertyValidationResultChange(propertyName);
            }




        }


        protected void NotifyPropertyValidationResultChange(string propertyName)
        {

            RaiseErrorsChanged(propertyName);
        }

        partial void RaiseErrorsChanged(string propertyName);


    }
}


