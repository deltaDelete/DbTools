using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MySqlConnector;

namespace DbTools; 

public partial class Database : IDisposable, IAsyncDisposable {

    private MySqlConnection _connection;

    public Database(MySqlConnectionStringBuilder stringBuilder) : this(stringBuilder.ConnectionString) {
        
    }

    public Database(string connectionString) {
        _connection = new MySqlConnection(connectionString);
        _connection.Open();
    }

    private static IEnumerable<ColumnInfo> GetColumns<T>() {
        return typeof(T)
            .GetProperties()
            .Where(it => it.GetCustomAttribute<ColumnAttribute>() is not null)
            .Select(
                it => new ColumnInfo(
                    it, 
                    it.GetCustomAttribute<ColumnAttribute>()!, 
                    it.GetCustomAttribute<DbTypeAttribute>()!.DbType)
            );
    }

    private static TableInfo GetTableName<T>() {
        var tableAttribute = typeof(T)
            .GetCustomAttribute<TableAttribute>();
        if (tableAttribute is null) {
            throw new Exception($"Type {nameof(T)} does not have Table attribute");
        }
        return new TableInfo(typeof(T), tableAttribute!.Name);
    }

    public static IEnumerable<ForeignKeyInfo> GetForeignKeys<T>() {
        // var props = typeof(T)
        //     .GetProperties()
        //     .Where(
        //         it => it.GetCustomAttribute<ForeignKeyAttribute>() is not null
        //               && it.GetCustomAttribute<ColumnAttribute>() is not null)
        //     .ToList();
        // var info = props.Select(
        //     it => new Database.ForeignKeyInfo(
        //         it.GetCustomAttribute<ColumnAttribute>()!.Name!,
        //         it.GetCustomAttribute<ForeignKeyAttribute>()!.Name));
        // return info;
        throw new NotImplementedException("This method needs further improvements");
    }

    public static ColumnInfo GetPrimaryKey<T>() {
        var prop = typeof(T)
            .GetProperties()
            .FirstOrDefault(it => it.GetCustomAttribute<KeyAttribute>() is not null
                                  && it.GetCustomAttribute<ColumnAttribute>() is not null);
        if (prop is null) {
            throw new Exception($"Type {nameof(T)} is not annotated with Key attribute");
        }
        return new ColumnInfo(
            prop,
            prop.GetCustomAttribute<ColumnAttribute>()!,
            prop.GetCustomAttribute<DbTypeAttribute>()!.DbType
        );
    }

    public record ColumnInfo(PropertyInfo Property, ColumnAttribute ColumnAttribute, MySqlDbType DbType) {
        public string ParameterName => $"@{ColumnAttribute.Name}";
    };

    public record TableInfo(Type Type, String Name);

    public record ForeignKeyInfo(string Column, string ForeignColumn);
}