using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace PeliculasAPI.Validaciones
{
    public class PesoArchivoValidacion: ValidationAttribute
    {
        private readonly int _pesoMaximoEnMegaBytes;
        public PesoArchivoValidacion(int pesoMaximoEnMegaBytes)
        {
            _pesoMaximoEnMegaBytes = pesoMaximoEnMegaBytes;
        }
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
            {
                return ValidationResult.Success;
            }
            IFormFile formfile = value as IFormFile;

            if (formfile == null)
            {
                return ValidationResult.Success;
            }

            if (formfile.Length > _pesoMaximoEnMegaBytes * 1024 * 1024)
            {
                return new ValidationResult($"El peso del archivo no debe ser mayor a {_pesoMaximoEnMegaBytes} MB");
            }
            return ValidationResult.Success;
        }
    }
}
