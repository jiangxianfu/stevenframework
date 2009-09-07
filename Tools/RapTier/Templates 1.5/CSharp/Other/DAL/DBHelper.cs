using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.Collections;
using System.Data;

namespace STN.Data.SqlServer.Base
{
    /// <summary>
    /// ���ݿ������
    /// </summary>
    public sealed class DBHelper : IDisposable
    {
        private SqlConnection _connection;
        private static readonly string _connectionString = Common.DBConfig.DBConn;

        public DBHelper()
        {
            _connection = new SqlConnection();
            _connection.ConnectionString = _connectionString;
            _connection.Open();

        }
        #region IDisposable ��Ա

        /// <summary>
        ///�ر�����
        /// </summary>
        private void Close()
        {
            if (null != _connection)
            {
                _connection.Close();
            }
        }
        /// <summary>
        /// ���ٶ���
        /// </summary>
        public void Dispose()
        {
            Close();
            if (null != _connection)
            {
                _connection.Dispose();
            }
        }

        #endregion

        #region  ִ�м�SQL���
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="trans"></param>
        /// <param name="condition"></param>
        private void PrepareCommand(SqlCommand cmd, SQLCondition condition)
        {
            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }
            cmd.Connection = _connection;
            cmd.CommandText = condition.CommandText.ToString();
            if (condition.ParamsCount > 0)
            {
                List<SqlParameter> paramList = condition.CommandParams;
                foreach (SqlParameter parm in paramList)
                {
                    cmd.Parameters.Add(parm);
                }
            }
        }

        /// <summary>
        /// ִ��SQL��䣬����Ӱ��ļ�¼��
        /// </summary>
        /// <param name="condition">SQLcondition</param>
        /// <returns>Ӱ��ļ�¼��</returns>
        public int ExecuteNonQuerySql(SQLCondition condition)
        {
            SqlCommand cmd = new SqlCommand();
            try
            {
                PrepareCommand(cmd, condition);
                int rows = cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
                return rows;
            }
            catch (System.Data.SqlClient.SqlException E)
            {
                this.Close();
                throw new Exception(E.Message);
            }
        }


        /// <summary>
        /// ִ�� SQL���,���ؽ�����еĵ�һ�У���һ�е�ֵ
        /// ִ��һ�������ѯ�����䣬���ز�ѯ�����object��
        /// </summary>
        /// <param name="SQLString"></param>
        /// <param name="cmdParms"></param>
        /// <returns>��һ�У���һ�е�ֵ</returns>
        public object ExecuteScalarSql(SQLCondition condition)
        {
            SqlCommand cmd = new SqlCommand();
            try
            {
                PrepareCommand(cmd, condition);
                object result = cmd.ExecuteScalar();
                cmd.Parameters.Clear();
                return result;
            }
            catch (System.Data.SqlClient.SqlException E)
            {
                this.Close();
                throw new Exception(E.Message);
            }
        }

        /// <summary>
        /// ִ�в�ѯ��䣬����SqlDataReader
        /// </summary>
        /// <param name="strSQL">��ѯ���</param>
        /// <returns>SqlDataReader</returns>
        public SqlDataReader ExecuteReader(SQLCondition condition)
        {
            SqlCommand cmd = new SqlCommand();
            try
            {
                PrepareCommand(cmd, condition);
                SqlDataReader myReader = cmd.ExecuteReader();
                cmd.Parameters.Clear();
                return myReader;
            }
            catch (System.Data.SqlClient.SqlException e)
            {
                this.Close();
                throw new Exception(e.Message);
            }
        }

        /// <summary>
        /// ִ�в�ѯ��䣬����DataSet
        /// </summary>
        /// <param name="SQLString">��ѯ���</param>
        /// <returns>DataSet</returns>
        public DataSet Query(SQLCondition condition)
        {
            SqlCommand cmd = new SqlCommand();
            PrepareCommand(cmd, condition);
            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
            {
                DataSet ds = new DataSet();
                try
                {
                    da.Fill(ds, "ds");
                    cmd.Parameters.Clear();
                }
                catch (System.Data.SqlClient.SqlException ex)
                {
                    this.Close();
                    throw new Exception(ex.Message);
                }
                return ds;
            }

        }

