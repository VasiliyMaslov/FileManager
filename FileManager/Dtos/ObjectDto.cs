using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FileManager.Dtos
{
    public class ObjectDto
    {
        public int objectId { get; set; }
        public int objId_new { get; set; }
        public int userId { get; set; }
        public string objectName { get; set; }
        public bool type { get; set; } 
        public IFormFile File { get; set; }
        public int parentUserId { get; set; }
    }
}
