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
        public IActionResult Authenticate([FromBody]UserDto userDto)
        {
            var user = _userService.Authenticate(userDto.login, userDto.password, out string exception);

            if (exception != null)
                return BadRequest(new { message = exception });

            // возврат данных пользователя (без пароля)
            return Ok(new
            {
                user.userId,
                user.login,
                user.name,
                user.secondName,
                user.Token
            });
        }

        [AllowAnonymous]
        [HttpPost, Route("register")]
        public IActionResult Register([FromBody]UserDto userDto)
        {
            userDto.name = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(userDto.name));
            userDto.secondName = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(userDto.secondName));

            var user = _mapper.Map<User>(userDto);
            user.Role = "User";

            _userService.Create(user, userDto.password, out string exception);
            if (exception != null)
                return BadRequest(new { message = exception });

            _userService.Authenticate(userDto.login, userDto.password, out string except);
            if (except != null)
                return BadRequest(new { message = except });

            _userService.AddBasicCatalog(_context.Users.Single(x =>
            x.login == userDto.login &&
            x.name == userDto.name));

            return Ok(new
            {
                user.userId,
                user.login,
                user.name,
                user.secondName,
                user.Token
            });
        }

        [Authorize(Roles = Role.Admin)]
        [HttpGet]
        public IActionResult GetAll()
        {
            var users = _userService.GetAll();
            var userDtos = _mapper.Map<IList<UserDto>>(users);
            return Ok(userDtos);
        }

        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var user = _userService.GetById(id);
            if (user == null)
            {
                return NotFound();
            }

            var userDto = _mapper.Map<UserDto>(user);

            var currentUserId = int.Parse(User.Identity.Name);
            if (id != currentUserId && !User.IsInRole(Role.Admin))
                return Forbid();

            return Ok(userDto);
        }

        [HttpPut("{id}")]
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
                    return BadRequest(new { message = exception });
            }
            return Ok(new
            {
                user.userId,
                user.login,
                user.name,
                user.secondName,
                user.Token
            });
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var currentUserId = int.Parse(User.Identity.Name);
            if (id != currentUserId && !User.IsInRole(Role.Admin))
                return Forbid();
            else
            {
                _userService.Delete(id, out string exception);
                if (exception != null)
                    return BadRequest(new { message = exception });
            }

            return Ok(new { message = $"Пользователь c id = {id} успешно удален" });
        }
    }
}
