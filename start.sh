 #!/bin/sh
 export LD_PRELOAD="./NWNX_Core.so"
 export NWNX_SQL_USERNAME="potm"
 export NWNX_SQL_PASSWORD="potmtest"
 export NWNX_SQL_DATABASE="potm"
 #export NWNX_SQL_TYPE="POSTGRESQL"
 export NWNX_METRICS_INFLUXDB_HOST="localhost"
 export NWNX_METRICS_INFLUXDB_PORT=8089
 export NWNX_MONO_ASSEMBLY="script_executor.so"
 export NWNX_CORE_LOG_LEVEL=7
 export NWNX_MONO_LOG_LEVEL=7
 export NWNX_MONO_CONFIG_PATH="/etc/mono/config"
 ./nwserver-linux -module "Exo's Script Tests" -servername "Exo's Script Tests" -dmpassword "123"
