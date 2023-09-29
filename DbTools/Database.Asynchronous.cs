using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Reflection;
using MySqlConnector;

namespace DbTools;

public partial class Database {
    public async ValueTask DisposeAsync() {
        await _connection.DisposeAsync();
    }

    public async Task<T?> GetByIdAsync<T>(int id) where T : new() {
        var columns = GetColumns<T>().ToList();
        var tableInfo = GetTableName<T>();
        var primaryKey = GetPrimaryKey<T>();

        await using var cmd = new MySqlCommand(
            $"""
            select * from `{tableInfo.Name}`
            where @keyName = @keyValue
            """,
            _connection);
        cmd.Parameters.AddWithValue("@keyName", primaryKey.ColumnAttribute.Name);
        cmd.Parameters.AddWithValue("@keyValue", id);
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

            return obj;
        }

        return default;
    }

    public async IAsyncEnumerable<T> GetAsync<T>() where T : new() {
        var columns = GetColumns<T>().ToList();
        var tableInfo = GetTableName<T>();

        if (_connection.State != ConnectionState.Open) _connection.Open();
        await using var cmd = new MySqlCommand($"select * from `{tableInfo.Name}`", _connection);
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

            yield return obj;
        }
    }

    public async Task InsertAsync<T>(T obj) where T : new() {
        var columns = GetColumns<T>()
            .Where(it => it.Property.GetCustomAttribute<KeyAttribute>() is null)
            .ToList();
        var columnStr = string.Join(',', columns.Select(it => it.ColumnAttribute.Name!));
        var valuesStr = string.Join(',', columns.Select(it => '@' + it.ColumnAttribute.Name!));

        var tableInfo = GetTableName<T>();

        if (_connection.State != ConnectionState.Open) _connection.Open();
        await using var cmd = new MySqlCommand(
            $"""
             insert into `{tableInfo.Name}`({columnStr})
             values({valuesStr});
             """
        );
        foreach (var columnInfo in columns) {
            cmd.Parameters.AddWithValue(columnInfo.ParameterName, columnInfo.Property.GetValue(obj));
        }

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateAsync<T>(int id, T obj) where T : new() {
        var columns = GetColumns<T>()
            .Where(it => it.Property.GetCustomAttribute<KeyAttribute>() is null)
            .ToList();
        var keyName = GetPrimaryKey<T>().ColumnAttribute.Name;
        var setters = string.Join(",\n",
            columns.Select(it => $"{it.ColumnAttribute.Name} = {it.ParameterName}"));

        var tableInfo = GetTableName<T>();

        if (_connection.State != ConnectionState.Open) _connection.Open();
        await using var cmd = new MySqlCommand(
            $"""
             update `{tableInfo.Name}`
             set
                 {setters}
             where {keyName} = {id};
             """
            , _connection);
        cmd.Parameters.AddWithValue("@keyName", keyName);
        cmd.Parameters.AddWithValue("@keyValue", id);
        foreach (var columnInfo in columns) {
            cmd.Parameters.AddWithValue(columnInfo.ParameterName, columnInfo.Property.GetValue(obj));
        }

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task RemoveAsync<T>(T obj) {
        var key = GetPrimaryKey<T>();
        var tableInfo = GetTableName<T>();

        if (_connection.State != ConnectionState.Open) _connection.Open();
        await using var cmd = new MySqlCommand(
            $"""
             delete from `{tableInfo.Name}`
             where @keyName = @keyValue;
             """
            , _connection);
        cmd.Parameters.AddWithValue("@keyName", key.ColumnAttribute.Name);
        cmd.Parameters.AddWithValue("@keyValue", key.Property.GetValue(obj));

        await cmd.ExecuteNonQueryAsync();
    }
    
    
    public async IAsyncEnumerable<T> GetAsync<T>(int skip, int take = 10) where T : new() {
        var columns = GetColumns<T>().ToList();
        var tableInfo = GetTableName<T>();

        if (_connection.State != ConnectionState.Open) _connection.Open();
        await using var cmd = new MySqlCommand(
            $"""
             select * from `{tableInfo.Name}`
             limit {skip}, {take};
             """,
            _connection);
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

            yield return obj;
        }
    }
}