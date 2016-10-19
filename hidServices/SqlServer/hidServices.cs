using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using System.Data.SqlClient;
using Microsoft.SqlServer.Types;
using System.IO;

using System.Collections;
using Hierarchy.Common;

namespace Hierarchy.SqlServer
{
    public class hidServices<T> where T : class, IHierarchy
    {
        public enum Commands
        {
            Promote,
            Demote,
            Up,
            Down
        }

        private DbContext db;
        //private DbSet Table;
        private string TableName;
        private string PKName;
        private GenericRepository<T> repo;


        public hidServices(DbContext dbc)
        {
            db = dbc;
            PKName = DbContextMetadata.FindPrimaryKey<T>(db).ToArray()[0].ToString();
            TableName = DbContextMetadata.FindTableName<T>(db);
            repo = new GenericRepository<T>(db);
        }

        public byte[] GetHid(string pk)
        // Obtém hid do registro
        {
            byte[] b = null;
            try
            {
                b = repo.Find(new object[] { long.Parse(pk) }).hid;
            }
            catch (Exception ex)
            {
                throw new Exception("Erro em Getid: " + ex.Message);
            }
            return b;
        }
        public long GetPk(byte[] hid)
        // Obtém pk a partir da hid
        {
            long pk = 0;
            if (hid != null)
            {
                try
                {
                    T t = repo.Get.Where(p => p.hid == hid).First();
                    pk = (long)t.GetType().GetProperty(PKName).GetValue(t, null);
                }
                catch (Exception ex)
                {
                    throw new Exception("Erro em GetPK: " + ex.Message);
                }
            }
            return pk;
        }

        public string WhereClause(ArrayList collapsed)
        {
            System.Text.StringBuilder wc = new System.Text.StringBuilder("");
            foreach (string item in collapsed)
            {
                SqlHierarchyId itemHid = Conversions.Bytes2HierarchyId(GetHid(item));
                wc.Append("AND ((cast(hid as hierarchyid).IsDescendantOf('" + itemHid.ToString() + "')=0 or cast(hid as hierarchyid) = '" + itemHid.ToString() + "'))");
            }
            string result = wc.ToString();
            if (result == "")
            {
                result = "1=1";
            }
            else
	        {
		         result =  result.Substring(3);
	        }
            return result;
        }
        
        public void Command(String pk, Commands command)
        {
            switch (command)
            {
                case Commands.Promote:
                    // Transfere nó para nível imediatamente inferior (número do nível) 
                    Promote(pk);
                    break;
                case Commands.Demote:
                    // Transfere nó para nível imediatamente superior (número do nível) 
                    // (como filho do irmão anterior)
                    Demote(pk);
                    break;
                case Commands.Up:
                    // Troca nó de posição com o irmão anterior ficando mais próximo à mãe  
                    Up(pk);
                    break;
                case Commands.Down:
                    // Troca nó de posição com o irmão posterior ficando mais afastado da mãe  
                    Down(pk);
                    break;
            }
        }

