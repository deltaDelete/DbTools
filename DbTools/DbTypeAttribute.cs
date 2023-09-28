using MySqlConnector;

namespace DbTools;

public class DbTypeAttribute : Attribute {
    public DbTypeAttribute(MySqlDbType type) {
        DbType = type;
    }

    public MySqlDbType DbType { get; }
}