        /// <summary>
        /// ִ�в�ѯ��䣬����DataTable
        /// </summary>
        /// <param name="SQLString"></param>
        /// <param name="cmdParms"></param>
        /// <returns></returns>
        public DataTable QueryDataTable(SQLCondition condition)
        {
            SqlCommand cmd = new SqlCommand();
            PrepareCommand(cmd, condition);
            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
            {
                DataTable dt = new DataTable();
                try
                {
                    da.Fill(dt);
                    cmd.Parameters.Clear();
                }
                catch (System.Data.SqlClient.SqlException ex)
                {
                    this.Close();
                    throw new Exception(ex.Message);
                }
                return dt;
            }

        }


        /// <summary>
        /// ִ�ж���SQL��䣬ʵ�����ݿ�����
        /// </summary>
        /// <param name="SQLStringList">����SQL���</param>		
        public void ExecuteSqlTran(List<SQLCondition> conditionList)
        {
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = _connection;
            SqlTransaction tx = _connection.BeginTransaction();
            cmd.Transaction = tx;
            try
            {
                foreach (SQLCondition condition in conditionList)
                {
                    PrepareCommand(cmd, condition);
                    cmd.ExecuteNonQuery();
                    cmd.Parameters.Clear();
                    tx.Commit();
                }
            }
            catch (System.Data.SqlClient.SqlException E)
            {
                tx.Rollback();
                this.Close();
                throw new Exception(E.Message);
            }
        }
        #endregion

        #region �洢���̲���

        /// <summary>
        /// ִ�д洢����
        /// </summary>
        /// <param name="storedProcName">�洢������</param>
        /// <param name="parameters">�洢���̲���</param>
        /// <returns>SqlDataReader</returns>
        public SqlDataReader RunProcedure(string storedProcName, IDataParameter[] parameters)
        {
            SqlDataReader returnReader;
            SqlCommand command = BuildQueryCommand(_connection, storedProcName, parameters);
            command.CommandType = CommandType.StoredProcedure;
            returnReader = command.ExecuteReader();
            return returnReader;
        }


        /// <summary>
        /// ִ�д洢����
        /// </summary>
        /// <param name="storedProcName">�洢������</param>
        /// <param name="parameters">�洢���̲���</param>
        /// <param name="tableName">DataSet����еı���</param>
        /// <returns>DataSet</returns>
        public DataSet RunProcedure(string storedProcName, IDataParameter[] parameters, string tableName)
        {
            DataSet dataSet = new DataSet();
            using (SqlDataAdapter sqlDA = new SqlDataAdapter())
            {
                sqlDA.SelectCommand = BuildQueryCommand(_connection, storedProcName, parameters);
                sqlDA.Fill(dataSet, tableName);
                return dataSet;
            }

        }


        /// <summary>
        /// ִ�д洢���̣�����Ӱ�������		
        /// </summary>
        /// <param name="storedProcName">�洢������</param>
        /// <param name="parameters">�洢���̲���</param>
        /// <param name="rowsAffected">Ӱ�������</param>
        /// <returns></returns>
        public int RunProcedure(string storedProcName, IDataParameter[] parameters, out int rowsAffected)
        {

            int result;
            SqlCommand command = BuildIntCommand(_connection, storedProcName, parameters);
            rowsAffected = command.ExecuteNonQuery();
            result = (int)command.Parameters["ReturnValue"].Value;
            return result;

        }


        /// <summary>
        /// ���� SqlCommand ����(��������һ���������������һ������ֵ)
        /// </summary>
        /// <param name="connection">���ݿ�����</param>
        /// <param name="storedProcName">�洢������</param>
        /// <param name="parameters">�洢���̲���</param>
        /// <returns>SqlCommand</returns>
        private SqlCommand BuildQueryCommand(SqlConnection connection, string storedProcName, IDataParameter[] parameters)
        {
            SqlCommand command = new SqlCommand(storedProcName, connection);
            command.CommandType = CommandType.StoredProcedure;
            foreach (SqlParameter parameter in parameters)
            {
                command.Parameters.Add(parameter);
            }
            return command;
        }

        /// <summary>
        /// ���� SqlCommand ����ʵ��(��������һ������ֵ)	
        /// </summary>
        /// <param name="storedProcName">�洢������</param>
        /// <param name="parameters">�洢���̲���</param>
        /// <returns>SqlCommand ����ʵ��</returns>
        private SqlCommand BuildIntCommand(SqlConnection connection, string storedProcName, IDataParameter[] parameters)
        {
            SqlCommand command = BuildQueryCommand(connection, storedProcName, parameters);
            command.Parameters.Add(new SqlParameter("ReturnValue",
                SqlDbType.Int, 4, ParameterDirection.ReturnValue,
                false, 0, 0, string.Empty, DataRowVersion.Default, null));
            return command;
        }        
        #endregion

    }
}
