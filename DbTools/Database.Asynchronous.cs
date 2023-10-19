using System.Data;
using System.Diagnostics;
using System.Reflection;
using MySqlConnector;
using SqlKata;
using SqlKata.Compilers;
using KeyAttribute = System.ComponentModel.DataAnnotations.KeyAttribute;

namespace DbTools;

public partial class Database {
    public async ValueTask DisposeAsync() {
        await _connection.DisposeAsync();
    }

    private readonly MySqlCompiler _compiler = new MySqlCompiler();

    public async Task<T?> GetByIdAsync<T>(int id) where T : new() {
        var columns = GetColumns<T>().ToList();
        var tableInfo = GetTableName<T>();
        var primaryKey = GetPrimaryKey<T>();
        var foreignKeys = GetForeignKeys<T>().ToList();

        var query = new Query(tableInfo.Name);
        if (foreignKeys.Any()) {
            query = foreignKeys.Aggregate(
                query,
                (current, fK) => current.Join(fK.ForeignKeyAttribute.TableName, fK.ForeignKeyAttribute.First,
                                              fK.ForeignKeyAttribute.Second));
        }

        query = query
                .Select("*")
                .Where(primaryKey.ColumnAttribute.Name, id);

        var compiled = _compiler.Compile(query);

        await using var cmd = new MySqlCommand(compiled.ToString(), _connection);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync()) {
            var obj = new T();
            foreach (var column in columns) {
                if (column.ColumnAttribute.Name is null) {
                    throw new Exception($"Column attribute of property {column.Property.Name} of type {nameof(T)} " +
                                        "does not have a defined name");
                }

                column.Property.SetValue(obj, reader.GetValue(column.ColumnAttribute.Name));
            }

            foreach (var foreignKey in foreignKeys) {
                var value = ResolveInnerJoin(foreignKey.PropertyInfo.PropertyType, reader,
                                             foreignKey.ForeignKeyAttribute);
                foreignKey.PropertyInfo.SetValue(obj, value);
            }

            return obj;
        }

        return default;
    }

    public async IAsyncEnumerable<T> GetAsync<T>() where T : new() {
        var columns = GetColumns<T>().ToList();
        var tableInfo = GetTableName<T>();
        var foreignKeys = GetForeignKeys<T>().ToList();

        if (_connection.State != ConnectionState.Open) _connection.Open();

        var query = new Query(tableInfo.Name)
            .Select("*");
        if (foreignKeys.Any()) {
            query = foreignKeys.Aggregate(
                query,
                (current, fK) => current.Join(fK.ForeignKeyAttribute.TableName, fK.ForeignKeyAttribute.First,
                                              fK.ForeignKeyAttribute.Second));
        }

        var compiled = _compiler.Compile(query);

        await using var cmd = new MySqlCommand(compiled.ToString(), _connection);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync()) {
            var obj = new T();
            foreach (var column in columns) {
                if (column.ColumnAttribute.Name is null) {
                    throw new Exception($"Column attribute of property {column.Property.Name} of type {nameof(T)} " +
                                        "does not have a defined name");
                }

                var value = reader.GetValue(column.ColumnAttribute.Name);
                value = value is DBNull ? null : value;
                column.Property.SetValue(obj, value);
            }

            foreach (var foreignKey in foreignKeys) {
                var value = ResolveInnerJoin(foreignKey.PropertyInfo.PropertyType, reader,
                                             foreignKey.ForeignKeyAttribute);
                foreignKey.PropertyInfo.SetValue(obj, value);
            }

            yield return obj;
        }
    }

    public async Task<object?> InsertAsync<T>(T obj) where T : new() {
        var columns = GetColumns<T>()
                      .Where(it => it.Property.GetCustomAttribute<KeyAttribute>() is null)
                      .ToList();

        var tableInfo = GetTableName<T>();

        if (_connection.State != ConnectionState.Open) _connection.Open();

        var columnValue = new Dictionary<string, object>();
        foreach (var columnInfo in columns) {
            columnValue.Add(columnInfo.ColumnAttribute.Name, columnInfo.Property.GetValue(obj)!);
        }

        var query = new Query(tableInfo.Name)
            .AsInsert(columnValue, true);
        var compiled = _compiler.Compile(query);
        Debug.WriteLine($"Executing an insert query:\n{compiled.Sql}");
        await using var cmd = new MySqlCommand(compiled.ToString(), _connection);

        return await cmd.ExecuteScalarAsync();
    }

    public async Task UpdateAsync<T>(int id, T obj) where T : new() {
        var columns = GetColumns<T>()
                      .Where(it => it.Property.GetCustomAttribute<KeyAttribute>() is null)
                      .ToList();
        var keyName = GetPrimaryKey<T>().ColumnAttribute.Name;
        var tableInfo = GetTableName<T>();

        if (_connection.State != ConnectionState.Open) _connection.Open();

        var values =
            columns.Select(c => new KeyValuePair<string, object>(c.ColumnAttribute.Name!, c.Property.GetValue(obj)!));

        var query = new Query(tableInfo.Name)
                    .AsUpdate(values)
                    .Where(keyName, id);
        var compiled = _compiler.Compile(query);
        await using var cmd = new MySqlCommand(compiled.ToString(), _connection);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task RemoveAsync<T>(T obj) {
        var key = GetPrimaryKey<T>();
        var tableInfo = GetTableName<T>();

        if (_connection.State != ConnectionState.Open) _connection.Open();

        var query = new Query(tableInfo.Name)
                    .AsDelete()
                    .Where(key.ColumnAttribute.Name, key.Property.GetValue(obj));
        var compiled = _compiler.Compile(query);

        await using var cmd = new MySqlCommand(compiled.ToString(), _connection);

        await cmd.ExecuteNonQueryAsync();
    }


    public async IAsyncEnumerable<T> GetAsync<T>(int skip, int take = 10) where T : new() {
        var columns = GetColumns<T>().ToList();
        var tableInfo = GetTableName<T>();
        var foreignKeys = GetForeignKeys<T>();

        if (_connection.State != ConnectionState.Open) _connection.Open();

        var query = new Query(tableInfo.Name)
                    .Select("*")
                    .Skip(skip)
                    .Take(take);
        if (foreignKeys.Any()) {
            query = foreignKeys.Aggregate(
                query,
                (current, fK) => current.Join(fK.ForeignKeyAttribute.TableName, fK.ForeignKeyAttribute.First,
                                              fK.ForeignKeyAttribute.Second));
        }

        var compiled = _compiler.Compile(query);

        await using var cmd = new MySqlCommand(compiled.ToString(), _connection);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (reader.Read()) {
            var obj = new T();
            foreach (var column in columns) {
                if (column.ColumnAttribute.Name is null) {
                    throw new Exception($"Column attribute of property {column.Property.Name} of type {nameof(T)} " +
                                        "does not have a defined name");
                }

                column.Property.SetValue(obj, reader.GetValue(column.ColumnAttribute.Name));
            }

            foreach (var foreignKey in foreignKeys) {
                var value = ResolveInnerJoin(foreignKey.PropertyInfo.PropertyType, reader,
                                             foreignKey.ForeignKeyAttribute);
                foreignKey.PropertyInfo.SetValue(obj, value);
            }


            yield return obj;
        }
    }
}