using Dapper;
using KSEB.Models;
using Npgsql;

public interface IAdminService
{
    Task<List<UsersList>> GetAllUsersFromFunctionAsync();
}

public class AdminService : IAdminService
{
    private readonly string _connectionString;

    public AdminService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    public async Task<List<UsersList>> GetAllUsersFromFunctionAsync()
    {
        using (var connection = new NpgsqlConnection(_connectionString))
        {
            await connection.OpenAsync();

            var sql = "SELECT * FROM fn_get_all_users()";

            var result = await connection.QueryAsync<UsersList>(sql);
            return result.ToList();
        }
    }
}