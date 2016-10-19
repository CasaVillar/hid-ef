namespace hidMy.Models
{
    using System;
    using System.Data.Entity;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;

    public partial class GLaccountsModel : DbContext
    {
        public GLaccountsModel()
            : base("name=GLaccountsModel")
        {
        }

        public virtual DbSet<GLaccounts> GLaccounts { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GLaccounts>()
                .Property(e => e.Name)
                .IsFixedLength();
            //modelBuilder.Entity<GLaccounts>()
            //    .ToTable("Contas");
        }
    }
}
