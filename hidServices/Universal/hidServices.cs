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

namespace Hierarchy.Universal
{
    public class hidServices<T> where T: class, IHierarchy
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
                    SqlHierarchyId maeHid = Conversions.Bytes2HierarchyId( repo.Find(new object[] { long.Parse(mae) }).hid);
                    long m = long.Parse(mae);

                    // byte[] LastChildHid = null; // = repo.Get.Where(p => p.Parent == m).Select(p => p.hid).Max();
                    SqlHierarchyId lastChHid = SqlHierarchyId.Null;
                    
                    foreach (T r  in repo.Get)
                    {
                        SqlHierarchyId h = Conversions.Bytes2HierarchyId(r.hid);
                        if (h.GetAncestor(1) == maeHid)
                        {
                            if (lastChHid.IsNull || h > lastChHid)
                            {
                                lastChHid = h;
                            }
                        }

                    }

                    if (lastChHid == SqlHierarchyId.Null)
                    {
                        b = Conversions.HierarchyId2Bytes(maeHid.GetDescendant(SqlHierarchyId.Null, SqlHierarchyId.Null));
                    }
                    else
                    { 
                        b = Conversions.HierarchyId2Bytes(maeHid.GetDescendant(lastChHid, SqlHierarchyId.Null));
                    }
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
                T t = repo.Find(new object[] { long.Parse(pk) });
                SqlHierarchyId hid = Conversions.Bytes2HierarchyId(t.hid);
                bool hasDescendant = false;
                foreach (var r in repo.Get) 
                {
                    SqlHierarchyId h = Conversions.Bytes2HierarchyId(r.hid);
                    if (h.IsDescendantOf(hid) && (h != hid))
                    {
                        hasDescendant = true;
                        break;
                    }

                }

