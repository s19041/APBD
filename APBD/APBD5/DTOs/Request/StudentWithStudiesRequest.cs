using System;
using System.ComponentModel.DataAnnotations;

namespace APBD5.DTOs.RequestModels
{
    public class StudentWithStudiesRequest
    {
        [Required]
        public string IndexNumber { get; set; }
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
        [Required]
        public DateTime BirthDate { get; set; }
        [Required]
        public string Studies { get; set; }
    }
}
