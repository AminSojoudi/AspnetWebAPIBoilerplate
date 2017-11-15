using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Owin.Security.OAuth;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using WebapiBoilerplate.Database;
using WebapiBoilerplate.Models;

namespace WebapiBoilerplate
{
    public class AuthRepository : IDisposable
    {
        private DatabaseContext _ctx;

        private PrincipalContext principalContext;

        private UserManager<User> _userManager;

        public AuthRepository()
        {
            _ctx = new DatabaseContext();
            string container = string.Empty;
            foreach (var item in Constants.DOMAIN_NAME.Split('.'))
            {
                container = $"{container}, DC={item}";
            }

            _userManager = new UserManager<User>(new UserStore<User>(_ctx));
            principalContext = new PrincipalContext(ContextType.Domain, Constants.DOMAIN_NAME , container, ContextOptions.SimpleBind);
        }

        public async Task<RegisterResult> RegisterUser(OAuthGrantResourceOwnerCredentialsContext context , LoginModel userModel)
        {
            using (HostingEnvironment.Impersonate())
            {
                var identity = DomainLogin(context, userModel);

                if (identity == null)
                {
                    return new RegisterResult { IdentityResult = null, Message = "Could not login to Domain" };
                }

                string lastName = "Not Defiend";
                UserType usertype = UserType.Client;
                if (identity.Claims.FirstOrDefault(x => x.Type == ClaimTypes.GivenName) != null)
                {
                    var givenName = identity.Claims.FirstOrDefault(x => x.Type == ClaimTypes.GivenName);
                    if (givenName != null)
                        lastName = givenName
                            .Value;
                }
                if (identity.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Role) != null)
                {
                    usertype = (UserType)Enum.Parse(typeof(UserType),
                        identity.Claims.First(x => x.Type == ClaimTypes.Role).Value);
                }

                User user = new User()
                {
                    UserName = userModel.UserName,
                    FullName = lastName,
                    UserType = usertype,
                    Permissions = new List<Permission>()
                };


                

                var result = await _userManager.CreateAsync(user, userModel.Password);

                FixOrganazationUnit(userModel.UserName, user.Id);
                FixPermissions(user.Id);


                return new RegisterResult
                {
                    IdentityResult = result,
                    Message = "ok"
                };

            }
        }

        public async Task<IdentityUser> FindUser(string userName, string password)
        {
            using (HostingEnvironment.Impersonate())
            {
                User user = await _userManager.FindAsync(userName, password);
                if (user != null)
                {
                    FixOrganazationUnit(userName, user.Id);
                    FixPermissions(user.Id);
                }
                return user;
            }
        }

        /// <summary>
        /// initial Permissions of users based on user type
        /// </summary>
        /// <param name="userId"></param>
        public void FixPermissions(string userId)
        {
            var dbUser = _ctx.Users.First(x => x.Id == userId);
            var userPermisions = dbUser.Permissions.Select(x => x.Text).ToList();

            if (dbUser.UserType == UserType.Admin)
            {
                var permisions = Enum.GetValues(typeof(PermisionsType)).Cast<PermisionsType>().ToList();

                permisions.Where(p => !userPermisions.Contains(p)).ToList().ForEach(item =>
                {
                    dbUser.Permissions.Add(new Permission { Text = item });
                });
            }
            else if(dbUser.UserType == UserType.Agent)
            {
                if (!userPermisions.Contains(PermisionsType.Permission4))
                {
                    dbUser.Permissions.Add(new Permission { Text = PermisionsType.Permission4 });
                }
            }
            else if (dbUser.UserType == UserType.AgentsLead)
            {
                if (!userPermisions.Contains(PermisionsType.Permission4))
                {
                    dbUser.Permissions.Add(new Permission { Text = PermisionsType.Permission4 });
                }
                if (!userPermisions.Contains(PermisionsType.Permission2))
                {
                    dbUser.Permissions.Add(new Permission { Text = PermisionsType.Permission2 });
                }
                if (!userPermisions.Contains(PermisionsType.Permission3))
                {
                    dbUser.Permissions.Add(new Permission { Text = PermisionsType.Permission3 });
                }
            }

            if (!userPermisions.Contains(PermisionsType.Permission1))
            {
                dbUser.Permissions.Add(new Permission { Text = PermisionsType.Permission1 });
            }
            _ctx.Entry(dbUser).State = EntityState.Modified;
            _ctx.SaveChanges();
        }

        /// <summary>
        /// Add organization if not exist or change if user department is changed in Active Directory
        /// </summary>
        /// <param name="username"></param>
        /// <param name="userId"></param>
        private void FixOrganazationUnit(string username , string userId)
        {
            var dbUser = _ctx.Users.First(x => x.Id == userId);
            string Department = GetOU(username);
            var dep = _ctx.Departments.FirstOrDefault(x => x.Text == Department);
            if (dep == null)
            {
                var dbDepartment = new Department { Text = Department };
                _ctx.Departments.Add(dbDepartment);
                dbUser.Department = dbDepartment;
            }
            else
            {
                dbUser.Department = dep;
            }
            _ctx.Entry(dbUser).State = EntityState.Modified;
            _ctx.SaveChanges();
        }

