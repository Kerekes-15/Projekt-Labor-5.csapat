using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;

namespace VTrailer.Models;

[Table("Audit_logs")]
public class AuditEntry : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("action")]
    public string? Action { get; set; } 

    [Column("table_name")]
    public string? TargetTable { get; set; }

    [Column("user_email")]
    public string? UserEmail { get; set; }

    [Column("details")]
    public string? Details { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
