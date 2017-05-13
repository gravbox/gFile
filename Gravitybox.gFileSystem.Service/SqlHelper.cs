using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gravitybox.gFileSystem.Service
{
    internal static class SqlHelper
    {
        public static object ExecuteWithReturn(string connectionString, string sql, List<SqlParameter> parameters)
        {
            try
            {
                object retval = null;
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        using (var command = connection.CreateCommand())
                        {
                            command.Transaction = transaction;
                            command.CommandTimeout = 10;
                            command.CommandText = sql;
                            command.CommandType = CommandType.Text;
                            command.Parameters.AddRange(parameters.ToList().Cast<ICloneable>().ToList().Select(x => x.Clone()).Cast<SqlParameter>().ToArray());
                            retval = command.ExecuteScalar();
                        }
                        transaction.Commit();
                    }
                }
                return retval;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
