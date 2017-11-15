using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Owin.Security.OAuth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;

namespace WebapiBoilerplate
{
    public class SimpleAuthorizationServerProvider : OAuthAuthorizationServerProvider
    {
        public override async Task ValidateClientAuthentication(OAuthValidateClientAuthenticationContext context)
        {
            context.Validated();
        }

        public override async Task GrantResourceOwnerCredentials(OAuthGrantResourceOwnerCredentialsContext context)
        {
            string UserName ="";
            // remove @domainName if exists
            if (context.UserName.ToLower().Contains("@" + Constants.DOMAIN_NAME))
            {
                UserName = context.UserName.Split('@')[0];
            }
            else
            {
                UserName = context.UserName;
            }


            context.OwinContext.Response.Headers.Add("Access-Control-Allow-Origin", new[] { "*" });

            using (AuthRepository _repo = new AuthRepository())
            {

                IdentityUser user = await _repo.FindUser(UserName, context.Password);

                if (user == null)
                {
                    RegisterResult result = await _repo.RegisterUser(context,new Models.LoginModel { UserName = UserName, Password = context.Password , ConfirmPassword = context.Password });
                    if (result.IdentityResult == null)
                    {
                        context.SetError("invalid_grant", result.Message ); // "The user name or password is incorrect.");
                        return;
                    }
                }

                var identity = _repo.DomainLogin(context,new Models.LoginModel { UserName = UserName, Password = context.Password});

                context.Validated(identity);
            }
        }
    }
}