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
    using Hierarchy.Universal;
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

        [NotMapped]
        public short? Level
        { get { return (short)Conversions.Bytes2HierarchyId(hid).GetLevel(); } }

        [NotMapped]
        public long parent
        {
            get
            {

                hidServices<GLaccounts> hs = new hidServices<GLaccounts>(new GLaccountsModel());
                byte[] parentHid = Conversions.HierarchyId2Bytes(Conversions.Bytes2HierarchyId(hid).GetAncestor(1));
                return hs.GetPk(parentHid);
            }
        }

        [NotMapped]
        public Boolean chk { get; set; }

    }
}
