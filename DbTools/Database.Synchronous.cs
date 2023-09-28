using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Reflection;
using MySqlConnector;

namespace DbTools; 

public partial class Database {
    /// <summary>
    /// Generic method that gets all rows from table using class annotations
    /// </summary>
    /// <typeparam name="T">Content type, annotated with Table, Key, Column attributes</typeparam>
    /// <returns>All table content</returns>
    public IEnumerable<T> Get<T>() where T : new() {
        var columns = GetColumns<T>().ToList();
        var tableInfo = GetTableName<T>();

        if (_connection.State != ConnectionState.Open) _connection.Open();
        using var cmd = new MySqlCommand($"select * from `{tableInfo.Name}`", _connection);
        var reader = cmd.ExecuteReader();

        while (reader.Read()) {
            var obj = new T();
            foreach (var column in columns) {
                if (column.ColumnAttribute.Name is null) {
                    throw new Exception(
                        $"Column attribute of property {column.Property.Name} of type {nameof(T)} " +
                                        "does not have a defined name");
                }

                column.Property.SetValue(obj, reader.GetValue(column.ColumnAttribute.Name));
            }

            yield return obj;
        }
    }

    public T? GetById<T>(int id) where T : new() {
        var columns = GetColumns<T>().ToList();
        var tableInfo = GetTableName<T>();
        var primaryKey = GetPrimaryKey<T>();

        using var cmd = new MySqlCommand($"select * from `{tableInfo.Name}` where `{primaryKey}` = {id}", _connection);
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) {
            var obj = new T();
            foreach (var column in columns) {
                if (column.ColumnAttribute.Name is null) {
                    throw new Exception($"Атрибут Column свойства {column.Property.Name} типа {nameof(T)} " +
                                        "не имеет заданного имени");
                }

                column.Property.SetValue(obj, reader.GetValue(column.ColumnAttribute.Name));
            }

            return obj;
        }

        cmd.Cancel();
        return default;
    }
    
    public void Dispose() {
        _connection.Dispose();
    }
}