using System;
using System.Data;
using System.Linq;
using System.Linq.Dynamic;
using System.Net;
using System.Web.Mvc;
using PagedList;
using System.Collections;
using Hierarchy.SqlServer;
using Hierarchy.Common;
using hidMy.Models;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Core.Objects;

namespace hidMy.Controllers
{
    public class GLaccountsController : Controller
    {
        private GLaccountsModel db = new GLaccountsModel();

        // GET: GLaccounts
        public ActionResult Index(int? page, string ipp)
        {

            if (TempData["ExclError"] != null)
            {
                ModelState.AddModelError("", TempData["ExclError"].ToString());
            }
            ArrayList recolhidas = (ArrayList)Session["ContasCollapsedList"];
            hidServices<GLaccounts> hs = new hidServices<GLaccounts>(db);
            string wc = hs.WhereClause(recolhidas);

            var result = db.Database.SqlQuery<GLaccounts>("select * from GLaccounts where " + wc + " order by hid").ToList();
            //var result = db.GLaccounts.Where(wc).OrderBy(c => c.hid).ToList();   // dynamic where for sql server using isDescendant at server
            //var result = db.GLaccounts.OrderBy(c => c.hid).ToList().Where(c => c.IsNotCollapsed(hs, recolhidas));
            int pageNumber = (page ?? 1);
            ipp = ipp ?? (string)Session["DefaultItemsPerPage"];
            return View(result.ToPagedList(pageNumber, int.Parse(ipp)));

        }

        // GET: GLaccounts/Details/5
        public ActionResult Details(long? id, int? page, string ipp)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            GLaccounts contas = db.GLaccounts.Find(id);
            if (contas == null)
            {
                return HttpNotFound();
            }
            ArrayList recolhidas = (ArrayList)Session["ContasCollapsedList"];
            hidServices<GLaccounts> hs = new hidServices<GLaccounts>(db);
            string wc = hs.WhereClause(recolhidas);   // Sql server only
            var result = db.Database.SqlQuery<GLaccounts>("select * from GLaccounts where " + wc + " order by hid").ToList(); // Sql server only
            //var result = db.GLaccounts.OrderBy(c => c.hid).ToList().Where(c => c.IsNotCollapsed(hs, recolhidas));
            int pageNumber = page ?? 1;
            ipp = ipp ?? (string)Session["DefaultItemsPerPage"];
            return View(contas);
        }


