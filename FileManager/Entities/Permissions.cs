using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FileManager.Entities
{
    public class Permissions
    {
        public int permissionId { get; set; }
        public int parentUserId { get; set; }
        public int childUserId { get; set; }
        public int objectId { get; set; }
        public byte write { get; set; }
        public byte read { get; set; }
    }
}
