using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SQLiteDBAccess.Decorators;

namespace SQLiteDBAccess
{
    public class SQLiteDBAccess
    {
        internal static List<SQLiteDBAccess> _dbAccesses = new List<SQLiteDBAccess>();

        public static SQLiteDBAccess Instance(string db, string path, bool isDBFileManaged = true)
        {
            SQLiteDBAccess dbAccess;
            if (IsCaseSensitiveFileSystem())
            {
                dbAccess = _dbAccesses.FirstOrDefault(dba => dba.dbName.Equals(db) && dba.dbFileFolderPath.Equals(path));

            }
            else
            {
                dbAccess = _dbAccesses.FirstOrDefault(dba => dba.dbName.ToLower().Equals(db.ToLower()) && dba.dbFileFolderPath.ToLower().Equals(path.ToLower()));

            }
            
            if (dbAccess != null)
            {
                dbAccess.IsFileManaged = isDBFileManaged;
                return dbAccess;
            }

            dbAccess = new SQLiteDBAccess(db, path, isDBFileManaged);
            _dbAccesses.Add(dbAccess);
            return dbAccess;
        }

        private string dbName;
        private string dbFileFolderPath;
        internal bool IsFileManaged;
        private SQLiteConnection con;
        private SQLiteCommand cmd;

        private SQLiteDBAccess(string db, string path, bool isDBFileManaged = true)
        {
            dbName = db;
            dbFileFolderPath = path;
            IsFileManaged = isDBFileManaged;
            if (File.Exists(path + $"/{db}.db")) return;
            using (File.Create(path + $"/{db}.db"));
        }
        
        [ManageFile]
        public void CreateTable(string tableName, string statement, bool replaceIfExists = true)
        {
            if (replaceIfExists)
            {
                cmd.CommandText = $"DROP TABLE IF EXISTS {tableName}";
                cmd.ExecuteNonQuery();
            }

            cmd.CommandText = $"CREATE TABLE IF NOT EXISTS {tableName}({statement})";
            cmd.ExecuteNonQuery();
        }
        
        [ManageFile]
        public void DropTable(string tableName)
        {
            cmd.CommandText = $"DROP TABLE IF EXISTS {tableName}";
            cmd.ExecuteNonQuery();
        }

        [ManageFile]
        public void Delete(string table, string attribute, string value)
        {
            cmd.CommandText = $"DELETE FROM {table} WHERE {attribute} = {value}";
            cmd.ExecuteNonQuery();
        }

        [ManageFile]
        public void Insert(string table, string keys, string values)
        {
            cmd.CommandText = $"INSERT INTO {table}({keys}) VALUES({values})";
            cmd.ExecuteNonQuery();
        }

        [ManageFile]
        public void UpdateSingle(string table, int id, string key, string value)
        {
            cmd.CommandText = $"UPDATE {table} SET {key} = {value} WHERE id = {id}";
            cmd.ExecuteNonQuery();
        }

        [ManageFile]
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

        [ManageFile(IsConnectionPreserved = true)]
        public SQLiteDataReader GetByAttribute(string table, string attribute, string value)
        {
            string command = $"SELECT * FROM {table} WHERE {attribute} = {value}";
            var getCmd = new SQLiteCommand(command, con);

            return getCmd.ExecuteReader();
        }

        [ManageFile(IsConnectionPreserved = true)]
        public SQLiteDataReader GetAll(string table)
        {
            string command = $"SELECT * FROM {table}";
            var getCmd = new SQLiteCommand(command, con);

            return getCmd.ExecuteReader();
        }

        [ManageFile]
        public bool CheckForExistingTable(string table)
        {
            string command = $"SELECT name FROM sqlite_master WHERE type= 'table' AND name={table}";
            var getCmd = new SQLiteCommand(command, con);
            var reader = getCmd.ExecuteReader();

            return reader.HasRows;
        }

        [ManageFile]
        public int GetLatestByAttribute(string table, string attribute)
        {
            string command = $"SELECT {attribute} FROM {table} ORDER BY {attribute} DESC LIMIT 1";

            var getCmd = new SQLiteCommand(command, con);
            var reader = getCmd.ExecuteReader();

            reader.Read();

            return reader.GetInt32(0);
        }

        [ManageFile]
        public bool CheckForExistingElementByAttribute(string table, string attribute, string value)
        {
            string command = $"SELECT id FROM {table} WHERE {attribute} like %{value}%";

            var getCmd = new SQLiteCommand(command, con);
            var reader = getCmd.ExecuteReader();

            return reader.Read();
        }

        public void OpenDBFile()
        {
            con = new SQLiteConnection(@"URI=file:" + dbFileFolderPath + $"/{dbName}.db");
            con.Open();
            cmd = new SQLiteCommand(con);
        }
        
        public void CloseDBFile()
        {
            con.Close();
            con.Dispose();
        }

        ~SQLiteDBAccess()
        {
            con.Close();
            con.Dispose();
        }

        private static bool IsCaseSensitiveFileSystem() {
            var tmp = Path.GetTempPath();
            return !Directory.Exists(tmp.ToUpper()) || !Directory.Exists(tmp.ToLower());
        }
    }
}