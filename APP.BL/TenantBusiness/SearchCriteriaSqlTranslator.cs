using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using APP.Components.Dto;
using APP.Components.EntityDto;
using Google.Protobuf.WellKnownTypes;
using System.Net;

namespace App.BL
{
    public class SearchCriteriaSqlTranslator
    {
        public SearchCriteriaSqlTranslator(string fieldName, SearchCriteriaDto criteria, bool IsSQlQueryFilter = true)
        {
            FieldName = fieldName;
            Criteria = criteria;
            OperatorFormat = GetOperatorFormat(Criteria.CriteriaOperator);
        }

        //Key: ParamterName ; Value
        Dictionary<string, object> dictParaName = new Dictionary<string, object>();

        private SearchCriteriaDto Criteria
        {
            get;
            set;
        }

        private string FieldName
        {
            get;
            set;
        }

        private string OperatorFormat
        {
            get;
            set;
        }

        private bool IsComputed
        {
            get;
            set;
        }

        private string ComputedCondition
        {
            get;
            set;
        }

        public string ComputeCondition()
        {
            if (!string.IsNullOrEmpty(OperatorFormat))
            {
                bool isNullEmpty =
                     (
                    Criteria.CriteriaOperator.OperatorType == EmAppCriteriaOperatorType.NotNullOrEmpty
                 || Criteria.CriteriaOperator.OperatorType == EmAppCriteriaOperatorType.NotNull
                 || Criteria.CriteriaOperator.OperatorType == EmAppCriteriaOperatorType.Null
                 || Criteria.CriteriaOperator.OperatorType == EmAppCriteriaOperatorType.NullOrEmpty);

                if (isNullEmpty)
                {
                    if (Criteria.CriteriaType == EmAppCriteriaType.Entity)
                    {
                        string format = string.Format(OperatorFormat, FieldName);

                        if (Criteria.CriteriaOperator.OperatorType == EmAppCriteriaOperatorType.NotNullOrEmpty)
                        {
                            return format + string.Format(@" AND {0} <> -1", FieldName);
                        }

                        if (Criteria.CriteriaOperator.OperatorType == EmAppCriteriaOperatorType.Null)
                        {
                            return "(" + format + string.Format(@" OR {0} = -1", FieldName) + ")";
                        }

                        if (Criteria.CriteriaOperator.OperatorType == EmAppCriteriaOperatorType.NotNull)
                        {
                            return format + string.Format(@" AND {0} <> -1", FieldName);
                        }

                        if (Criteria.CriteriaOperator.OperatorType == EmAppCriteriaOperatorType.NullOrEmpty)
                        {
                            return "(" + format + string.Format(@" OR {0} = -1", FieldName) + ")";
                        }
                    }
                    else
                    {
                        return string.Format(OperatorFormat, FieldName);
                    }
                }

            }

            if (
                string.IsNullOrEmpty(OperatorFormat)
                || Criteria.Values == null
                || Criteria.Values.Count <= 0
                || Criteria.Values.Cast<object>().All(o => o == null)
                )

            {
                ComputedCondition = string.Empty;
            }
            else if (!Criteria.CriteriaOperator.IsEditorRequired)
            {
                ComputedCondition = string.Format(OperatorFormat, FieldName);
            }
            else if (Criteria.CriteriaOperator.OperatorType == EmAppCriteriaOperatorType.In)
            {
                ComputedCondition = ComputeInClause();
            }

            else if (Criteria.CriteriaOperator.OperatorType == EmAppCriteriaOperatorType.Between)
            {
                ComputedCondition = ComputeBetweenClause();
            }
            else
            {
                ComputedCondition = ComputeDefaultClause();
            }

            IsComputed = true;
            return ComputedCondition;
        }

