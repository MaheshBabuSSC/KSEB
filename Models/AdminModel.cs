
    namespace KSEB.Models
    {
        public class UsersList
        {
            public int UserId { get; set; }
            public string EmployeeId { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string UserName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string MobileNo { get; set; } = string.Empty;
            public int RoleId { get; set; }
            public string Site { get; set; } = string.Empty;
            public string Shift { get; set; } = string.Empty;
            public string Location { get; set; } = string.Empty;
            public bool IsActive { get; set; }
            public DateTime CreatedDate { get; set; }
        }
    }

