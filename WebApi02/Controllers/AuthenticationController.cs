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

        /// <summary>
        /// 获取授权Token
        /// </summary>
        /// <param name="request">LoginDto对象</param>
        /// <remarks>
        /// {
        ///     "username": "admin",
        ///     "password": "123456"
        /// }
        /// </remarks>
        /// <returns></returns>
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