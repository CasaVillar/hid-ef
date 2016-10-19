using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Types;
using System.IO;
using System.Web;

namespace Hierarchy.Common
{
    public static class Conversions
    {
        public static SqlHierarchyId Bytes2HierarchyId(byte[] b)
        {
            SqlHierarchyId h;
            if (b == null)
            {
                return SqlHierarchyId.Null;
            }
            var stream = new MemoryStream(b, false);
            BinaryReader br;
            br = new BinaryReader(stream);
            h.Read(br);
            br.Close();
            br.Dispose();
            stream.Close();
            stream.Dispose();
            return h;
        }

        public static byte[] HierarchyId2Bytes(SqlHierarchyId h)
        {
            byte[] b = null;
            if (!h.IsNull)
            {
                var stream = new MemoryStream();
                BinaryWriter bw;
                bw = new BinaryWriter(stream);
                h.Write(bw);
                b = stream.ToArray();
                stream.Close();
                stream.Dispose();
                bw.Close();
                bw.Dispose();
            }
            return b;
        }
    }
}