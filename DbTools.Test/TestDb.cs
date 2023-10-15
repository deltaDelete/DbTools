using DbTools;
using MySqlConnector;

namespace DbTools.Test;

public class TestDb : Database {
    public static readonly MySqlConnectionStringBuilder ConnectionStringBuilder = new MySqlConnectionStringBuilder() {
        Server = "localhost",
        Database = "test_db",
        UserID = "dev",
        Password = "devPassword"
    };

    public TestDb() : base(ConnectionStringBuilder.ConnectionString) {
    }
}