        public string ComputeDataTableSelectCondition()
        {
            if (!string.IsNullOrEmpty(OperatorFormat))
            {
                bool isNullEmpty =
                     (
                    Criteria.CriteriaOperator.OperatorType == EmAppCriteriaOperatorType.NotNullOrEmpty
                 || Criteria.CriteriaOperator.OperatorType == EmAppCriteriaOperatorType.NotNull
                 || Criteria.CriteriaOperator.OperatorType == EmAppCriteriaOperatorType.Null
                 || Criteria.CriteriaOperator.OperatorType == EmAppCriteriaOperatorType.NullOrEmpty);

                if (isNullEmpty)
                {
                    if (Criteria.CriteriaType == EmAppCriteriaType.Entity)
                    {
                        string format = string.Format(OperatorFormat, FieldName);

                        if (Criteria.CriteriaOperator.OperatorType == EmAppCriteriaOperatorType.NotNullOrEmpty)
                        {
                            return format + string.Format(@" AND {0} <> -1", FieldName);
                        }

                        if (Criteria.CriteriaOperator.OperatorType == EmAppCriteriaOperatorType.Null)
                        {
                            return format + string.Format(@" OR {0} = -1", FieldName);
                        }

                        if (Criteria.CriteriaOperator.OperatorType == EmAppCriteriaOperatorType.NotNull)
                        {
                            return format + string.Format(@" AND {0} <> -1", FieldName);
                        }

                        if (Criteria.CriteriaOperator.OperatorType == EmAppCriteriaOperatorType.NullOrEmpty)
                        {
                            return format + string.Format(@" OR {0} = -1", FieldName);
                        }
                    }
                    else
                    {
                        return string.Format(OperatorFormat, FieldName);
                    }
                }
            }

            if (string.IsNullOrEmpty(OperatorFormat)
                || Criteria.Values == null
                || Criteria.Values.Count <= 0
                || Criteria.Values.Cast<object>().All(o => o == null))


            {
                ComputedCondition = string.Empty;
            }
            else if (!Criteria.CriteriaOperator.IsEditorRequired)
            {
                ComputedCondition = string.Format(OperatorFormat, FieldName);
            }
            else if (Criteria.CriteriaOperator.OperatorType == EmAppCriteriaOperatorType.In)
            {
                ComputedCondition = ComputeInClause();
            }

            else if (Criteria.CriteriaOperator.OperatorType == EmAppCriteriaOperatorType.Between)
            {
                ComputedCondition = ComputeBetweenClause();
            }

            else
            {
                ComputedCondition = ComputeDataTableDefaultClause();
            }

            IsComputed = true;
            return ComputedCondition;
        }



        private string ComputeDefaultClause()
        {
            StringBuilder clause = new StringBuilder();

            clause.Append("(");

            if (Criteria.CriteriaOperator != null && Criteria.CriteriaOperator.OperatorType.HasValue
               && Criteria.CriteriaOperator.OperatorType == EmAppCriteriaOperatorType.Like && Criteria.Values.Count == 1)
            {
                string criteriaTextValue = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(Criteria.Values.First());

                string decodedValue = WebUtility.UrlDecode(criteriaTextValue);                

                bool isFirst = true;

                foreach (object value in decodedValue.Split(' ').Where(o => !string.IsNullOrEmpty(o)))
                {
                    string sqlValue = GetDataTableSqlValue(Criteria.CriteriaType, value, Criteria.CriteriaOperator.OperatorType);

                    if (!string.IsNullOrWhiteSpace(sqlValue))
                    {
                        if (!isFirst)
                        {
                            clause.Append(" AND ");
                        }

                        clause.AppendFormat(ComputeOperatorFormatForOneValue(sqlValue));

                        isFirst = false;
                    }
                }
            }
            else
            {
                bool isFirst = true;

                foreach (object value in Criteria.Values)
                {
                    string sqlValue = GetDataTableSqlValue(Criteria.CriteriaType, value, Criteria.CriteriaOperator.OperatorType);

                    if (sqlValue != null)
                    {
                        //Decimal outInt;
                        //if ( ! Decimal.TryParse(sqlValue, out outInt))
                        //{
                        //    sqlValue = $"'{sqlValue}'";
                        //}

                        if (!isFirst)
                        {
                            clause.Append(" OR ");
                        }

                        clause.AppendFormat(ComputeOperatorFormatForOneValue(sqlValue));

                        isFirst = false;
                    }
                }
            }

            clause.Append(")");

            if (clause.Length <= 2)
            {
                return string.Empty;
            }

            return clause.ToString();
        }

        private string ComputeDataTableDefaultClause()
        {
            StringBuilder clause = new StringBuilder();

            //clause.Append("(");

            if (Criteria.CriteriaOperator != null && Criteria.CriteriaOperator.OperatorType.HasValue
                && Criteria.CriteriaOperator.OperatorType == EmAppCriteriaOperatorType.Like && Criteria.Values.Count == 1)
            {
                string criteriaTextValue = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(Criteria.Values.First());

                string decodedValue = WebUtility.UrlDecode(criteriaTextValue);

                bool isFirst = true;

                foreach (object value in decodedValue.Split(' ').Where(o => !string.IsNullOrEmpty(o)))
                {
                    string sqlValue = GetDataTableSqlValue(Criteria.CriteriaType, value, Criteria.CriteriaOperator.OperatorType);

                    if (!string.IsNullOrWhiteSpace(sqlValue))
                    {
                        if (!isFirst)
                        {
                            clause.Append(" AND ");
                        }

                        clause.AppendFormat(ComputeOperatorFormatForOneValue(sqlValue));

                        isFirst = false;
                    }
                }
            }
            else
            {
                bool isFirst = true;

                foreach (object value in Criteria.Values)
                {
                    string sqlValue = GetDataTableSqlValue(Criteria.CriteriaType, value, Criteria.CriteriaOperator.OperatorType);

                    if (sqlValue != null)
                    {
                        if (!isFirst)
                        {
                            clause.Append(" OR ");
                        }

                        clause.AppendFormat(ComputeOperatorFormatForOneValue(sqlValue));

                        isFirst = false;
                    }
                }

                //  clause.Append(")");

                if (clause.Length <= 2)
                {
                    return string.Empty;
                }

            }


            return clause.ToString();
        }

