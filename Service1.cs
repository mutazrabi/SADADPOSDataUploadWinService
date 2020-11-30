using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace SADADPOSDataUploadWinService
{
    public partial class Service1 : ServiceBase
    {
        Timer timer = new Timer();
        private static readonly Random getrandom = new Random();

        public Service1()
        {
            InitializeComponent();
        }
        protected override void OnStart(string[] args)
        {
            Logger.WriteLog("Service is started at " + DateTime.Now,"Info", "OnStart");
            timer.Elapsed += new ElapsedEventHandler(OnElapsedTime);
            timer.Interval =double.Parse(System.Configuration.ConfigurationManager.AppSettings["Interval"]); //number in milisecinds  
            timer.Enabled = true;
        }
        protected override void OnStop()
        {
            timer.Enabled = false;
            Logger.WriteLog("Service is stopped at " + DateTime.Now, "Info", "OnStop");

        }
        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            try
            {
                #region Declare Variables
                DataSet dataSet = new DataSet();
                DataTable table = new DataTable();
                string CreditCardNo = string.Empty;
                string customerNo = string.Empty;
                string customerName = string.Empty;
                #endregion

                
                int deletedRows=OracleSQLHelper.ExecuteNonQuery("DeleteTemp", CommandType.Text, "delete from SADADTransactions_Temp");
                if (deletedRows > -1)
                {
                    int _rows = OracleSQLHelper.ExecuteNonQuery("AddRecords", CommandType.Text, "insert into SADADTransactions_Temp (PMNTID,UniqueIdentifier,FileName,BillingAcct,Amount,ProcDate,PmtRefNo,TRX_TYPE) select PMNTID,UniqueIdentifier,FileName,BillingAcct,Amount,ProcDate,PmtRefNo,TRX_TYPE from SADADTransactions where SADADTransactions.Selected='N' and SADADTransactions.Approved='N' ");
                    if (_rows > 0)
                    {
                        dataSet = OracleSQLHelper.ExecuteDataSet("ReturnRecords", CommandType.Text, "select PMNTID,UniqueIdentifier,FileName,BillingAcct,Amount,ProcDate,PmtRefNo,TRX_TYPE from SADADTransactions_Temp ");
                        OracleSQLHelper.ExecuteNonQuery("UpdateRecords", CommandType.Text, "update SADADTransactions set Selected='Y' where(SADADTransactions.PMNTID,SADADTransactions.BillingAcct, SADADTransactions.PmtRefNo) in (select SADADTransactions_Temp.PMNTID,SADADTransactions_Temp.BillingAcct, SADADTransactions_Temp.PmtRefNo from SADADTransactions_Temp) ");
                        object returnValue = null;
                        bool isExists = false;

                        if (dataSet != null)
                        {
                            if (dataSet.Tables.Count > 0)
                            {
                                if (dataSet.Tables[0].Rows.Count > 0)
                                {
                                    foreach (DataRow dRow in dataSet.Tables[0].Rows)
                                    {
                                        returnValue = null;
                                        isExists = false;

                                        returnValue = OracleSQLHelper.ExecuteScalar("ExistsRecord", CommandType.Text, "select 1 as Result from SADADTransactions where BILLINGACCT='" + dRow["BillingAcct"].ToString() + "' and FILENAME='" + dRow["FileName"].ToString() + "' and AMOUNT='" + dRow["Amount"].ToString() + "' and PMNTID='"+ dRow["PMNTID"].ToString() + "' and APPROVED='Y'");
                                        if (returnValue != null)
                                        {
                                            if (returnValue.ToString() == "1")
                                                isExists = true;
                                        }

                                        if (isExists == false)
                                        {
                                            if (dRow["TRX_TYPE"].ToString() == "SAD")
                                            {
                                                #region SADAD Upload Data to NI and Bank Charge Entry

                                                table = Search(dRow["BillingAcct"].ToString(), dRow["PmtRefNo"].ToString());
                                                if (table != null)
                                                {
                                                    if (table.Rows.Count > 0)
                                                    {
                                                        foreach (DataRow row in table.Rows)
                                                        {
                                                            CreditCardNo = string.Empty;

                                                            if (row["PROD"].ToString() == "LOAN")
                                                            {
                                                                #region Loans - Call SADAD Data Upload & Bank Charge Entry Procedure
                                                                SADADDataUpload(ulong.Parse(row["AGR_ID"].ToString()), decimal.Parse(dRow["Amount"].ToString()), dRow["FileName"].ToString(), dRow["BillingAcct"].ToString(), dRow["PmtRefNo"].ToString());
                                                                BankChargeEntry("LOAN", "SAD", decimal.Parse(dRow["Amount"].ToString()), "", row["CUSTOMER_NO"].ToString(), row["CUSTOMER_NAME"].ToString(), dRow["BillingAcct"].ToString(), dRow["PmtRefNo"].ToString());
                                                                #endregion
                                                            }
                                                            else if (row["PROD"].ToString() == "CARD")
                                                            {
                                                                #region Cards - Call NI Service & Bank Charge Entry Procedure
                                                                if (!string.IsNullOrWhiteSpace(row["KEY_CARD"].ToString()))
                                                                {
                                                                    CreditCardNo = EncryptDecryptEngine.DecryptStringRijndael(row["KEY_CARD"].ToString().Trim());
                                                                    if (!string.IsNullOrWhiteSpace(CreditCardNo))
                                                                    {
                                                                        if (CreditCardNo.StartsWith("000"))
                                                                            CreditCardNo = CreditCardNo.Trim().Substring(3);

                                                                        NICardPaymentAPI(CreditCardNo, "SADAD", dRow["Amount"].ToString(), "Customer No : " + row["CUSTOMER_NO"].ToString(), dRow["BillingAcct"].ToString(), dRow["PmtRefNo"].ToString());
                                                                        BankChargeEntry("CARD", "SAD", decimal.Parse(dRow["Amount"].ToString()), "", row["CUSTOMER_NO"].ToString(), row["CUSTOMER_NAME"].ToString(), dRow["BillingAcct"].ToString(), dRow["PmtRefNo"].ToString());
                                                                    }
                                                                    else
                                                                    {
                                                                        UnprocessedRec(dRow["BillingAcct"].ToString(), dRow["PmtRefNo"].ToString());
                                                                        Logger.TrackLogs("Bill Account '" + dRow["BillingAcct"].ToString(), "Invalid Credit Card Number or Empty");
                                                                    }

                                                                }
                                                                else
                                                                {
                                                                    UnprocessedRec(dRow["BillingAcct"].ToString(), dRow["PmtRefNo"].ToString());
                                                                    Logger.TrackLogs("Bill Account '" + dRow["BillingAcct"].ToString(), "Invalid Key Card or Empty");
                                                                }

                                                                #endregion
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        UnprocessedRec(dRow["BillingAcct"].ToString(), dRow["PmtRefNo"].ToString());
                                                        Logger.TrackLogs("Bill Account '" + dRow["BillingAcct"].ToString() + "' not exists", "Search in 'SADAD_POS_TRAN_VIEW' View");
                                                    }

                                                }
                                                else
                                                {
                                                    UnprocessedRec(dRow["BillingAcct"].ToString(), dRow["PmtRefNo"].ToString());
                                                    Logger.TrackLogs("Bill Account '" + dRow["BillingAcct"].ToString() + "' not exists", "Search in 'SADAD_POS_TRAN_VIEW' View");
                                                }


                                                #endregion
                                            }
                                            else
                                            {
                                                #region POS Upload Data to NI and Bank Charge Entry
                                                customerName = string.Empty;
                                                customerNo = string.Empty;
                                                CreditCardNo = string.Empty;

                                                if (dRow["PmtRefNo"] != null)
                                                {
                                                    if (dRow["PmtRefNo"].ToString().Trim().Contains("LOAN_"))
                                                    {
                                                        POSDataUpload(ulong.Parse(dRow["BillingAcct"].ToString()), Convert.ToDecimal(dRow["AMOUNT"]), "POS PAYMENT FOR AGGREMAENT " + dRow["PMNTID"].ToString(), dRow["PmtRefNo"].ToString().Trim());
                                                        if (dRow["PmtRefNo"].ToString().Trim().Length > 5)
                                                        {
                                                            customerNo = dRow["FileName"].ToString().Trim().Substring(0, dRow["FileName"].ToString().Trim().IndexOf("---"));
                                                            customerName = dRow["FileName"].ToString().Trim().Substring(dRow["FileName"].ToString().Trim().IndexOf("---") + 3);
                                                            BankChargeEntry("LOAN", "POS", decimal.Parse(dRow["Amount"].ToString()), dRow["PmtRefNo"].ToString().Trim().Substring(dRow["PmtRefNo"].ToString().Trim().IndexOf("_") + 1), customerNo, customerName, dRow["BillingAcct"].ToString(), dRow["PmtRefNo"].ToString());
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (!string.IsNullOrWhiteSpace(dRow["UniqueIdentifier"].ToString()))
                                                        {
                                                            Logger.WriteLog("UniqueIdentifier", dRow["UniqueIdentifier"].ToString(), "OnElapsedTime");

                                                            CreditCardNo = EncryptDecryptEngine.DecryptStringRijndael(dRow["UniqueIdentifier"].ToString().Trim());
                                                            if (!string.IsNullOrWhiteSpace(CreditCardNo))
                                                            {

                                                                if (CreditCardNo.StartsWith("000"))
                                                                    CreditCardNo = CreditCardNo.Trim().Substring(3);

                                                                customerNo = dRow["FileName"].ToString().Trim().Substring(0, dRow["FileName"].ToString().Trim().IndexOf("---"));
                                                                customerName = dRow["FileName"].ToString().Trim().Substring(dRow["FileName"].ToString().Trim().IndexOf("---") + 3);
                                                                NICardPaymentAPI(CreditCardNo, "POS", dRow["Amount"].ToString(), "Customer No : " + customerNo, dRow["BillingAcct"].ToString(), dRow["PmtRefNo"].ToString());
                                                                BankChargeEntry("CARD", "POS", decimal.Parse(dRow["Amount"].ToString()), dRow["PmtRefNo"].ToString().Trim().Substring(dRow["PmtRefNo"].ToString().Trim().IndexOf("_") + 1), customerNo, customerName, dRow["BillingAcct"].ToString(), dRow["PmtRefNo"].ToString());
                                                            }
                                                            else
                                                            {
                                                                UnprocessedRec(dRow["BillingAcct"].ToString(), dRow["PmtRefNo"].ToString());
                                                                Logger.TrackLogs("Invalid CreditCardNo or Empty", "NICardPaymentAPI");
                                                            }

                                                        }
                                                        else
                                                        {
                                                            UnprocessedRec(dRow["BillingAcct"].ToString(), dRow["PmtRefNo"].ToString());
                                                            Logger.TrackLogs("Invalid UniqueIdentifier or Empty", "NICardPaymentAPI");
                                                        }

                                                    }
                                                }

                                                #endregion
                                            }
                                        }
                                    }

                                    OracleSQLHelper.ExecuteNonQuery("UpdateRecords", CommandType.Text, "update SADADTransactions set Approved='Y' where(SADADTransactions.PMNTID,SADADTransactions.BillingAcct, SADADTransactions.PmtRefNo) in (select SADADTransactions_Temp.PMNTID,SADADTransactions_Temp.BillingAcct, SADADTransactions_Temp.PmtRefNo from SADADTransactions_Temp) ");
                                    OracleSQLHelper.ExecuteNonQuery("AddRecords", CommandType.Text, "insert into SADADTransactions_Hist (UniqueIdentifier,FileName,BillingAcct,Amount,ProcDate,PmtRefNo,TRX_TYPE) select UniqueIdentifier,FileName,BillingAcct,Amount,ProcDate,PmtRefNo,TRX_TYPE from SADADTransactions_Temp");

                                }
                            }

                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex.Message, ex.StackTrace, "OnElapsedTime");
            }
        }

        protected DataTable Search(string billNumber,string paymentRefNo)
        {
            DataTable table = new DataTable();
            try
            {
                if (!string.IsNullOrWhiteSpace(billNumber))
                {
                    DataSet data = new DataSet();
                    data = OracleSQLHelper.ExecuteDataSet("Search", CommandType.Text, "select PROD,ACCTNO,KEY_CARD,AGR_ID,NIN,CUSTOMER_NO,CUSTOMER_NAME,STAGE,MIN_DUE,OUTSTANDING_DUES,MIN_DUE from SADAD_POS_TRAN_VIEW where SADAD_KEY='" + billNumber + "'");
                    if (data != null)
                    {
                        if (data.Tables.Count > 0)
                        {
                            if (data.Tables[0].Rows.Count > 0)
                            {
                                table = data.Tables[0];
                            }
                            else
                            {
                                UnprocessedRec(billNumber, paymentRefNo);
                                Logger.TrackLogs("Bill Account '" + billNumber + "' not exists", "Search in 'SADAD_POS_TRAN_VIEW' View");
                            }

                        }
                        else
                        {
                            UnprocessedRec(billNumber, paymentRefNo);
                            Logger.TrackLogs("Bill Account '" + billNumber + "' not exists", "Search in 'SADAD_POS_TRAN_VIEW' View");
                        }

                    }
                    else
                    {
                        UnprocessedRec(billNumber, paymentRefNo);
                        Logger.TrackLogs("Bill Account '" + billNumber + "' not exists", "Search in 'SADAD_POS_TRAN_VIEW' View");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex.Message, ex.StackTrace, "Search");
            }
            return table;
        }
        private int GetRandomNumber(int min, int max)
        {
            lock (getrandom) // synchronize
            {
                return getrandom.Next(min, max);
            }
        }
        private void SADADDataUpload(ulong agreementID, decimal amount, string remarks,string billNumber , string paymentRef)
        {
            try
            {
                #region Call stored Procedure

                using (var command = new Oracle.DataAccess.Client.OracleCommand())
                {
                    command.Connection = OraConnection.GetConnection();
                    if (command.Connection.State != ConnectionState.Open)
                        command.Connection.Open();

                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "SADAD_manual_data_upload";

                    command.Parameters.Add(new Oracle.DataAccess.Client.OracleParameter("p_Agreementid", Oracle.DataAccess.Client.OracleDbType.Int64)).Value = agreementID;
                    command.Parameters.Add(new Oracle.DataAccess.Client.OracleParameter("p_Chqamount", Oracle.DataAccess.Client.OracleDbType.Double)).Value = amount;
                    command.Parameters.Add(new Oracle.DataAccess.Client.OracleParameter("p_remarks", Oracle.DataAccess.Client.OracleDbType.Varchar2, 100)).Value = remarks;

                    Oracle.DataAccess.Client.OracleParameter errorcodeparam = new Oracle.DataAccess.Client.OracleParameter("Error_code", Oracle.DataAccess.Client.OracleDbType.Int32);
                    errorcodeparam.Direction = ParameterDirection.Output;

                    Oracle.DataAccess.Client.OracleParameter errormsgparam = new Oracle.DataAccess.Client.OracleParameter("Error_msg", Oracle.DataAccess.Client.OracleDbType.Varchar2, 100);
                    errorcodeparam.Direction = ParameterDirection.Output;

                    command.Parameters.Add(errorcodeparam);
                    command.Parameters.Add(errormsgparam);

                    command.ExecuteNonQuery();

                    string errorcode = command.Parameters["Error_code"].Value.ToString();
                    string errorMessage = command.Parameters["Error_msg"].Value.ToString();

                    if (!string.IsNullOrWhiteSpace(errorcode))
                    {
                        if (errorcode.ToLower() != "null")
                        {
                            UnprocessedRec(billNumber, paymentRef);
                            Logger.TrackLogs("Invalid Agreement ID: '" + agreementID + "' with amount : '" + amount + "' , Error Code Message : '" + errorcode + "' , Error Message : '" + errorMessage + "'", "after calling 'SADAD_manual_data_upload' Procedure");
                        }
                    }
                    else
                        Logger.TrackLogs("Agreement ID: '" + agreementID + "' with amount : '" + amount + "' Added Successfully", "after calling 'SADAD_manual_data_upload' Procedure");


                    command.Connection.Close();
                }
                #endregion
            }
            catch (Exception ex)
            {
                UnprocessedRec(billNumber, paymentRef);
                Logger.TrackLogs("Invalid Agreement ID: '" + agreementID + "' with amount : '" + amount + "' , Error Code Message : '" + ex.Source + "' , Error Message : '" + ex.Message + "'", "after calling 'SADAD_manual_data_upload' Procedure");
            }
        }
        private void POSDataUpload(ulong agreementID, decimal amount, string remarks,string paymentRefNo)
        {
            try
            {
                #region Call stored Procedure

                using (var command = new Oracle.DataAccess.Client.OracleCommand())
                {
                    command.Connection = OraConnection.GetConnection();
                    if (command.Connection.State != ConnectionState.Open)
                        command.Connection.Open();

                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "POS_manual_data_upload";

                    command.Parameters.Add(new Oracle.DataAccess.Client.OracleParameter("p_Agreementid", Oracle.DataAccess.Client.OracleDbType.Int64)).Value = agreementID;
                    command.Parameters.Add(new Oracle.DataAccess.Client.OracleParameter("p_Chqamount", Oracle.DataAccess.Client.OracleDbType.Double)).Value = amount;
                    command.Parameters.Add(new Oracle.DataAccess.Client.OracleParameter("p_remarks", Oracle.DataAccess.Client.OracleDbType.Varchar2, 100)).Value = remarks;

                    Oracle.DataAccess.Client.OracleParameter errorcodeparam = new Oracle.DataAccess.Client.OracleParameter("Error_code", Oracle.DataAccess.Client.OracleDbType.Int32);
                    errorcodeparam.Direction = ParameterDirection.Output;

                    Oracle.DataAccess.Client.OracleParameter errormsgparam = new Oracle.DataAccess.Client.OracleParameter("Error_msg", Oracle.DataAccess.Client.OracleDbType.Varchar2, 100);
                    errorcodeparam.Direction = ParameterDirection.Output;

                    command.Parameters.Add(errorcodeparam);
                    command.Parameters.Add(errormsgparam);

                    command.ExecuteNonQuery();

                    string errorcode = command.Parameters["Error_code"].Value.ToString();
                    string errorMessage = command.Parameters["Error_msg"].Value.ToString();

                    if (!string.IsNullOrWhiteSpace(errorcode))
                    {
                        if (errorcode.ToLower() != "null")
                        {
                            UnprocessedRec(agreementID.ToString(), paymentRefNo);
                            Logger.TrackLogs("Invalid Agreement ID: '" + agreementID + "' with amount : '" + amount + "' , Error Code Message : '" + errorcode + "' , Error Message : '" + errorMessage + "'", "after calling 'POS_manual_data_upload' Procedure");
                        }
                    }
                    else
                        Logger.TrackLogs("Agreement ID: '" + agreementID + "' with amount : '" + amount + "' Added Successfully", "after calling 'POS_manual_data_upload' Procedure");

                    command.Connection.Close();
                }
                #endregion
            }
            catch (Exception ex)
            {
                UnprocessedRec(agreementID.ToString(), paymentRefNo);
                Logger.TrackLogs("Invalid Agreement ID: '" + agreementID + "' with amount : '" + amount + "' , Error Code Message : '" + ex.Source + "' , Error Message : '" + ex.Message + "'", "after calling 'POS_manual_data_upload' Procedure");
            }
        }
        private void BankChargeEntry(string transactionType, string channel, decimal amount, string cardType, string customerNo, string customerName, string billNumber, string paymentRef)
        {
            try
            {
                #region Call stored Procedure

                using (var command = new Oracle.DataAccess.Client.OracleCommand())
                {
                    command.Connection = OraConnection.GetConnection();
                    if (command.Connection.State != ConnectionState.Open)
                        command.Connection.Open();

                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "trx_bank_charge_entry";

                    command.Parameters.Add(new Oracle.DataAccess.Client.OracleParameter("TRX_TYPE", Oracle.DataAccess.Client.OracleDbType.Varchar2)).Value = transactionType;
                    command.Parameters.Add(new Oracle.DataAccess.Client.OracleParameter("TRX_CHANEL", Oracle.DataAccess.Client.OracleDbType.Varchar2)).Value = channel;
                    command.Parameters.Add(new Oracle.DataAccess.Client.OracleParameter("TRX_AMT", Oracle.DataAccess.Client.OracleDbType.Decimal, 100)).Value = amount;
                    command.Parameters.Add(new Oracle.DataAccess.Client.OracleParameter("CARD_TYPE", Oracle.DataAccess.Client.OracleDbType.Varchar2)).Value = cardType;
                    command.Parameters.Add(new Oracle.DataAccess.Client.OracleParameter("customer_no", Oracle.DataAccess.Client.OracleDbType.Varchar2)).Value = customerNo;
                    command.Parameters.Add(new Oracle.DataAccess.Client.OracleParameter("CUSTOMER_NAME", Oracle.DataAccess.Client.OracleDbType.Varchar2)).Value = customerName;

                    Oracle.DataAccess.Client.OracleParameter errorcodeparam = new Oracle.DataAccess.Client.OracleParameter("VERROR", Oracle.DataAccess.Client.OracleDbType.Varchar2);
                    errorcodeparam.Direction = ParameterDirection.Output;

                    command.Parameters.Add(errorcodeparam);

                    command.ExecuteNonQuery();

                    string errorMessage = command.Parameters["VERROR"].Value.ToString();
                    if (!string.IsNullOrWhiteSpace(errorMessage))
                    {
                        if (errorMessage.ToLower() != "null")
                        {
                            UnprocessedRec(billNumber, paymentRef);
                            Logger.TrackLogs("Invalid Bank Charge Entry Process for Transaction Type : '" + transactionType + "' , Customer No : '" + customerNo + "' , Customer Name : '" + customerName + "' , Card Type :'" + cardType + "' , Error Message : '" + errorMessage + "'", "after calling 'trx_bank_charge_entry' Procedure");
                        }
                    }
                    else
                        Logger.TrackLogs("Transaction Type : '" + transactionType + "' , Customer No : '" + customerNo + "' , Customer Name : '" + customerName + "' , Card Type :'" + cardType + "' , Added Successfully", "after calling 'trx_bank_charge_entry' Procedure");


                    command.Connection.Close();
                }
                #endregion
            }
            catch (Exception ex)
            {
                UnprocessedRec(billNumber, paymentRef);
                Logger.TrackLogs("Invalid Bank Charge Entry Process for Transaction Type : '" + transactionType + "' , Customer No : '" + customerNo + "' , Customer Name : '" + customerName + "' , Card Type :'" + cardType + "' , Error Message : '" + ex.Message + "'", "after calling 'trx_bank_charge_entry' Procedure");

            }
        }
        private NIWebService.response_card_payment NICardPaymentAPI(string cardNo, string channel, string amount, string description, string transRefNo, string paymentRef)
        {
            var results = new NIWebService.response_card_payment();
            try
            {
                using (NIWebService.BasicHttpsBinding_ICreditCardService service = new NIWebService.BasicHttpsBinding_ICreditCardService())
                {
                    var input = new NIWebService.request_card_payment
                    {
                        headerField = new NIWebService.HeaderType
                        {
                            versionField = System.Configuration.ConfigurationManager.AppSettings["CardPaymentVersion"],
                            bank_idField = System.Configuration.ConfigurationManager.AppSettings["CardPaymentBankID"],
                            instance_idField = string.Empty,
                            msg_functionField = System.Configuration.ConfigurationManager.AppSettings["CardPaymentMsgFunction"],
                            msg_idField = GetRandomNumber(1000, 9999).ToString(),
                            msg_typeField = System.Configuration.ConfigurationManager.AppSettings["CardPaymentMsgType"],
                            src_applicationField = System.Configuration.ConfigurationManager.AppSettings["CardPaymentSrcApplication"],
                            target_applicationField = System.Configuration.ConfigurationManager.AppSettings["CardPaymentTargetApplication"],
                            timestampField = DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:ss.fff"),
                            statusField = string.Empty,
                            tracking_idField = GetRandomNumber(1000, 9999).ToString(),
                            user_idField = string.Empty,
                            work_station_idField = string.Empty
                        },
                        bodyField = new NIWebService.CardPaymentReq()
                        {
                            card_noField = cardNo,
                            channelField = channel,
                            transaction_amountField = amount,
                            transaction_descriptionField = description,
                            txn_reference_idField = transRefNo
                        }
                    };

                    if (results != null)
                    {

                        results = service.AllowCreditCardPayment(input);

                        if (results != null)
                        {

                            if (results.exception_detailsField.statusField != "S")
                            {
                                UnprocessedRec(transRefNo, paymentRef);
                                Logger.TrackLogs("Invalid NI invoke Process for Transaction No : '" + transRefNo + "' , Description : '" + description + "' , Amount : '" + amount + "' , Channel :'" + channel + "' , after calling 'NI Webservice'", "NICardPaymentAPI");
                            }
                            else
                                Logger.TrackLogs("The Process for Transaction No : '" + transRefNo + "' , Description : '" + description + "' , Amount : '" + amount + "' , Channel :'" + channel + "' , Added after calling 'NI Webservice'", "NICardPaymentAPI");

                        }
                        else
                        {
                            UnprocessedRec(transRefNo, paymentRef);
                            Logger.TrackLogs("Invalid NI invoke Process for Transaction No : '" + transRefNo + "' , Description : '" + description + "' , Amount : '" + amount + "' , Channel :'" + channel + "' , after calling 'NI Webservice'", "NICardPaymentAPI");
                        }


                    }
                    else
                    {
                        UnprocessedRec(transRefNo, paymentRef);
                        Logger.TrackLogs("Invalid NI invoke Process for Transaction No : '" + transRefNo + "' , Description : '" + description + "' , Amount : '" + amount + "' , Channel :'" + channel + "' , after calling 'NI Webservice'", "NICardPaymentAPI");
                    }

                }
            }
            catch (Exception ex)
            {
                UnprocessedRec(transRefNo, paymentRef);
                Logger.TrackLogs("Invalid NI invoke Process for Transaction No : '" + transRefNo + "' , Description : '" + description + "' , Amount : '" + amount + "' , Channel :'" + channel + "' , Error Message '"+ex.Message+"' , after calling 'NI Webservice'", "NICardPaymentAPI");
                results = null;
            }
            return results;
        }
        private void UnprocessedRec(string billNumber, string paymentRefNo)
        {
            try
            {
                OracleSQLHelper.ExecuteNonQuery("AddRecord", CommandType.Text, "insert into SADADTransactions_Unprocessed(UniqueIdentifier,FileName,BillingAcct,Amount,ProcDate,PmtRefNo) select UniqueIdentifier,FileName,BillingAcct,Amount,ProcDate,PmtRefNo from SADADTransactions_Temp where BillingAcct='" + billNumber + "' and PmtRefNo='" + paymentRefNo + "'");
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex.Message, ex.StackTrace, "UnprocessedRec");
            }
        }


    }
}
