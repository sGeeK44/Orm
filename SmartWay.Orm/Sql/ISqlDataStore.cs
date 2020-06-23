using System.Collections.Generic;
using System.Data;
using SmartWay.Orm.Entity.Constraints;
using SmartWay.Orm.Interfaces;
using SmartWay.Orm.Queries;

namespace SmartWay.Orm.Sql
{
    public interface ISqlDataStore : IDataStore
    {
        IDbTransaction CurrentTransaction { get; }
        bool TableExists(string tableName);
        void CreateTable(IDbConnection connection, IEntityInfo entity);
        void VerifiyPrimaryKey(PrimaryKey primaryKey);
        void VerifyForeignKey(ForeignKey foreignKey);
        void VerifyIndex(Index index);
        void TruncateTable(string tableName);
        int ExecuteNonQuery(string sql);
        int ExecuteNonQuery(IDbCommand command);
        object ExecuteScalar(string sql);
        IDataReader ExecuteReader(string sql);

        IEnumerable<TIEntity> ExecuteReader<TIEntity>(IDbCommand command, IEntityBuilder<TIEntity> builder)
            where TIEntity : class;

        IDbConnection GetReadConnection();
        IDbConnection GetWriteConnection();
        void BeginTransaction();
        void Commit();
        void Rollback();
        void BeginTransaction(IsolationLevel unspecified);
        void Compact();
        void Shrink();
        void Optimize();
        void DropAllIndexes(IDbConnection connection);
        bool IndexExists(IDbConnection connection, IDbTransaction transaction, string tableName, string indexName);
        void DropIndex(IDbConnection connection, IDbTransaction transaction, string tableName, string indexName);
    }
}