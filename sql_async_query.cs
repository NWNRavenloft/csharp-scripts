using System;
using System.Threading;
using System.Data;
using MySql.Data.MySqlClient;
using NWN;
using System.Configuration;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace NWN.Scripts
{
    public class sql_async_query
    {
        static bool initialized = false;

        struct QueryResult
        {
            public NWN.Object obj;
            public string[] results;
            public string callback;
        }

        public class QueryThreadParams
        {
            public NWN.Object obj;
            public string query;
            public string values;
            public string callback;
        }

        static ConcurrentStack<QueryResult> result_stack = new ConcurrentStack<QueryResult>();

        static public int Main ()
        {
            if (!initialized)
            {
                Console.WriteLine("Not initialized yet, adding MainLoopTick callback..");
                Events.MainLoopTick += CheckQueryResult;
                initialized = true;
            }
            else
            {
                Console.WriteLine("Already initialized.");
            }

            Thread query_thread = new Thread(RunQuery);

            QueryThreadParams t_params = new QueryThreadParams();
            t_params.obj = Object.OBJECT_SELF;
            Console.WriteLine("Reading query..");
            Console.Out.Flush();
            t_params.query = NWScript.GetLocalString(Object.OBJECT_SELF, "sql_query") as string;
            Console.WriteLine("Reading values..");
            Console.Out.Flush();
            try 
            {
                t_params.values = (NWScript.GetLocalString(Object.OBJECT_SELF, "sql_values") as string);
            }
            catch
            {
                t_params.values = "";
            }
            Console.WriteLine("Reading callback..");
            Console.Out.Flush();
            try
            {
                t_params.callback = (NWScript.GetLocalString(Object.OBJECT_SELF, "sql_callback") as string);
            }
            catch
            {
                t_params.callback = "";
            }
            Console.Out.Flush();
            query_thread.Start(t_params);
            return 1;
        }

        static public void RunQuery(object _t_params)
        {
            QueryThreadParams t_params = (QueryThreadParams)_t_params;

            string connectionString =
              "database=potm;" +
              "user=potm;" +
              "password=potmtest;";

            MySqlConnection dbcon;
            dbcon = new MySqlConnection(connectionString);
            dbcon.Open();
            MySqlCommand dbcmd = dbcon.CreateCommand();

            dbcmd.CommandText = t_params.query;
            dbcmd.Prepare();

            // Read the values for our query for prepared statement parts
            int value_i = 1;
            while (t_params.values.Length > 0)
            {
                Console.WriteLine("Values: " + t_params.values);
                Console.Out.Flush();
                string param_value = "";
                Console.WriteLine("Index: " + t_params.values.IndexOf('¦'));
                Console.Out.Flush();
                if (t_params.values.IndexOf('¦') >= 0)
                {
                    param_value = t_params.values.Substring(0, t_params.values.IndexOf('¦'));
                    // Remove the value from the values string
                    t_params.values = t_params.values.Substring(t_params.values.IndexOf('¦') + 1); 
                }
                else
                {
                    param_value = t_params.values;
                    t_params.values = "";
                }
                Console.WriteLine("Value: " + param_value);
                Console.Out.Flush();
                dbcmd.Parameters.AddWithValue("@" + value_i.ToString(), param_value);
                value_i++;
            }

            List<string> result_list = new List<string>();

            IDataReader reader = dbcmd.ExecuteReader();
            while(reader.Read()) {
                string column_result = "";
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    column_result += reader[i];
                    if (i+1 < reader.FieldCount)
                        column_result += "¦";
                }
                result_list.Add(column_result);
            }

            QueryResult query_result = new QueryResult();
            query_result.obj = t_params.obj;
            query_result.results = result_list.ToArray();
            query_result.callback = t_params.callback;

            result_stack.Push(query_result);

            dbcmd.Dispose();
            dbcmd = null;
            dbcon.Close();
            dbcon = null;
        }

        static public void CheckQueryResult(ulong frame)
        {
            QueryResult query_result;
            if (result_stack.TryPop(out query_result))
            {
                if (query_result.callback.Length <= 0)
                    return;
                int i = 0;
                foreach (string result in query_result.results)
                {
                    NWScript.SetLocalString(query_result.obj, "sql_result_" + i, result);
                    i++;
                }
                NWScript.ExecuteScript(query_result.callback, query_result.obj);
            }
        }
    }
}
