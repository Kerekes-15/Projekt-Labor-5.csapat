using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace VTrailer.Models;

[Table("Users")]
public class User : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("FullName")]
    public string? FullName { get; set; }

    [Column("Password")]
    public string? Password { get; set; }

    [Column("Role")]
    public string? Role { get; set; }

    [Column("Email")]
    public string? Email { get; set; }

    [Column("PhoneNumber")]
    public string? PhoneNumber { get; set; }
}
