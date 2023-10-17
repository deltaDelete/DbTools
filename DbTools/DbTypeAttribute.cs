using MySqlConnector;

namespace DbTools;

[AttributeUsage(AttributeTargets.Property)]
public class DbTypeAttribute : Attribute {
    public DbTypeAttribute(MySqlDbType type) {
        DbType = type;
    }

    public MySqlDbType DbType { get; }
}