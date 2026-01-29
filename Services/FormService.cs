using Microsoft.EntityFrameworkCore;
using KSEB.Models;
using Npgsql;
using System.Text.Json;

public class FormService
{
    private readonly AppDbContext _context;
    private readonly ILogger<FormService> _logger;

    public FormService(AppDbContext context, ILogger<FormService> logger = null)
    {
        _context = context;
        _logger = logger;
    }

    // Create form using PostgreSQL procedure
    public void CreateForm(
        string title,
        string description,
        string createdBy,
        List<FormField> fields)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Form title is required");

        if (fields == null || !fields.Any())
            throw new ArgumentException("At least one field is required");

        var fieldsJson = JsonSerializer.Serialize(fields);

        try
        {
            // PostgreSQL: Use CALL for procedures
            _context.Database.ExecuteSqlRaw(
                "CALL sp_createform({0}, {1}, {2}, {3}::json)",
                title.Trim(),
                description?.Trim() ?? "",
                createdBy?.Trim() ?? "Anonymous",
                fieldsJson
            );

            _logger?.LogInformation("Form created: {Title}", title);
        }
        catch (PostgresException pgEx)
        {
            _logger?.LogError(pgEx, "PostgreSQL error creating form: {Title}", title);
            throw new Exception($"Database error: {pgEx.Message}", pgEx);
        }
    }

    // Get forms using PostgreSQL function
    public List<FormSummary> GetForms()
    {
        try
        {
            return _context.Set<FormSummary>()
                .FromSqlRaw("SELECT * FROM get_forms()")
                .AsNoTracking()
                .ToList();
        }
        catch (PostgresException pgEx)
        {
            _logger?.LogError(pgEx, "Error getting forms from PostgreSQL");
            // Fallback: query the table directly if function doesn't exist
            return _context.Set<FormSummary>()
                .FromSqlRaw("SELECT formid as FormId, formtitle as FormTitle, formdescription as FormDescription, createdby as CreatedBy, createdat as CreatedAt, updatedat as UpdatedAt, isactive as IsActive, tablename as TableName FROM tbl_forms ORDER BY createdat DESC")
                .AsNoTracking()
                .ToList();
        }
    }

    // Get fields from a form table
    public List<DynamicFormField> GetFields(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            return new List<DynamicFormField>();

        try
        {
            return _context.Set<DynamicFormField>()
                .FromSqlRaw("SELECT * FROM get_form_fields({0})", tableName.ToLower())
                .AsNoTracking()
                .ToList();
        }
        catch
        {
            // Fallback if function doesn't exist
            return new List<DynamicFormField>();
        }
    }

    // Save form data using stored procedure
    public void SaveFormData(
        string tableName,
        Dictionary<string, string> values,
        int submittedByUserId)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Table name is required");

        // Remove antiforgery token if exists
        values.Remove("__RequestVerificationToken");

        // Convert to JSON for PostgreSQL procedure
        var valuesJson = JsonSerializer.Serialize(values);

        try
        {
            _context.Database.ExecuteSqlRaw(
                "CALL save_form_data({0}, {1}::json, {2})",
                tableName.ToLower(),
                valuesJson,
                submittedByUserId
            );
        }
        catch (PostgresException pgEx)
        {
            _logger?.LogError(pgEx, "Error saving form data to table: {TableName}", tableName);
            throw new Exception($"Error saving data: {pgEx.Message}", pgEx);
        }
    }

    // Other methods remain the same...
}