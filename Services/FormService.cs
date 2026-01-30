using KSEB.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Text.Json;

public class FormService
{
    private readonly AppDbContext _context;
    private readonly ILogger<FormService> _logger;

    public FormService(AppDbContext context, ILogger<FormService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // 1. CREATE FORM
    public int CreateForm(string title, string description, string createdBy, List<FormField> fields, bool isActive = true)
    {
        try
        {
            var fieldsJson = JsonSerializer.Serialize(fields);

            using (var connection = _context.Database.GetDbConnection())
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    // Use the correct parameter names that match your function
                    command.CommandText = "SELECT create_form(@p_form_title, @p_form_description, @p_created_by, @p_fields_json::jsonb)";

                    command.Parameters.Add(new NpgsqlParameter("@p_form_title", title));
                    command.Parameters.Add(new NpgsqlParameter("@p_form_description", description ?? ""));
                    command.Parameters.Add(new NpgsqlParameter("@p_created_by", createdBy));
                    command.Parameters.Add(new NpgsqlParameter("@p_fields_json", fieldsJson));
                    // Note: Your function doesn't have p_is_active parameter

                    var result = command.ExecuteScalar();
                    var formId = Convert.ToInt32(result);

                    _logger.LogInformation("Form created with ID: {FormId}", formId);
                    return formId;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating form: {Title}", title);
            throw new Exception($"Failed to create form: {ex.Message}", ex);
        }
    }
   
    // 2. GET ALL FORMS
    public List<FormSummary> GetForms()
    {
        try
        {
            return _context.Forms
                .FromSqlRaw("SELECT * FROM vw_forms")
                .AsNoTracking()
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting forms");
            throw new Exception($"Failed to get forms: {ex.Message}", ex);
        }
    }

    // 3. GET FORM FIELDS - FIXED! Using table structure instead of form_definitions
    public List<FormField> GetFormFields(int formId)
    {
        try
        {
            var forms = GetForms();
            var form = forms.FirstOrDefault(f => f.FormId == formId);

            if (form == null)
                throw new Exception($"Form {formId} not found");

            if (string.IsNullOrEmpty(form.TableName))
                throw new Exception($"Form {formId} has no table name");

            _logger.LogInformation($"Getting fields for table: {form.TableName}");

            using (var connection = _context.Database.GetDbConnection())
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    // Use get_form_fields function which already exists
                    command.CommandText = "SELECT * FROM get_form_fields(@tableName)";
                    command.Parameters.Add(new NpgsqlParameter("@tableName", form.TableName));

                    var fields = new List<FormField>();
                    int order = 1;

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var columnName = reader["column_name"]?.ToString();
                            var dataType = reader["data_type"]?.ToString();

                            // Use EXACT column name from database (preserve case)
                            fields.Add(new FormField
                            {
                                FieldName = columnName, // This will be "Name" (capital N)
                                FieldType = MapDataTypeToFieldType(dataType),
                                IsRequired = false, // Based on your table, "Name" is nullable
                                Placeholder = $"Enter {columnName}",
                                FieldOrder = order++
                            });
                        }
                    }

                    _logger.LogInformation($"Found {fields.Count} columns: {string.Join(", ", fields.Select(f => f.FieldName))}");
                    return fields;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting form fields");

            // Return field with exact column name "Name"
            return new List<FormField>
        {
            new FormField
            {
                FieldName = "Name", // Capital N
                FieldType = "text",
                IsRequired = true,
                Placeholder = "Enter Name",
                FieldOrder = 1
            }
        };
        }
    }
    private string MapDataTypeToFieldType(string dataType)
    {
        if (string.IsNullOrEmpty(dataType))
            return "text";

        dataType = dataType.ToLower();

        return dataType switch
        {
            "integer" or "bigint" or "numeric" => "number",
            "boolean" => "checkbox",
            "date" => "date",
            "timestamp" => "datetime",
            "text" or "character varying" => "text",
            _ => "text"
        };
    }

    private List<FormField> GetSampleFields()
    {
        return new List<FormField>
        {
            new FormField
            {
                FieldName = "sample_field",
                FieldType = "text",
                IsRequired = true,
                Placeholder = "Enter data here",
                FieldOrder = 1
            }
        };
    }

    // 4. SAVE FORM DATA
    public void SaveFormData(int formId, Dictionary<string, string> values, int submittedByUserId)
    {
        NpgsqlConnection connection = null;

        try
        {
            _logger.LogInformation($"Starting SaveFormData for form {formId}");

            values.Remove("__RequestVerificationToken");

            // 1. Open ONE connection
            connection = new NpgsqlConnection(_context.Database.GetConnectionString());
            connection.Open();

            // 2. Get form info and field types using the SAME connection
            var (tableName, fieldTypes) = GetFormInfoAndTypes(connection, formId);

            if (string.IsNullOrEmpty(tableName))
                throw new Exception($"Form {formId} not found or has no table");

            _logger.LogInformation($"Table: {tableName}, Fields: {string.Join(", ", fieldTypes.Keys)}");

            // 3. Prepare SQL with correct types
            var (columns, parameters, sqlParams) = PrepareInsertData(values, fieldTypes, submittedByUserId);

            // 4. Build and execute SQL using the SAME connection
            var sql = BuildInsertSql(tableName, columns, parameters);

            _logger.LogInformation($"Executing SQL: {sql}");

            using (var command = new NpgsqlCommand(sql, connection))
            {
                command.Parameters.AddRange(sqlParams);
                int rowsAffected = command.ExecuteNonQuery();
                _logger.LogInformation($"Insert successful. Rows affected: {rowsAffected}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error saving form data for form {formId}");
            throw new Exception($"Failed to save form data: {ex.Message}", ex);
        }
        finally
        {
            // 5. Clean up connection properly
            connection?.Close();
            connection?.Dispose();
        }
    }

    // Helper methods using the same connection
    private (string tableName, Dictionary<string, string> fieldTypes) GetFormInfoAndTypes(NpgsqlConnection connection, int formId)
    {
        string tableName = "";
        var fieldTypes = new Dictionary<string, string>();

        try
        {
            // Get form table name
            using (var command = new NpgsqlCommand())
            {
                command.Connection = connection;
                command.CommandText = @"
                SELECT table_name 
                FROM forms 
                WHERE form_id = @formId";

                command.Parameters.AddWithValue("@formId", formId);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        tableName = reader["table_name"]?.ToString();
                    }
                }
            }

            if (!string.IsNullOrEmpty(tableName))
            {
                // Get column types from the table
                using (var command = new NpgsqlCommand())
                {
                    command.Connection = connection;
                    command.CommandText = @"
                    SELECT column_name, data_type
                    FROM information_schema.columns 
                    WHERE LOWER(table_name) = LOWER(@tableName)
                    AND column_name NOT IN ('id', 'submitted_by_user_id', 'created_at')
                    ORDER BY ordinal_position";

                    command.Parameters.AddWithValue("@tableName", tableName);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var columnName = reader["column_name"]?.ToString();
                            var dataType = reader["data_type"]?.ToString();

                            if (!string.IsNullOrEmpty(columnName))
                            {
                                fieldTypes[columnName] = MapPgDataTypeToFieldType(dataType);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting form info and types");
        }

        return (tableName, fieldTypes);
    }

    private (List<string> columns, List<string> parameters, NpgsqlParameter[] sqlParams)
        PrepareInsertData(Dictionary<string, string> values, Dictionary<string, string> fieldTypes, int submittedByUserId)
    {
        var columns = new List<string>();
        var parameters = new List<string>();
        var sqlParams = new List<NpgsqlParameter>();

        int paramIndex = 0;

        // Add form fields
        foreach (var kvp in values)
        {
            if (fieldTypes.ContainsKey(kvp.Key))
            {
                var fieldType = fieldTypes[kvp.Key];
                var value = kvp.Value;

                columns.Add($"\"{kvp.Key}\"");
                parameters.Add($"@p{paramIndex}");

                var paramValue = ConvertFormValue(value, fieldType);
                sqlParams.Add(new NpgsqlParameter($"@p{paramIndex}", paramValue));

                paramIndex++;
            }
        }

        // Add system columns
        columns.Add("submitted_by_user_id");
        parameters.Add($"@p{paramIndex}");
        sqlParams.Add(new NpgsqlParameter($"@p{paramIndex}", submittedByUserId));
        paramIndex++;

        columns.Add("created_at");
        parameters.Add($"@p{paramIndex}");
        sqlParams.Add(new NpgsqlParameter($"@p{paramIndex}", DateTime.Now));

        return (columns, parameters, sqlParams.ToArray());
    }

    private string BuildInsertSql(string tableName, List<string> columns, List<string> parameters)
    {
        var columnsStr = string.Join(", ", columns);
        var parametersStr = string.Join(", ", parameters);

        return $"INSERT INTO \"{tableName}\" ({columnsStr}) VALUES ({parametersStr})";
    }

    private object ConvertFormValue(string value, string fieldType)
    {
        if (string.IsNullOrWhiteSpace(value))
            return DBNull.Value;

        try
        {
            return fieldType.ToLower() switch
            {
                "number" => int.TryParse(value, out int intVal) ? intVal : DBNull.Value,
                "decimal" => decimal.TryParse(value, out decimal decVal) ? decVal : DBNull.Value,
                "date" => DateTime.TryParse(value, out DateTime dateVal) ? dateVal : DBNull.Value,
                "datetime" => DateTime.TryParse(value, out DateTime tsVal) ? tsVal : DBNull.Value,
                "checkbox" =>
                    value.ToLower() == "true" || value == "1" || value.ToLower() == "on",
                _ => value // text, email, textarea, dropdown
            };
        }
        catch
        {
            return DBNull.Value;
        }
    }

    private string MapPgDataTypeToFieldType(string pgDataType)
    {
        if (string.IsNullOrEmpty(pgDataType))
            return "text";

        pgDataType = pgDataType.ToLower();

        return pgDataType switch
        {
            "integer" or "int" or "int4" or "bigint" or "int8" => "number",
            "numeric" or "decimal" => "decimal",
            "boolean" or "bool" => "checkbox",
            "date" => "date",
            "timestamp" or "timestamptz" => "datetime",
            _ => "text"
        };
    }

    private List<string> GetTableColumnsWithCase(NpgsqlConnection connection, string tableName)
    {
        var columns = new List<string>();

        try
        {
            // Use the same connection that's already open
            using (var command = new NpgsqlCommand())
            {
                command.Connection = connection;
                command.CommandText = @"
                SELECT column_name
                FROM information_schema.columns 
                WHERE LOWER(table_name) = LOWER(@tableName)
                ORDER BY ordinal_position";

                command.Parameters.AddWithValue("@tableName", tableName);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        columns.Add(reader["column_name"].ToString());
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting table columns");
            // Return at least the known columns
            columns.Add("Name");
            columns.Add("created_at");
            columns.Add("submitted_by_user_id");
            columns.Add("id");
        }

        return columns;
    }
    // 5. VALIDATE FORM DATA
    public List<string> ValidateFormData(int formId, Dictionary<string, string> formData)
    {
        var errors = new List<string>();

        try
        {
            var formFields = GetFormFields(formId);

            foreach (var field in formFields.Where(f => f.IsRequired))
            {
                if (!formData.ContainsKey(field.FieldName) ||
                    string.IsNullOrWhiteSpace(formData[field.FieldName]))
                {
                    errors.Add($"{field.FieldName} is required");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating form data");
            errors.Add("Validation error occurred");
        }

        return errors;
    }

    public Dictionary<string, string> GetFormFieldTypes(int formId)
    {
        try
        {
            var formFields = GetFormFields(formId);

            // Create mapping: FieldName -> FieldType
            return formFields.ToDictionary(
                f => f.FieldName,
                f => f.FieldType.ToLower()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting form field types");
            return new Dictionary<string, string>();
        }
    }
}