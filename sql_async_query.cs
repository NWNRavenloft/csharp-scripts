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
            public NWN.Object db_object;
            public string[] results;
            public string callback_script;
            public NWN.Object callback_object;
            public string metadata;
        }

        public class QueryThreadParams
        {
            public NWN.Object db_object;
            public string query;
            public string values;
            public string callback_script;
            public NWN.Object callback_object;
            public string metadata;
        }

        static ConcurrentStack<QueryResult> result_stack = new ConcurrentStack<QueryResult>();
        static ConcurrentStack<QueryThreadParams> query_stack = new ConcurrentStack<QueryThreadParams>();

        static public int Main ()
        {
            if (!initialized)
            {
                Console.WriteLine("Not initialized yet, adding MainLoopTick callback..");
                Events.MainLoopTick += CheckQueryResult;
                Thread query_thread = new Thread(PollQueries);
                query_thread.IsBackground = true;
                query_thread.Start();
                initialized = true;
            }

            QueryThreadParams t_params = new QueryThreadParams();
            t_params.db_object = Object.OBJECT_SELF;
            t_params.query = NWScript.GetLocalString(t_params.db_object, "sql_query") as string;
            try 
            {
                t_params.values = (NWScript.GetLocalString(t_params.db_object, "sql_values") as string);
            }
            catch
            {
                t_params.values = "";
            }
            try
            {
                t_params.callback_script = (NWScript.GetLocalString(t_params.db_object, "sql_callback_script") as string);
                t_params.callback_object = (NWScript.GetLocalObject(t_params.db_object, "sql_callback_object"));
            }
            catch
            {
                t_params.callback_script = "";
                t_params.callback_object = null;
            }
            try
            {
                t_params.metadata = (NWScript.GetLocalString(t_params.db_object, "sql_metadata") as string);
            }
            catch
            {
                t_params.metadata = "";
            }
            query_stack.Push(t_params);
            
            return 1;
        }

        static public void PollQueries()
        { 
            while (true)
            {
                QueryThreadParams query_params;
                if (query_stack.TryPop(out query_params))
                    RunQuery(query_params);
                Thread.Sleep(1);
            }
        }

        static public void RunQuery(QueryThreadParams t_params)
        {
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

            Console.WriteLine("Executing reader.");
            Console.Out.Flush();
            IDataReader reader = dbcmd.ExecuteReader();
            while(reader.Read()) {
                string column_result = "";
                Console.WriteLine("Reading results: ", reader.FieldCount);
                Console.Out.Flush();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    column_result += reader[i];
                    if (i+1 < reader.FieldCount)
                        column_result += "¦";
                }
                result_list.Add(column_result);
            }

            Console.WriteLine("Pushing results.");
            Console.Out.Flush();
            QueryResult query_result = new QueryResult();
            query_result.db_object = t_params.db_object;
            query_result.results = result_list.ToArray();
            query_result.callback_script = t_params.callback_script;
            query_result.callback_object = t_params.callback_object;
            query_result.metadata = t_params.metadata;

            result_stack.Push(query_result);

            Console.WriteLine("Cleaning DB stuff..");
            Console.Out.Flush();
            dbcmd.Dispose();
            dbcmd = null;
            dbcon.Close();
            dbcon = null;

            Console.WriteLine("Cleaned!");
            Console.Out.Flush();
        }

        static public void CheckQueryResult(ulong frame)
        {
            QueryResult query_result;
            if (result_stack.TryPop(out query_result))
            {
                Console.WriteLine("Have a result!");
                if (query_result.callback_script.Length <= 0)
                    return;
                int i = 0;
                foreach (string result in query_result.results)
                {
                    NWScript.SetLocalString(query_result.db_object, "sql_result_" + i, result);
                    i++;
                }
                NWScript.SetLocalString(query_result.db_object, "sql_metadata", query_result.metadata);
                Console.WriteLine("Executing callback script " + query_result.callback_script);
                Console.Out.Flush();
                NWScript.ExecuteScript(query_result.callback_script, query_result.callback_object);
            }
        }
    }
}
