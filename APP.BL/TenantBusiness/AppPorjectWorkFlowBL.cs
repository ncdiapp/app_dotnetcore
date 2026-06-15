using System.Collections.Generic;
using System.Data;
using System.Linq;
using APP.LBL;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Globalization;
using APP.Framework.Validation;
using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;
using System;
using System.Text.RegularExpressions;

using APP.Framework;
namespace App.BL
{
    public class AppPorjectWorkFlowBL
    {

        // Condition Action Formula Help Methods:

        private static Regex FormulaRegex = new Regex(@"\[.+?\]");
        public static string[] FormulaConstString = { "(", ")", "=", "==", "!=", ">", "<", ">=", "<=", "+", "-", "*", "/", "%", "&&", "||", "&", "|", "true", "false", "!", "::", "," };
                                                    //".TotalMinutes", ".TotalHours", ".TotalDays", ".AddMinutes", ".AddHours", ".AddDays"};
        public static string TransactionFieldFormulaPrefix = "transactionfieldid_";
        public static string TransactionFieldUIPrefix = "Trans_";


        public static string TaskFieldPrefix = "TaskField_";
        internal static void InitialTransactionFieldFormularDisplayName(AppTransactionExDto transactionExDto, List<AppTransactionFieldExDto> transactionFieldList)
        {
            transactionFieldList.ForAll(o =>
            {
                var aUnit = transactionExDto.DictAllTransactionUnitIdExDto[o.TransactionUnitId.ToString()];
                if (aUnit.IsMasterSiblingUnit.HasValue && aUnit.IsMasterSiblingUnit.Value)
                {
                    o.FormulaDisplayName = "[" + TransactionFieldUIPrefix + transactionExDto.Id + "." + aUnit.DataBaseTableName + "." + o.DataBaseFieldName + "]";
                }
                else
                {
                    o.FormulaDisplayName = "[" + TransactionFieldUIPrefix + transactionExDto.Id + "." + o.DataBaseFieldName + "]";
                }

            });
        }


        public static AppProjectWorkFlowConditionExDto InFormatConditionFormulaExpress(List<AppTransactionFieldExDto> transactionFieldList, AppProjectWorkFlowConditionExDto aDto)
        {
            if (!string.IsNullOrEmpty(aDto.FormulaExpressionUI))
            {
                string expression = aDto.FormulaExpressionUI;
                expression = InFormatExpressionString(transactionFieldList, expression);
                aDto.FormulaExpression = expression;
            }
            return aDto;
        }

        public static AppProjectWorkFlowConditionExDto OutFormatConditionFormulaExpress(List<AppTransactionFieldExDto> transactionFieldList, AppProjectWorkFlowConditionExDto aDto)
        {
            if (!string.IsNullOrEmpty(aDto.FormulaExpression))
            {
                string expression = aDto.FormulaExpression;
                expression = OutFormatExpressionString(transactionFieldList, expression);
                aDto.FormulaExpressionUI = expression;
            }
            return aDto;
        }

        public static AppProjectWorkFlowActionExDto InFormatActionFormulaExpress(List<AppTransactionFieldExDto> transactionFieldList, AppProjectWorkFlowActionExDto aDto)
        {
            if (!string.IsNullOrEmpty(aDto.FormulaExpressionUI))
            {
                string expression = aDto.FormulaExpressionUI;
                expression = InFormatExpressionString(transactionFieldList, expression);
                aDto.FormulaExpression = expression;
            }
            return aDto;
        }

        public static AppProjectWorkFlowActionExDto OutFormatActionFormulaExpress(List<AppTransactionFieldExDto> transactionFieldList, AppProjectWorkFlowActionExDto aDto)
        {
            if (!string.IsNullOrEmpty(aDto.FormulaExpression))
            {
                if (aDto.FormulaExpression.Contains(AppTransactionFormulaBL.FormulaLineEnd))
                {
                    aDto.FormulaExpressionUI = string.Empty;
                    string[] formulaExpressionList = aDto.FormulaExpression.Split(AppTransactionFormulaBL.FormulaLineEnd.ToArray());
                    foreach (string aFormulaExpression in formulaExpressionList)
                    {
                        if (!string.IsNullOrWhiteSpace(aFormulaExpression))
                        {
                            string expression = OutFormatExpressionString(transactionFieldList, aFormulaExpression);
                            aDto.FormulaExpressionUI += expression + AppTransactionFormulaBL.FormulaLineEnd + "\n";
                        }
                    }                    
                }
                else
                {
                    string expression = aDto.FormulaExpression;
                    expression = OutFormatExpressionString(transactionFieldList, expression);
                    aDto.FormulaExpressionUI = expression;
                }
            }
            return aDto;
        }


