using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Common;
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
               .Where(
                   it => it.GetCustomAttribute<ColumnAttribute>() is not null
                         && it.GetCustomAttribute<DbTypeAttribute>() is not null
               )
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
        var props = typeof(T)
                    .GetProperties()
                    .Where(
                        it => it.GetCustomAttribute<ForeignKeyAttribute>() is not null
                    )
                    .ToList();
        var info = props.Select(
            it => new ForeignKeyInfo(
                it,
                it.GetCustomAttribute<ForeignKeyAttribute>()!
            ));
        return info;
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

    public record ForeignKeyInfo(PropertyInfo PropertyInfo, ForeignKeyAttribute ForeignKeyAttribute);

    private static T ResolveInnerJoin<T>(DbDataReader reader, ForeignKeyAttribute foreignKeyAttribute)
        where T : new() {
        var columns = GetColumns<T>().ToList();
        var tableInfo = GetTableName<T>();
        var primaryKey = GetPrimaryKey<T>();

        var obj = new T();
        foreach (var column in columns) {
            if (column.ColumnAttribute.Name is null) {
                throw new Exception($"Column attribute of property {column.Property.Name} of type {nameof(T)} " +
                                    "does not have a defined name");
            }

            column.Property.SetValue(obj, reader.GetValue(column.ColumnAttribute.Name));
        }

        return obj;
    }

    private static object? ResolveInnerJoin(Type type, DbDataReader reader, ForeignKeyAttribute foreignKeyAttribute) {
        var columns = GetColumns(type).ToList();
        var tableInfo = GetTableName(type);
        var primaryKey = GetPrimaryKey(type);

        var obj = Activator.CreateInstance(type);
        foreach (var column in columns) {
            if (column.ColumnAttribute.Name is null) {
                throw new Exception($"Column attribute of property {column.Property.Name} of type {type.Name} " +
                                    "does not have a defined name");
            }

            column.Property.SetValue(obj, reader.GetValue(column.ColumnAttribute.Name));
        }

        return obj;
    }

    private static IEnumerable<ColumnInfo> GetColumns(Type t) {
        return t
               .GetProperties()
               .Where(
                   it => it.GetCustomAttribute<ColumnAttribute>() is not null
                         && it.GetCustomAttribute<DbTypeAttribute>() is not null
               )
               .Select(
                   it => new ColumnInfo(
                       it,
                       it.GetCustomAttribute<ColumnAttribute>()!,
                       it.GetCustomAttribute<DbTypeAttribute>()!.DbType)
               );
    }

    private static TableInfo GetTableName(Type t) {
        var tableAttribute = t
            .GetCustomAttribute<TableAttribute>();
        if (tableAttribute is null) {
            throw new Exception($"Type {t.Name} does not have Table attribute");
        }

        return new TableInfo(t, tableAttribute!.Name);
    }

    public static IEnumerable<ForeignKeyInfo> GetForeignKeys(Type t) {
        var props = t
                    .GetProperties()
                    .Where(
                        it => it.GetCustomAttribute<ForeignKeyAttribute>() is not null
                    )
                    .ToList();
        var info = props.Select(
            it => new ForeignKeyInfo(
                it,
                it.GetCustomAttribute<ForeignKeyAttribute>()!
            ));
        return info;
    }

    public static ColumnInfo GetPrimaryKey(Type t) {
        var prop = t
                   .GetProperties()
                   .FirstOrDefault(it => it.GetCustomAttribute<KeyAttribute>() is not null
                                         && it.GetCustomAttribute<ColumnAttribute>() is not null);
        if (prop is null) {
            throw new Exception($"Type {t.Name} is not annotated with Key attribute");
        }

        return new ColumnInfo(
            prop,
            prop.GetCustomAttribute<ColumnAttribute>()!,
            prop.GetCustomAttribute<DbTypeAttribute>()!.DbType
        );
    }
}