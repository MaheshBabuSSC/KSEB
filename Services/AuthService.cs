using Microsoft.EntityFrameworkCore;
using KSEB.Models;
using KSEB.Security;

namespace KSEB.Services
{
    public class AuthService
    {
        private readonly AppDbContext _context;

        public AuthService(AppDbContext context)
        {
            _context = context;
        }

        public List<RoleModel> GetActiveRoles()
        {
            try
            {
                return _context.Database
                    .SqlQueryRaw<RoleModel>(
                        "SELECT RoleId, RoleName FROM tbl_roles WHERE IsActive = true ORDER BY RoleName")
                    .AsEnumerable()
                    .ToList();
            }
            catch
            {
                return new List<RoleModel>
                {
                    new RoleModel { RoleId = 1, RoleName = "Admin" },
                    new RoleModel { RoleId = 2, RoleName = "User" }
                };
            }
        }

        public int RegisterUser(RegisterModel model, string createdBy)
        {
            try
            {
                PasswordHelper.CreatePasswordHash(
                    model.Password,
                    out byte[] hash,
                    out byte[] salt
                );

                string hashBase64 = Convert.ToBase64String(hash);
                string saltBase64 = Convert.ToBase64String(salt);

                var sql = @"
                    INSERT INTO tbl_users (
                        EmployeeId, FullName, UserName, Email, 
                        PasswordHash, PasswordSalt, MobileNo, 
                        DepartmentId, RoleId, Site, Shift, Location,
                        IsActive, CreatedBy, CreatedDate
                    ) VALUES (
                        {0}, {1}, {2}, {3}, 
                        {4}, {5}, {6}, 
                        {7}, {8}, {9}, {10}, {11},
                        {12}, {13}, NOW()
                    ) RETURNING UserId";

                var userId = _context.Database
                    .SqlQueryRaw<int>(sql,
                        model.EmployeeId,
                        model.FullName,
                        model.UserName,
                        model.Email.ToLower(),
                        hashBase64,
                        saltBase64,
                        model.MobileNo,
                        model.DepartmentId,
                        model.RoleId,
                        model.Site ?? "Default",
                        model.Shift ?? "General",
                        model.Location ?? "Default",
                        true,
                        createdBy ?? "System"
                    )
                    .AsEnumerable()
                    .FirstOrDefault();

                return userId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Registration error: {ex.Message}");
                throw new Exception($"Registration failed: {ex.Message}");
            }
        }

        public int ValidateLogin(string email, string password)
        {
            try
            {
                var user = _context.Database
                    .SqlQueryRaw<LoginResult>(
                        "SELECT UserId, PasswordHash, PasswordSalt FROM tbl_users WHERE LOWER(Email) = LOWER({0}) AND IsActive = true",
                        email
                    )
                    .AsEnumerable()
                    .FirstOrDefault();

                if (user == null) return 0;

                byte[] hash = Convert.FromBase64String(user.PasswordHash);
                byte[] salt = Convert.FromBase64String(user.PasswordSalt);

                bool isValid = PasswordHelper.VerifyPassword(password, hash, salt);

                return isValid ? user.UserId : 0;
            }
            catch
            {
                return 0;
            }
        }

        public UserModel GetUserByEmail(string email)
        {
            try
            {
                var sql = @"
                    SELECT 
                        u.UserId,
                        u.EmployeeId,
                        u.FullName,
                        u.UserName,
                        u.Email,
                        u.MobileNo,
                        u.RoleId,
                        r.RoleName,
                        u.DepartmentId,
                        u.Site,
                        u.Shift,
                        u.Location
                    FROM tbl_users u
                    LEFT JOIN tbl_roles r ON u.RoleId = r.RoleId
                    WHERE LOWER(u.Email) = LOWER({0}) AND u.IsActive = true";

                var user = _context.Database
                    .SqlQueryRaw<UserModel>(sql, email)
                    .AsEnumerable()
                    .FirstOrDefault();

                return user;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting user by email: {ex.Message}");
                return null;
            }
        }

        private class LoginResult
        {
            public int UserId { get; set; }
            public string PasswordHash { get; set; } = string.Empty;
            public string PasswordSalt { get; set; } = string.Empty;
        }
    }
}