using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MySqlConnector;

namespace DbTools.Test.TestModels;

[Table("genders")]
public class Gender {
    [Key]
    [Column("gender_id")]
    [DbType(MySqlDbType.Int32)]
    public int Id { get; set; }
    
    [Column("gender_name")]
    [DbType(MySqlDbType.VarChar)]
    public string Name { get; set; } = string.Empty;
}