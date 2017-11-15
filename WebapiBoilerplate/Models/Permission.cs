using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebapiBoilerplate.Models
{
    public class Permission
    {
        public int id { get; set; }
        public PermisionsType Text { get; set; }
    }


    public enum PermisionsType
    {
        Permission1 = 0,
        Permission2 = 1,
        Permission3 = 2,
        Permission4 = 3,
        Permission5 = 4,
    }
}