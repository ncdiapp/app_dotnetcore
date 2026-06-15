using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework;
using APP.Framework.Collections;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    public partial class AppTransactionUnitFormulaExDto
    {


        public static readonly string FormaulPrefix = "transactionfieldid_";

        private IList<DateTimeOperator> _DateTimeOperators = null;
        private string _BaseDateTime;

        public char[] DateOperators = { '+', '-' };

        public bool IsConditionalAssignMent
        {
            get
            {
                if (this.IsAssignment)
                {
                    if (this.ConditionFieldId.HasValue)
                    {
                        if (this.ConditionFieldId.Value != -1)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        public bool IsAssignment
        {
            get
            {
                string expression = this.FormulaExpression;

                for (int i = 1; i < expression.Length; i++)
                {
                    if (expression[i] == '='
                        && expression[i - 1] != '=' && expression[i - 1] != '!' && expression[i - 1] != '>' && expression[i - 1] != '<')
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public bool IsContainDateConst
        {
            get
            {
                string aFormulaExpression = this.FormulaExpression;

                return CheckIsDateFormula(aFormulaExpression);
            }
        }

        private static bool CheckIsDateFormula(string aFormulaExpression)
        {
            if (
                aFormulaExpression.Contains(EmAppDateTimeTokenType.Days.ToString()) ||
                aFormulaExpression.Contains(EmAppDateTimeTokenType.Hours.ToString()) ||
                aFormulaExpression.Contains(EmAppDateTimeTokenType.Minutes.ToString()) ||
                aFormulaExpression.Contains(EmAppDateTimeTokenType.Months.ToString()) ||
                aFormulaExpression.Contains(EmAppDateTimeTokenType.Now.ToString()) ||
                aFormulaExpression.Contains(EmAppDateTimeTokenType.Seconds.ToString()) ||
                aFormulaExpression.Contains(EmAppDateTimeTokenType.Today.ToString()) ||
                aFormulaExpression.Contains(EmAppDateTimeTokenType.Weeks.ToString()) ||
                aFormulaExpression.Contains(EmAppDateTimeTokenType.Null.ToString()) ||
                aFormulaExpression.Contains(EmAppDateTimeTokenType.Years.ToString())
                )
                return true;

            //foreach (BlockSubItem aBlockSubItem in this.Block.SubControlItem)
            //{
            //    string uDFDCUToken = BlockFormula.DCUFormaulPrefix + aBlockSubItem.SubItemID.ToString();
            //    if (aFormulaExpression.Contains(uDFDCUToken) && aBlockSubItem.ControlType.Value == ControlType.DATE.GetHashCode())
            //        return true;

            //}

            return false;
        }

        public IList<DateTimeOperator> DateTimeOperators
        {
            get
            {
                if (_DateTimeOperators == null)
                {
                    _DateTimeOperators = new List<DateTimeOperator>();
                    GetDateTimeOperators();
                }

                return _DateTimeOperators;
            }
        }

        private void GetDateTimeOperators()
        {
            char[] sep = { ' ' };

            if (this.RightSideExpression != null ||
                this.RightSideExpression != string.Empty)
            {
                string rightSideStr = this.RightSideExpression;
                int posOfFirstOp = rightSideStr.IndexOfAny(DateOperators);
                // bug
                if (posOfFirstOp != -1)
                {
                    string rightSideStrWithoutBase = rightSideStr.Substring(posOfFirstOp).Trim();
                    if (rightSideStrWithoutBase != string.Empty)
                    {
                        string[] operands = rightSideStrWithoutBase.Split(DateOperators, StringSplitOptions.RemoveEmptyEntries);
                        string[] operators = rightSideStrWithoutBase.Split(operands, StringSplitOptions.RemoveEmptyEntries);
                        if (operands.Length != operators.Length)
                        {
                            throw new Exception("Operands and operators does not match !");
                        }
                        for (int i = 0; i < operands.Length; i++)
                        {
                            //subitemid_4754 = [Today] - [5 Days]
                            string operandStr = operands[i].Replace("[", "").Replace("]", "").Trim();
                            string[] ops = operandStr.Split(sep, StringSplitOptions.RemoveEmptyEntries);

                            if (ops.Length >= 2)
                            {
                                string amount = ops[0];
                                string dateTimeUnit = ops[1];
                                string optor = operators[i];

                                DateTimeOperator aDateTimeOperator = new DateTimeOperator(optor, dateTimeUnit, amount);

                                _DateTimeOperators.Add(aDateTimeOperator);
                            }
                        }
                    }
                }
            }
        }

        public string BaseDateTime
        {
            get
            {
                if (_BaseDateTime == null || _BaseDateTime == string.Empty)
                {
                    GetBaseDateTime();
                }
                return _BaseDateTime;
            }
            set
            {
                _BaseDateTime = value.Trim();
            }
        }

        private void GetBaseDateTime()
        {
            if (this.RightSideExpression != null ||
                this.RightSideExpression != string.Empty)
            {
                string rightSideStr = this.RightSideExpression;

                string[] tokens = rightSideStr.Split(DateOperators, StringSplitOptions.RemoveEmptyEntries);

                if (tokens.Length > 0)
                {
                    string firstStr = tokens[0].Trim();
                    if (firstStr.StartsWith(FormaulPrefix))
                    {
                        int posOfUnderScore = firstStr.IndexOf("_");
                        _BaseDateTime = firstStr.Substring(posOfUnderScore + 1);
                    }
                    else
                    {
                        firstStr = firstStr.Replace("[", "").Replace("]", "");
                        _BaseDateTime = firstStr.Trim();
                    }
                }
            }
        }

        public string RightSideExpression
        {
            get
            {
                if (this.IsAssignment)
                {

                    string formelaExpress = this.FormulaExpression;
                    return GetrightSideEXpress(formelaExpress);
                }
                else if (IsContainDateConst)  // Datime vlidation string
                {
                    int indOfLogic = this.FormulaExpression.IndexOf("==");
                    if (indOfLogic != -1)
                        return this.FormulaExpression.Substring(indOfLogic + 2).Trim();

                    indOfLogic = this.FormulaExpression.IndexOf(">=");
                    if (indOfLogic != -1)
                        return this.FormulaExpression.Substring(indOfLogic + 2).Trim();

                    indOfLogic = this.FormulaExpression.IndexOf("<=");
                    if (indOfLogic != -1)
                        return this.FormulaExpression.Substring(indOfLogic + 2).Trim();

                    indOfLogic = this.FormulaExpression.IndexOf("<");
                    if (indOfLogic != -1)
                        return this.FormulaExpression.Substring(indOfLogic + 1).Trim();

                    indOfLogic = this.FormulaExpression.IndexOf(">");
                    if (indOfLogic != -1)
                        return this.FormulaExpression.Substring(indOfLogic + 1).Trim();
                }

                return string.Empty;
            }
        }

        private static string GetrightSideEXpress(string formelaExpress)
        {
            if (formelaExpress != null && formelaExpress != string.Empty)
            {
                int indOfEqual = formelaExpress.IndexOf("=");
                string rightString = formelaExpress.Substring(indOfEqual + 1).Trim();

                return rightString;
            }
            else
            {
                return string.Empty;
            }
        }

        // using same prefix Subitem for GridColumnID
        public int AssignmentLeftSideFieldId
        {
            get
            {
                if (this.IsAssignment)
                {
                    string formulaExpression = this.FormulaExpression;

                    return GetAssignmentLeftSideFieldId(formulaExpression);

                }
                else
                {
                    return -1;
                }
            }
        }

        public static int GetAssignmentLeftSideFieldId(string FormulaExpression)
        {

            if (!string.IsNullOrWhiteSpace(FormulaExpression))
            {
                int indOfEqual = FormulaExpression.IndexOf("=");
                string leftString = FormulaExpression.Substring(0, indOfEqual).Trim();
                int indOfUnderScore = leftString.IndexOf("_");
                string subItemID = leftString.Substring(indOfUnderScore + 1);
                try
                {
                    int subItemId = int.Parse(subItemID);
                    return subItemId;
                }
                catch
                {
                    return -1;
                }
            }
            else
            {
                return -1;
            }





        }


        public static int ConvertTransactionFieldExpressionToId(string transactionFieldExpression)
        {

            if (!string.IsNullOrWhiteSpace(transactionFieldExpression))
            {
                transactionFieldExpression = transactionFieldExpression.Trim();
                int indOfUnderScore = transactionFieldExpression.IndexOf("_");
                string subItemID = transactionFieldExpression.Substring(indOfUnderScore + 1);
                try
                {
                    int subItemId = int.Parse(subItemID);
                    return subItemId;
                }
                catch
                {
                    return -1;
                }
            }
            else
            {
                return -1;
            }





        }


        public string ValidationDateTimeOperator
        {
            get
            {
                if (IsContainDateConst)  // Datime vlidation string
                {
                    string ex = this.FormulaExpression;
                    //int indOfLogic = ex.Contains("==");
                    if (ex.Contains("=="))
                        return "==";

                    if (ex.Contains(">="))
                        return ">=";

                    if (ex.Contains("<="))
                        return "<=";

                    if (ex.Contains("<"))
                        return "<";

                    if (ex.Contains(">"))
                        return ">";
                }

                return string.Empty;
            }
        }

        public string ValidationLeftSideDatetimeString
        {
            get
            {
                if (this.IsContainDateConst)
                {
                    string aEx = this.FormulaExpression.Trim();
                    if (aEx.StartsWith(EmAppDateTimeTokenType.Today.ToString()))
                        return EmAppDateTimeTokenType.Today.ToString();
                    if (aEx.StartsWith(EmAppDateTimeTokenType.Now.ToString()))
                        return EmAppDateTimeTokenType.Now.ToString();

                    char[] indexofnay = { '=', '>', '<' };
                    string subitemprefix = aEx.Substring(0, aEx.IndexOfAny(indexofnay));
                    return subitemprefix.Substring(subitemprefix.IndexOf("_") + 1);
                }

                return string.Empty;
            }
        }

    }
}