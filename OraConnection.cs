using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Oracle.DataAccess.Client;


namespace SADADPOSDataUploadWinService
{
    public class OraConnection
    {
        private static OracleConnection conn = new OracleConnection(System.Configuration.ConfigurationManager.ConnectionStrings["OraConn"].ConnectionString);
        public static OracleConnection GetConnection()
        {
            return new OracleConnection(System.Configuration.ConfigurationManager.ConnectionStrings["OraConn"].ConnectionString); ;
        }

    }
}
