using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace hidMy.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;
    using System.Data.SqlClient;
    using System.Collections;
    using Microsoft.SqlServer.Types;
    using System.IO;
    using Hierarchy.SqlServer;
    using Hierarchy.Common;

    public partial class GLaccounts : IHierarchy
    {
        [Key]
        [DatabaseGenerated(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.Identity)]
        public long Account { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; }

        [MaxLength(892)]
        public byte[] hid { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public short? Level { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public long? parent { get; set; }

        [NotMapped]
        public Boolean chk { get; set; }

    }
}
