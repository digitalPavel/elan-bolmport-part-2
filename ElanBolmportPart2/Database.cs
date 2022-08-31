using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient; // requires adding via nuget

namespace JsonParsing;

class Database : IDisposable
{
    string connectionString = "";

    public string LastError { get; private set; }

    public Database(Configuration config)
    {
        connectionString = $"Data Source={config.SqlServer};Initial Catalog={config.SqlCatalog};User ID={config.SqlUser}; Password={config.SqlPassword}";
    }

    public Database()
    {
        Configuration config = new Configuration();

        connectionString = $"Data Source={config.SqlServer};Initial Catalog={config.SqlCatalog};User ID={config.SqlUser}; Password={config.SqlPassword}";
    }

    /// <summary>
    /// Runs the given query on the database.
    /// </summary>
    /// <param name="query">SQL query to run.</param>
    /// <param name="onlyReturnScalarResult">Indicates if we dont' want a full result set, but only the result of executeScalar returning the first row of the first record.</param>
    /// <returns>The result of the query as requested, or null if there was an error.  See LastError property for reason of the failure.</returns>
    private object RunQuery(string query, bool onlyReturnScalarResult = true)
    {
        try
        {
            LastError = "";
            DataSet results = new DataSet();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(query, connection))
                {
                    command.CommandTimeout = 10000;
                    if (onlyReturnScalarResult)
                    {
                        return command.ExecuteScalar();
                    }
                    else
                    {
                        var adapter = new SqlDataAdapter(command);
                        adapter.Fill(results);

                        return results;
                    }
                }
            }
        }
        catch (Exception exception)
        {
            LastError = exception.Message;
            return null;
        }
    }

    /// <summary>
    /// Runs a set of database queries bundled together as a transaction.  If any fail, none will commit.
    /// </summary>
    /// <param name="queries">The collection of sql query strings</param>
    /// <returns>Any error messages resulting from running, or an empty string if there were no known errors.</returns>
    public string RunTransactionSet(List<string> queries)
    {
        SqlTransaction transaction = null;
        using (var connection = new SqlConnection(connectionString))
        {
            try
            {
                connection.Open();
            }
            catch
            {
                return "Failed to open connection to database.";
            }
            transaction = connection.BeginTransaction();
            try
            {
                foreach (var query in queries)
                {
                    new SqlCommand(query, connection, transaction).ExecuteNonQuery();
                }

                transaction.Commit();
                return "";
            }
            catch (SqlException sqlException)
            {
                if (transaction != null)
                {
                    transaction.Rollback();
                }

                return sqlException.Message;
            }
            catch (Exception exception)
            {
                return exception.Message;
            }
        }
    }

    /// <summary>
    /// Runs the given query and returns the scalar result.
    /// </summary>
    /// <param name="query">SQL query to run.</param>
    /// <returns>The scalar result, or null on failure.  See LastError for failure reason.</returns>
    public object GetScalar(string query)
    {
        return RunQuery(query, true);
    }

    /// <summary>
    /// Runs a given SQL query.
    /// </summary>
    /// <param name="query">The SQL query to run.</param>
    /// <returns>A tuple with a string containing any direct failure messages, and the resulting dataset of the query.</returns>
    public Tuple<string, DataSet> GetResults(string query)
    {
        var result = (DataSet)RunQuery(query, false);
        return new Tuple<string, DataSet>(LastError, result);
    }

    #region IDisposable Support
    private bool disposedValue = false; // To detect redundant calls

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // nothing managed to actually dispose, since all db stuff is internally wrapped in "using" blocks anyway.
            }

            disposedValue = true;
        }
    }

    // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    // ~Database() {
    //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
    //   Dispose(false);
    // }

    // This code added to correctly implement the disposable pattern.
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        Dispose(true);
    }
    #endregion
}
