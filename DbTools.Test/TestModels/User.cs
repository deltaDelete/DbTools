using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MySqlConnector;

namespace DbTools.Test.TestModels;

[Table("users")]
public class User {
    [Key]
    [Column("user_id")]
    [DbType(MySqlDbType.Int32)]
    public int Id { get; set; }

    [Column("full_name")]
    [DbType(MySqlDbType.VarChar)]
    public string FullName { get; set; } = string.Empty;

    [Column("gender_id")]
    [DbType(MySqlDbType.Int32)]
    public int GenderId { get; set; }

    [ForeignKey("users.gender_id", "genders.gender_id", "genders")]
    public Gender? Gender { get; set; }
}