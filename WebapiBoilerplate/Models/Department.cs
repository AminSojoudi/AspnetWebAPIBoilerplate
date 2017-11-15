using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace WebapiBoilerplate.Models
{
    [Table("Departments")]
    public class Department
    {
        public int id { get; set; }
        [StringLength(450)]
        [Index(IsUnique = true)]
        public string Text { get; set; }

    }
}