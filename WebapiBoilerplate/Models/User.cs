using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.Identity.EntityFramework;

namespace WebapiBoilerplate.Models
{
    public class User : IdentityUser
    {
        public string FullName { get; set; }
        public UserType UserType { get; set; }
        public virtual Department Department { get; set; }
        public virtual ICollection<Permission> Permissions { get; set; }
    }

    public enum UserType
    {
        Client = 0,
        Agent = 1,
        AgentsLead = 3,
        Admin = 10
    }
}