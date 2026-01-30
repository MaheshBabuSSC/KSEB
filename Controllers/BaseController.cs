using KSEB.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class BaseController : Controller
{
    protected readonly FormService _formService;
    private readonly ILogger<BaseController> _logger;

    public BaseController(FormService formService, ILogger<BaseController> logger)
    {
        _formService = formService;
        _logger = logger;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);
        LoadForms();
    }

    protected void LoadForms()
    {
        try
        {
            var allForms = _formService.GetForms();
            _logger.LogInformation($"Total forms from service: {allForms?.Count ?? 0}");

            var activeForms = allForms?.Where(f => f.IsActive).ToList();
            _logger.LogInformation($"Active forms: {activeForms?.Count ?? 0}");

            ViewBag.Forms = activeForms ?? new List<FormSummary>();
            ViewData["ActiveForms"] = activeForms ?? new List<FormSummary>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading forms in BaseController");
            ViewBag.Forms = new List<FormSummary>();
            ViewData["ActiveForms"] = new List<FormSummary>();
        }
    }
}