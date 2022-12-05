using System.Data;
using System.Threading.Tasks;

using Dapper;
using MySqlConnector;

namespace MyShared
{
    public class DbConfig
    {
        public string host { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public string database { get; set; }
        public int port { get; set; }
    }

    public class InstanceData
    {
        public int id { get; set; }
        public int id_community { get; set; }
        public string database { get; set; }
        public string server_guid { get; set; }
    }

    public interface IDbCoreProps
    {
        int id_instance { get; set; }
        MySqlConnection dbCon { get; set; }
    }
    public class DbCore
    {
        public DbCore()
        {

        }

        public static DbConfig getDbConfig(bool replica = false)
        {
            var settings = AppConfig.Settings();
            dynamic props;
            if (replica)
            {
                props = settings.mysql["replica"];
            }
            else
            {
                props = bool.Parse(settings.isProduction) ? settings.mysql["master"] : settings.mysql["development"];
            }

            DbConfig config = new DbConfig
            {
                host = props.host,
                username = props.username,
                password = props.password,
                database = props.database.instancesDb,
                port = props.port
            };
            return config;
        }

        public static Task<MySqlConnection> getDbInstanceAsync(bool replica = false, string dbName = null)
        {
            return Task.Run(() =>
            {
                var config = DbCore.getDbConfig(replica);
                string defaultDb = dbName != null ? dbName : config.database;
                string link = string.Format(@" username={0};password={1};server={2};port={3};database={4}", config.username, config.password, config.host, config.port, defaultDb);
                MySqlConnection connection = new MySqlConnection(link);
                return connection;
            });
        }

        public static MySqlConnection getDbInstance(bool replica = false, string dbName = null)
        {
            var config = DbCore.getDbConfig(replica);
            string defaultDb = dbName != null ? dbName : config.database;
            string link = string.Format(@" username={0};password={1};server={2};port={3};database={4}", config.username, config.password, config.host, config.port, defaultDb);
            MySqlConnection connection = new MySqlConnection(link);
            return connection;
        }

        public static InstanceData getInstanceData(int id, bool replica = false)
        {
            var instanceData = new InstanceData();
            MySqlConnection dbCon = DbCore.getDbInstance(replica);
            using (dbCon)
            {
                DbConfig settings = DbCore.getDbConfig();
                dbCon.ChangeDatabaseAsync(settings.database);
                if (dbCon.State != ConnectionState.Open)
                {
                    dbCon.OpenAsync();
                }
                string sql = string.Format(@"SELECT id,id_community,`database`,server_guid 
                FROM `{0}`.`my__instance`
                WHERE id={1};", settings.database, id);
                instanceData = dbCon.QueryFirst<InstanceData>(sql);
            }
            dbCon.Close();
            return instanceData;
        }


    }


}