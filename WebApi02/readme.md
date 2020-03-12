# WebApi添加Jwt认证
> 环境：asp .net core 3.1   
> Nuget包:Microsoft.AspNetCore.Authentication.JwtBearer

1. **创建一个简单的POCO类，用来存储签发或者验证jwt时用到的信息。（==TokenManagement.cs==）**
```
using Newtonsoft.Json;

namespace WebApi02
{
    public class TokenManagement
    {
        [JsonProperty("secret")]
        public string Secret { get; set; }

        [JsonProperty("issuer")]
        public string Issuer { get; set; }

        [JsonProperty("audience")]
        public string Audience { get; set; }

        [JsonProperty("accessExpiration")]
        public int AccessExpiration { get; set; }

        [JsonProperty("refreshExpiration")]
        public int RefreshExpiration { get; set; }
    }
}
```

2. **在 appsettings.Development.json 增加jwt使用到的配置信息（如果是生成环境在appsettings.json添加即可）**
```
  "tokenManagement": {
    "secret": "123456123456123456",
    "issuer": "user",
    "audience": "test",
    "accessExpiration": 30,
    "refreshExpiration": 60
  },
```
==secret需大于等于16位==
3. **startup类的==ConfigureServices==方法中增加读取配置信息**
```
services.Configure<TokenManagement>(Configuration.GetSection("tokenManagement"));

var token = Configuration.GetSection("tokenManagement").Get<TokenManagement>();
```
4. **webapi中注入jwt的验证服务，并在中间件管道中启用authentication中间件。**
```
//ConfigureServices方法
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
services.AddAuthentication(x => {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(x=> {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(token.Secret)),
                    ValidIssuer = token.Issuer,
                    ValidAudience = token.Audience,
                    ValidateIssuer = false,
                    ValidateAudience = false,                
                };
            });
```
```
//Configure方法
app.UseAuthentication();
```
5. **新增请求DTO类（==LoginRequestDTO.cs==）**
```
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace WebApi02
{
    public class LoginRequestDTO
    {
        [Required]
        [JsonProperty("username")]
        public string Username { get; set; }

        [Required]
        [JsonProperty("password")]
        public string Password { get; set; }
    }
}
```

6. **新增==AuthenticationController.cs==控制器进行token签发（后续需修改）**
```
[Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        [AllowAnonymous]
         [HttpPost, Route("requestToken")]
        public ActionResult RequestToken([FromBody] LoginRequestDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid Request");
            }
 
            return Ok();
 
        }
    }
```

7. **创建签发token服务接口==IAuthenticateService.cs==和实现接口==TokenAuthenticationService.cs==（后续需修改）**
```
namespace WebApi02
{
    public interface IAuthenticateService
    {
        bool IsAuthenticated(LoginRequestDTO request, out string token);
    }
}
```
```

public class TokenAuthenticationService : IAuthenticateService
    {
        public bool IsAuthenticated(LoginRequestDTO request, out string token)
        {
            throw new NotImplementedException();
        }
    }
```
8.**==startup.cs==内注册服务**
```
services.AddScoped<IAuthenticateService, TokenAuthenticationService>();
```

9. **第六步的==AuthenticationController.cs==内注入签发服务及完善逻辑**
```
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi02.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController : Controller
    {
        private readonly IAuthenticateService _authService;

        public AuthenticationController(IAuthenticateService authService)
        {
            this._authService = authService;
        }

        [AllowAnonymous]
        [HttpPost, Route("requestToken")]
        public ActionResult RequestToken([FromBody] LoginRequestDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("无效请求");
            }
            string token;
            if (_authService.IsAuthenticated(request, out token))
            {
                return Ok(token);
            }
            return BadRequest("无效请求");
        }
    }
}
```

10. **增加token用户管理服务，正常情况需通过用户名密码认证，此例假设所有用户请求都合法，新建用户管理服务接口==IUserService.cs==和实现==UserService.cs==，独立管理是遵循职责单一原则**
```
namespace WebApi02
{
    public interface IUserService
    {
        bool IsValid(LoginRequestDTO req);
    }
}
```
```
namespace WebApi02
{
    public class UserService : IUserService
    {
        public bool IsValid(LoginRequestDTO req)
        {
            return true;
        }
    }
}
```
11. 注册到容器
```
services.AddScoped<IUserService, UserService>();
```

12. **完善==TokenAuthenticationService.cs==签发token的逻辑，首先要注入IUserService 和 TokenManagement，然后实现具体的业务逻辑**
```
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace WebApi02
{
    public class TokenAuthenticationService : IAuthenticateService
    {
        private readonly IUserService _userService;
        private readonly TokenManagement _tokenManager;

        public TokenAuthenticationService(IUserService userService, IOptions<TokenManagement> tokenManager)
        {
            this._userService = userService;
            this._tokenManager = tokenManager.Value;
        }

        public bool IsAuthenticated(LoginRequestDTO request, out string token)
        {
            token = string.Empty;
            if (!_userService.IsValid(request))
            {
                return false;
            }
            var claims = new[]
            {
                new Claim(ClaimTypes.Name,request.Username)
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_tokenManager.Secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var jwtToken = new JwtSecurityToken(_tokenManager.Issuer, _tokenManager.Audience, claims, expires: DateTime.Now.AddMinutes(_tokenManager.AccessExpiration), signingCredentials: credentials);

            token = new JwtSecurityTokenHandler().WriteToken(jwtToken);
            return true;
        }
    }
}
```
13. **在测试的api打上Authorize特性表明需要授权**
```
[ApiController]
    [Route("[controller]")]
    [Authorize]
    public class WeatherForecastController : ControllerBase
    {
…………
```
