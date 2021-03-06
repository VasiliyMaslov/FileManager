﻿using FileManager.Dtos;
using FileManager.Entities;
using FileManager.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Math;

namespace FileManager.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/catalog")]
    public class ObjectController : ControllerBase
    {
        private ApplicationContext _context;
        private readonly long max_size;

        private readonly ILogger _logger;

        public ObjectController(ApplicationContext context, ILogger<ObjectController> logger)
        {
            _logger = logger;
            _context = context;
            max_size = 21474836480;
        }

        // Загрузка файла
        [HttpPost, Route("upload_file")]
        [RequestSizeLimit(22548578304)] // ограничение веса запроса 21 гб
        // Принимает file - просто файл с form-data. objId - директория, в которую загрузить
        public async Task<IActionResult> Upload([FromForm(Name = "file")]IFormFileCollection file,
            [FromForm(Name = "objId")]int directoryId)
        {
            try
            {
                List<object> data = new List<object>();
                
                var directory = _context.Objects
                .Single(c => c.objectId == directoryId && c.type == true);

                if (CheckWriteAllow(directory) == false)
                    return Ok(new { error = true, message = "Недостаточно прав" });

                var objects = from t in _context.Objects
                              where directory.userId == t.userId && t.type == false
                              select t;

                long size_files = 0;
                foreach (var x in objects)
                    size_files += x.binaryData.Length;

                foreach (var f in file)
                {
                    if (f == null)
                        return Ok(new { error = true, message = "Файл не выбран" });
                    if (f.FileName.Length >= 50)
                        return Ok(new { error = true, message = "Название файла должно быть меньше 50-ти символов" });
                    if (f.Length > max_size)
                        return Ok(new { error = true, message = "Размер файла не должен превышать 20 Гб" });

                    size_files += f.Length;
                    var available_size = max_size - size_files;

                    if ((available_size - f.Length) <= 0)
                        return Ok(new { error = true, message = "Недостаточно места" });

                    Objects obj = new Objects();

                    using (var memoryStream = new MemoryStream())
                    {
                        await f.CopyToAsync(memoryStream);
                        obj.binaryData = memoryStream.ToArray();
                    }
                    obj.objectName = f.FileName;
                    obj.userId = directory.userId;
                    obj.type = false;
                    obj.left = directory.right;
                    obj.right = directory.right + 1;
                    obj.level = directory.level + 1;
                    foreach (var t in _context.Objects)
                    {
                        if (t.left >= obj.left && t.userId == directory.userId)
                            t.left += 2;
                        if (t.right >= obj.left && t.objectId != obj.objectId && t.userId == directory.userId)
                            t.right += 2;
                        _context.Objects.Update(t);
                    }

                    _context.Objects.Add(obj);

                    var permissions = new Permissions
                    {
                        parentUserId = directory.userId,
                        childUserId = int.Parse(User.Identity.Name),
                        read = true,
                        write = true,
                        objectId = obj.objectId
                    };

                    _context.Permissions.Add(permissions);

                    data.Add(new
                    {
                        obj.objectName,
                        obj.left,
                        obj.right,
                        obj.level,
                        weight = obj.binaryData.LongLength,
                        obj.userId
                    });
                }
                _context.SaveChanges();

                List<string> files = new List<string>();
                foreach (var x in file)
                    files.Add(x.FileName);

                string loc = null;

                for (int i = 0; i < files.Count(); i++)
                {
                    loc += files[i];
                    if (i < files.Count() - 1)
                        loc += ", ";
                }
                
                return Ok(new
                {
                    error = false,
                    message = $"Файлы {loc} загружены",
                    data
                });
            }
            catch (Exception e)
            {
                return BadRequest(new { error = true, e.Message });
            }
        }
        
        [HttpGet, Route("download")]
        [RequestSizeLimit(22548578304)] // ограничение веса запроса 21 гб
        // принимает fileId - id файла, который скачать
        public IActionResult Download(int fileId)
        {
            var obj = _context.Objects
                .Single(c => (c.type == false) && (c.objectId == fileId));

            if (CheckReadAllow(obj) == false)
                return Ok(new { error = true, message = "Недостаточно прав" });

            return Ok(new
            {
                error = false,
                file = File(obj.binaryData, GetContentType(obj.objectName), Path.GetFileName(obj.objectName))
            });
        }

        [HttpPost, Route("create_directory")]
        // принимает objectId (директория, в которой сейчас юзер), objectName (назв. новой директории)
        public IActionResult CreateDirectory([FromBody]ObjectDto objectDto)
        {
            try
            {
                var directory = _context.Objects
                .Single(c => (c.type == true) && (c.objectId == objectDto.objectId));

                if (CheckWriteAllow(directory) == false)
                    return Ok(new { error = true, message = "Недостаточно прав" });

                var obj = new Objects
                {
                    objectName = objectDto.objectName,
                    left = directory.right,
                    right = directory.right + 1,
                    level = directory.level + 1,
                    type = true,
                    userId = directory.userId
                };

                foreach (var t in _context.Objects)
                {
                    if (t.left >= obj.left && t.userId == directory.userId)
                        t.left += 2;
                    if (t.right >= obj.left && t.objectId != obj.objectId && t.userId == directory.userId)
                        t.right += 2;
                    _context.Objects.Update(t);
                }
                
                _context.Objects.Add(obj);
                _context.SaveChanges();

                var permissions = new Permissions
                {
                    parentUserId = directory.userId,
                    childUserId = int.Parse(User.Identity.Name),
                    read = true,
                    write = true,
                    objectId = obj.objectId
                };

                _context.Permissions.Add(permissions);
                _context.SaveChanges();

                var data = new
                {
                    obj.objectId,
                    obj.objectName,
                    obj.type,
                    obj.left,
                    obj.right,
                    obj.level,
                    obj.userId
                };

                return Ok(new
                {
                    error = false,
                    message = $"Директория {obj.objectName} успешно создана",
                    data
                });
            }
            catch (Exception e)
            {
                return BadRequest(new { error = true, e.Message });
            }
        }
        
        [HttpPut, Route("relocate")]
        [RequestSizeLimit(22548578304)] // ограничение веса запроса 21 гб
        // принимает objectId (директория, которую переместить), objId_new (куда переместить)
        public IActionResult Relocate([FromBody]ObjectDto objectDto)
        {
            List<object> data = new List<object>();

            var directory_this = _context.Objects
            .Single(c => c.objectId == objectDto.objectId);

            var directory_new = _context.Objects
            .Single(c => c.objectId == objectDto.objId_new);

            if (CheckWriteAllow(directory_new) == false)
                return Ok(new { error = true, message = "Недостаточно прав" });

            var catalog = from c in _context.Objects
                          where directory_this.userId == c.userId &&
                          c.left >= directory_this.left && c.right <= directory_this.right
                          orderby c.right
                          select c;

            List<Objects> obj_relocate = new List<Objects>();
            foreach (var x in catalog)
                obj_relocate.Add(x);

            var obj = from i in _context.Objects
                      where directory_this.userId == i.userId
                      select i;

            List<Objects> obj_unrelocate = new List<Objects>();
            foreach (var x in obj)
                obj_unrelocate.Add(x);

            foreach (var x in obj_relocate)
            {
                foreach (var t in obj_unrelocate)
                {
                    if (obj_relocate.Contains(t) == false)
                    {
                        if (t.left > x.right)
                            t.left -= 2;
                        if (t.right > x.right)
                            t.right -= 2;
                    }
                }

                foreach (var r in obj_relocate)
                {
                    r.left -= 2;
                    r.right -= 2;
                }
            }

            obj_relocate = obj_relocate.OrderBy(c => c.left).ToList();

            List<Objects> obj_level = new List<Objects>();
            int m = 0;
            foreach (var x in obj_relocate)
            {
                if (m == 0)
                    x.level = directory_new.level + 1;
                m = 2;
                obj_level.Add(x);
                foreach (var r in obj_relocate)
                {
                    if (obj_level.Contains(r) == false && x.level != r.level)
                        r.level = x.level + 1;
                }
            }

            foreach (var x in obj_relocate)
            {
                x.left = directory_new.right;
                x.right = directory_new.right + 1;

                foreach (var t in obj_unrelocate)
                {
                    if (t.left >= directory_new.right && t.objectId != x.objectId)
                        t.left += 2;
                    if (t.right >= directory_new.right && t.objectId != x.objectId)
                        t.right += 2;
                }

                if (directory_new.level != x.level)
                    directory_new = x;

                obj_unrelocate.Append(x);
            }

            _context.Objects.UpdateRange(obj_unrelocate);
            _context.SaveChanges();


            foreach (var x in obj_relocate)
            {
                data.Add(new
                {
                    x.objectId,
                    x.objectName,
                    x.left,
                    x.right,
                    x.level,
                    weight = x.binaryData == null ? default(long) : x.binaryData.LongLength,
                    x.userId
                });
            }

            return Ok(new
            {
                error = false,
                message = $"Перемещение выполнено",
                relocated_objects = data,
                id_parent_directory = objectDto.objectId,
                id_new_directory = objectDto.objId_new
            });
        }

        [HttpPut, Route("rename")]
        [RequestSizeLimit(22548578304)] // ограничение веса запроса 21 гб
        // принимает objectId - что переименовать, objectName - новое имя
        public IActionResult Rename([FromBody]ObjectDto objectDto)
        {
            try
            {
                var obj = _context.Objects
                .Single(c => c.objectId == objectDto.objectId);

                if (CheckWriteAllow(obj) == false)
                    return Ok(new { error = true, message = "Недостаточно прав" });

                string name_pattern = @"[\%\/\\\&\?\,\'\;\:\!\-]";
                string name_pattern1 = @"^([^\%\/\\\&\?\,\'\;\:\!\-]){2,50}$";


                if (string.IsNullOrWhiteSpace(objectDto.objectName))
                    return Ok(new { error = true, message = "Вы забыли ввести название" });

                if (Regex.IsMatch(objectDto.objectName, name_pattern))
                    return Ok(new { error = true, message = "Название содержит запрещенные символы" });
                if (!Regex.IsMatch(objectDto.objectName, name_pattern1, RegexOptions.IgnoreCase))
                    return Ok(new { error = true, message = "Допустимая длина от 2 до 50 символов" });

                if (obj.type == true)
                    obj.objectName = objectDto.objectName;
                if (obj.type == false)
                    obj.objectName = objectDto.objectName + Path.GetExtension(obj.objectName).ToLowerInvariant();

                _context.Objects.Update(obj);
                _context.SaveChanges();

                var data = new
                {
                    obj.objectId,
                    obj.objectName,
                    obj.left,
                    obj.right,
                    obj.level,
                    obj.type,
                    obj.userId
                };

                return Ok(new
                {
                    error = false,
                    message = $"Данный объект переименован в {obj.objectName}",
                    data
                });
            }
            catch (Exception e)
            {
                return BadRequest(new { error = true, e.Message });
            }
        }

        [HttpDelete, Route("delete")]
        [RequestSizeLimit(22548578304)] // ограничение веса запроса 21 гб
        // принимает objectId (либо файл, либо директория)
        public IActionResult Delete(int objectId)
        {
            try
            {
                //_logger.LogError($"111111111111111111111111   {obj.Count()}");
                var directory_this = _context.Objects
                    .Single(c => c.objectId == objectId);

                if (CheckWriteAllow(directory_this) == false)
                    return Ok(new { error = true, message = "Недостаточно прав" });

                var catalog = from c in _context.Objects
                              where directory_this.userId == c.userId && 
                              c.left >= directory_this.left && c.right <= directory_this.right
                              select c;
                
                var permissions = from p in _context.Permissions
                                  where directory_this.userId == p.parentUserId
                                  select p;

                var obj = from i in _context.Objects
                          where directory_this.userId == i.userId
                          select i;

                List<Permissions> delete_permissions = new List<Permissions>();
                List<Objects> obj_undelete = new List<Objects>();
                List<Objects> obj_delete = new List<Objects>();

                foreach (var x in catalog)
                {
                    obj_delete.Add(x);

                    foreach (var p in permissions)
                        if (x.objectId == p.objectId)
                            delete_permissions.Add(p);
                }

                obj_delete = obj_delete.OrderBy(c => c.right).ToList();
                
                foreach (var ob in obj)
                    obj_undelete.Add(ob);

                foreach (var x in obj_delete)
                {
                    foreach (var c in obj_undelete)
                    {
                        if (c.left > x.left)
                            c.left -= 2;
                        if (c.right > x.right)
                            c.right -= 2;
                    }
                }

                _context.Objects.UpdateRange(obj_undelete);
                _context.Permissions.RemoveRange(delete_permissions);
                _context.Objects.RemoveRange(obj_delete);
                _context.SaveChanges();

                return Ok(new
                {
                    error = false,
                    message = $"Объект или группа объектов успешно удалены"
                });
            }
            catch (Exception e)
            {
                return BadRequest(new { error = true, e.Message });
            }
        }
        
        [HttpPost, Route("add_permission")]
        [RequestSizeLimit(22548578304)] // ограничение веса запроса 21 гб
        // принимает objectId (объект, на который дать права), массив logins (список логинов, которым дать права),
        // логические write и read (например с чекбоксов)
        public IActionResult AddPermission([FromForm(Name = "objectId")]int objectId,
            [FromForm(Name = "logins")]string[] logins, [FromForm(Name = "read")]bool read,
            [FromForm(Name = "write")]bool write)
        {
            try
            {
                List<User> list_user = new List<User>();
                
                var obj_this = _context.Objects
                    .Single(ob => ob.objectId == objectId);

                // не должно выводиться, т.к. подразумевается, что юзер, дающий разрешения - итак владелец
                if (obj_this.userId != int.Parse(User.Identity.Name))
                    return Ok(new { error = true, message = "Вы не являетесь владельцем файла" });

                var user_parent = _context.Users
                .Single(up => int.Parse(User.Identity.Name) == up.userId);

                var catalog = from c in _context.Objects
                              where (c.left >= obj_this.left) && (c.right <= obj_this.right) &&
                              (c.userId == user_parent.userId)
                              select c;

                string users = null;

                if (read == true && write != true)
                    users += "чтение";
                if (write == true && read != true)
                    users += "запись";
                if (write == true && read == true)
                    users += "чтение и запись";
                users += " для пользователей ";
                foreach (var login in logins)
                {
                    var user_this = _context.Users
                .Single(up => login == up.login);

                    foreach (var c in catalog)
                    {
                        var perm = _context.Permissions.SingleOrDefault(x => x.parentUserId == int.Parse(User.Identity.Name) &&
                        x.childUserId == user_this.userId && x.objectId == c.objectId);

                        if (perm == null)
                        {
                            _context.Permissions.Add(
                                new Permissions
                                {
                                    parentUserId = int.Parse(User.Identity.Name),
                                    childUserId = user_this.userId,
                                    read = read,
                                    write = write,
                                    objectId = c.objectId
                                });
                        }
                        else
                        {
                            perm.read = read;
                            perm.write = write;
                            _context.Permissions.Update(perm);
                        }
                    }

                    list_user.Add(user_this);
                }

                List<object> data = new List<object>();
                foreach (var x in list_user)
                {
                    data.Add(new
                    {
                        x.userId,
                        x.login,
                        x.name,
                        x.secondName,
                        write,
                        read
                    });
                }

                for (int i = 0; i < logins.Count(); i++)
                {
                    users += logins[i];
                    if (i < logins.Count() - 1)
                        users += ", ";
                }
                
                _context.SaveChanges();

                return Ok(
                    new
                    {
                        error = false,
                        message = $"Вы предоставили разрешения на {users}. Открыт доступ для {catalog.Count()} объектов",
                        data
                    });
            }
            catch (Exception e)
            {
                return BadRequest(new { error = true, e.Message });
            }
        }

        [HttpGet, Route("get_objects")]
        [RequestSizeLimit(22548578304)] // ограничение веса запроса 21 гб
        // При открытии какой-либо директории, должен передаваться ее objId.
        // Можно не указывать ТОЛЬКО при регистрации, т.е. пользователю итак вернется базовый каталог
        public IActionResult GetObjects(int objId)
        {
            Objects this_dir = null;
            if (objId == default(int))
            {
                this_dir = _context.Objects
                .Single(c => (int.Parse(User.Identity.Name) == c.userId) &&
                (c.left == 0) && (c.level == 0));
            }
            else
                this_dir = _context.Objects.Single(c => c.objectId == objId);

            if (CheckReadAllow(this_dir) == false)
                return Ok(new { error = true, message = "Недостаточно прав" });

            var user = _context.Users.Single(u => u.userId == this_dir.userId);

            var catalog = from c in _context.Objects
                          where ((c.level == this_dir.level + 1) || (c.level == this_dir.level)) &&
                          c.left >= this_dir.left && c.right <= this_dir.right && c.userId == this_dir.userId
                          select c;

            List<object> data = new List<object>();
            foreach (var x in catalog)
            {
                if (x.type == true)
                    data.Add(new
                    {
                        x.objectId,
                        x.objectName,
                        x.type,
                        x.level,
                        x.left,
                        x.right,
                        user.userId,
                        user.login
                    });

                if (x.type == false)
                    data.Add(new
                    {
                        x.objectId,
                        x.objectName,
                        weight = x.binaryData.LongLength,
                        x.type,
                        x.level,
                        x.left,
                        x.right,
                        user.userId,
                        user.login
                    });
            }

            return Ok(new { error = false, data });

        }

        [HttpGet, Route("shared_objects")]
        [RequestSizeLimit(22548578304)] // ограничение веса запроса 21 гб
        public IActionResult Shared(int objId)
        {
            List<object> data = new List<object>();

            try
            {
                var this_obj = _context.Objects.SingleOrDefault(c => c.objectId == objId);
                if (this_obj == null)
                {
                    List<Objects> collection = new List<Objects>();
                    var catalog = from c in _context.Permissions
                                  where c.childUserId == int.Parse(User.Identity.Name) && c.childUserId != c.parentUserId
                                  select c;

                    foreach (var x in catalog)
                    {
                        collection.Add(_context.Objects
                        .Single(c => c.objectId == x.objectId));
                    }
                    int i = collection.Min(c => c.level);
                    collection.RemoveAll(x => x.level != i);

                    foreach (var x in collection)
                    {
                        if (x.type == true)
                            data.Add(new
                            {
                                x.objectId,
                                x.objectName,
                                x.type,
                                x.level,
                                parent = x.userId,
                                parent_login = _context.Users.Single(c => c.userId == x.userId).login,
                                _context.Permissions.Single(c => c.parentUserId == x.userId &&
                                c.childUserId == int.Parse(User.Identity.Name) && c.objectId == x.objectId).write,
                                _context.Permissions.Single(c => c.parentUserId == x.userId &&
                                c.childUserId == int.Parse(User.Identity.Name) && c.objectId == x.objectId).read
                            });

                        if (x.type == false)
                            data.Add(new
                            {
                                x.objectId,
                                x.objectName,
                                weight = x.binaryData.LongLength,
                                x.type,
                                x.level,
                                parent = x.userId,
                                parent_login = _context.Users.Single(c => c.userId == x.userId).login,
                                _context.Permissions.Single(c => c.parentUserId == x.userId &&
                                c.childUserId == int.Parse(User.Identity.Name) && c.objectId == x.objectId).write,
                                _context.Permissions.Single(c => c.parentUserId == x.userId &&
                                c.childUserId == int.Parse(User.Identity.Name) && c.objectId == x.objectId).read
                            });
                    }

                    return Ok(new { error = false, data });
                }
                else
                {
                    List<Objects> collection = new List<Objects>();
                    var catalog = from c in _context.Permissions
                                  where c.childUserId == int.Parse(User.Identity.Name) && c.childUserId != c.parentUserId
                                  select c;

                    foreach (var x in catalog)
                    {
                        collection.Add(_context.Objects
                        .Single(c => c.objectId == x.objectId));
                    }

                    collection.RemoveAll(x => x.level != (this_obj.level + 1));
                    
                    foreach (var x in collection)
                    {
                        if (x.type == true)
                            data.Add(new
                            {
                                x.objectId,
                                x.objectName,
                                x.type,
                                x.level,
                                parent = x.userId,
                                parent_login = _context.Users.Single(c => c.userId == x.userId).login,
                                _context.Permissions.Single(c => c.parentUserId == x.userId &&
                                c.childUserId == int.Parse(User.Identity.Name) && c.objectId == x.objectId).write,
                                _context.Permissions.Single(c => c.parentUserId == x.userId &&
                                c.childUserId == int.Parse(User.Identity.Name) && c.objectId == x.objectId).read
                            });

                        if (x.type == false)
                            data.Add(new
                            {
                                x.objectId,
                                x.objectName,
                                weight = x.binaryData.LongLength,
                                x.type,
                                x.level,
                                parent = x.userId,
                                parent_login = _context.Users.Single(c => c.userId == x.userId).login,
                                _context.Permissions.Single(c => c.parentUserId == x.userId &&
                                c.childUserId == int.Parse(User.Identity.Name) && c.objectId == x.objectId).write,
                                _context.Permissions.Single(c => c.parentUserId == x.userId &&
                                c.childUserId == int.Parse(User.Identity.Name) && c.objectId == x.objectId).read
                            });
                    }

                    return Ok(new { error = false, data });
                }
            }

            catch (Exception ex)
            {
                InvalidOperationException e = new InvalidOperationException();
                if (ex.GetType() == e.GetType())
                    return Ok(new { error = false, data }); 
                else
                    return BadRequest(new { error = true, e.Message});
            }
        }

        [HttpGet, Route("added_users")]
        [RequestSizeLimit(22548578304)] // ограничение веса запроса 21 гб
        // Вывод списка пользователей, которым предоставлены разрешения на файл. Принимает id данного файла
        public IActionResult AddedUsers(int objId)
        {
            try
            {
                var this_dir = _context.Objects.Single(c => c.objectId == objId);

                if (int.Parse(User.Identity.Name) != this_dir.userId)
                    return Ok(new { error = true, message = "Недостаточно прав" });

                var permission = from p in _context.Permissions
                                 where p.objectId == objId
                                 select p;

                List<object> childs = new List<object>();
                foreach (var x in permission)
                {
                    if (_context.Users.SingleOrDefault(c => (c.userId == x.childUserId) &&
                    (x.parentUserId != x.childUserId)) != null)
                    childs.Add(new
                    {
                        _context.Users.Single(c => (c.userId == x.childUserId) && (x.parentUserId != x.childUserId)).login,
                        x.read,
                        x.write
                    });
                }

                var parent = _context.Users.Single(c => c.userId == this_dir.userId).login;

                return Ok(new { error = false, childs, parent });
            }
            catch (Exception e)
            {
                InvalidOperationException ex = new InvalidOperationException();
                if (e.GetType() == e.GetType())
                    return Ok(new { error = false, message = "Пользователь с указанным логином не найден" });
                else
                    return BadRequest(new { error = true, e.Message });
            }
        }

        [HttpGet, Route("this_user")]
        [RequestSizeLimit(22548578304)] // ограничение веса запроса 21 гб
        // текущий пользователь
        public IActionResult ThisUsers()
        {
            try
            {
                var user_1 = _context.Users.Single(up => int.Parse(User.Identity.Name) == up.userId);
                var error = new { error = false };
                var user = new
                {
                    user_1.userId,
                    user_1.login,
                    user_1.name,
                    user_1.secondName
                };

                return Ok(new
                {
                    error,
                    user
                });
            }
            catch (Exception e)
            {
                return BadRequest(new { error = true, e.Message });
            }
        }

        [HttpDelete, Route("remove_permissions")]
        [RequestSizeLimit(22548578304)] // ограничение веса запроса 21 гб
        // принимает login, objectId (тот, на который права нужно отозвать
        // забирает любые права на объект и дочерние объекты у данного пользователя
        public IActionResult RemovePermissions(int objectId, string login)
        {
            try
            {
                var user_this = _context.Users
                .Single(up => login == up.login);

                var obj_this = _context.Objects
                .Single(ob => ob.objectId == objectId);

                if (int.Parse(User.Identity.Name) != obj_this.userId)
                    return Ok(new { error = true, message = "Недостаточно прав" });

                var catalog = from c in _context.Objects
                              where (c.left >= obj_this.left && c.right <= obj_this.right && c.userId == obj_this.userId)
                              select c;

                var t = catalog;

                foreach (var x in catalog)
                {
                    var perm = _context.Permissions.Single(p => p.childUserId == user_this.userId &&
                    p.objectId == x.objectId && p.childUserId != p.parentUserId);
                    _context.Permissions.Remove(perm);
                }

                _context.SaveChanges();

                return Ok(new
                {
                    error = false,
                    message = $"У пользователя с логином {login} были удалены права на {obj_this.objectName} и дочерние объекты"
                });
            }
            catch (Exception e)
            {
                return BadRequest(new { error = true, e.Message });
            }
        }

        [HttpGet, Route("storage_size")]
        [RequestSizeLimit(22548578304)] // ограничение веса запроса 21 гб
        // ничего не принимает
        public IActionResult CheckSizeStorage()
        {
            var userId = int.Parse(User.Identity.Name);
            var objects = from t in _context.Objects
                          where (userId == t.userId) && (t.type == false)
                          select t;

            long size_files = 0;
            foreach (var x in objects)
                size_files += x.binaryData.LongLength;

            return Ok(new
            {
                error = false,
                message = $"Использовано {Funct(size_files)}. Доступно {Funct(max_size - size_files)}",
                used = $"{size_files}",
                available = $"{max_size - size_files}"

            });

        }

        // дальше идут вспомогательные методы
        private string Funct(long param)
        {
            string local_var = "";

            if (param < 1024)
                local_var = param.ToString() + " Б";
            if (param >= 1024 && param < 1048576)
                local_var = Round((double)param / 1024, 2) + " Кб";
            if (param >= 1048576 && param < 1073741824)
                local_var = Round((double)param / 1048576, 2) + " Мб";
            if (param >= 1073741824 && param < 22548578304)
                local_var = Round((double)param / 1073741824, 2) + " Гб";

            return local_var;
        }

        private string GetContentType(string path)
        {
            var types = new Dictionary<string, string>
            {
                {".txt", "text/plain"},
                {".pdf", "application/pdf"},
                {".doc", "application/vnd.ms-word"},
                {".docx", "application/vnd.ms-word"},
                {".xls", "application/vnd.ms-excel"},
                {".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"},
                {".png", "image/png"},
                {".jpg", "image/jpeg"},
                {".jpeg", "image/jpeg"},
                {".gif", "image/gif"},
                {".csv", "text/csv"},
                {".mp4", "video/mp4" },
                {".mp3", "audio/mpeg"},
                {"", "application/octet-stream"},
                {".ogg", "application/ogg"},
                {".zip", "application/zip"},
                {".xml", "application/xml"},
                {".wav", "audio/vnd.wave"},
                {".svg", "image/svg+xml"},
                {".ico", "image/vnd.microsoft.icon"},
                {".css", "text/css"},
                {".html", "text/html"},
                {".3gp", "video/3gpp"},
                {".webm", "video/webm"},
                {".exe", "application/octet-stream"},
                {".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation"},
                {".ppt", "application/vnd-mspowerpoint"},
                {".apk", "application/vnd.android.package-archive"}
            };
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return types[ext];
        }

        private bool CheckWriteAllow(Objects obj)
        {
            var permission = _context.Permissions
                .SingleOrDefault(c => obj.userId == c.parentUserId &&
                c.childUserId == int.Parse(User.Identity.Name) && obj.objectId == c.objectId);
            if (permission != null)
            {
                if (permission.write != true)
                    return false;
            }
            else
                return false;
            return true;
        }

        private bool CheckReadAllow(Objects obj)
        {
            var permission = _context.Permissions
                .SingleOrDefault(c => obj.userId == c.parentUserId &&
                c.childUserId == int.Parse(User.Identity.Name) && obj.objectId == c.objectId);
            if (permission != null)
            {
                if (permission.read != true)
                    return false;
            }
            else
                return false;
            return true;
        }
    }
}