                if (hasDescendant)
                {
                    throw new Exception("Existem descendentes.");
                }
                db.Entry(t).State = EntityState.Deleted;
                db.SaveChanges();
            }
            catch (Exception ex)
            {

                throw new Exception("Problema na exclusão: " + ex.Message);
            }

        }

        public void DeleteTree(String pk)
        // remove toda uma sub árvore
        {
            try
            {

                long tree = long.Parse(pk);
                T t = repo.Find(new object[] { tree });
                SqlHierarchyId hid = Conversions.Bytes2HierarchyId(t.hid);
                Int16 maxNiv = 0;

                foreach (var r in repo.Get)
                {
                    SqlHierarchyId h = Conversions.Bytes2HierarchyId(r.hid);
                    if (h.IsDescendantOf(hid))
                    {
                        if(h.GetLevel() > maxNiv) { maxNiv = (Int16)h.GetLevel(); }
                    }
                }


                for (int i = maxNiv; i > 0; i--)
                {
                    foreach (T r in repo.Get)
                    {
                        SqlHierarchyId h = Conversions.Bytes2HierarchyId(r.hid);
                        if (h.IsDescendantOf(hid)) { db.Entry(r).State = EntityState.Deleted; }
                    }
                }

                db.SaveChanges();

            }
            catch (Exception ex)
            { throw new Exception("Erro em exclusão de sub-árvore: " + ex.Message); }
        }

        public void Promote(String pk)
        {
            // Transfere nó para nível imediatamente inferior (número do nível) 
            try
            {
                byte[] b = GetHid(pk);
                SqlHierarchyId h = Conversions.Bytes2HierarchyId(b);
                if (h.GetLevel() < 2)
                { throw new Exception("Não é possível a promoção de nível 1 ou 0."); }

                long novamae = 0;
                byte[] grandP = Conversions.HierarchyId2Bytes(h.GetAncestor(2));
                foreach (var t in repo.Get)
                {
                    byte[] bh = t.hid;
                    if (bh.SequenceEqual(grandP))
                    {
                        novamae = (long)t.GetType().GetProperty(PKName).GetValue(t, null);
                        break;
                    }
                }
                MovSubTree(pk, novamae.ToString());
            }
            catch (Exception ex)
            { throw new Exception("Erro na promoção: " + ex.Message); }
        }

        public void Demote(String pk)
        // Transfere nó para nível imediatamente superior (número do nível) 
        // (como filho do irmão anterior)
        {
            long novaMae = 0;
            try
            {
                // Irmão anterior (mais próximo à mãe)
                byte[] b = GetHid(pk);
                SqlHierarchyId h = Conversions.Bytes2HierarchyId(b);
                foreach (var t in repo.Get.OrderByDescending(p => p.hid))
                {
                    SqlHierarchyId hid = Conversions.Bytes2HierarchyId(t.hid);
                    if (Conversions.Bytes2HierarchyId(t.hid) < h && hid.GetAncestor(1) == h.GetAncestor(1))
                    {
                        novaMae = (long)t.GetType().GetProperty(PKName).GetValue(t, null);
                        break;
                    }
                }
                if (novaMae == 0)
                {
                    throw new Exception("");
                }
                MovSubTree(pk, novaMae.ToString());
            }
            catch (Exception ex)
            { throw new Exception("Não é possível o rebaixamento de nível: não existe irmão mais próximo à mãe." + ex.Message); }
        }

        public void Up(String pk)
        // Troca nó de posição com o irmão anterior ficando mais próximo à mãe  
        {
            long irmaoAnterior = 0;
            try
            {
                // Irmão anterior (mais próximo à mãe)
                byte[] b = GetHid(pk);
                SqlHierarchyId h = Conversions.Bytes2HierarchyId(b);
                foreach (var t in repo.Get.OrderByDescending(p => p.hid))
                {
                    SqlHierarchyId hid = Conversions.Bytes2HierarchyId(t.hid);
                    if (Conversions.Bytes2HierarchyId(t.hid) < h && hid.GetAncestor(1) == h.GetAncestor(1))
                    {
                        irmaoAnterior = (long)t.GetType().GetProperty(PKName).GetValue(t, null);
                        break;
                    }
                }
                if (irmaoAnterior > 0)
                {
                    TrocaPosicaoFilhos(pk, irmaoAnterior.ToString());
                }
            }
            catch (Exception)
            { }
        }

        public void Down(String pk)
        // Troca nó de posição com o irmão posterior ficando mais afastado da mãe  
        {
            long irmaoposterior = 0;
            try
            {
                // Irmão posterior (mais afastado da mãe)
                byte[] b = GetHid(pk);
                SqlHierarchyId h = Conversions.Bytes2HierarchyId(b);
                foreach (var t in repo.Get.OrderBy(p => p.hid))
                {
                    SqlHierarchyId hid = Conversions.Bytes2HierarchyId(t.hid);
                    if (Conversions.Bytes2HierarchyId(t.hid) > h && hid.GetAncestor(1) == h.GetAncestor(1))
                    {
                        irmaoposterior = (long)t.GetType().GetProperty(PKName).GetValue(t, null);
                        break;
                    }
                }
                if (irmaoposterior > 0)
                {
                    TrocaPosicaoFilhos(pk, irmaoposterior.ToString());
                }
            }
            catch (Exception)
            { }
        }

        public void MovSubTree(String pk, String novaMae)
        {
            try
            {

                long tree = long.Parse(pk);
                long novaM = long.Parse(novaMae);

                T t = repo.Find(new object[] { tree });
                byte[] hidAsByte = t.hid;
                SqlHierarchyId hid = Conversions.Bytes2HierarchyId(t.hid);
                byte[] novoHidAsByte = GetNextSonHid(novaMae); // novo hid da subtree
                SqlHierarchyId novoHid = Conversions.Bytes2HierarchyId(novoHidAsByte);
                t.hid = novoHidAsByte; // novo hid do nó da subtree
                db.Entry(t).State = EntityState.Modified;

                t = repo.Find(new object[] { novaM });
                SqlHierarchyId novaMaehid = Conversions.Bytes2HierarchyId(t.hid);

                foreach (T r in repo.Get) // alterar os hids das descendentes
                {
                    SqlHierarchyId h = Conversions.Bytes2HierarchyId(r.hid);
                    if (h.IsDescendantOf(hid) && hidAsByte != r.hid )
                    {
                        r.hid = Conversions.HierarchyId2Bytes(h.GetReparentedValue(hid, novoHid));
                        db.Entry(r).State = EntityState.Modified;
                    }
                }
                db.SaveChanges();
            }
            catch (Exception ex)
            { throw new Exception("Erro ao mover nó: " + ex.Message); }
        }

        public void TrocaPosicaoFilhos(String Filho1, String Filho2)
        {

            try
            {
                // Trocar posições
                T t = repo.Find(new object[] { long.Parse(Filho1) });
                byte[] hid1B = t.hid;
                T u = repo.Find(new object[] { long.Parse(Filho2) });
                byte[] hid2B = u.hid;
                t.hid = hid2B;
                u.hid = hid1B;
                db.Entry(t).State = EntityState.Modified;
                db.Entry(u).State = EntityState.Modified;
                SqlHierarchyId hid1 = Conversions.Bytes2HierarchyId(hid1B);
                SqlHierarchyId hid2 = Conversions.Bytes2HierarchyId(hid2B);

                // reorganiza descendentes
                foreach (T r in repo.Get)
                {
                    if (r.hid != hid1B && r.hid != hid2B)
                    {
                        if (Conversions.Bytes2HierarchyId(r.hid).IsDescendantOf(hid1))
                        {
                            r.hid = Conversions.HierarchyId2Bytes(Conversions.Bytes2HierarchyId(r.hid).GetReparentedValue(hid1, hid2));
                            db.Entry(r).State = EntityState.Modified;
                        }
                        else
                        if (Conversions.Bytes2HierarchyId(r.hid).IsDescendantOf(hid2))
                        {
                            r.hid = Conversions.HierarchyId2Bytes(Conversions.Bytes2HierarchyId(r.hid).GetReparentedValue(hid2, hid1));
                            db.Entry(r).State = EntityState.Modified;
                        }
                    }
                }
                db.SaveChanges();
            }
            catch (Exception ex)
            { throw new Exception("Erro ao trocar posição filhos: " + ex.Message); }
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
