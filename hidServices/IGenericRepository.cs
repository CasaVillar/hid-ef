using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Hierarchy.Common
{
    interface IGenericRepository<T> where T : class
    {
        IQueryable<T> Get { get; }
        IQueryable<T> GetIncluding(params Expression<Func<T, object>>[] includeProperties);
        T Find(object[] keyValues);
        void Add(T entity);
        void Update(T entity);
        void AddOrUpdate(T entity);
        void Delete(object[] keyValues);
    }
}
