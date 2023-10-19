using System.Data;
using MySqlConnector;
using SqlKata;

namespace DbTools;

public partial class Database {
    public async Task<MySqlDataReader> ExecuteReaderAsync(string sql) {

        if (_connection.State != ConnectionState.Open) _connection.Open();

        await using var cmd = new MySqlCommand(sql, _connection);
        var reader = await cmd.ExecuteReaderAsync();
        return reader;
    }
    
    public MySqlDataReader ExecuteReader(string sql) {

        if (_connection.State != ConnectionState.Open) _connection.Open();

        using var cmd = new MySqlCommand(sql, _connection);
        var reader = cmd.ExecuteReader();
        return reader;
    }

    public async Task<T?> ExecuteScalarAsync<T>(string sql) {

        if (_connection.State != ConnectionState.Open) _connection.Open();

        await using var cmd = new MySqlCommand(sql, _connection);
        var obj = await cmd.ExecuteScalarAsync();
        if (obj is T value)
            return value;

        if (obj == null)
            return default;

        try {
            return (T)Convert.ChangeType(obj, typeof(T));
        }
        catch (InvalidCastException) {
            return default;
        }
    }

    public T? ExecuteScalar<T>(string sql) {

        if (_connection.State != ConnectionState.Open) _connection.Open();

        using var cmd = new MySqlCommand(sql, _connection);
        var obj = cmd.ExecuteScalar();
        if (obj is T value)
            return value;

        if (obj == null)
            return default;

        try {
            return (T)Convert.ChangeType(obj, typeof(T));
        }
        catch (InvalidCastException) {
            return default;
        }
    }

    public async Task<int> ExecuteNonQueryAsync(string sql) {

        if (_connection.State != ConnectionState.Open) _connection.Open();

        await using var cmd = new MySqlCommand(sql, _connection);
        return await cmd.ExecuteNonQueryAsync();
    }

    public int ExecuteNonQuery(string sql) {

        if (_connection.State != ConnectionState.Open) _connection.Open();

        using var cmd = new MySqlCommand(sql, _connection);
        return cmd.ExecuteNonQuery();
    }
}