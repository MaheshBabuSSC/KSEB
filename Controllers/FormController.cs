using KSEB.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

[Route("")]
[Authorize]
public class FormBuilderController : BaseController
{
    private readonly ILogger<FormBuilderController> _logger;

    public FormBuilderController(FormService service,
        ILogger<FormBuilderController> logger,
        ILogger<BaseController> baseLogger)
        : base(service, baseLogger)
    {
        _logger = logger;
    }

    // 1. CREATE FORM PAGE
    [HttpGet("FormBuilder")]
    public IActionResult Index()
    {
        var model = new CreateFormRequest
        {
            FormTitle = "New Form",
            FormDescription = "",
            CreatedBy = User.Identity?.Name ?? "MVC-User",
            Fields = new List<FormField>()
        };

        return View("~/Views/KSEB/FormBuilder.cshtml", model);
    }

    // 2. SAVE CREATED FORM
    [HttpPost("FormBuilder/Create")]
    [ValidateAntiForgeryToken]
    public IActionResult Create([FromForm] CreateFormRequest request)
    {
        try
        {
            request.CreatedBy = User.Identity?.Name ?? "MVC-User";
            request.Fields ??= new List<FormField>();

            var formId = _formService.CreateForm(
                request.FormTitle?.Trim() ?? "Untitled Form",
                request.FormDescription?.Trim() ?? "",
                request.CreatedBy,
                request.Fields,
                true
            );

            TempData["SuccessMessage"] = $"Form '{request.FormTitle}' created successfully!";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating form");
            ViewBag.ErrorMessage = $"Error creating form: {ex.Message}";
            return View("~/Views/KSEB/FormBuilder.cshtml", request);
        }
    }

    // 3. SHOW FORM FOR SUBMISSION
    [HttpGet("FormBuilder/Submit/{id:int}")]
    public IActionResult SubmitForm(int id)
    {
        try
        {
            var forms = _formService.GetForms();
            var form = forms.FirstOrDefault(f => f.FormId == id);

            if (form == null)
            {
                TempData["ErrorMessage"] = $"Form with ID {id} not found";
                return RedirectToAction("List");
            }

            if (!form.IsActive)
            {
                TempData["ErrorMessage"] = "This form is currently inactive";
                return RedirectToAction("List");
            }

            var formFields = _formService.GetFormFields(id);

            var viewModel = new DynamicFormViewModel
            {
                FormId = form.FormId,
                FormTitle = form.FormTitle,
                FormDescription = form.FormDescription,
                TableName = form.TableName,
                Fields = formFields.Select(f => new DynamicFormFieldViewModel
                {
                    Name = f.FieldName,
                    Label = f.FieldName.Replace("_", " "),
                    Type = f.FieldType.ToLower(),
                    Required = f.IsRequired,
                    Options = !string.IsNullOrEmpty(f.Options)
                        ? f.Options.Split(',').Select(o => o.Trim()).ToList()
                        : new List<string>(),
                    Placeholder = f.Placeholder,
                    Order = f.FieldOrder
                }).OrderBy(f => f.Order).ToList()
            };

            ViewData["Title"] = form.FormTitle;
            return View("~/Views/KSEB/FormSubmit.cshtml", viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading form");
            TempData["ErrorMessage"] = $"Error loading form: {ex.Message}";
            return RedirectToAction("List");
        }
    }

    // 4. SAVE SUBMITTED FORM DATA
    [HttpPost("FormBuilder/SubmitFormData/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitFormData(int id, [FromForm] IFormCollection formData)
    {
        try
        {
            var forms = _formService.GetForms();
            var form = forms.FirstOrDefault(f => f.FormId == id);

            if (form == null)
            {
                TempData["ErrorMessage"] = $"Form with ID {id} not found";
                return RedirectToAction("SubmitForm", new { id });
            }

            // Convert form data to dictionary
            var data = new Dictionary<string, string>();
            foreach (var key in formData.Keys)
            {
                if (key != "__RequestVerificationToken" && key != "FormId" && key != "TableName")
                {
                    data[key] = formData[key];
                }
            }

            // Get user ID
            var userId = User.FindFirst("UserId")?.Value;
            int submittedByUserId = userId != null ? int.Parse(userId) : 0;

            // Save using formId so we can get field types
            _formService.SaveFormData(id, data, submittedByUserId);

            TempData["SuccessMessage"] = "Form submitted successfully!";
            return RedirectToAction("SubmitForm", new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting form: {FormId}", id);
            TempData["ErrorMessage"] = $"Error submitting form: {ex.Message}";
            return RedirectToAction("SubmitForm", new { id });
        }
    }
}