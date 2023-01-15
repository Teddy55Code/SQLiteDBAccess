using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using SQLiteDBAccess.Decorators;

namespace SQLiteDBAccess
{
    public class SQLiteDBAccess
    {
        private static List<SQLiteDBAccess> _dbAccesses = new List<SQLiteDBAccess>();

        /// <summary>
        /// Register a new SQLite Database file and instance of SQLiteDBAccess which is added to _dbAccesses.
        /// </summary>
        /// <param name="db">The Name of the db file. (Database creates Database.db)</param>
        /// <param name="path">The Path to the Folder that the Database should be created in.</param>
        /// <param name="isDBFileManaged">Set if the file should be automatically opened and closed. Exceptions are methods that return SQLiteDataReader they need to be closed manually.</param>
        /// <returns>an instance of SQLiteDBAccess</returns>
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
        
        /// <summary>
        /// Creates a new table with given attributes.
        /// Format: <code>CREATE TABLE IF NOT EXISTS {tableName}({statement})</code>
        /// </summary>
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
        
        /// <summary>
        /// Drop a table if it exists.
        /// Format: <code>DROP TABLE IF EXISTS {tableName}</code>
        /// </summary>
        [ManageFile]
        public void DropTable(string tableName)
        {
            cmd.CommandText = $"DROP TABLE IF EXISTS {tableName}";
            cmd.ExecuteNonQuery();
        }
        
        /// <summary>
        /// Delete all row from table where attribute is value.
        /// Format: <code>DELETE FROM {table} WHERE {attribute} = {value}</code>
        /// </summary>
        [ManageFile]
        public void Delete(string table, string attribute, string value)
        {
            cmd.CommandText = $"DELETE FROM {table} WHERE {attribute} = {value}";
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Insert row into table.
        /// Format: <code>INSERT INTO {table}({keys}) VALUES({values})</code>
        /// </summary>
        [ManageFile]
        public void Insert(string table, string keys, string values)
        {
            cmd.CommandText = $"INSERT INTO {table}({keys}) VALUES({values})";
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Update a single attribute in a row.
        /// Format <code>UPDATE {table} SET {key} = {value} WHERE {whereAttribute} = {whereValue}</code>
        /// </summary>
        [ManageFile]
        public void UpdateSingle(string table, string whereAttribute, string whereValue, string key, string value)
        {
            cmd.CommandText = $"UPDATE {table} SET {key} = {value} WHERE {whereAttribute} = {whereValue}";
            cmd.ExecuteNonQuery();
         }

        /// <summary>
        /// Update an entire row.
        /// columnList is columns formatted as "key = value"
        /// Format
        /// <code>UPDATE {table} SET {string.Join(", ", columnList)} WHERE {whereAttribute} = {whereValue}</code>
        /// </summary>
        [ManageFile]
        public void Update(string table, string whereAttribute, string whereValue, Dictionary<string, string> columns)
        {
            List<string> columnList = new List<string>();

            // Creates strings that can be used in an SQL statement from input dictionary.
            foreach (var pair in columns)
            {
                columnList.Add($"{pair.Key} = {pair.Value}");
            }

            cmd.CommandText = $"UPDATE {table} SET {string.Join(", ", columnList)} WHERE {whereAttribute} = {whereValue}";
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Get all rows in a table where attribute is value
        /// Format <code>SELECT * FROM {table} WHERE {attribute} = {value}</code>
        /// </summary>
        /// <returns>SQLiteDataReader</returns>
        [ManageFile(IsConnectionPreserved = true)]
        public SQLiteDataReader GetByAttribute(string table, string attribute, string value)
        {
            string command = $"SELECT * FROM {table} WHERE {attribute} = {value}";
            var getCmd = new SQLiteCommand(command, con);

            return getCmd.ExecuteReader();
        }

        /// <summary>
        /// Get all rows of a table
        /// Format: <code>SELECT * FROM {table}</code>
        /// </summary>
        /// <returns>SQLiteDataReader</returns>
        [ManageFile(IsConnectionPreserved = true)]
        public SQLiteDataReader GetAll(string table)
        {
            string command = $"SELECT * FROM {table}";
            var getCmd = new SQLiteCommand(command, con);

            return getCmd.ExecuteReader();
        }

        /// <summary>
        /// Check if a table exists.
        /// Format: <code>SELECT name FROM sqlite_master WHERE type= 'table' AND name={table}</code>
        /// </summary>
        /// <returns>if the table exists</returns>
        [ManageFile]
        public bool CheckForExistingTable(string table)
        {
            string command = $"SELECT name FROM sqlite_master WHERE type= 'table' AND name={table}";
            var getCmd = new SQLiteCommand(command, con);
            var reader = getCmd.ExecuteReader();

            return reader.HasRows;
        }
        
        /// <summary>
        /// Get the row in a table where attribute is the highest. The main purpose of this method is to get the row with the highest id. (the last added row)
        /// Format <code>SELECT {attribute} FROM {table} ORDER BY {attribute} DESC LIMIT 1</code>
        /// </summary>
        /// <returns>SQLiteDataReader</returns>
        [ManageFile(IsConnectionPreserved = true)]
        public SQLiteDataReader GetLatestByAttribute(string table, string attribute)
        {
            string command = $"SELECT {attribute} FROM {table} ORDER BY {attribute} DESC LIMIT 1";
            var getCmd = new SQLiteCommand(command, con);
            
            return getCmd.ExecuteReader();
        }

        /// <summary>
        /// Check if a row where attribute has value exists in a table.
        /// Format: <code>SELECT * FROM {table} WHERE {attribute} = {value}</code>
        /// </summary>
        /// <returns>if row exists</returns>
        [ManageFile]
        public bool CheckForExistingElementByAttribute(string table, string attribute, string value)
        {
            string command = $"SELECT * FROM {table} WHERE {attribute} = {value}";

            var getCmd = new SQLiteCommand(command, con);
            var reader = getCmd.ExecuteReader();

            return reader == null || reader.Read();
        }

        /// <summary>
        /// Opens and loads the Database file.
        /// </summary>
        public void OpenDBFile()
        {
            con = new SQLiteConnection(@"URI=file:" + dbFileFolderPath + $"/{dbName}.db");
            con.Open();
            cmd = new SQLiteCommand(con);
        }
        
        /// <summary>
        /// Closes the Database file.
        /// </summary>
        /// <param name="reader">A reader that is due to be closed.</param>
        public void CloseDBFile(SQLiteDataReader reader = null)
        {
            if (reader != null) reader.Dispose();
            con.Dispose();
            GC.Collect();
        }

        ~SQLiteDBAccess()
        {
            con.Dispose();
        }

        private static bool IsCaseSensitiveFileSystem() {
            var tmp = Path.GetTempPath();
            return !Directory.Exists(tmp.ToUpper()) || !Directory.Exists(tmp.ToLower());
        }
    }
}