        [HttpPost]
        public ActionResult HCommand(GLaccounts conta, int? page, string ipp, string refresh, string promote, string up, string down, string demote)
        {

            hidServices<GLaccounts> hs = new hidServices<GLaccounts>(db);
            if (string.IsNullOrEmpty(refresh))
            {
                var pk = Session["itemSelecionado"] as String;
                try
                {

                    if (!string.IsNullOrEmpty(promote)) { hs.Command(pk, hidServices<GLaccounts>.Commands.Promote); }
                    if (!string.IsNullOrEmpty(up)) { hs.Command(pk, hidServices<GLaccounts>.Commands.Up); }
                    if (!string.IsNullOrEmpty(down)) { hs.Command(pk, hidServices<GLaccounts>.Commands.Down); }
                    if (!string.IsNullOrEmpty(demote)) { hs.Command(pk, hidServices<GLaccounts>.Commands.Demote); }
                    
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }
            ArrayList recolhidas = (ArrayList)Session["ContasCollapsedList"];
            db = new GLaccountsModel();  // to refresh entity after sql command
            string wc = hs.WhereClause(recolhidas);
            var result = db.Database.SqlQuery<GLaccounts>("select * from GLaccounts where " + wc + " order by hid").ToList(); // Sql server only
            //var result = db.GLaccounts.OrderBy(c => c.hid).ToList().Where(c => c.IsNotCollapsed(hs, recolhidas));
            int pageNumber = page ?? 1;
            ipp = ipp ?? (string)Session["DefaultItemsPerPage"];
            return View("Index", result.ToPagedList(pageNumber, int.Parse(ipp)));
        }

        public ActionResult Expand(long id, int? pagenumber, string ipp)
        {
            AtualizaListaRecolhidos(id.ToString(), false);
            ArrayList recolhidas = (ArrayList)Session["ContasCollapsedList"];
            hidServices<GLaccounts> hs = new hidServices<GLaccounts>(db);
            string wc = hs.WhereClause(recolhidas);
            var result = db.Database.SqlQuery<GLaccounts>("select * from GLaccounts where " + wc + " order by hid").ToList(); // Sql server only
            //var result = db.GLaccounts.OrderBy(c => c.hid).ToList().Where(c => c.IsNotCollapsed(hs, recolhidas));
            int pageNumber = (pagenumber ?? 1);
            ipp = ipp ?? (string)Session["DefaultItemsPerPage"];
            return View("Index", result.ToPagedList(pageNumber, int.Parse(ipp)));
        }
        public ActionResult Collapse(long id, int? pagenumber, string ipp)
        {
            AtualizaListaRecolhidos(id.ToString(), true);
            ArrayList recolhidas = (ArrayList)Session["ContasCollapsedList"];
            hidServices<GLaccounts> hs = new hidServices<GLaccounts>(db);
            string wc = hs.WhereClause(recolhidas);
            var result = db.Database.SqlQuery<GLaccounts>("select * from GLaccounts where " + wc + " order by hid").ToList(); // Sql server only
            //var result = db.GLaccounts.OrderBy(c => c.hid).ToList().Where(c => c.IsNotCollapsed(hs, recolhidas));
            int pageNumber = (pagenumber ?? 1);
            ipp = ipp ?? (string)Session["DefaultItemsPerPage"];
            return View("Index", result.ToPagedList(pageNumber, int.Parse(ipp)));
        }

        [HttpPost]
        public ActionResult ItemsPerPage(int? page, string ipp)
        {
            string url = Url.Content("~/GLaccounts/Index/");
            return Json(url + "?page=" + page.ToString() + "&ipp=" + ipp);
        }

        [HttpPost]
        public ActionResult LinhaSelecionada(String id, int? page, string ipp)
        {
            GLaccounts contas = db.GLaccounts.Find(long.Parse(id));
            if (contas == null)
            {
                return HttpNotFound();
            }

            if ((String)Session["itemSelecionado"] == contas.Account.ToString())
            { Session["itemSelecionado"] = ""; }
            else
            { Session["itemSelecionado"] = contas.Account.ToString(); }

            string url = Url.Content("~/GLaccounts/Index/");
            return Json(url + "?page=" + page.ToString() + "&ipp=" + ipp);
        }

        // GET: GLaccounts/Create
        public ActionResult Create(int? page, string ipp)
        {
            var mae = Session["itemSelecionado"] as String;
            hidServices<GLaccounts> hs = new hidServices<GLaccounts>(db);
            if (!hs.RootExists())
            {
                ViewBag.mensagem = "As root account";
            }
            else
            {
                try
                {
                    GLaccounts r = db.GLaccounts.Find(long.Parse(mae));
                    ViewBag.mensagem = "As last child of account " + mae + " (" + r.Name + ")";
                }
                catch (Exception)
                {

                    mae = "";
                }
                if (string.IsNullOrEmpty(mae))
                {
                    ModelState.AddModelError("", "Root exists. Please select parent account.");
                    ArrayList recolhidas = (ArrayList)Session["ContasCollapsedList"];
                    string wc = hs.WhereClause(recolhidas);
                    var result = db.Database.SqlQuery<GLaccounts>("select * from GLaccounts where " + wc + " order by hid").ToList(); // Sql server only
                    //var result = db.GLaccounts.OrderBy(c => c.hid).ToList().Where(c => c.IsNotCollapsed(hs, recolhidas));
                    int pageNumber = (page ?? 1);
                    ipp = ipp ?? (string)Session["DefaultItemsPerPage"];
                    return View("Index", result.ToPagedList(pageNumber, int.Parse(ipp)));
                }
            }
            return View();
        }

        // POST: GLaccounts/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Account,Name,hid")] GLaccounts gLaccounts, int? page, string ipp)
        {

            int pageNumber = (page ?? 1);
            ipp = ipp ?? (string)Session["DefaultItemsPerPage"];
            if (ModelState.IsValid)
            {
                var mae = Session["itemSelecionado"] as String;
                hidServices<GLaccounts> hs = new hidServices<GLaccounts>(db);

                gLaccounts.hid = hs.GetNextSonHid(mae);
                db.GLaccounts.Add(gLaccounts);
                db.SaveChanges();
                //gLaccounts.InsertNewConta(gLaccounts.Name, mae);
                ArrayList recolhidas = (ArrayList)Session["ContasCollapsedList"];
                string wc = hs.WhereClause(recolhidas);
                var result = db.Database.SqlQuery<GLaccounts>("select * from GLaccounts where " + wc + " order by hid").ToList(); // Sql server only
                //var result = db.GLaccounts.OrderBy(c => c.hid).ToList().Where(c => c.IsNotCollapsed(hs, recolhidas));
                return View("Index", result.ToPagedList(pageNumber, int.Parse(ipp)));
            }
            return View(gLaccounts);
        }

        // GET: GLaccounts/Edit/5
        public ActionResult Edit(long? id, int? page, string ipp)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            GLaccounts contas = db.GLaccounts.Find(id);
            TempData["hid"] = contas.hid;
            if (contas == null)
            {
                return HttpNotFound();
            }
            return View(contas);
        }

