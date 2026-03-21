using System.IdentityModel.Tokens.Jwt;
using System.Text;
using DevExpress.ExpressApp.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.Annotations;

namespace Visa2026.Blazor.Server.JWT {
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController : ControllerBase {
        readonly SignInManager signInManager;
        readonly IConfiguration configuration;

        public AuthenticationController(SignInManager signInManager, IConfiguration configuration) {
            this.signInManager = signInManager;
            this.configuration = configuration;
        }

        [HttpPost("Authenticate")]
        public IActionResult Authenticate(
            [FromBody]
            [SwaggerRequestBody(@"For example: <br /> { ""userName"": ""Admin"", ""password"": """" }")]
            AuthenticationStandardLogonParameters logonParameters
        ) {
            var authenticationResult = signInManager.AuthenticateByLogonParameters(logonParameters);

            if(authenticationResult.Succeeded) {
                var issuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(configuration["Authentication:Jwt:IssuerSigningKey"]!));

                var token = new JwtSecurityToken(
                    issuer: configuration["Authentication:Jwt:Issuer"],
                    audience: configuration["Authentication:Jwt:Audience"],
                    claims: authenticationResult.Principal.Claims,
                    expires: DateTime.Now.AddHours(2),
                    signingCredentials: new SigningCredentials(issuerSigningKey, SecurityAlgorithms.HmacSha256)
                );

                return Ok(new JwtSecurityTokenHandler().WriteToken(token));
            }

            return Unauthorized("User name or password is incorrect.");
        }
    }
}