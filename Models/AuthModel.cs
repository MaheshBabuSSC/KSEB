namespace KSEB.Models
{
    // For registration
    public class RegisterModel
    {
        public string EmployeeId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string MobileNo { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        public int RoleId { get; set; }
        public string Site { get; set; } = string.Empty;
        public string Shift { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public List<RoleModel> Roles { get; set; } = new List<RoleModel>();
    }

    // For roles
    public class RoleModel
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
    }

    // For login
    public class LoginModel
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }

    // User model for login response (in same namespace)
    public class UserModel
    {
        public int UserId { get; set; }
        public string EmployeeId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string MobileNo { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        public string Site { get; set; } = string.Empty;
        public string Shift { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
    }
}