using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.Spatial;
using System.Data.SqlClient;
using System.Collections;
using Microsoft.SqlServer.Types;
using Hierarchy.SqlServer;
using Hierarchy.Common;

namespace hidMy.Models
{
    public partial class GLaccounts
    {
        #region Auxiliares da Hierarquia
        private Boolean IsDescendantOf(hidServices<GLaccounts> hs, string pk)
        {
            SqlHierarchyId h = Conversions.Bytes2HierarchyId(hid);
            SqlHierarchyId pkHid = Conversions.Bytes2HierarchyId(hs.GetHid(pk));
            return (Boolean)h.IsDescendantOf(pkHid);
        }

        public Boolean IsNotCollapsed(hidServices<GLaccounts> hs, ArrayList c)
        {
            Boolean condition = true;
            foreach (string i in c)
            {
                if ((!IsDescendantOf(hs, i)) || (Account == long.Parse(i)))
                {
                    condition = true;
                }
                else
                {
                    condition = false;
                    return false;
                }
            }
            return condition;
        }
        #endregion
    }
}