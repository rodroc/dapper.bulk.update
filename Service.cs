using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;

using Dapper;
using Faithlife.Utility.Dapper;
using MySqlConnector;

using MyShared;
using MyProfileOnly.Models;

namespace MyProfileOnly
{
    public class Service
    {
        private readonly ConfigSettings settings = AppConfig.Settings();
        private readonly DbConfig dbConfig = DbCore.getDbConfig();
        private string instancesDb = "";
        private MySqlConnection dbReplica;
        private MySqlConnection dbCon;

        public Service()
        {
            this.instancesDb = this.dbConfig.database;
            this.dbCon = DbCore.getDbInstance();
            this.dbReplica = DbCore.getDbInstance(true);
        }

        public ConfigSettings GetSettings()
        {
            return this.settings;
        }

        public async Task<IEnumerable<InstanceData>> GetActiveInstances()
        {
            if (this.dbReplica.State != ConnectionState.Open)
            {
                await this.dbReplica.OpenAsync();
            }
            await this.dbReplica.ChangeDatabaseAsync(this.instancesDb);

            string sql = string.Format(@"SELECT i.id,i.id_community,s.schema_name as `database`,i.server_guid
                FROM information_schema.schemata s
                JOIN information_schema.tables t ON t.table_schema = s.schema_name
                JOIN `{0}`.`my__instance` i ON i.`database`=s.SCHEMA_NAME AND i.is_active=1
                JOIN `{0}`.`myi__import` m ON m.id_instance=i.id AND m.integration_name {1} IN ('test')
                WHERE s.schema_name NOT IN ('test_server')
                AND t.table_name LIKE 'table__profiler'
                GROUP BY s.schema_name;
                ", this.instancesDb, bool.Parse(this.settings.isProduction) ? " NOT " : "");
            return await this.dbReplica.QueryAsync<InstanceData>(sql);
        }

		public async Task<IEnumerable<InstanceData>> GetInstancesByIDs(List<int> list)
        {
            if (list.Count == 0)
            {
                await Task.Run(() => { new List<InstanceData>(); });
            }
            var idList = string.Join(",", list);
            if (this.dbReplica.State != ConnectionState.Open)
            {
                await this.dbReplica.OpenAsync();
            }
            await this.dbReplica.ChangeDatabaseAsync(this.instancesDb);
            string sql = string.Format(@"SELECT i.id,i.id_community,s.schema_name as `database`,i.server_guid
                FROM information_schema.schemata s
                JOIN information_schema.tables t ON t.table_schema = s.schema_name
                JOIN `{0}`.`my__instance` i ON i.`database`=s.SCHEMA_NAME AND i.id IN ({1})
                JOIN `{0}`.`myi__import` m ON m.id_instance=i.id
                WHERE s.schema_name NOT IN ('test_server')
                AND t.table_name LIKE 'table__profiler'
                GROUP BY s.schema_name;
                ", this.instancesDb, idList);
            return await this.dbReplica.QueryAsync<InstanceData>(sql);                                                                      
        }

        public async Task<IEnumerable<UserProfile>> GetUserProfilesWithoutEmail(InstanceData instance)
        {
            if (this.dbReplica.State != ConnectionState.Open)
            {
                await this.dbReplica.OpenAsync();
            }
            await this.dbReplica.ChangeDatabaseAsync(instance.database);
            string sql = string.Format(@"SELECT p.id AS id_profile,p.display_name AS profileDisplayName,p.email AS profileEmail,p.id_user,r.username,r.email as userEmail,p.is_valid_email,p.is_local
                FROM `{0}`.`table__profiler` p
                LEFT JOIN `{1}`.`my__role` r ON r.id=p.id_user
                WHERE p.is_deleted=0 AND (p.email IS NULL OR TRIM(p.email)='') AND
                    (r.email IS NULL OR TRIM(r.email)='')
                ;
                ", instance.database, this.instancesDb);
            return await this.dbReplica.QueryAsync<UserProfile>(sql);                                                                     
        }

        public async Task<int> BulkUpdate(InstanceData instance, List<ProfileOnly> list)
        {
            int inserted = 0;
            string temp = "TEMPORARY";
            string tableName = string.Format(@"`{0}`.`{1}`", instance.database, "tmp_profileonly");
            try
            {
                if (this.dbCon.State != ConnectionState.Open)
                {
                    await this.dbCon.OpenAsync();
                }
                await this.dbCon.ChangeDatabaseAsync(instance.database);
                using (MySqlCommand cmd = new MySqlCommand("", this.dbCon))
                {
                    var sql = string.Format(@"DROP {0} TABLE IF EXISTS {1} ;CREATE {0} TABLE {1}(
                            pk INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                            id_profile INT NOT NULL
                        )", temp, tableName);
                    cmd.CommandText = sql;
                    await cmd.ExecuteNonQueryAsync();
                    sql = string.Format(@"INSERT INTO {0}(id_profile) VALUES(@id_profile)...", tableName);
                    inserted = await this.dbCon.BulkInsertAsync(sql, list);
                    if (inserted > 0)
                    {
                        sql = string.Format(@"UPDATE `{0}`.`table__profiler` p
                            JOIN {1} t ON t.id_profile=p.id
                            SET p.is_local=1", instance.database, tableName);
                        cmd.CommandText = sql;
                        await cmd.ExecuteNonQueryAsync();
                    }
                    sql = string.Format(@"DROP {0} TABLE IF EXISTS {1} ;", temp, tableName);
                    cmd.CommandText = sql;
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch (Exception error)
            {
                Console.WriteLine(error);
            }
            return inserted;

        }

    }

}