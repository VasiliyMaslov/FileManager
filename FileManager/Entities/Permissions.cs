using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace FileManager.Entities
{
    public class Permissions
    {
        [Key]
        public int permissionId { get; set; }
        [ForeignKey("parentUserID")]
        public int? parentUserId { get; set; }
        [ForeignKey("childUserID")]
        public int? childUserId { get; set; }
        public int objectId { get; set; }
        public bool write { get; set; }
        public bool read { get; set; }
        public Objects obj { get; set; }
        public virtual User parentUser { get; set; }
        public virtual User childUser { get; set; }
    }
}
