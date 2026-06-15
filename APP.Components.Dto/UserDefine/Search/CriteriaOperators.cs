using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    public static class CriteriaOperators
    {

        public static CriteriaOperatorDto EqualsOp = new CriteriaOperatorDto()
        {
            Display = "=",
            IsMultipleValuesAllowed = true,
            OperatorType = EmAppCriteriaOperatorType.Equals,
            IsEditorRequired = true,
        };

        public static CriteriaOperatorDto Null = new CriteriaOperatorDto()
        {
            Display = "Null",
            IsMultipleValuesAllowed = false,
            OperatorType = EmAppCriteriaOperatorType.Null,
            IsEditorRequired = false,
        };

        public static CriteriaOperatorDto NotNull = new CriteriaOperatorDto()
        {
            Display = "Not Null",
            IsMultipleValuesAllowed = false,
            OperatorType = EmAppCriteriaOperatorType.NotNull,
            IsEditorRequired = false,
        };

        public static CriteriaOperatorDto GreaterThan = new CriteriaOperatorDto()
        {
            Display = ">",
            IsMultipleValuesAllowed = true,
            OperatorType = EmAppCriteriaOperatorType.GreaterThan,
            IsEditorRequired = true,
        };

        public static CriteriaOperatorDto GreaterThanOrEquals = new CriteriaOperatorDto()
        {
            Display = ">=",
            IsMultipleValuesAllowed = true,
            OperatorType = EmAppCriteriaOperatorType.GreaterThanOrEquals,
            IsEditorRequired = true,
        };

        public static CriteriaOperatorDto LessThan = new CriteriaOperatorDto()
        {
            Display = "<",
            IsMultipleValuesAllowed = true,
            OperatorType = EmAppCriteriaOperatorType.LessThan,
            IsEditorRequired = true,
        };

        public static CriteriaOperatorDto LessThanOrEquals = new CriteriaOperatorDto()
        {
            Display = "<=",
            IsMultipleValuesAllowed = true,
            OperatorType = EmAppCriteriaOperatorType.LessThanOrEquals,
            IsEditorRequired = true,
        };

        public static CriteriaOperatorDto Like = new CriteriaOperatorDto()
        {
            Display = "Like",
            IsMultipleValuesAllowed = true,
            OperatorType = EmAppCriteriaOperatorType.Like,
            IsEditorRequired = true,
        };

        public static CriteriaOperatorDto NullOrEmpty = new CriteriaOperatorDto()
        {
            Display = "Empty",
            IsMultipleValuesAllowed = false,
            OperatorType = EmAppCriteriaOperatorType.NullOrEmpty,
            IsEditorRequired = false,
        };

        public static CriteriaOperatorDto NotNullOrEmpty = new CriteriaOperatorDto()
        {
            Display = "Not Empty",
            IsMultipleValuesAllowed = false,
            OperatorType = EmAppCriteriaOperatorType.NotNullOrEmpty,
            IsEditorRequired = false,
        };

        public static CriteriaOperatorDto StartWith = new CriteriaOperatorDto()
        {
            Display = "Start Width",
            IsMultipleValuesAllowed = true,
            OperatorType = EmAppCriteriaOperatorType.StartWith,
            IsEditorRequired = true,
        };

        public static CriteriaOperatorDto EndWith = new CriteriaOperatorDto()
        {
            Display = "End Width",
            IsMultipleValuesAllowed = true,
            OperatorType = EmAppCriteriaOperatorType.EndWith,
            IsEditorRequired = true,
        };

        public static CriteriaOperatorDto In = new CriteriaOperatorDto()
        {
            Display = "In",
            IsMultipleValuesAllowed = true,
            OperatorType = EmAppCriteriaOperatorType.In,
            IsEditorRequired = true,
        };


        public static CriteriaOperatorDto Between = new CriteriaOperatorDto()
        {
            Display = "Between",
            IsMultipleValuesAllowed = true,
            OperatorType = EmAppCriteriaOperatorType.Between,
            IsEditorRequired = true,
        };

        public static CriteriaOperatorDto NotEqual = new CriteriaOperatorDto()
        {
            Display = "<>",
            IsMultipleValuesAllowed = true,
            OperatorType = EmAppCriteriaOperatorType.NotEqual,
            IsEditorRequired = true,
        };
    }
}
