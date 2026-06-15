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

    public partial class AppTransactionUnitFormulaDto
    {
        public ValidationResult ValidateDto(string transactionUnitName)
        {
            ValidationResult aValidationResult = new ValidationResult();

            CustomerValidateProperty(AppTransactionUnitFormulaDto.TransactionUnitIdProperty, aValidationResult, transactionUnitName);
            CustomerValidateProperty(AppTransactionUnitFormulaDto.CaculationFlowSortProperty, aValidationResult, transactionUnitName);
            CustomerValidateProperty(AppTransactionUnitFormulaDto.OperationTypeProperty, aValidationResult, transactionUnitName);

            CustomerValidateProperty(AppTransactionUnitFormulaDto.FormulaExpressionProperty, aValidationResult, transactionUnitName);
            CustomerValidateProperty(AppTransactionUnitFormulaDto.WarningMessageProperty, aValidationResult, transactionUnitName);

            return aValidationResult;
        }

        public void CustomerValidateProperty(string PropertyName, ValidationResult aValidationResult, string transactionUnitName)
        {
            string sort = string.Empty;
            if (CaculationFlowSort.HasValue)
            {
                sort = CaculationFlowSort.Value.ToString();
            }

            if (PropertyName == AppTransactionUnitFormulaDto.TransactionUnitIdProperty)
            {
                //if (!TransactionUnitId.HasValue)
                //{
                //    ValidationItem item = aValidationResult.AddItem(null, "App_TransactionUnitFormula_TransactionUnitIdIsEmpty", ValidationItemType.Error,
                //        "TransactionUnitId Is Empty, On Unit [{0}] Formular Sort [{1}].", PropertyName);
                //    item.MessageParameters.Add(transactionUnitName);
                //}
            }

            if (PropertyName == AppTransactionUnitFormulaDto.CaculationFlowSortProperty)
            {
                if (!CaculationFlowSort.HasValue)
                {
                    ValidationItem item = aValidationResult.AddItem(null, "App_TransactionUnitFormula_CaculationFlowSortIsEmpty", ValidationItemType.Error,
                        "Sort Is Empty On Unit [{0}].", PropertyName);
                    item.MessageParameters.Add(transactionUnitName);
                }
            }

            if (PropertyName == AppTransactionUnitFormulaDto.OperationTypeProperty)
            {
                if (!OperationType.HasValue && !ChildTransactionUnitId.HasValue)
                {
                    ValidationItem item = aValidationResult.AddItem(null, "App_TransactionUnitFormula_OperationTypeAndChildTransactionUnitIdAreBothEmpty", ValidationItemType.Error,
                        "Child Transaction Unit And Operation Type Are Both Empty, On Unit [{0}] Formular Sort [{1}].", PropertyName);
                    item.MessageParameters.Add(transactionUnitName);
                    item.MessageParameters.Add(sort);

                }

                //if (OperationType.HasValue && ChildTransactionUnitId.HasValue)
                //{
                //    ValidationItem item = aValidationResult.AddItem(null, "App_TransactionUnitFormula_ChildTransactionUnitIsSelectedOperationTypeAndExpressionWillNotBeUsedOnCalculation", ValidationItemType.Warning,
                //        "Child Transaction Unit Is Selected, Operation Type And Expression Will Not Be Used On Calculation, On Unit [{0}] Formular Sort [{1}].", PropertyName);
                //    item.MessageParameters.Add(transactionUnitName);
                //    item.MessageParameters.Add(sort);

                //}
            }

            if (PropertyName == AppTransactionUnitFormulaDto.FormulaExpressionProperty)
            {
                if (!ChildTransactionUnitId.HasValue && OperationType.HasValue && 
                    (OperationType.Value == (int)EmAppFormularType.Assignment || OperationType.Value == (int)EmAppFormularType.SqlScarlarAssignment))
                {
                    if (!string.IsNullOrEmpty(transactionUnitName))
                    {
                        if (string.IsNullOrEmpty(FormulaExpression) || string.IsNullOrEmpty(FormulaExpression.Trim()))
                        {
                            ValidationItem item = aValidationResult.AddItem(null, "App_TransactionUnitFormula_FormulaExpressionIsEmpty", ValidationItemType.Error,
                                "Expression Is Empty On Assignment Formula, On Unit [{0}] Formular Sort [{1}].", PropertyName);
                            item.MessageParameters.Add(transactionUnitName);
                            item.MessageParameters.Add(sort);
                        }
                        else
                        {
                            if (!IsValidFormulaExpression(FormulaExpression.Trim()))
                            {
                                ValidationItem item = aValidationResult.AddItem(null, "App_TransactionUnitFormula_FormulaExpressionIsInvalid", ValidationItemType.Error,
                                "Expression Is Invalid, On Unit [{0}] Formular Sort [{1}]. Expression: {2}", PropertyName);
                                item.MessageParameters.Add(transactionUnitName);
                                item.MessageParameters.Add(sort);
                                item.MessageParameters.Add(FormulaExpression);
                            }
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(FormulaExpression) && !IsValidFormulaExpression(FormulaExpression.Trim()))
                        {
                            ValidationItem item = aValidationResult.AddItem(null, "App_TransactionUnitFormula_FormulaExpressionIsInvalid", ValidationItemType.Error,
                            "Formular Is Invalid");                            
                        }
                    }
                }
            }

            if (PropertyName == AppTransactionUnitFormulaDto.WarningMessageProperty)
            {
                if (!ChildTransactionUnitId.HasValue &&
                    OperationType.HasValue && OperationType.Value == (int)EmAppFormularType.BooleanExpressionWarning &&
                    (string.IsNullOrEmpty(WarningMessage) || string.IsNullOrEmpty(WarningMessage.Trim()))
                    )
                {
                    ValidationItem item = aValidationResult.AddItem(null, "App_TransactionUnitFormula_WarningMessageIsEmpty", ValidationItemType.Error,
                        "Warning Message Is Empty On Boolean Expression Formula, On Unit [{0}] Formular Sort [{1}].", PropertyName);
                    item.MessageParameters.Add(transactionUnitName);
                    item.MessageParameters.Add(sort);
                }
            }

        }


        //Helper Methods

        public static bool IsValidFormulaExpression(string formulaExpression)
        {
            int leftParen = 0;
            int rightParen = 0;
            int leftBracket = 0;
            int rightBracket = 0;

            JudgeSign(ref leftParen, ref rightParen, ref leftBracket, ref rightBracket, formulaExpression);

            if ((leftParen != rightParen) || (leftBracket != rightBracket))
            {
                return false;
            }
            if (leftBracket == 0)
            {
                return false;
            }

            return true;
        }

        private static void JudgeSign(ref int leftParen, ref int rightParen, ref int leftBracket, ref int rightBracket, string formulaExpression)
        {
            for (int i = 0; i < formulaExpression.Length; i++)
            {
                if (formulaExpression[i] == '(')
                {
                    leftParen++;
                }
                if (formulaExpression[i] == ')')
                {
                    rightParen++;
                }
                if (formulaExpression[i] == '[')
                {
                    leftBracket++;
                }
                if (formulaExpression[i] == ']')
                {
                    rightBracket++;
                }
            }
        }
    }
}