        public string GetOU(string userName)
        {
            using (HostingEnvironment.Impersonate())
            {
                string result = string.Empty;
                using (HostingEnvironment.Impersonate())
                {
                    //Finding the user
                    UserPrincipal user = UserPrincipal.FindByIdentity(principalContext, userName);

                    //If the user found
                    if (user != null)
                    {
                        // Getting the DirectoryEntry
                        DirectoryEntry directoryEntry = (user.GetUnderlyingObject() as DirectoryEntry);
                        //if the directoryEntry is not null
                        if (directoryEntry != null)
                        {
                            //Getting the directoryEntry's path and spliting with the "," character
                            string[] directoryEntryPath = directoryEntry.Path.Split(',');
                            //Getting the each items of the array and spliting again with the "=" character
                            foreach (var splitedPath in directoryEntryPath)
                            {
                                string[] eleiments = splitedPath.Split('=');
                                //If the 1st element of the array is "OU" string then get the 2dn element
                                if (eleiments[0].Trim() == "OU")
                                {
                                    result = eleiments[1].Trim();
                                    break;
                                }
                            }
                        }
                    }
                }
                return result;
            }
        }

        public ClaimsIdentity DomainLogin(OAuthGrantResourceOwnerCredentialsContext context , LoginModel model)
        {
            using (HostingEnvironment.Impersonate())
            {
                bool isAuthenticated = false;
                UserPrincipal userPrincipal = null;
                try
                {
                    isAuthenticated =
                        principalContext.ValidateCredentials(model.UserName, model.Password, ContextOptions.Negotiate);
                    if (isAuthenticated)
                    {
                        userPrincipal =
                            UserPrincipal.FindByIdentity(principalContext, IdentityType.SamAccountName, model.UserName);
                    }
                }
                catch (Exception)
                {
                    isAuthenticated = false;
                    userPrincipal = null;
                }
                if (!isAuthenticated)
                {
                    try
                    {
                        isAuthenticated =
                            principalContext.ValidateCredentials(model.UserName, model.Password,
                                ContextOptions.Negotiate);
                        if (isAuthenticated)
                        {
                            userPrincipal =
                                UserPrincipal.FindByIdentity(principalContext, IdentityType.Name, model.UserName);
                        }
                    }
                    catch (Exception)
                    {
                        isAuthenticated = false;
                        userPrincipal = null;
                    }
                }

                if (!isAuthenticated || userPrincipal == null)
                {
                    // return BadRequest("Username or Password is not correct");
                    return null;
                }

                if (userPrincipal.IsAccountLockedOut())
                {
                    // here can be a security related discussion weather it is worth 
                    // revealing this information
                    //return BadRequest("Your account is locked.");
                    return null;
                }

                if (userPrincipal.Enabled.HasValue && userPrincipal.Enabled.Value == false)
                {
                    // here can be a security related discussion weather it is worth 
                    // revealing this information
                    //return BadRequest("Your account is disabled");
                    return null;
                }

                var identity = CreateIdentity(context, principalContext, userPrincipal);

                return identity;
            }
        }

        private ClaimsIdentity CreateIdentity(OAuthGrantResourceOwnerCredentialsContext context, PrincipalContext principalContext, UserPrincipal userPrincipal)
        {
            using (HostingEnvironment.Impersonate())
            {
                var identity = new ClaimsIdentity(context.Options.AuthenticationType);
                identity.AddClaim(new Claim(ClaimTypes.Name, userPrincipal.SamAccountName));
                if (userPrincipal.Surname != null)
                {
                    identity.AddClaim(new Claim(ClaimTypes.GivenName, userPrincipal.Surname));
                }
                else
                {
                    identity.AddClaim(new Claim(ClaimTypes.GivenName,
                        string.IsNullOrEmpty(userPrincipal.SamAccountName)
                            ? "Not Defined"
                            : userPrincipal.SamAccountName));
                }

                GroupPrincipal group = GroupPrincipal.FindByIdentity(principalContext, Constants.ACTIVE_DIRECTORY_ADMINS_GROUP_NAME);
                if (group != null)
                {
                    if (userPrincipal.IsMemberOf(group))
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Role, UserType.Admin.ToString()));
                    }
                }
                else
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, UserType.Client.ToString()));
                }
                if (!String.IsNullOrEmpty(userPrincipal.EmailAddress))
                {
                    identity.AddClaim(new Claim(ClaimTypes.Email, userPrincipal.EmailAddress));
                }

                // add your own claims if you need to add more information stored on the cookie

                return identity;
            }
        }

        public void Dispose()
        {
            _ctx.Dispose();
            _userManager.Dispose();

        }
    }

    public class RegisterResult
    {
        public IdentityResult IdentityResult;
        public string Message;
    }
}