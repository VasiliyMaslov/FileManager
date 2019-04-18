using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using FileManager.Services;
using FileManager.Dtos;
using FileManager.Entities;
using FileManager.Helpers;
using System.Linq;
using static System.Text.Encoding;

namespace FileManager.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private IUserService _userService;
        private IMapper _mapper;
        private ApplicationContext _context;

        public AccountController(
            IUserService userService,
            IMapper mapper, ApplicationContext context)
        {
            _context = context;
            _userService = userService;
            _mapper = mapper;
        }

        [AllowAnonymous]
        [HttpPost, Route("authenticate")]
        //принимает login, password
        public IActionResult Authenticate([FromBody]UserDto userDto)
        {
            var user = _userService.Authenticate(userDto.login, userDto.password, out string exception);

            if (exception != null)
                return Ok(new { error = true, message = exception });

            // возврат данных пользователя (без пароля)
            return Ok(new
            {
                error = false,
                message = $"Вход выполнен",
                user.userId,
                user.login,
                user.name,
                user.secondName,
                user.Token
            });
        }

        [AllowAnonymous]
        [HttpPost, Route("register")]
        // принимает name, secondName, login, password
        public IActionResult Register([FromBody]UserDto userDto)
        {
            userDto.name = UTF8.GetString(UTF8.GetBytes(userDto.name));
            userDto.secondName = UTF8.GetString(UTF8.GetBytes(userDto.secondName));

            var user = _mapper.Map<User>(userDto);
            user.Role = "User";

            _userService.Create(user, userDto.password, out string exception);
            if (exception != null)
                return Ok(new { error = true, message = exception });

            _userService.Authenticate(userDto.login, userDto.password, out string except);
            if (except != null)
                return Ok(new { error = true, message = except });

            _userService.AddBasicCatalog(_context.Users.Single(x =>
            x.login == userDto.login &&
            x.name == userDto.name));

            return Ok(new
            {
                error = false,
                message = $"Регистрация прошла успешно",
                user.userId,
                user.login,
                user.name,
                user.secondName,
                user.Token
            });
        }
        
        [Authorize(Roles = Role.Admin)]
        [HttpGet]
        // ничего не принимает
        public IActionResult GetAll()
        {
            var users = _userService.GetAll();
            var userDtos = _mapper.Map<IList<UserDto>>(users);
            return Ok(userDtos);
        }

        [HttpGet("{id}")]
        // принимает id пользователя
        public IActionResult GetById(int id)
        {
            var user = _userService.GetById(id);
            if (user == null)
            {
                return NotFound();
            }

            var data = _mapper.Map<UserDto>(user);

            var currentUserId = int.Parse(User.Identity.Name);
            if (id != currentUserId && !User.IsInRole(Role.Admin))
                return Forbid();

            return Ok(new
            {
                error = false,
                data
            });
        }

        [HttpPut("{id}")]
        // принимает id пользователя
        public IActionResult Update(int id, [FromBody]UserDto userDto)
        {
            var user = _mapper.Map<User>(userDto);
            user.userId = id;

            var currentUserId = int.Parse(User.Identity.Name);
            if (id != currentUserId && !User.IsInRole(Role.Admin))
                return Forbid();
            else
            {
                _userService.Update(user, out string exception, userDto.password);
                if (exception != null)
                    return Ok(new { error = true, message = exception });
            }
            return Ok(new
            {
                error = false,
                user.userId,
                user.login,
                user.name,
                user.secondName,
                user.Token
            });
        }

        [HttpDelete("{id}")]
        // принимает id пользователя
        public IActionResult Delete(int id)
        {
            var currentUserId = int.Parse(User.Identity.Name);
            if (id != currentUserId && !User.IsInRole(Role.Admin))
                return Forbid();
            else
            {
                _userService.Delete(id, out string exception);
                if (exception != null)
                    return Ok(new { error = true, message = exception });
            }

            return Ok(new { error = false, message = $"Пользователь c id = {id} успешно удален" });
        }
    }
}
