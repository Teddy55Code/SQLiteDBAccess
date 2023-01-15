using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace SQLiteDBAccess
{
    public class SQLiteDBAccess
    {
        private static List<SQLiteDBAccess> _dbAccesses = new List<SQLiteDBAccess>();

        public static SQLiteDBAccess Instance(string db, string path)
        {
            var dbAccess = _dbAccesses.FirstOrDefault(dba => dba.dbName.Equals(db) && dba.dbFileFolder.Equals(path));
            if (dbAccess != null) return dbAccess;

            dbAccess = new SQLiteDBAccess(db, path);
            _dbAccesses.Add(dbAccess);
            return dbAccess;
        }

        private string dbName;
        private string dbFileFolder;
        private SQLiteConnection con;
        private SQLiteCommand cmd;

        private SQLiteDBAccess(string db, string path)
        {
            dbName = db;
            dbFileFolder = path;
            if (!File.Exists(path + $"\\{db}.db"))
            {
                using (File.Create(path + $"\\{db}.db"));
            }
            
            path = @"URI=file:" + path + $"\\{db}.db";
            con = new SQLiteConnection(path);
            con.Open();
            cmd = new SQLiteCommand(con);
        }

        public void CreateTable(string tableName, string statement)
        {
            cmd.CommandText = $"DROP TABLE IF EXISTS {tableName}";
            cmd.ExecuteNonQuery();

            cmd.CommandText = $"CREATE TABLE {tableName}({statement})";
            cmd.ExecuteNonQuery();
        }
        
        public void DropTable(string tableName)
        {
            cmd.CommandText = $"DROP TABLE IF EXISTS {tableName}";
            cmd.ExecuteNonQuery();
        }

        public void Delete(string table, string attribute, string value)
        {
            cmd.CommandText = $"DELETE FROM {table} WHERE {attribute} = '{value}'";
            cmd.ExecuteNonQuery();
        }

        public void Insert(string table, string keys, string values)
        {
            cmd.CommandText = $"INSERT INTO {table}({keys}) VALUES({values})";
            cmd.ExecuteNonQuery();
        }

        public void UpdateSingle(string table, int id, string key, string value)
        {
            cmd.CommandText = $"UPDATE {table} SET {key} = {value} WHERE id = {id}";
            cmd.ExecuteNonQuery();
        }

        public void Update(string table, int id, Dictionary<string, string> columns)
        {
            List<string> columnList = new List<string>();

            // Creates strings that can be used in an SQL statement from input dictionary.
            foreach (var pair in columns)
            {
                columnList.Add($"{pair.Key} = {pair.Value}");
            }

            cmd.CommandText = $"UPDATE {table} SET {string.Join(", ", columnList)} WHERE id = {id}";
            cmd.ExecuteNonQuery();
        }

        public SQLiteDataReader GetByAttribute(string table, string attribute, string value)
        {
            string command = $"SELECT * FROM {table} WHERE {attribute} = '{value}'";
            var getCmd = new SQLiteCommand(command, con);

            return getCmd.ExecuteReader();
        }

        public SQLiteDataReader GetAll(string table)
        {
            string command = $"SELECT * FROM {table}";
            var getCmd = new SQLiteCommand(command, con);

            return getCmd.ExecuteReader();
        }

        public bool CheckForExistingTable(string table)
        {
            string command = $"SELECT name FROM sqlite_master WHERE type='table' AND name='{table}'";
            var getCmd = new SQLiteCommand(command, con);
            var reader = getCmd.ExecuteReader();

            return reader.HasRows;
        }

        public int GetLatestByAttribute(string table, string attribute)
        {
            string command = $"SELECT {attribute} FROM {table} ORDER BY {attribute} DESC LIMIT 1";

            var getCmd = new SQLiteCommand(command, con);
            var reader = getCmd.ExecuteReader();

            reader.Read();

            return reader.GetInt32(0);
        }

        public bool CheckForExistingElementByAttribute(string table, string attribute, string value)
        {
            string command = $"SELECT id FROM {table} WHERE {attribute} like '%{value}%'";

            var getCmd = new SQLiteCommand(command, con);
            var reader = getCmd.ExecuteReader();

            return reader.Read();
        }
    }
}