        // POST: GLaccounts/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Account,Name,hid")] GLaccounts gLaccounts, int? page, string ipp)
        {
            if (ModelState.IsValid)
            {
                gLaccounts.hid = (byte[])TempData["hid"];
                db.Entry(gLaccounts).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                int pageNumber = (page ?? 1);
                ipp = ipp ?? (string)Session["DefaultItemsPerPage"];
                ArrayList recolhidas = (ArrayList)Session["ContasCollapsedList"];
                hidServices<GLaccounts> hs = new hidServices<GLaccounts>(db);
                string wc = hs.WhereClause(recolhidas);
                var result = db.Database.SqlQuery<GLaccounts>("select * from GLaccounts where " + wc + " order by hid").ToList(); // Sql server only
                //var result = db.GLaccounts.OrderBy(c => c.hid).ToList().Where(c => c.IsNotCollapsed(hs, recolhidas));
                return View("Index", result.ToPagedList(pageNumber, int.Parse(ipp)));
            }
            return View(gLaccounts);
        }

        // GET: GLaccounts/Delete/5
        public ActionResult Delete(long? id, int? page, string ipp)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            GLaccounts gLaccounts = db.GLaccounts.Find(id);
            if (gLaccounts == null)
            {
                return HttpNotFound();
            }
            return View(gLaccounts);
        }

        // POST: GLaccounts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(long id, int? page, string ipp)
        {
            GLaccounts gLaccounts = db.GLaccounts.Find(id);
            hidServices<GLaccounts> hs = new hidServices<GLaccounts>(db);
            hidServices<GLaccounts> hh = new hidServices<GLaccounts>(db);
            try
            {
                hh.delete(id.ToString());
            }
            catch (Exception ex)
            {
                TempData["ExclError"] = "Error deleting: " + ex.Message;
                //ModelState.AddModelError("", "Erro na remoção: " + ex.Message);
                return RedirectToAction("Index", new { page = (Request.QueryString["page"] ?? (string)Session["DefaultItemsPerPage"]), ipp = (Request.QueryString["ipp"] ?? (string)Session["DefaultItemsPerPage"]) });
            }
            int pageNumber = (page ?? 1);
            ipp = ipp ?? (string)Session["DefaultItemsPerPage"];
            ArrayList recolhidas = (ArrayList)Session["ContasCollapsedList"];
            string wc = hs.WhereClause(recolhidas);
            var result = db.Database.SqlQuery<GLaccounts>("select * from GLaccounts where " + wc + " order by hid").ToList(); // Sql server only
            //var result = db.GLaccounts.OrderBy(c => c.hid).ToList().Where(c => c.IsNotCollapsed(hs, recolhidas));
            return View("Index", result.ToPagedList(pageNumber, int.Parse(ipp)));
        }

        public ActionResult Documentation()
        {
            ViewBag.Message = "Hierachyid with Entity Framework and MySql.";

            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private void AtualizaListaRecolhidos(string pk, Boolean recolher)
        {
            ArrayList a = (ArrayList)Session["ContasCollapsedList"];
            if (recolher)
            {
                if (!a.Contains(pk))
                {
                    a.Add(pk);
                }
            }
            else
            {
                a.Remove(pk);
            }
            Session["ContasCollapsedList"] = a;
        }

    }
}
