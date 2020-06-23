using System;
using System.Data;
using System.IO;
using Microsoft.Data.Sqlite;
using SmartWay.Orm.Sql;

namespace SmartWay.Orm.Sqlite
{
    public class SqliteDbEngine : IDbEngine
    {
        private readonly string _connectionString;
        private readonly string _datasource;

        public SqliteDbEngine(string datasource, string password)
        {
            _datasource = datasource;
            _connectionString = $"Data Source={_datasource};";

            if (!string.IsNullOrEmpty(password)) _connectionString += $"Password={password};";
        }

        public bool DatabaseExists => File.Exists(_datasource);

        public string Name { get; }

        public void DeleteDatabase()
        {
            if (!DatabaseExists)
                return;

            File.Delete(_datasource);
        }

        public void CreateDatabase()
        {
            using (File.Create(_datasource)) { }
        }

        public IDbConnection GetNewConnection()
        {
            return new SqliteConnection(_connectionString);
        }

        public void Compact()
        {
            throw new NotImplementedException();
        }

        public void RepairDb()
        {
            throw new NotImplementedException();
        }

        public void Shrink()
        {
            throw new NotImplementedException();
        }
    }
}