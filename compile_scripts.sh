csc -platform:x86 -r:System.dll -r:System.Data.dll -r:System.Threading.Tasks.Extensions.dll -r:System.Buffers.dll -r:MySqlConnector.dll -t:library -out:potm_scripts.so INTERNAL_Internal.cs INTERNAL_Types.cs INTERNAL_Events.cs INTERNAL_nwscript.cs nwnx.cs nwnx_events.cs sql_async_query.cs script_executor.cs