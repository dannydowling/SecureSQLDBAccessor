using System;
using System.Data;
using System.Security.Permissions;
using System.Text.RegularExpressions;
using System.Net;
using Microsoft.Win32;
using System.Data.SqlClient;

namespace SecureSQL
{
    public class SqlSecureQuery
    {
        [SqlClientPermissionAttribute(SecurityAction.PermitOnly, AllowBlankPassword = false)]
        [RegistryPermissionAttribute(SecurityAction.PermitOnly, Read = @"HKEY_LOCAL_MACHINE\SOFTWARE\Client")]
        static string GetName(string Id)
        {
            SqlCommand cmd = null;
            string Status = "Name Unknown";

            try
            {
                //check for valid Shipping ID
                Regex r = new Regex(@"^\d{4,10}$");
                if (!r.Match(Id).Success)
                {
                    throw new Exception("Invalid Id");
                }

                //get connection string from registry
                SqlConnection sqlConn = new SqlConnection(ConnectionString);

                //Add shipping Id parameter
                string str = "sp_GetName";
                cmd = new SqlCommand(str, sqlConn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@ID", (SqlDbType)Convert.ToInt64(Id));

                cmd.Connection.Open();
                Status = cmd.ExecuteScalar().ToString();

            }
            catch (Exception e)
            {
                var Ip = HttpContext.Current.Request.HttpContext.Connection.RemoteIpAddress.ToString();
                var Ip2 = IPAddress.Loopback.ToString();

                if (Ip == Ip2)
                {
                    Status = e.ToString();
                }
                else
                {
                    Status = "error processing request";
                }
            }
            finally
            {
                //shut down the connection, even on failure
                if (cmd != null)
                {
                    cmd.Connection.Close();
                }
            }
            return Status;
        }
        //Get connection string
        internal static string ConnectionString
        {
            get
            {
                return (string)Registry
                    .LocalMachine
                    .OpenSubKey(@"SOFTWARE\Client\")
                    .GetValue("ConnectionString");
            }
        }

        public static class HttpContext
        {
            private static Microsoft.AspNetCore.Http.IHttpContextAccessor m_httpContextAccessor;
            public static void Configure(Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor)
            {
                m_httpContextAccessor = httpContextAccessor;
            }

            public static Microsoft.AspNetCore.Http.HttpContext Current
            {
                get
                {
                    return m_httpContextAccessor.HttpContext;
                }
            }
        }
    }
}

