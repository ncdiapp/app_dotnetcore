using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace APP.Framework
{
    #region -------- DBError

    public enum DBError
    {
        AllOk
       
    }

    #endregion -------- DBError

    #region -------- ICommonDBAccess Interface

 

    #endregion -------- ICommonDBAccess Interface

    public partial class DBInteractionBase : IDisposable
    {
        #region -------- Class Member Declarations

        //  [NonSerialized]
        public SqlConnection _mainConnection;

      
        protected Int32 _errorCode;
  
        private bool _isDisposed;
        private ArrayList _mixDetails;

        private SortedList _searchLikeFields = new SortedList();
        private string _searchExtraClause = " ";
        private string _searchOrderByFields = " ";
        private string[] _requiredFields;

        [NonSerialized]
        private IList<SqlParameter> _Sqlparams = new List<SqlParameter>();

        [NonSerialized]
        private IList<SqlBetweenAnd> _SqlBetweenAnds = new List<SqlBetweenAnd>();

        [NonSerialized]
        private IList<SqlUnitaryPredict> _SqlUnitaryPredict = new List<SqlUnitaryPredict>();

        private bool _isDistinct = false;

        public static readonly int NewObjectID = -1;
        public static readonly int NoExistObjectID = -1;
       // private string ConstTableName = "TableName";

        private DataTable _AllTable;

        private ArrayList _AllEntity;

        public string NewLine = " \\n";

        private string _CountFiled = " * ";


        #endregion -------- Class Member Declarations

        #region -------- Class Property Declarations


        public Int32 ErrorCode
        {
            get
            {
                return _errorCode;
            }
        }

     

        public string DBServerName
        {
            get
            {
                return _mainConnection.DataSource;
            }
        }

        public string DBMSName
        {
            get
            {
                return _mainConnection.Database;
            }
        }

        public ArrayList MixItems
        {
            get
            {
                if (_mixDetails == null)
                    _mixDetails = new ArrayList();
                return _mixDetails;
            }
        }

        public SortedList SearchLikeFields
        {
            get
            {
                return _searchLikeFields;
            }
        }

        public string SearchExtraClause
        {
            get
            {
                return _searchExtraClause;
            }
            set
            {
                _searchExtraClause = value;
            }
        }

        public string SearchOrderByFields
        {
            get
            {
                return _searchOrderByFields;
            }
            set
            {
                _searchOrderByFields = value;
            }
        }

        public string[] SearchRequiredFields
        {
            get { return _requiredFields; }
            set { _requiredFields = value; }
        }

        public IList<SqlParameter> Sqlparams
        {
            get
            {
                return _Sqlparams;
            }
        }

        public IList<SqlBetweenAnd> SqlBetweenAnds
        {
            get
            {
                return _SqlBetweenAnds;
            }
        }

        public IList<SqlUnitaryPredict> SqlUnitaryPredicts
        {
            get
            {
                return _SqlUnitaryPredict;
            }
        }

        public string CountFiled
        {
            get
            {
                return _CountFiled;
            }
            set
            {
                _CountFiled = value;
            }
        }

        public string FullDBOName
        {
            get
            {
                return "[" + DBServerName + "].[" + DBMSName + "].[dbo].";
            }
        }

        public bool IsDistinct
        {
            get
            {
                return _isDistinct;
            }
            set
            {
                _isDistinct = value;
            }
        }

        public virtual DataTable AllTable
        {
            get
            {
                if (_AllTable == null)
                {
                    _AllTable = this.SelectAll();
                }
                return _AllTable;
            }
        }

        public ArrayList AllEntity
        {
            get
            {
                if (_AllEntity == null)
                {
                    _AllEntity = new ArrayList();

                    System.Type aType = this.GetType();
                    MethodInfo aMethodInfo = aType.GetMethod("getFld_Name");

                    string sortBy = aMethodInfo.Invoke(this, null).ToString();

                    foreach (DataRow aRow in AllTable.Select("", sortBy))
                    {
                        _AllEntity.Add(ConvertDataRowToObject(aRow));
                    }
                }
                return _AllEntity;
            }
        }


        public static string GenerateColumnInClauseWithAndCondition(int[] ids, string IDColumnName, bool isInsertAnd)
        {
            string inclause = string.Empty;

            if (ids != null)
            {
                foreach (int pid in ids)
                {
                    inclause += pid + ",";
                }
            }

            if (inclause != string.Empty)
            {
                inclause = inclause.Substring(0, inclause.Length - 1);
                if (isInsertAnd)
                {
                    inclause = "  and  " + IDColumnName + " in ( " + inclause + " ) ";
                }
                else
                {
                    inclause = "  " + IDColumnName + " in ( " + inclause + " ) ";
                }
            }
            return inclause;
        }

        // Implement Ientity Interface

        public virtual ArrayList getAllArrayList()
        {
            return AllEntity;
        }

        public virtual DataTable getAllList()
        {
            return AllTable;
        }

        public virtual SqlInt32 getMappUserDefineEnityValueID()
        {
            return SqlInt32.Null;
        }

        #endregion -------- Class Property Declarations

        public DBInteractionBase(string connectionString )
        {
            // Initialize the class' members.

            //if (string.IsNullOrEmpty(connectionString))
            //{
            //    connectionString = DBInteractionBase.APPConnectionString;
            //}
            _mainConnection = new SqlConnection();
            _mainConnection.ConnectionString = connectionString;
            _errorCode = (int)DBError.AllOk;
            _isDisposed = false;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        public static System.Data.DataTable GetDataTableQueryResult(SqlConnection conn, string queryString, List<SqlParameter> listParamters = null)
        {
            SqlCommand cmd = new SqlCommand(queryString, conn);

            if (listParamters != null)
            {
                foreach (var parameter in listParamters)
                {
                    cmd.Parameters.Add(parameter);
                }
            }

            SqlDataAdapter adapter = new SqlDataAdapter(cmd);
            System.Data.DataTable resultTabel = new DataTable();
            adapter.Fill(resultTabel);

            // need to update FKEntity Name

            return resultTabel;




        }


        #region -------- Customer Search Engine

        // DataTable First Column as In Value

        public void ClearQueryParameters()
        {
            this.Sqlparams.Clear();
        }

        // only for single entity
        public DataTable SearchDBPageDataTableWithAllFields(string keyColumn, int pageIndex, int pagerSize, out int totalRows, string aTableName)
        {
            string whereClause = SetupWhereClause();

            totalRows = Convert.ToInt32(this.RetriveSigleValue(" select count (" + keyColumn + ")" + " from " + aTableName + whereClause));

            int firstRowNum = (pageIndex - 1) * pagerSize + 1;

            string maxIDValue = string.Empty;

            if (firstRowNum <= totalRows)
            {
                StringBuilder aQuery = new StringBuilder();
                aQuery.Append(@" declare @maxIDValue  varchar (300) ");
                aQuery.Append(@"set rowcount " + firstRowNum);
                aQuery.Append(" select @maxIDValue  = " + keyColumn + " from " + aTableName + whereClause + " order by  " + keyColumn);
                aQuery.Append(" select @maxIDValue  ");
                maxIDValue = Convert.ToString(this.RetriveSigleValue(aQuery.ToString()));

                aQuery = new StringBuilder();
                aQuery.Append(@"set rowcount " + pagerSize);
                aQuery.Append(" select *  from " + aTableName + " where " + keyColumn + " >='" + maxIDValue + "'" + whereClause + " order by  " + keyColumn);
                return this.RetriveDataTable(aQuery.ToString());
            }

            return null;
        }


        public string SetupRequiredFieldsSqlQuery(string TableNameField)
        {
            StringBuilder aQuery = new StringBuilder();

            if (this.IsDistinct)
            {
                aQuery.Append(SqlQuery.SelectDistinct);
            }
            else
            {
                aQuery.Append(SqlQuery.SELECT);
            }

            foreach (string aField in SearchRequiredFields)
            {
                aQuery.Append("[" + aField + "]");
                aQuery.Append(',');
            }

            aQuery.Remove(aQuery.Length - 1, 1);

            aQuery.Append(SqlQuery.FROM);
            aQuery.Append(TableNameField);

            aQuery.Append(SetupWhereClause());

            if (SearchOrderByFields.Trim().Length > 0)
                aQuery.Append(SqlQuery.ORDER_BY + SearchOrderByFields);

            return aQuery.ToString();
        }

        // need count Query after set up properties
        public string SetupSqlCountQuery(string countField, string TableNameField)
        {
            if (countField == null || countField == string.Empty)
            {
                countField = "*";
            }
            if (countField != "*")
            {
                countField = " [" + countField + "]";
            }

            StringBuilder aQuery = new StringBuilder();
            if (this.IsDistinct)
            {
                aQuery.Append(SqlQuery.SelectDistinct + @" count ( " + countField + " )");
            }
            else
            {
                aQuery.Append(SqlQuery.SELECT + @" count ( " + countField + " )");
            }

            aQuery.Append(SqlQuery.FROM);
            aQuery.Append(TableNameField);

            aQuery.Append(SetupWhereClause());

            if (SearchOrderByFields.Trim().Length > 0)
                aQuery.Append(SqlQuery.ORDER_BY + SearchOrderByFields);

            return aQuery.ToString();
        }

        //sqlOperator  = > < <> != like

      //  static string ConstSpace3 = "   ";

        public static string CreatePredicateFiled(string fileName, string sqlPredicate, string filedValue)
        {
            if (filedValue == string.Empty)
                return string.Empty;

            else if (sqlPredicate == SqlQuery.LIKE)
            {
                return fileName + SqlQuery.LIKE + "'%" + filedValue + "%'";
            }
            else
            {
                return fileName + sqlPredicate + "'" + filedValue + "'";
            }
        }

        private string SetupWhereClause()
        {
            StringBuilder aQuery = new StringBuilder();

            // Like property

            int likeCount = 0;
            foreach (DictionaryEntry aLikeField in SearchLikeFields)
            {
                // old method
                //string strValue = aLikeField.Value.ToString().Replace("'", "''");
                //aQuery.Append(aLikeField.Key + " LIKE '%" + strValue + "%' " + SqlQuery.Condition.AND);

                string likeParame = "@sortLike" + likeCount;
                string aLikeFileValue = "%" + aLikeField.Value.ToString() + "%";

                aQuery.Append(" [" + aLikeField.Key + "] " + SqlQuery.LIKE + likeParame + SqlQuery.Condition.AND);
                Sqlparams.Add(new SqlParameter(likeParame, aLikeFileValue));
                likeCount++;
            }

            //check  Between and clause

            int beTweenKey = 0;
            foreach (SqlBetweenAnd aSqlBetweenAnd in this.SqlBetweenAnds)
            {
                //and createddate between '2005-04-07' and  '2007-05-07'
                if (!string.IsNullOrEmpty(aSqlBetweenAnd.BeteenAndFiled))
                {
                    if (aSqlBetweenAnd.BetweenValue != null && aSqlBetweenAnd.AndValue != null)
                    {
                        aQuery.Append(" [" + aSqlBetweenAnd.BeteenAndFiled + "] " + SqlQuery.BETWEEN + "@dynBet" + beTweenKey + SqlQuery.AND + "@dynAnd" + beTweenKey + SqlQuery.Condition.AND);
                        Sqlparams.Add(new SqlParameter("@dynBet" + beTweenKey, aSqlBetweenAnd.BetweenValue));
                        Sqlparams.Add(new SqlParameter("@dynAnd" + beTweenKey, aSqlBetweenAnd.AndValue));
                    }
                }

                beTweenKey++;
            }

            int cnt = 0;
            foreach (SqlUnitaryPredict aSqlUnitaryPredict in this.SqlUnitaryPredicts)
            {
                string predictFiled = " [" + aSqlUnitaryPredict.UnitaryOperFiled + "]";
                switch (aSqlUnitaryPredict.EmSqlUnitaryOperator)
                {
                    case EmSqlUnitaryOperator.EQUAL:
                        aQuery.Append(predictFiled + SqlQuery.EQUAL + "@Uni" + cnt + SqlQuery.Condition.AND);
                        Sqlparams.Add(new SqlParameter("@Uni" + cnt, aSqlUnitaryPredict.UValue));
                        break;

                    case EmSqlUnitaryOperator.GREAT:
                        aQuery.Append(predictFiled + SqlQuery.GREAT + "@Uni" + cnt + SqlQuery.Condition.AND);
                        Sqlparams.Add(new SqlParameter("@Uni" + cnt, aSqlUnitaryPredict.UValue));
                        break;

                    case EmSqlUnitaryOperator.GREAT_EQUAL:
                        aQuery.Append(predictFiled + SqlQuery.GREAT_EQUAL + "@Uni" + cnt + SqlQuery.Condition.AND);
                        Sqlparams.Add(new SqlParameter("@Uni" + cnt, aSqlUnitaryPredict.UValue));
                        break;

                    case EmSqlUnitaryOperator.IS_NOT_NULL:
                        aQuery.Append(predictFiled + SqlQuery.IS_NOT_NULL + SqlQuery.Condition.AND);
                        break;

                    case EmSqlUnitaryOperator.IS_NULL:
                        aQuery.Append(predictFiled + SqlQuery.IS_NULL + SqlQuery.Condition.AND);
                        break;

                    case EmSqlUnitaryOperator.LESS:
                        aQuery.Append(predictFiled + SqlQuery.LESS + "@Uni" + cnt + SqlQuery.Condition.AND);
                        Sqlparams.Add(new SqlParameter("@Uni" + cnt, aSqlUnitaryPredict.UValue));
                        break;

                    case EmSqlUnitaryOperator.LESS_EQUAL:
                        aQuery.Append(predictFiled + SqlQuery.LESS_EQUAL + "@Uni" + cnt + SqlQuery.Condition.AND);
                        Sqlparams.Add(new SqlParameter("@Uni" + cnt, aSqlUnitaryPredict.UValue));
                        break;

                    case EmSqlUnitaryOperator.LIKE:
                        string aLikeFileValue = "%" + aSqlUnitaryPredict.UValue.ToString() + "%";
                        aQuery.Append(predictFiled + SqlQuery.LIKE + "@Uni" + cnt + SqlQuery.Condition.AND);
                        Sqlparams.Add(new SqlParameter("@Uni" + cnt, aLikeFileValue));
                        break;

                    case EmSqlUnitaryOperator.NOT_EQUAL:
                        aQuery.Append(predictFiled + SqlQuery.NOT_EQUAL + "@Uni" + cnt + SqlQuery.Condition.AND);
                        Sqlparams.Add(new SqlParameter("@Uni" + cnt, aSqlUnitaryPredict.UValue));
                        break;

                    default:
                        break;
                }

                cnt++;
            }

            // Extra Clause Property
            if (SearchExtraClause.Trim().Length > 0)
                aQuery.Append(SearchExtraClause + SqlQuery.Condition.AND);

            // Trim last AND
            if (aQuery.Length > 0)
            {
                aQuery.Remove(aQuery.Length - SqlQuery.AND.Length, SqlQuery.AND.Length - 1);
            }

            // Add Where Clause
            if (aQuery.Length > 0)
                aQuery.Insert(0, SqlQuery.WHERE);

            return aQuery.ToString();
        }

       

        #endregion -------- Customer Search Engine

        #region -------- Retreive   By  dbmsQuery

        private void SetupSqlCommandParameter(SqlCommand cmdToExecute)
        {
            foreach (SqlParameter aPameter in this.Sqlparams)
                cmdToExecute.Parameters.Add(aPameter);
        }

        public virtual ArrayList RetriveMutilValues(string dbmsQuery)
        {
            ArrayList objects = new ArrayList();

            SqlCommand cmdToExecute = new SqlCommand();
            SetupSqlCommandParameter(cmdToExecute);

            cmdToExecute.CommandText = dbmsQuery;
            cmdToExecute.CommandType = CommandType.Text;
            DataTable toReturn = new DataTable();
            SqlDataAdapter adapter = new SqlDataAdapter(cmdToExecute);

            // Use base class' connection object
            cmdToExecute.Connection = _mainConnection;

            try
            {
                _mainConnection.Open();



                // Execute query.
                adapter.Fill(toReturn);

                if (toReturn.Rows.Count > 0)
                {
                    foreach (DataRow aRow in toReturn.Rows)
                    {
                        objects.Add(aRow[0]);
                    }
                }

                return objects;
            }
            catch (Exception ex)
            {
                // some error occured. Bubble it to caller and encapsulate Exception object
                throw new Exception("RetriveMutilObjects::" + dbmsQuery + "::Error occured.", ex);
            }
            finally
            {

                // Close connection.
                _mainConnection.Close();

                cmdToExecute.Dispose();
                adapter.Dispose();
            }
        }

        public virtual DataRow RetrieveOneDataRow(string dbmsQuery)
        {
            SqlCommand cmdToExecute = new SqlCommand();
            SetupSqlCommandParameter(cmdToExecute);

            cmdToExecute.CommandText = dbmsQuery;
            cmdToExecute.CommandType = CommandType.Text;
            DataTable toReturn = new DataTable();
            SqlDataAdapter adapter = new SqlDataAdapter(cmdToExecute);

            // Use base class' connection object
            cmdToExecute.Connection = _mainConnection;

            try
            {


                _mainConnection.Open();
                adapter.Fill(toReturn);
                if (toReturn.Rows.Count > 0)
                {
                    return toReturn.Rows[0];
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                // some error occured. Bubble it to caller and encapsulate Exception object
                throw new Exception("RetriveOneObject::" + dbmsQuery + "::Error occured.", ex);
            }
            finally
            {

                // Close connection.
                _mainConnection.Close();

                cmdToExecute.Dispose();
                adapter.Dispose();
            }
        }

        public virtual DataTable RetriveDataTable(string dbmsQuery)
        {
            SqlCommand cmdToExecute = new SqlCommand();
            SetupSqlCommandParameter(cmdToExecute);

            cmdToExecute.CommandText = dbmsQuery;
            cmdToExecute.CommandType = CommandType.Text;
            DataTable toReturn = new DataTable();
            SqlDataAdapter adapter = new SqlDataAdapter(cmdToExecute);
            cmdToExecute.CommandTimeout = 120;


            // Use base class' connection object
            cmdToExecute.Connection = _mainConnection;

            try
            {
                _mainConnection.Open();
                adapter.Fill(toReturn);

                return toReturn;
            }
            catch (Exception ex)
            {
                // some error occured. Bubble it to caller and encapsulate Exception object
                throw new Exception("RetriveDataTable::" + dbmsQuery + "::Error occured.", ex);
            }
            finally
            {

                // Close connection.
                _mainConnection.Close();

                cmdToExecute.Dispose();
                adapter.Dispose();
            }
        }

        public virtual object RetriveSigleValue(string dbmsQuery)
        {
            SqlCommand cmdToExecute = new SqlCommand();
            SetupSqlCommandParameter(cmdToExecute);

            cmdToExecute.CommandText = dbmsQuery;
            cmdToExecute.CommandType = CommandType.Text;
            cmdToExecute.Connection = _mainConnection;

            try
            {
                _mainConnection.Open();

                // Execute query.
                return cmdToExecute.ExecuteScalar();
            }
            catch (Exception ex)
            {
                // some error occured. Bubble it to caller and encapsulate Exception object
                throw new Exception("RetriveSigleValue::" + dbmsQuery + "::Error occured.", ex);
            }
            finally
            {

                _mainConnection.Close();

                cmdToExecute.Dispose();
            }
        }

        #endregion -------- Retreive   By  dbmsQuery

        #region ---- old page retrive, will be drop

        // only for samll DataTabe page  index start from 1 ;
        public DataTable RetrivePageTable(DataTable aDataTable, int pageIndex, int pagerSize, out int totalPages)
        {
            DataTable aReturn = aDataTable.Clone();

            int totalRows = aDataTable.Rows.Count;

            int startRowIndex = (pageIndex - 1) * pagerSize;

            int endRowIndex = startRowIndex + pagerSize;

            totalPages = Convert.ToInt32(Math.Ceiling((double)totalRows / pagerSize));

            if (totalPages == pageIndex)
                endRowIndex = totalRows;

            DataRow dr = null;

            if (totalRows > 0)
            {
                for (int i = startRowIndex; i < endRowIndex; i++)
                {
                    dr = aDataTable.Rows[i];
                    aReturn.ImportRow(dr);
                }
            }

            return aReturn;
        }

        public DataRow[] RetrivePageRows(DataRow[] aRows, int pageIndex, int pagerSize, out int totalPages)
        {
            int totalRows = aRows.Length;
            int startRowIndex = (pageIndex - 1) * pagerSize;
            int endRowIndex = startRowIndex + pagerSize;
            totalPages = Convert.ToInt32(Math.Ceiling((double)totalRows / pagerSize));
            if (totalPages == pageIndex)
                endRowIndex = totalRows;
            DataRow[] aReturn = null;

            if (totalRows > 0)
            {
                aReturn = new DataRow[pagerSize];
                int count = 0;
                for (int i = startRowIndex; i < endRowIndex; i++)
                {
                    aReturn[count] = aRows[i];
                    count++;
                }
            }
            return aReturn;
        }

        // only for small DataTable
        public ArrayList RetrivePageList(DataTable aDataTable, int pageIndex, int pagerSize, out int totalPages)
        {
            ArrayList aList = new ArrayList();

            int totalRows = aDataTable.Rows.Count;

            int startRowIndex = (pageIndex - 1) * pagerSize;
            int endRowIndex = startRowIndex + pagerSize;

            totalPages = Convert.ToInt32(Math.Ceiling((double)totalRows / pagerSize));

            if (totalPages == pageIndex)
                endRowIndex = totalRows;

            if (totalRows > 0)
            {
                for (int i = startRowIndex; i < endRowIndex; i++)
                {
                    aList.Add(this.ConvertDataRowToObject(aDataTable.Rows[i]));
                }
            }

            return aList;
        }

        // for  any table samll or Very big

        public DataTable RetrivePageEntity(string aTableName, string keyColumn, int pageIndex, int pagerSize, out int totalRows)
        {
            return RetrivePageEntity(aTableName, keyColumn, pageIndex, pagerSize, out  totalRows, string.Empty);
        }

        public DataTable RetrivePageEntity(string aTableName, string keyColumn, int pageIndex, int pagerSize, out int totalRows, string whereClause)
        {
            string totalRowsWhere = string.Empty;
            if (whereClause != string.Empty)
            {
                totalRowsWhere = " where " + whereClause;
            }
            totalRows = Convert.ToInt32(this.RetriveSigleValue(" select count (" + keyColumn + ")" + " from " + aTableName + totalRowsWhere));

            int firstRowNum = (pageIndex - 1) * pagerSize + 1;

            string maxIDValue = string.Empty;

            if (firstRowNum <= totalRows)
            {
                StringBuilder aQuery = new StringBuilder();
                aQuery.Append(@" declare @maxIDValue  varchar (300) ");
                aQuery.Append(@"set rowcount " + firstRowNum);
                aQuery.Append(" select @maxIDValue  = " + keyColumn + " from " + aTableName + totalRowsWhere + " order by  " + keyColumn);
                aQuery.Append(" select @maxIDValue  ");
                maxIDValue = Convert.ToString(this.RetriveSigleValue(aQuery.ToString()));

                string retriveRowsWhere = string.Empty;
                if (whereClause != string.Empty)
                {
                    retriveRowsWhere = " and  " + whereClause;
                }
                aQuery = new StringBuilder();
                aQuery.Append(@"set rowcount " + pagerSize);
                aQuery.Append(" select *  from " + aTableName + " where " + keyColumn + " >='" + maxIDValue + "'" + retriveRowsWhere + " order by  " + keyColumn);
                return this.RetriveDataTable(aQuery.ToString());
            }

            return null;
        }

        #endregion ---- old page retrive, will be drop

        #region -------- ICommonDBAccess

        protected virtual void AppDBConnection()
        {
            throw new NotImplementedException();

            //AppSettingsReader _configReader = new AppSettingsReader();

            // Set connection string of the sqlconnection object
            // _mainConnection.ConnectionString = 	_configReader.GetValue("sql2000Connection", typeof(string)).ToString();

            //			_mainConnection.ConnectionString = "data source=webapp;initial catalog=amarella;UID=sa;PWD=";
            //_mainConnection.ConnectionString = "data source=hornet;Initial Catalog=devlppdm;UID=sa;PWD=";
            // _mainConnection.ConnectionString = "data source=WebApp;Initial Catalog=devlppdm2;UID=sa;PWD=";
        }

        protected virtual void Dispose(bool isDisposing)
        {
            // Check to see if Dispose has already been called.
            if (!_isDisposed)
            {
                if (isDisposing)
                {

                    // Object is created in this class, so destroy it here.
                    _mainConnection.Close();
                    _mainConnection.Dispose();



                    _mainConnection = null;
                }
            }
            _isDisposed = true;
        }

        public virtual bool Insert()
        {
            // No implementation, throw exception
            throw new NotImplementedException();
        }

        public virtual void IntializeWDefaultValue()
        {
            throw new NotImplementedException();
        }

        public virtual bool Delete()
        {
            // No implementation, throw exception
            throw new NotImplementedException();
        }

        public virtual bool Update()
        {
            // No implementation, throw exception
            throw new NotImplementedException();
        }

        public virtual DataTable Retrive()
        {
            // No implementation, throw exception
            throw new NotImplementedException();
        }

        public virtual DataTable SelectAll()
        {
            // No implementation, throw exception
            throw new NotImplementedException();
        }

        public virtual void InitObjWDefault()
        {
        }

        public virtual object ConvertDataRowToObject(DataRow aRow)
        {
            throw new NotImplementedException();
        }

        #endregion -------- ICommonDBAccess

        #region -------- Convert Functions

        private static object ConvertDataRowColumnToSqlType(System.Type aType, object aValue)
        {
            if (aType == typeof(SqlInt32))
            {
                return new SqlInt32((int)aValue);
            }

            if (aType == typeof(SqlString))
            {
                return new SqlString(aValue.ToString());
            }

            if (aType == typeof(SqlBoolean))
            {
                return new SqlBoolean((bool)aValue);
            }

            if (aType == typeof(SqlInt16))
            {
                return new SqlInt16((Int16)aValue);
            }

            if (aType == typeof(SqlDateTime))
            {
                return new SqlDateTime((DateTime)aValue);
            }

            if (aType == typeof(SqlByte))
            {
                return new SqlByte((byte)aValue);
            }

            if (aType == typeof(SqlDecimal))
            {
                return new SqlDecimal((decimal)aValue);
            }

            if (aType == typeof(SqlDouble))
            {
                return new SqlDouble((double)aValue);
            }

            if (aType == typeof(SqlGuid))
            {
                return new SqlGuid(aValue.ToString());
            }

            if (aType == typeof(SqlBinary))
            {
                return new SqlBinary((byte[])aValue);
            }

            if (aType == typeof(SqlMoney))
            {
                return new SqlMoney((double)aValue);
            }

            if (aType == typeof(SqlInt64))
            {
                return new SqlInt64((Int64)aValue);
            }

            if (aType == typeof(SqlSingle))
            {
                return new SqlSingle((double)aValue);
            }

            return null;
        }

        #endregion -------- Convert Functions

        #region -------- DB Functions

        public void ExecuteNonQuery(string dbmsQuery)
        {
            SqlCommand cmdToExecute = new SqlCommand();
            SetupSqlCommandParameter(cmdToExecute);
            cmdToExecute.CommandText = dbmsQuery;
            cmdToExecute.CommandType = CommandType.Text;

            // Use base class' connection object
            cmdToExecute.Connection = _mainConnection;

            try
            {
                _mainConnection.Open();

                int retval = cmdToExecute.ExecuteNonQuery();

                // Execute query.
            }
            catch (Exception ex)
            {
                // some error occured. Bubble it to caller and encapsulate Exception object
                throw new Exception("Execute NonQuery::" + dbmsQuery + "::Error occured.", ex);
            }
            finally
            {

                // Close connection.
                _mainConnection.Close();

                cmdToExecute.Dispose();
            }
        }

        private static string ChageFirstCharToUpCase(string aString)
        {
            if (aString == null || aString == string.Empty)
                return aString;
            StringBuilder ab = new StringBuilder(aString.Trim());
            ab.Replace(ab[0].ToString(), ab[0].ToString().ToUpper(), 0, 1);
            return ab.ToString();

            ////char aChar = ab[0];
        }

        private int RetriveMaxID(string primayKey, string tableName)
        {
            SqlCommand cmdToExecute = new SqlCommand();
            cmdToExecute.CommandText = " select max( " + primayKey + " ) from " + tableName;
            cmdToExecute.CommandType = CommandType.Text;

            cmdToExecute.Connection = _mainConnection;

            SqlDataReader aSqlDataReader = null;

            try
            {
                _mainConnection.Open();

                aSqlDataReader = cmdToExecute.ExecuteReader();

                // assume maxId from 1
                int maxRow = 1;
                while (aSqlDataReader.Read())
                {
                    if (aSqlDataReader[0] != null)
                    {
                        maxRow = int.Parse(aSqlDataReader[0].ToString());
                    }

                    //Console.WriteLine();
                }

                return maxRow;
            }
            catch (Exception ex)
            {
                // some error occured. Bubble it to caller and encapsulate Exception object
                throw new Exception("Retrive::" + cmdToExecute.CommandText + "::Error occured.", ex);
            }
            finally
            {

                // Close connection.
                _mainConnection.Close();

                cmdToExecute.Dispose();
                if (aSqlDataReader != null)
                {
                    if (!aSqlDataReader.IsClosed)
                    {
                        aSqlDataReader.Close();
                    }
                }
            }
        }

        public virtual void SetCustomerPrimaryKeySeed(string primayKey, string tableName, string primayKeySeedValue)
        {
            // if customer already
            int customerprimayKey = int.Parse(primayKeySeedValue);
            int currentLastID = RetriveMaxID(primayKey, tableName);
            if (customerprimayKey > currentLastID)
            {
                //currentLastID++;
                string sqlQuery = " dbcc checkident ( " + tableName + ",reseed," + customerprimayKey + ")";

                SqlCommand cmdToExecute = new SqlCommand();
                cmdToExecute.CommandText = sqlQuery;
                cmdToExecute.CommandType = CommandType.Text;
                cmdToExecute.Connection = _mainConnection;

                try
                {
                    _mainConnection.Open();

                    cmdToExecute.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    // some error occured. Bubble it to caller and encapsulate Exception object
                    throw new Exception("Retrive::" + sqlQuery + "::Error occured.", ex);
                }
                finally
                {

                    // Close connection.
                    _mainConnection.Close();

                    cmdToExecute.Dispose();
                }
            }
        }

        public virtual void ResetDBMaxID(string primayKey, string tableName)
        {
            int currentLastID = RetriveMaxID(primayKey, tableName);
            if (currentLastID != NoExistObjectID)
            {
                //currentLastID++;
                string sqlQuery = " dbcc checkident ( " + tableName + ",reseed," + currentLastID + ")";

                SqlCommand cmdToExecute = new SqlCommand();
                cmdToExecute.CommandText = sqlQuery;
                cmdToExecute.CommandType = CommandType.Text;
                cmdToExecute.Connection = _mainConnection;

                try
                {
                    _mainConnection.Open();

                    cmdToExecute.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    // some error occured. Bubble it to caller and encapsulate Exception object
                    throw new Exception("Retrive::" + sqlQuery + "::Error occured.", ex);
                }
                finally
                {

                    // Close connection.
                    _mainConnection.Close();

                    cmdToExecute.Dispose();
                }
            }
        }

        #endregion -------- DB Functions

      

        #region------------------ GenerateInClause

        public static string GenerateInClause(string aFLD_Name, DataTable aInDataTable)
        {
            if (aInDataTable.Rows.Count == 0)
                return string.Empty;

            StringBuilder aInClause = new StringBuilder();

            aInClause.Append(" [" + aFLD_Name + "] IN ( ");

            foreach (DataRow aRow in aInDataTable.Rows)
            {
                object aValue = aRow[0];
                if (aValue != null)
                {
                    aInClause.Append(aValue.ToString() + ", ");
                }
            }

            aInClause.Remove(aInClause.Length - 2, 2);

            aInClause.Append(" ) ");

            return aInClause.ToString();
        }

        public static string GenerateInClause(string aFLD_Name, string subQuery)
        {
            return " [" + aFLD_Name + "] IN ( " + subQuery + " ) ";
        }

        public static string GenerateNotInClause(string aFLD_Name, string subQuery)
        {
            return " [" + aFLD_Name + "] NOT IN ( " + subQuery + " ) ";
        }

        public static string GenerateColumnInClauseWithAndCondition(IEnumerable<int> ids, string IDColumnName, bool isInsertAnd)
        {
            string inclause = string.Empty;

            if (ids != null)
            {
                foreach (object pid in ids)
                {
                    inclause += pid + ",";
                }
            }

            if (inclause != string.Empty)
            {
                inclause = inclause.Substring(0, inclause.Length - 1);
                if (isInsertAnd)
                {
                    inclause = "  and  " + IDColumnName + " in ( " + inclause + " ) ";
                }
                else
                {
                    inclause = "  " + IDColumnName + " in ( " + inclause + " ) ";
                }
            }
            return inclause;
        }

        public static string GenerateColumnInClauseWithAndCondition(IEnumerable<string> ids, string IDColumnName, bool isInsertAnd)
        {
            string inclause = string.Empty;

            if (ids != null)
            {
                foreach (string pid in ids)
                {
                    inclause += "'" + pid + "',";
                }
            }

            if (inclause != string.Empty)
            {
                inclause = inclause.Substring(0, inclause.Length - 1);
                if (isInsertAnd)
                {
                    inclause = "  and  " + IDColumnName + " in ( " + inclause + " ) ";
                }
                else
                {
                    inclause = "  " + IDColumnName + " in ( " + inclause + " ) ";
                }
            }
            return inclause;
        }


        #endregion
    }

    public struct SqlBetweenAnd
    {
        private string _BeteenAndFiled;
        private object _BetweenValue;
        private object _AndValue;

        public SqlBetweenAnd(string aBeteenAndFiled, object aBetweenValue, object aAndValue)
        {
            _BeteenAndFiled = aBeteenAndFiled;
            _BetweenValue = aBetweenValue;
            _AndValue = aAndValue;
        }

        public string BeteenAndFiled
        {
            get
            {
                return _BeteenAndFiled;
            }
        }

        public object BetweenValue
        {
            get
            {
                return _BetweenValue;
            }
        }

        public object AndValue
        {
            get
            {
                return _AndValue;
            }
        }
    }

    public struct SqlUnitaryPredict
    {
        private string _UnitaryOperFiled;
        private EmSqlUnitaryOperator _EmSqlUnitaryOperator;
        private object _UValue;

        public SqlUnitaryPredict(string aUnitaryOperFiled, EmSqlUnitaryOperator aEmSqlUnitaryOperator, object aUValue)
        {
            _UnitaryOperFiled = aUnitaryOperFiled;
            _EmSqlUnitaryOperator = aEmSqlUnitaryOperator;
            _UValue = aUValue;
        }

        public string UnitaryOperFiled
        {
            get
            {
                return _UnitaryOperFiled;
            }
        }

        public EmSqlUnitaryOperator EmSqlUnitaryOperator
        {
            get
            {
                return _EmSqlUnitaryOperator;
            }
        }

        public object UValue
        {
            get
            {
                return _UValue;
            }
        }
    }

    public enum EmSqlUnitaryOperator { EQUAL = 1, GREAT = 2, LESS = 3, NOT_EQUAL = 4, GREAT_EQUAL = 5, LESS_EQUAL = 6, IS_NULL = 7, IS_NOT_NULL = 8, LIKE = 9 }

 
    public class SqlQuery
    {
        public static readonly string WHERE = " WHERE ";
        public static readonly string ORDER_BY = " ORDER BY ";
        public static readonly string ASC = " ASC ";
        public static readonly string DESC = " DESC ";
        public static readonly string GETDATEONLY = " CONVERT(CHAR(10),GETDATE(),110) ";
        public static readonly string GETDATE = " GETDATE() ";
        public static readonly string AND = " AND ";
        public static readonly string OR = " OR ";
        public static readonly string NOT = " NOT ";
        public static readonly string LEFT_JOIN = " LEFT JOIN  ";
        public static readonly string RIGHT_JOIN = " RIGHT JOIN  ";
        public static readonly string FULL_JOIN = " FULL OUTER JOIN  ";
        public static readonly string JOIN = " JOIN ";
        public static readonly string INNER_JOIN = " inner join ";
        public static readonly string ON = " ON ";
        public static readonly string EQUAL = " = ";
        public static readonly string NOT_EQUAL = " <> ";
        public static readonly string GREAT = " > ";
        public static readonly string GREAT_EQUAL = " >= ";
        public static readonly string LESS = " < ";
        public static readonly string LESS_EQUAL = " <= ";
        public static readonly string IS_NULL = " IS NULL ";
        public static readonly string IS_NOT_NULL = " IS NOT NULL ";
        public static readonly string BETWEEN = " BETWEEN ";
        public static readonly string LIKE = " LIKE ";
        public static readonly string CONTAINS = " CONTAINS ";
        public static readonly string FREETEXT = " FREETEXT ";
        public static readonly string EXISTS = " EXISTS ";
        public static readonly string IN = " IN ";
        public static readonly string ANY = " ANY ";
        public static readonly string ALL = " ALL ";
        public static readonly string GROUP_BY = " GROUP BY ";
        public static readonly string HAVING = " HAVING ";
        public static readonly string WITH_CUBE = " WITH CUBE ";
        public static readonly string WITH_ROLLUP = " WITH ROLLUP ";
        public static readonly string UPDATE = " UPDATE ";
        public static readonly string SET = " SET ";
        public static readonly string SelectDistinctAllColumn = " SELECT DISTINCT  *  ";
        public static readonly string SelectDistinct = " SELECT DISTINCT   ";
        public static readonly string SelectAll = " SELECT * ";
        public static readonly string SELECT = " SELECT ";
        public static readonly string SelectTop1 = " Select  top 1 * ";
        public static readonly string FROM = " FROM ";
        public static readonly string AS = " AS ";
        public static readonly string UNION = " UNION ";
        public static readonly string LeftBracket = "[";
        public static readonly string RightBracket = "]";
        public static readonly string Comma = " , ";
        public static readonly string WhiteSpace = " ' ' ";

        public static string CreateConditionIn(string aInSubQueryString)
        {
            return " IN ( " + aInSubQueryString + " ) ";
        }

        public class Condition
        {
            public static readonly string AND = " AND ";
            public static readonly string OR = " OR ";
            public static readonly string NOT = " NOT ";

            // outer join
            public static readonly string LEFT_JOIN = " LEFT JOIN  ";
            public static readonly string RIGHT_JOIN = " RIGHT JOIN  ";
            public static readonly string FULL_JOIN = " FULL OUTER JOIN  ";

            // DEFAULT join == Inner join
            public static readonly string JOIN = " JOIN ";

            public static readonly string ON = " ON ";
        }

        public class Predicate
        {
            public static readonly string EQUAL = " = ";
            public static readonly string LIKE = " LIKE ";
            public static readonly string GREAT = " > ";
            public static readonly string LESS = " < ";

            public static readonly string NOT_EQUAL = " <> ";

            public static readonly string GREAT_EQUAL = " >= ";
            public static readonly string LESS_EQUAL = " <= ";
            public static readonly string IS_NULL = " IS  NULL  ";
            public static readonly string IS_NOT_NULL = " IS  NOT NULL  ";
            public static readonly string BETWEEN = " BETWEEN  ";//BETWEEN 4095 AND 12000 ( include boundary4096 and 12000 )
            public static readonly string AND = " AND ";

            public static readonly string NOT = " NOT ";

            public static readonly string CONTAINS = " CONTAINS "; //Is the name of a specific column that has been registered for full-text searching. Columns of the character string data types are valid full-text searching columns.
            public static readonly string FREETEXT = " FREETEXT "; // must index this text

            //expression [,...n] OR Use IN with a subquery
            //public static readonly string IN = " IN ";

            //EXISTSU sed with a subquery to test for the existence of rows returned by the subquery

            //WHERE EXISTS
            public static readonly string EXISTS = " EXISTS ";
            public static readonly string IN = " IN ";

            // WHERE Advance > ANY ( SELECT Advance)
            public static readonly string ANY = " ANY ";
            public static readonly string ALL = " ALL ";

            public static readonly string GROUP_BY = " GROUP BY ";
            public static readonly string HAVING = "  HAVING  ";
            public static readonly string WITH_CUBE = " WITH CUBE ";
            public static readonly string WITH_ROLLUP = " WITH ROLLUP ";

            // Group wiht Cube and Rollup
        }
    }

    public enum WhereOperator : byte { AND = 1, OR = 2 }

  
  

   
}