        private string ComputeInClause()
        {
            StringBuilder inClause = new StringBuilder();

            foreach (object value in Criteria.Values)
            {
                string sqlValue = GetDataTableSqlValue(Criteria.CriteriaType, value, Criteria.CriteriaOperator.OperatorType);

                if (sqlValue != null)
                {
                    inClause.AppendFormat("{0},", sqlValue);
                }
            }

            if (inClause.Length <= 0)
            {
                return null;
            }

            string finalInClause = inClause.ToString().Substring(0, inClause.Length - 1);

            return ComputeOperatorFormatForOneValue(finalInClause);
        }

        private string ComputeBetweenClause()
        {
            if (Criteria.Values.Count < 2)
                return string.Empty;

            object value1 = Criteria.Values[0];
            object value2 = Criteria.Values[1];

            value1 = GetDataTableSqlValue(Criteria.CriteriaType, value1, null);
            value2 = GetDataTableSqlValue(Criteria.CriteriaType, value2, null);

            if (value1 == null || value2 == null || string.IsNullOrEmpty(value1.ToString()) || string.IsNullOrEmpty(value2.ToString()))
                return string.Empty;

            return ComputeOperatorFormatForTwoValue(value1.ToString(), value2.ToString());
        }

        private string ComputeOperatorFormatForOneValue(string value)
        {
            return string.Format(OperatorFormat, FieldName, value);
        }

        private string ComputeOperatorFormatForTwoValue(string value1, string value2)
        {
            return string.Format(OperatorFormat, FieldName, value1, value2);
        }

        private static string GetOperatorFormat(CriteriaOperatorDto criteriaOperator)
        {
            if (criteriaOperator != null && criteriaOperator.OperatorType.HasValue)
            {
                switch (criteriaOperator.OperatorType)
                {
                    case EmAppCriteriaOperatorType.Null:
                        return "{0} IS NULL";
                    case EmAppCriteriaOperatorType.NotNull:
                        return "{0} IS NOT NULL";
                    case EmAppCriteriaOperatorType.GreaterThan:
                        return "{0} > {1}";
                    case EmAppCriteriaOperatorType.GreaterThanOrEquals:
                        return "{0} >= {1}";
                    case EmAppCriteriaOperatorType.LessThan:
                        return "{0} < {1}";
                    case EmAppCriteriaOperatorType.LessThanOrEquals:
                        return "{0} <= {1}";
                    case EmAppCriteriaOperatorType.Like:
                        return "{0} Like '%{1}%'";
                    case EmAppCriteriaOperatorType.NullOrEmpty:
                        return "({0}='' OR {0} IS NULL)";
                    case EmAppCriteriaOperatorType.NotNullOrEmpty:
                        return "({0} <> '' AND {0} IS NOT NULL)";
                    case EmAppCriteriaOperatorType.StartWith:
                        return "{0} LIKE '%{1}'";
                    case EmAppCriteriaOperatorType.EndWith:
                        return "{0} LIKE '{1}%'";
                    case EmAppCriteriaOperatorType.In:
                        return "{0} IN ({1})";
                    case EmAppCriteriaOperatorType.Between:
                        return " {0} between {1} and {2}  ";
                    case EmAppCriteriaOperatorType.NotEqual:
                        return "({0} <> {1} OR {0} IS NULL)";
                    case EmAppCriteriaOperatorType.Equals:
                    default:
                        return "{0}={1}";
                }
            }

            return string.Empty;
        }

        private string GetDataTableSqlValue(EmAppCriteriaType criteriaType, object value, EmAppCriteriaOperatorType? aOperatorType)
        {
            if (value == null
                || (criteriaType == EmAppCriteriaType.Boolean
                    && value is int))
            {
                return string.Empty;
            }

            switch (criteriaType)
            {
                case EmAppCriteriaType.Entity:
                    // return ((LookupItemDto)value).Id.ToString();
                    {
                        decimal decValue; // it is numeric
                        if (decimal.TryParse(value.ToString(), out decValue))
                        {
                            return value.ToString();
                        }
                        else // it i sstring
                        {
                            return $@"'{value}'";
                        }


                    }

                case EmAppCriteriaType.Numeric:
                    return value.ToString();
                case EmAppCriteriaType.FolderTree:
                    return value.ToString();
                case EmAppCriteriaType.Boolean:
                    {
                        if ((bool)value)
                        {
                            return "'true'";
                        }
                        else
                        {
                            return "'false'";
                        }
                    }
                case EmAppCriteriaType.Date:
                    return "'" + ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss.fff") + "'";
                case EmAppCriteriaType.Text:
                    value = value.ToString().Replace("'", "''");
                    if (
                         aOperatorType == EmAppCriteriaOperatorType.Like
                         || aOperatorType == EmAppCriteriaOperatorType.StartWith
                         || aOperatorType == EmAppCriteriaOperatorType.EndWith

                        )
                    {
                        return value.ToString();
                    }
                    else
                    {
                        return "'" + value + "'";
                    }


                default:
                    return "'" + value + "'";
            }
        }
    }
}