using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Oracle.DataAccess.Client;
using System.Data;


namespace SADADPOSDataUploadWinService
{
    public class SQLParameters
    {
        public string ParameterName { get; set; }
        public object ParameterValue { get; set; }
        public OracleDbType DataType { get; set; }
        public System.Data.ParameterDirection ParameterDirection;

        public SQLParameters(string name, object value)
        {
            ParameterDirection = ParameterDirection.Input;
            ParameterName = name;
            ParameterValue = value;
        }
        public SQLParameters(string name, object value, OracleDbType dbType)
        {
            ParameterDirection = ParameterDirection.Input;
            ParameterName = name;
            ParameterValue = value;
            DataType = dbType;
        }
        public SQLParameters(string name, object value, OracleDbType dbType, ParameterDirection Direction)
        {
            ParameterDirection = Direction;
            ParameterName = name;
            ParameterValue = value;
            DataType = dbType;
        }
        internal OracleParameter GetParam()
        {
            if (ParameterDirection == System.Data.ParameterDirection.Input)
            {
                return new OracleParameter(ParameterName, ParameterValue);
            }
            else
            {
                return new OracleParameter(ParameterName, DataType, ParameterValue, ParameterDirection);
            }

        }
    }
}