        internal static string InFormatExpressionString(List<AppTransactionFieldExDto> transactionFieldList, string expression)
        {
            expression = expression.Replace("\n", " ");
            MatchCollection matchList = FormulaRegex.Matches(expression);
            for (int i = 0; i < matchList.Count; i++)
            {
                string fieldFormularDisplayName = matchList[i].Value.ToString().Trim();

                AppTransactionFieldExDto aAppTransactionFieldDto = transactionFieldList.FirstOrDefault(o => o.FormulaDisplayName.ToLower().Trim() == fieldFormularDisplayName.ToLower().Trim());


                if (aAppTransactionFieldDto != null)
                {
                    expression = expression.Replace(fieldFormularDisplayName, TransactionFieldFormulaPrefix + aAppTransactionFieldDto.Id.ToString());
                }

                else if (fieldFormularDisplayName.Contains(TaskFieldPrefix) && fieldFormularDisplayName.StartsWith("[") && fieldFormularDisplayName.EndsWith("]"))
                {
                    string taskName = fieldFormularDisplayName.Substring(1, fieldFormularDisplayName.Length - 2);
                    expression = expression.Replace(fieldFormularDisplayName, taskName);
                }

            }

            return expression;
        }
        internal static string OutFormatExpressionString(List<AppTransactionFieldExDto> transactionFieldList, string expression)
        {
            var members = expression.Split(FormulaConstString, StringSplitOptions.RemoveEmptyEntries);
            foreach (string info in members)
            {
                if (info.Trim().StartsWith(TransactionFieldFormulaPrefix))
                {
                    string id = info.Replace(TransactionFieldFormulaPrefix, "").Trim();

                    AppTransactionFieldExDto aAppTransactionFieldDto = transactionFieldList.FirstOrDefault(o => object.Equals(o.Id.ToString().Trim(), id.Trim()));
                    if (aAppTransactionFieldDto != null)
                    {
                        expression = expression.Replace(info, aAppTransactionFieldDto.FormulaDisplayName);
                    }


                }
                else if (info.Trim().StartsWith(TaskFieldPrefix))
                {
                    string taskFieldDispay = "[" + info.Trim() + "]";
                    expression = expression.Replace(info, taskFieldDispay);
                }
            }

            return expression;
        }




        internal static void InFormatWorkflowExDtoConditionAndActionFormularExpression(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto, List<AppTransactionFieldExDto> transactionFieldList)
        {
            if (aAppProjectOrWorkFlowExDto != null && transactionFieldList != null)
            {

                foreach (var conditionDto in aAppProjectOrWorkFlowExDto.AppProjectWorkFlowConditionList)
                {
                    InFormatConditionFormulaExpress(transactionFieldList, conditionDto);

                    foreach (var actionDto in conditionDto.AppProjectWorkFlowActionList)
                    {
                        InFormatActionFormulaExpress(transactionFieldList, actionDto);
                    }
                }

            }
        }

        internal static void OutFormatWorkflowEntityConditionAndActionFormularExpression(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto, List<AppTransactionFieldExDto> transactionFieldList)
        {
            if (aAppProjectOrWorkFlowExDto != null && transactionFieldList != null)
            {

                foreach (var conditionDto in aAppProjectOrWorkFlowExDto.AppProjectWorkFlowConditionList)
                {
                    OutFormatConditionFormulaExpress(transactionFieldList, conditionDto);

                    foreach (var actionDto in conditionDto.AppProjectWorkFlowActionList)
                    {
                        OutFormatActionFormulaExpress(transactionFieldList, actionDto);
                    }
                }

            }
        }




    }

}


