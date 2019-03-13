using FileManager.Entities;
using FileManager.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static System.Convert;

namespace FileManager.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/catalog")]
    public class ObjectController : ControllerBase
    {
        //private ApplicationContext _context;
        //private readonly long max_size;
        //private List<string> suffixes = new List<string> { " Б", " КБ", " МБ", " ГБ" };

        //public ObjectController(ApplicationContext context)
        //{
        //    _context = context;
        //    max_size = 21474836480;
        //}

        //// Загрузка файла
        //[HttpPost, Route("upload_file")]
        //[RequestSizeLimit(22548578304)] // ограничение веса запроса 21 гб
        //// objId - ИД директории
        //public async Task<IActionResult> Upload(IFormFile file, int objId, int parentUserID, int childUserID = default(int))
        //{
        //    var directory = _context.Objects
        //        .Single(c => (parentUserID == c.userId) &&
        //        (ToBoolean(c.type) == true) && (c.objectId == objId));

        //    if (CheckPermissions(directory, objId, parentUserID, childUserID) == false)
        //        return BadRequest(new { message = "Недостаточно прав" });

        //    if (file == null || file.Length == 0)
        //        return Content("Файл не выбран");
        //    if (file.FileName.Length >= 50)
        //        return Content("Название файла должно быть меньше 50-ти символов");
        //    if (file.Length > max_size)
        //        return Content("Размер файла не должен превышать 20 Гб");
        //    var objects = (IEnumerable<Objects>)_context.Objects
        //        .Select(c => (parentUserID == c.userId) && (ToBoolean(c.type) == false));

        //    int size_files = 0;
        //    foreach (var x in objects)
        //        size_files += x.binaryData.Length;

        //    var available_size = max_size - size_files;

        //    if ((available_size - file.Length) <= 0)
        //        return Content("Недостаточно места");

        //    Objects obj = new Objects();

        //    using (var memoryStream = new MemoryStream())
        //    {
        //        await file.CopyToAsync(memoryStream);
        //        obj.binaryData = memoryStream.ToArray();
        //    }
        //    obj.objectName = file.FileName;
        //    obj.userId = parentUserID;
        //    obj.type = 0;
        //    obj.right = directory.right;
        //    obj.left = directory.left;

        //    try
        //    {
        //        _context.Objects.Add(obj);
        //        _context.SaveChanges();

        //        var permissions = new Permissions
        //        {
        //            parentUserId = parentUserID,
        //            childUserId = childUserID == default(int) ? default(int) : childUserID,
        //            read = 1,
        //            write = 1,
        //            objectId = _context.Objects
        //            .Single(c => (parentUserID == c.userId) &&
        //            (obj.left == directory.left) && (obj.right == directory.right)).objectId
        //        };

        //        _context.Permissions.Add(permissions);
        //        _context.SaveChanges();
        //    }
        //    catch (Exception e)
        //    {
        //        return BadRequest(new { e.Message });
        //    }

        //    return Ok($"Файл {obj.objectName} загружен");
        //}




        //[HttpGet, Route("download")]
        //[RequestSizeLimit(22548578304)] // ограничение веса запроса 21 гб
        //public IActionResult Download(int objId, int parentUserID, int childUserID = default(int))
        //{
        //    var directory = _context.Objects
        //        .Single(c => (parentUserID == c.userId) &&
        //        (ToBoolean(c.type) == true) && (c.objectId == objId));

        //    if (CheckPermissions(directory, objId, parentUserID, childUserID) == false)
        //        return BadRequest(new { message = "Недостаточно прав" });

        //    var obj = _context.Objects
        //        .Single(c => (parentUserID == c.userId) &&
        //        (ToBoolean(c.type) == false) && (directory.objectId == objId));

        //    return Ok(File(obj.binaryData, GetContentType(obj.objectName), Path.GetFileName(obj.objectName)));
        //}


        //[HttpPost, Route("create_directory")]
        //public IActionResult CreateDirectory(string name, int objId, int parentUserID, int childUserID = default(int))
        //{

        //    var directory = _context.Objects
        //        .Single(c => (parentUserID == c.userId) &&
        //        (ToBoolean(c.type) == true) && (c.objectId == objId));

        //    if (CheckPermissions(directory, objId, parentUserID, childUserID) == false)
        //        return BadRequest(new { message = "Недостаточно прав" });

        //    int left_root = directory.right;

        //    var catalog = (IEnumerable<Objects>)_context.Objects
        //        .Select(c => (parentUserID == c.userId) &&
        //        (ToBoolean(c.type) == true) && (c.left > directory.left) && (c.right > directory.right));

        //    foreach (var x in catalog)
        //    {
        //        x.right += 2;
        //        x.left += 2;
        //        _context.Objects.Update(x);
        //    }

        //    var obj = new Objects
        //    {
        //        objectName = name,
        //        left = left_root,
        //        right = left_root + 1,
        //        type = 1,
        //        userId = parentUserID
        //    };

        //    try
        //    {
        //        _context.Objects.Add(obj);
        //        _context.SaveChanges();

        //        var permissions = new Permissions
        //        {
        //            parentUserId = parentUserID,
        //            childUserId = childUserID == default(int) ? default(int) : childUserID,
        //            read = 1,
        //            write = 1,
        //            objectId = _context.Objects
        //           .Single(c => (parentUserID == c.userId) &&
        //           (obj.left == directory.left) && (obj.right == directory.right)).objectId
        //        };

        //        _context.Permissions.Add(permissions);
        //        _context.SaveChanges();
        //    }
        //    catch (Exception e)
        //    {
        //        return BadRequest(new { e.Message });
        //    }

        //    return Ok($"Директория {obj.objectName} успешно создана");
        //}


        //[HttpPut, Route("relocate")]
        //public IActionResult Relocate(int objId, int objId_new, int parentUserID, int childUserID = default(int))
        //{
        //    var directory_this = _context.Objects
        //        .Single(c => (parentUserID == c.userId) &&
        //        (ToBoolean(c.type) == true) && (c.objectId == objId));

        //    var directory_new = _context.Objects
        //        .Single(c => (parentUserID == c.userId) && (c.objectId == objId_new));

        //    if (CheckPermissions(directory_new, objId, parentUserID, childUserID) == false)
        //        return BadRequest(new { message = "Недостаточно прав" });

        //    int left_root = directory_this.right;

        //    var catalog = (IEnumerable<Objects>)_context.Objects
        //        .Select(c => (parentUserID == c.userId) &&
        //        (ToBoolean(c.type) == true) && (c.left > directory.left) && (c.right > directory.right));

        //    foreach (var x in catalog)
        //    {
        //        x.right += 2;
        //        x.left += 2;
        //        _context.Objects.Update(x);
        //    }



        //    try
        //    {
        //        _context.Objects.Update(obj);
        //        _context.SaveChanges();

        //    }
        //    catch (Exception e)
        //    {
        //        return BadRequest(new { e.Message });
        //    }

        //    return Ok($"Директория  успешно создана");
        //}


        //[HttpGet, Route("storage_size")]
        //[RequestSizeLimit(22548578304)] // ограничение веса запроса 21 гб
        //public IActionResult CheckSizeStorage()
        //{
        //    var objects = (IEnumerable<Objects>)_context.Objects
        //        .Select(c => (int.Parse(User.Identity.Name) == c.userId) && (ToBoolean(c.type) == false));

        //    int size_files = 0;
        //    foreach (var x in objects)
        //        size_files += x.binaryData.Length;

        //    var available_size = max_size - size_files;

        //    string Funct(long param)
        //    {
        //        for (int i = 0; i < suffixes.Count; i++)
        //        {
        //            long temp = param / (int)Math.Pow(1024, i + 1);
        //            if (temp == 0)
        //                return (param / (int)Math.Pow(1024, i)) + suffixes[i];
        //        }
        //        return param.ToString();
        //    }

        //    return Ok($"Использовано {Funct(size_files)}. Доступно {Funct(available_size)}");
        //}

        //private string GetContentType(string path)
        //{
        //    var types = new Dictionary<string, string>
        //    {
        //        {".txt", "text/plain"},
        //        {".pdf", "application/pdf"},
        //        {".doc", "application/vnd.ms-word"},
        //        {".docx", "application/vnd.ms-word"},
        //        {".xls", "application/vnd.ms-excel"},
        //        {".xlsx", "application/vnd.openxmlformatsofficedocument.spreadsheetml.sheet"},
        //        {".png", "image/png"},
        //        {".jpg", "image/jpeg"},
        //        {".jpeg", "image/jpeg"},
        //        {".gif", "image/gif"},
        //        {".csv", "text/csv"},
        //        {".mp4", "video/mp4" }
        //    };
        //    var ext = Path.GetExtension(path).ToLowerInvariant();
        //    return types[ext];
        //}

        //private bool CheckPermissions(Objects directory, int objId, int parentUserID, int childUserID)
        //{
        //    if (int.Parse(User.Identity.Name) == childUserID)
        //    {
        //        var catalog = (IEnumerable<Objects>)_context.Objects
        //        .Select(c => (parentUserID == c.userId) &&
        //        (ToBoolean(c.type) == true) && (c.left <= directory.left) && (c.right >= directory.right));

        //        foreach (var x in catalog)
        //        {
        //            var permission = _context.Permissions
        //                .Single(c => x.userId == c.parentUserId &&
        //                 x.objectId == c.objectId);
        //            if (permission.write != 1)
        //                return false;
        //        }
        //    }
        //    return true;
        //}

    }
}