        public byte[] GetNextSonHid(String mae)
        // Obtém hid do próximo novo filho
        {
            byte[] b = null;
            try
            {
                // obter hid da mãe
                if (string.IsNullOrEmpty(mae))
                {
                    b = Conversions.HierarchyId2Bytes(SqlHierarchyId.GetRoot());
                }
                else
                {
                    string command = "DECLARE @last_child hierarchyid, @maeId as hierarchyid, @mae as bigint " + Environment.NewLine +
                    " SET TRANSACTION ISOLATION LEVEL SERIALIZABLE " + Environment.NewLine +
                    "                BEGIN TRANSACTION" + Environment.NewLine +
                    " set @mae = " + (mae == null ? " null" : mae) + Environment.NewLine +
                    " declare @hid as hierarchyid" + Environment.NewLine +
                    " if @mae is null" + Environment.NewLine +
                    "	set @maeID = null" + Environment.NewLine +
                    "                Else" + Environment.NewLine +
                    "                    begin" + Environment.NewLine +
                    "		select @maeId =  cast(hid as hierarchyid) from " + TableName + " where " + PKName + " = @mae" + Environment.NewLine +
                    "                End" + Environment.NewLine +
                    " if @maeID is null" + Environment.NewLine +
                    "	set @hid = hierarchyid::GetRoot() " + Environment.NewLine +
                    "                Else" + Environment.NewLine +
                    "                    begin" + Environment.NewLine +
                    "		select @last_child = cast(MAX(hid) as hierarchyid) from " + TableName + " where cast(hid as hierarchyid).GetAncestor(1) = @maeId" + Environment.NewLine +
                    "		set @hid = @maeID.GetDescendant(@last_child,NULL)" + Environment.NewLine +
                    "                End" + Environment.NewLine +
                    "       select cast(@hid as varbinary) as hid " + Environment.NewLine +
                    "                COMMIT";

                    var result = db.Database.SqlQuery<byte[]>(command).ToList();
                    b = result.SelectMany(a => a).ToArray();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Erro em GetNextSonHid: " + ex.Message);
            }
            return b;
        }

        public void delete(string pk)
        {
            try
            {
                string command = "select COUNT(*) from " + TableName + " t where Cast(hid as hierarchyid).IsDescendantOf((Select cast(hid as hierarchyid) from " + TableName + " where " + PKName + " = " + pk + ")) = 1";
                var result = db.Database.SqlQuery<int>(command).ToList();
                //long[] count = result.SelectMany(a => a).ToArray();
                if (result[0] > 1)
                {
                    throw new Exception("There are descendants.");
                }
                db.Database.ExecuteSqlCommand("delete " + TableName + " where " + PKName + " = " + pk);
            }
            catch (Exception ex)
            {

                throw new Exception("Deletion error: " + ex.Message);
            }
        }

        public void DeleteTree(String pk)
        // remove toda uma sub árvore
        {
            try
            {
                 string Command =
                "declare @maxniv as int, @hidArv as hierarchyid, @arv as bigint " + Environment.NewLine +
                "set @arv = " + pk + Environment.NewLine +
                "set @hidArv = (select cast(hid as hierarchyid) from " + TableName + " where " + PKName + " = @arv) " + Environment.NewLine +
                "set @maxniv = (select max(Lhid) from " + TableName + " where cast(hid as hierarchyid).IsDescendantOf(@hidArv) = 1) " + Environment.NewLine +
                "WHILE  @maxniv > 0 " + Environment.NewLine +
                "  BEGIN " + Environment.NewLine +
                "    delete " + TableName + " where cast(hid as hierarchyid).IsDescendantOf(@hidArv) = 1 and Lhid = @maxniv " + Environment.NewLine +
                "	 set @maxniv = @maxniv - 1 " + Environment.NewLine +
                "  End ";
                db.Database.ExecuteSqlCommand(Command);
            }
            catch (Exception ex)
            { throw new Exception("Error deleting sub tree: " + ex.Message); }
        }

        public void Promote(String pk)
        {
            // Transfere nó para nível imediatamente inferior (número do nível) 
            try
            {
                byte[] b = GetHid(pk);
                SqlHierarchyId h = Conversions.Bytes2HierarchyId(b);
                if (h.GetLevel() < 2)
                { throw new Exception("It is not allowed promotion from level 1 or 0."); }
                var result = db.Database.SqlQuery<long>("select " + PKName + " from " + TableName + " where hid = cast((Select cast(hid as hierarchyid) from " + TableName + " where " + PKName + " = " + pk + ").GetAncestor(2) as varbinary)").ToList();
                long novamae = result[0];
                MovSubTree(pk, novamae.ToString());
            }
            catch (Exception ex)
            { 
                throw new Exception("Error on promote." + ex.Message); 
            }
        }

        public void Demote(String pk)
        // Transfere nó para nível imediatamente superior (número do nível) 
        // (como filho do irmão anterior)
        {
            try
            {
                // Irmão anterior (mais próximo à mãe)
                string command = "select top 1 " + PKName + " from " + TableName + " where cast(hid as hierarchyid).GetAncestor(1) = " +
                "(select cast(hid as hierarchyid).GetAncestor(1) from " + TableName + " where " + PKName + " = " + pk + ") and hid < (select hid from " + TableName + " where " + PKName + " = " + pk + ") " +
                "order by hid desc";
                var result = db.Database.SqlQuery<long>(command).ToList();
                long novamae = result[0];
                MovSubTree(pk, novamae.ToString());
            }
            catch (Exception)
            { 
                throw new Exception("Demotion not allowed: there is no closer sibling to parent."); 
            }
        }

        public void Up(String pk)
        // Troca nó de posição com o irmão anterior ficando mais próximo à mãe  
        {
            try
            {
                // Irmão anterior (mais próximo à mãe)
                string command =
                "declare @hid as hierarchyid" + Environment.NewLine +
                "set @hid = (select cast(hid as hierarchyid) from " + TableName + " where " + PKName + " = " + pk + ")" + Environment.NewLine +
                "select top 1 " + PKName + " from " + TableName + " where hid < @hid and cast(hid as hierarchyid).GetAncestor(1) = @hid.GetAncestor(1) order by hid desc";
                try
                {
                    var result = db.Database.SqlQuery<long>(command).ToList();
                    long irmaoAnterior = result[0];
                    TrocaPosicaoFilhos(pk, irmaoAnterior.ToString());
                }
                catch (Exception)
                {
                    //throw new Exception("Erro:" + ex.Message);
                }
            }
            catch (Exception)
            { }
        }

        public void Down(String pk)
        // Troca nó de posição com o irmão posterior ficando mais afastado da mãe  
        {
            try
            {
                // Irmão posterior (mais afastado da mãe)
                string command =
                "declare @hid as hierarchyid" + Environment.NewLine +
                "set @hid = (select cast(hid as hierarchyid) from " + TableName + " where " + PKName + " = " + pk + ")" + Environment.NewLine +
                "select top 1 " + PKName + " from " + TableName + " where hid > @hid and cast(hid as hierarchyid).GetAncestor(1) = @hid.GetAncestor(1) order by hid asc";
                try
                {
                    var result = db.Database.SqlQuery<long>(command).ToList();
                    long irmaoPosterior = result[0];
                    TrocaPosicaoFilhos(pk, irmaoPosterior.ToString());
                }
                catch (Exception)
                {
                    //throw new Exception("Erro:" + ex.Message);
                }
            }
            catch (Exception)
            { }
        }

        public void MovSubTree(String pk, String novaMae)
        {
            try
            {
                string command =
                    "declare @hid as hierarchyId, @novaHid as hierarchyid, @maeHid as hierarchyId, @novaMaeHid as hierarchyid" + Environment.NewLine +
                    "declare @pk as bigint, @novaMae as bigint" + Environment.NewLine +
                    "set @pk = " + pk + Environment.NewLine +
                    "set @novaMae = " + novaMae + Environment.NewLine +
                    "SET TRANSACTION ISOLATION LEVEL SERIALIZABLE " + Environment.NewLine +
                    "BEGIN TRANSACTION" + Environment.NewLine +
                    "select @hid = cast(hid as hierarchyid), @maeHid= cast(hid as hierarchyid).GetAncestor(1) from " + TableName + " where " + PKName + " = @pk" + Environment.NewLine +
                    "select @novaMaeHid = cast(hid as hierarchyid) from " + TableName + " where " + PKName + " = @novaMae" + Environment.NewLine +
                    "SELECT @novaHid = @novaMaeHid.GetDescendant(cast(max(hid) as hierarchyid), NULL) " + Environment.NewLine +
                    "FROM " + TableName + " WHERE cast(hid as hierarchyid).GetAncestor(1)=@novaMaeHid" + Environment.NewLine +
                    "-- move o nó" + Environment.NewLine +
                    "update " + TableName + " set hid = cast(@novaHid as varbinary) where " + PKName + " = @pk " + Environment.NewLine +
                    "-- move os descendentes" + Environment.NewLine +
                    "update " + TableName + " set hid = cast(cast(hid as hierarchyid).GetReparentedValue(@hid,@novaHid) as varbinary)" + Environment.NewLine +
                    "where cast(hid as hierarchyid).IsDescendantOf(@hid) = 1 and hid <> cast(@hid as varbinary)" + Environment.NewLine +
                    "COMMIT";
                db.Database.ExecuteSqlCommand(command);
            }
            catch (Exception ex)
            { throw new Exception("Error moving sub tree: " + ex.Message); }
        }

        public void TrocaPosicaoFilhos(String Filho1, String Filho2)
        {
            try
            {
                string command =
                    "declare @hid1 as hierarchyId, @hid2 as hierarchyId" + Environment.NewLine +
                    "declare @filho1 as BigInt, @filho2 as BigInt" + Environment.NewLine +
                    "set @filho1 = " + Filho1 + Environment.NewLine +
                    "set @filho2 = " + Filho2 + Environment.NewLine +
                    "SET TRANSACTION ISOLATION LEVEL SERIALIZABLE " + Environment.NewLine +
                    "BEGIN TRANSACTION" + Environment.NewLine +
                    "select @hid1 = cast(hid as hierarchyid) from " + TableName + " where " + PKName + " = @filho1" + Environment.NewLine +
                    "select @hid2 = cast(hid as hierarchyid) from " + TableName + " where " + PKName + " = @filho2" + Environment.NewLine +
                    "-- Troca posicoes" + Environment.NewLine +
                    "update " + TableName + " set hid = case when " + PKName + Environment.NewLine +
                    " = @filho1 then cast(@hid2 as varbinary) when " + PKName + " = @filho2 then cast(@hid1 as varbinary) else hid end where " + PKName + " in (@filho1,@filho2) " + Environment.NewLine +
                    "-- Reorganiza descendente" + Environment.NewLine +
                    "update " + TableName + " set hid = cast(case " + Environment.NewLine +
                    " when cast(hid as hierarchyid).IsDescendantOf(@hid1)=1  then cast(hid as hierarchyid).GetReparentedValue(@hid1,@hid2)" + Environment.NewLine +
                    " when cast(hid as hierarchyid).IsDescendantOf(@hid2)=1 then cast(hid as hierarchyid).GetReparentedValue(@hid2,@hid1) end as varbinary)" + Environment.NewLine +
                    "where (cast(hid as hierarchyid).IsDescendantOf(@hid1) =1 or cast(hid as hierarchyid).IsDescendantOf(@hid2) =1) and cast(hid as hierarchyid) not in  (@hid1, @hid2)" + Environment.NewLine +
                    "COMMIT";
                db.Database.ExecuteSqlCommand(command); ;
            }
            catch (Exception ex)
            { throw new Exception("Error changing sibling positions: " + ex.Message); }
        }

        public Boolean RootExists()
        {
            try
            {
                byte[] bRoot = Conversions.HierarchyId2Bytes(SqlHierarchyId.GetRoot());
                long nr = repo.Get.Where(p => p.hid == bRoot).Count();
                return (nr > 0);
            }
            catch (Exception ex)
            { throw new Exception("Erro ao verificar root: " + ex.Message); }
        }


    }
}
