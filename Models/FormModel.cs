using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KSEB.Models
{
    // KEEP ONLY THESE MODELS:

    public class FormField
    {
        public string FieldName { get; set; } = string.Empty;
        public string FieldType { get; set; } = string.Empty;
        public string Options { get; set; } = string.Empty;
        public int FieldOrder { get; set; }
        public bool IsRequired { get; set; } = false;
        public string Placeholder { get; set; } = string.Empty;
    }

    public class CreateFormRequest
    {
        public string FormTitle { get; set; }
        public string FormDescription { get; set; }
        public string CreatedBy { get; set; }
        public List<FormField> Fields { get; set; }
    }

    public class FormSummary
    {
        public int FormId { get; set; }
        public string FormTitle { get; set; }
        public string FormDescription { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public string TableName { get; set; }
    }

    public class DynamicFormViewModel
    {
        public int FormId { get; set; }
        public string FormTitle { get; set; }
        public string FormDescription { get; set; }
        public string TableName { get; set; }
        public List<DynamicFormFieldViewModel> Fields { get; set; } = new();
    }

    public class DynamicFormFieldViewModel
    {
        public string Name { get; set; }
        public string Label { get; set; }
        public string Type { get; set; }
        public bool Required { get; set; }
        public List<string> Options { get; set; } = new();
        public string Placeholder { get; set; }
        public int Order { get; set; }
    }
}