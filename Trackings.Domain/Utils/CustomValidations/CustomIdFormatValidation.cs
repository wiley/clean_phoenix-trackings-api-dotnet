using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace Trackings.Domain.Utils.CustomValidations
{
    public class CustomIdFormatValidation : ValidationAttribute
    {
        private static readonly string INVALID_ID_FORMAT = "The Id is in wrong format.";

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            bool isValid = Guid.TryParse(value.ToString(), out Guid pattern);

            if (!isValid)
                return new ValidationResult(INVALID_ID_FORMAT);

            return ValidationResult.Success;
        }

    }
}
