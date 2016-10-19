using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hierarchy.SqlServer
{
    public interface IHierarchy
    {
        byte[] hid { get; set; }
        short? Level { get; set; }
        long? parent { get; set; }
        Boolean chk { get; set; }
    }
}
