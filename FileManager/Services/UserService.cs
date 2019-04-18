using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using FileManager.Entities;
using FileManager.Helpers;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace FileManager.Services
{
    public interface IUserService
    {
        User Authenticate(string username, string password, out string exception);
        IEnumerable<User> GetAll();
        User GetById(int id);
        User Create(User user, string password, out string exception);
        void AddBasicCatalog(User user);
        void Update(User userParam, out string exception, string password = null);
        void Delete(int id, out string exception);
    }

    public class UserService : IUserService
    {
        private ApplicationContext _context;
        private readonly ILogger _logger;
        private IConfiguration _config;

        public UserService(ApplicationContext context, ILogger<UserService> logger, IConfiguration config)
        {
            _context = context;
            _logger = logger;
            _config = config;
        }

        public User Authenticate(string login, string password, out string exception)
        {
            exception = null;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                exception = "Вы забыли ввести логин и/или пароль";
                return null;
            }

            var user = _context.Users.SingleOrDefault(x => x.login == login);

            // проверка, существует ли пользователь
            if (user == null)
            {
                exception = "Информация по данному пользователю не найдена";
                return null;
            }

            // проверка введенного пароля на соответствие
            if (exception == null)
            {
                if (!VerifyPasswordHash(password, user.PasswordHash, user.HashKey, out string localExpt))
                {
                    exception = localExpt;
                    return null;
                }
            }

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_config["AppSettings:Secret"]);
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new Claim[]
                    {
                    new Claim(ClaimTypes.Name, user.userId.ToString()),
                    new Claim(ClaimTypes.Role, user.Role)
                    }),
                    // здесь можно изменить время на addminutes, действует почти как время сессии
                    Expires = DateTime.UtcNow.AddMinutes(20),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                user.Token = tokenHandler.WriteToken(token);

            }
            catch { exception = "Произошла внутренняя ошибка сервера. For developers: TokenGenerate"; }
            if (login == user.login)
            {
                _context.Users.Update(user);
                _context.SaveChanges();
            }

            // если успешная авторизация
            return user;
        }

        public IEnumerable<User> GetAll()
        {
            return _context.Users;
        }

        public User GetById(int id)
        {
            return _context.Users.Find(id);
        }

        public User Create(User user, string password, out string exception)
        {
            exception = null;

            string password_pattern = @"^(?=.*[a-zA-Z])(?=.*[0-9])\S{8,16}$";
            string login_pattern = @"^\S{3,20}$";

            if (string.IsNullOrWhiteSpace(user.login) || string.IsNullOrWhiteSpace(password))
                exception = "Вы забыли ввести логин и/или пароль";

            if (!Regex.IsMatch(user.login, login_pattern, RegexOptions.IgnoreCase))
                exception = "Логин не должен содержать символ пробела. Допустимая длина от 3 до 20 символов";

            if (!Regex.IsMatch(password, password_pattern, RegexOptions.IgnoreCase))
                exception = "Пароль должен содержать латинские символы и цифры. Длина пароля от 8 до 16 символов";

            if (_context.Users.Any(x => x.login == user.login))
                exception = $"Пользователь с логином {user.login} уже существует";

            if (string.IsNullOrWhiteSpace(user.name))
                exception = "Введите имя";

            if (string.IsNullOrWhiteSpace(user.secondName))
                exception = "Введите фамилию";

            if (exception == null)
            {
                CreatePasswordHash(password, out byte[] passwordHash, out byte[] HashKey, out string localExpt);
                if (localExpt != null)
                    exception = localExpt;

                if (exception == null)
                {
                    user.PasswordHash = passwordHash;
                    user.HashKey = HashKey;

                    _context.Users.Add(user);
                    _context.SaveChanges();
                }
            }

            return user;
        }

        public void AddBasicCatalog(User user)
        {
            var obj = new Objects
            {
                objectName = "Каталог",
                right = 1,
                left = 0,
                type = true,
                level = 0,
                userId = user.userId
            };
            _context.Objects.Add(obj);
            _context.SaveChanges();

            var permissions = new Permissions
            {
                parentUserId = user.userId,
                childUserId = user.userId,
                read = true,
                write = true,
                objectId = obj.objectId
            };

            _context.Permissions.Add(permissions);
            _context.SaveChanges();
        }

        public void Update(User userParam, out string exception, string password = null)
        {
            exception = null;
            var user = _context.Users.Find(userParam.userId);

            if (user == null)
                exception = "Пользователь не найден";

            if (!string.IsNullOrWhiteSpace(userParam.login))
            {
                if (_context.Users.Any(x => x.login == user.login))
                    exception = $"Пользователь с логином {userParam.login} уже существует";

                string login_pattern = @"^\S{3,20}$";

                if (!Regex.IsMatch(userParam.login, login_pattern, RegexOptions.IgnoreCase))
                    exception = "Логин не должен содержать символ пробела. Допустимая длина от 3 до 20 символов";
                else
                    user.login = userParam.login;
            }

            if (!string.IsNullOrWhiteSpace(userParam.name))
                user.name = userParam.name;

            if (!string.IsNullOrWhiteSpace(userParam.secondName))
                user.secondName = userParam.secondName;

            if (!string.IsNullOrWhiteSpace(password))
            {
                string password_pattern = @"^(?=.*[a-zA-Z])(?=.*[0-9])\S{8,16}$";

                if (!Regex.IsMatch(password, password_pattern, RegexOptions.IgnoreCase))
                    exception = "Пароль должен содержать латинские символы и цифры. Длина пароля от 8 до 16 символов";
                else
                {
                    CreatePasswordHash(password, out byte[] passwordHash, out byte[] HashKey, out string localExpt);

                    if (localExpt != null) 
                        exception = localExpt; 

                    if (exception == null)
                    {
                        user.PasswordHash = passwordHash;
                        user.HashKey = HashKey;

                        _context.Users.Update(user);
                        _context.SaveChanges();
                    }
                }
            }
        }

        public void Delete(int id, out string exception)
        {
            exception = null;
            var user = _context.Users.Find(id);

            if (user != null)
            {
                _context.Users.Remove(user);
                _context.SaveChanges();
            }
            else exception = $"Пользователь с id = {id} не найден";
        }

        private static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] HashKey, out string localExpt)
        {
            passwordHash = null;
            HashKey = null;
            localExpt = null;

            using (var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                HashKey = hmac.Key;
                passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            }
        }

        private static bool VerifyPasswordHash(string password, byte[] storedHash, byte[] HashKey, out string localExpt)
        {
            localExpt = null;

            if (storedHash.Length != 64)
            {
                localExpt = "Произошла внутренняя ошибка сервера. For developers: VerifyPasswordHash";
                return false;
            }
            else
            {

                using (var hmac = new System.Security.Cryptography.HMACSHA512(HashKey))
                {
                    var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
                    for (int i = 0; i < computedHash.Length; i++)
                    {
                        if (computedHash[i] != storedHash[i])
                        {
                            localExpt = "Некорректный пароль";
                            return false;
                        }
                    }
                }
            }

            return true;
        }
    }
}
