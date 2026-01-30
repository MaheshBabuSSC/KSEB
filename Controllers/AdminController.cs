using Microsoft.AspNetCore.Mvc;
using KSEB.Models;

public class AdminController : Controller
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [Route("UserList")]
    public async Task<IActionResult> UserList()
    {
        var users = await _adminService.GetAllUsersFromFunctionAsync();
        return View("~/Views/KSEB/UserList.cshtml", users);
    }
}