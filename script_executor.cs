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
    public class script_executor
    {
        static bool initialized = false;

        struct ScriptExecutionDescription
        {
            public string script;
            public string payload;
        }

        static ConcurrentStack<ScriptExecutionDescription> execution_stack = new ConcurrentStack<ScriptExecutionDescription>();

        static NWN.Object db_object;

        static public int Main ()
        {
            if (!initialized)
            {
                Console.WriteLine("Not initialized yet, adding MainLoopTick callback..");
                Events.MainLoopTick += CheckScriptQuery;
                initialized = true;

                db_object = NWScript.GetObjectByTag("db_object");

                Thread poll_thread = new Thread(PollScriptsToExecute);
                poll_thread.IsBackground = true;
                poll_thread.Start();
            }
            else
            {
                Console.WriteLine("ERROR: Script Execturor already initialized. Why are we calling it?");
            }
            return 1;
        }

        static public void PollScriptsToExecute()
        {
            string connectionString =
              "database=potm;" +
              "user=potm;" +
              "password=potmtest;";

            MySqlConnection dbcon;
            dbcon = new MySqlConnection(connectionString);
            dbcon.Open();
            MySqlCommand dbcmd = dbcon.CreateCommand();
            dbcmd.CommandText = "SELECT * FROM scripts_to_execute";

            while (true)
            {
                Console.Out.Flush();
                IDataReader reader = dbcmd.ExecuteReader();
                List<Byte[]> to_delete = new List<Byte[]>();
                while(reader.Read()) {
                    Console.Out.Flush();
                    ScriptExecutionDescription ex_desc = new ScriptExecutionDescription();
                    ex_desc.script = reader[1] as string;
                    ex_desc.payload = reader[2] as string;
                    execution_stack.Push(ex_desc);
                    to_delete.Add((Byte[])reader[0]);
                }
                reader.Close();

                foreach (var id in to_delete)
                {
                    MySqlCommand del_ex_cmd = dbcon.CreateCommand();
                    Console.WriteLine("Data: " + System.Text.Encoding.ASCII.GetString(id));
                    // Convert to hex.
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    foreach (byte b in id)
                        sb.Append(b.ToString("X2"));

                    string hexString = sb.ToString();
                    Console.WriteLine("As hex: 0x" + hexString);
                    del_ex_cmd.CommandText = "DELETE FROM scripts_to_execute WHERE id=0x" + hexString + ";";
                    del_ex_cmd.ExecuteNonQuery();
                    del_ex_cmd.Dispose();
                    del_ex_cmd = null;
                }

                Thread.Sleep(250);
            }

            dbcmd.Dispose();
            dbcmd = null;
            dbcon.Close();
            dbcon = null;
        }

        static public void CheckScriptQuery(ulong frame)
        {
            ScriptExecutionDescription ex_desc;
            if (execution_stack.TryPop(out ex_desc))
            {
                if (ex_desc.script.Length <= 0)
                {
                    Console.WriteLine("Error: Popped ScriptExecutionDescription without script string set!");
                    return;
                }

                NWScript.SetLocalString(db_object, "script_payload", ex_desc.payload);
                NWScript.ExecuteScript(ex_desc.script, db_object);
            }
        }
    }
}
