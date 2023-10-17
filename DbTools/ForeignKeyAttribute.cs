namespace DbTools; 

[AttributeUsage(AttributeTargets.Property)]
public class ForeignKeyAttribute : Attribute {
    public string First { get; }
    public string Second { get; }
    public string TableName { get; }

    public ForeignKeyAttribute(string first, string second, string tableName) {
        First = first;
        Second = second;
        TableName = tableName;
    }
}