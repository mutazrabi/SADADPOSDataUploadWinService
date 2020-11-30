using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using Oracle.DataAccess.Client;

namespace SADADPOSDataUploadWinService
{
    public class OracleSQLHelper
    {
        public OracleSQLHelper()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        public static DataSet ExecuteDataSet(string ProcessName, CommandType CommandType, string CommandText)
        {

            DataSet ds = new DataSet();
            try
            {
                using (OracleConnection conn = new OracleConnection(System.Configuration.ConfigurationManager.ConnectionStrings["OraConn"].ConnectionString))
                {
                    OracleCommand cmd = new OracleCommand();
                    cmd.CommandType = CommandType;
                    cmd.CommandText = CommandText;
                    cmd.Connection = conn;
                    // Create the DataAdapter & DataSet
                    using (OracleDataAdapter da = new OracleDataAdapter(cmd))
                    {
                        //Open Connection for filling Data
                        if (conn.State != ConnectionState.Open)
                            conn.Open();

                        // Fill the DataSet using default values for DataTable names, etc
                        da.Fill(ds);
                        // Detach the OracleParameters from the command object, so they can be used again
                        cmd.Parameters.Clear();

                    }
                }
            }
            catch (OracleException ex)
            {
                ds = null;
                Logger.WriteLog(ex.Message, ex.StackTrace, "ExecuteDataSet");
            }
            return ds;

        }
        public static int ExecuteNonQuery(string ProcessName, CommandType CommandType, string CommandText)
        {
            int retval = -1;
            try
            {
                using (OracleConnection conn = new OracleConnection(System.Configuration.ConfigurationManager.ConnectionStrings["OraConn"].ConnectionString))
                {
                    // Create a command and prepare it for execution
                    using (OracleCommand cmd = new OracleCommand())
                    {
                        cmd.CommandType = CommandType;
                        cmd.CommandText = CommandText;
                        cmd.Connection = conn;

                        if (conn.State != ConnectionState.Open)
                            conn.Open();

                        // Finally, execute the command
                        retval = cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (OracleException ex)
            {
                retval = -1;
                Logger.WriteLog(ex.Message, ex.StackTrace, "ExecuteNonQuery");

            }
            return retval;
        }
        public static int ExecuteNonQuery(string ProcessName, CommandType CommandType, string CommandText, params SQLParameters[] parameters)
        {
            int retval = -1;
            try
            {
                using (OracleConnection conn = new OracleConnection(System.Configuration.ConfigurationManager.ConnectionStrings["OraConn"].ConnectionString))
                {
                    using (OracleCommand cmd = new OracleCommand())
                    {
                        cmd.CommandType = CommandType;
                        cmd.CommandText = CommandText;
                        cmd.Connection = conn;

                        //Load Parameters
                        foreach (SQLParameters item in parameters)
                        {
                            cmd.Parameters.Add(item.GetParam());
                        }

                        //Open Connection for filling Data
                        if (conn.State != ConnectionState.Open)
                            conn.Open();

                        retval = cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (OracleException ex)
            {
                Logger.WriteLog(ex.Message, ex.StackTrace, "ExecuteNonQuery");
            }
            return retval;
        }
        public static string ExecuteNonQuery(string ProcessName, CommandType CommandType, string CommandText, string outPram, params SQLParameters[] parameters)
        {
            string retval = "";
            try
            {
                using (OracleConnection conn = new OracleConnection(System.Configuration.ConfigurationManager.ConnectionStrings["OraConn"].ConnectionString))
                {
                    using (OracleCommand cmd = new OracleCommand())
                    {
                        cmd.CommandType = CommandType;
                        cmd.CommandText = CommandText;
                        cmd.Connection = conn;

                        //Load Parameters
                        foreach (SQLParameters item in parameters)
                        {
                            cmd.Parameters.Add(item.GetParam());
                        }

                        //Open Connection for filling Data
                        if (conn.State != ConnectionState.Open)
                            conn.Open();

                        // Finally, execute the command
                        cmd.ExecuteNonQuery();
                        retval = cmd.Parameters[outPram].Value.ToString();
                    }
                }
            }
            catch (OracleException ex)
            {
                Logger.WriteLog(ex.Message, ex.StackTrace, "ExecuteNonQuery");
            }
            return retval;
        }
        public static object ExecuteScalar(string ProcessName, CommandType CommandType, string CommandText)
        {
            object retval = null;
            try
            {
                using (OracleConnection conn = new OracleConnection(System.Configuration.ConfigurationManager.ConnectionStrings["OraConn"].ConnectionString))
                {
                    // Create a command and prepare it for execution
                    using (OracleCommand cmd = new OracleCommand())
                    {
                        //Set Command Attributes
                        cmd.CommandType = CommandType;
                        cmd.CommandText = CommandText;
                        cmd.Connection = conn;

                        if (conn.State != ConnectionState.Open)
                            conn.Open();

                        retval = cmd.ExecuteScalar();
                    }
                }

            }
            catch (OracleException ex)
            {
                Logger.WriteLog(ex.Message, ex.StackTrace, "ExecuteScalar");
            }
            return retval;
        }
        internal static object ExecuteScalar(CommandType commandType, string commandText, OracleTransaction transaction, params SQLParameters[] sqlCoreParameters)
        {
            object retval = null;

            using (OracleConnection conn = new OracleConnection(System.Configuration.ConfigurationManager.ConnectionStrings["OraConn"].ConnectionString))
            {
                using (OracleCommand cmd = new OracleCommand())
                {
                    //Set Command Attributes
                    cmd.CommandType = commandType;
                    cmd.CommandText = commandText;
                    cmd.Transaction = transaction;
                    cmd.Connection = conn;

                    foreach (SQLParameters item in sqlCoreParameters)
                    {
                        cmd.Parameters.Add(item.GetParam());
                    }

                    if (conn.State != ConnectionState.Open)
                        conn.Open();
                    // Execute the command & return the results
                    retval = cmd.ExecuteScalar();
                }
            }
            return retval;
        }
        public static OracleDataReader ExecuteDataReader(CommandType CommandType, string CommandText, params SQLParameters[] parameters)
        {
            OracleDataReader reader;
            try
            {
                using (OracleConnection conn = new OracleConnection(System.Configuration.ConfigurationManager.ConnectionStrings["OraConn"].ConnectionString))
                {
                    OracleCommand cmd = new OracleCommand();

                    cmd.CommandType = CommandType;
                    cmd.CommandText = CommandText;
                    cmd.Connection = conn;

                    foreach (SQLParameters item in parameters)
                    {
                        cmd.Parameters.Add(item.GetParam());
                    }

                    if (conn.State != ConnectionState.Open)
                        conn.Open();

                    reader = cmd.ExecuteReader();

                }
            }
            catch (Exception ex)
            {
                reader = null;
                Logger.WriteLog(ex.Message, ex.StackTrace, "ExecuteDataReader");
            }

            return reader;
        }
    }
}
