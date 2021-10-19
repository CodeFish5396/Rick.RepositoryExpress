using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Rick.RepositoryExpress.DataBase.Models;
using Rick.RepositoryExpress.IService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Rick.RepositoryExpress.Service
{
    public class BaseService : IBaseService
    {
        protected RickDBConext rickDBConext { get; private set; }
        private IDbContextTransaction dbContextTransaction { get; set; }
        public BaseService(RickDBConext dbContext)
        {
            this.rickDBConext = dbContext;
        }

        public int Add<T>(T t) where T : class
        {
            rickDBConext.Set<T>().Add(t);
            return rickDBConext.SaveChanges();
        }
        public int Delete<T>(long id) where T : class
        {
            rickDBConext.Set<T>().Remove(rickDBConext.Set<T>().Find(id));
            return rickDBConext.SaveChanges();
        }
        public T Find<T>(long id) where T : class
        {
            return rickDBConext.Set<T>().Find(id);
        }
        public T Find<T>(Expression<Func<T, bool>> expression) where T : class
        {
            return rickDBConext.Set<T>().Where(expression).SingleOrDefault();
        }
        public IQueryable<T> Query<T>() where T : class
        {
            return rickDBConext.Set<T>();
        }

        public IQueryable<T> Query<T>(Expression<Func<T, bool>> expression) where T : class
        {
            return rickDBConext.Set<T>().Where(expression);
        }

        public int Count<T>(Expression<Func<T, bool>> expression) where T : class
        {
            return rickDBConext.Set<T>().Where(expression).Count();
        }

        public int Update<T>(T t) where T : class
        {
            rickDBConext.Set<T>().Attach(t);
            rickDBConext.Set<T>().Update(t);
            return rickDBConext.SaveChanges();
        }

        public void BeginTranstraction()
        {
            dbContextTransaction = rickDBConext.Database.BeginTransaction();
        }

        public void Commit()
        {
            dbContextTransaction.Commit();
        }
        public async Task<int> AddAsync<T>(T t) where T : class
        {
            rickDBConext.Set<T>().Add(t);
            return await rickDBConext.SaveChangesAsync();
        }
        public async Task<int> DeleteAsync<T>(long id) where T : class
        {
            rickDBConext.Set<T>().Remove(rickDBConext.Set<T>().Find(id));
            return await rickDBConext.SaveChangesAsync();
        }
        public async Task<T> FindAsync<T>(long id) where T : class
        {
            return await rickDBConext.Set<T>().FindAsync(id);
        }
        public async Task<T> FindAsync<T>(Expression<Func<T, bool>> expression) where T : class
        {
            return await rickDBConext.Set<T>().Where(expression).SingleOrDefaultAsync();
        }
        public async Task<IList<T>> QueryAsync<T>() where T : class
        {
            return await rickDBConext.Set<T>().ToListAsync();
        }

        public async Task<IList<T>> QueryAsync<T>(Expression<Func<T, bool>> expression) where T : class
        {
            return await rickDBConext.Set<T>().Where(expression).ToListAsync();
        }
        //public async Task<IList<T>> QueryAsync<T>(Expression<Func<T, bool>> expression, int index, int pageSize) where T : class
        //{
        //    return await rickDBConext.Set<T>().Where(expression).Skip(pageSize * (index-1)).Take(pageSize).ToListAsync();
        //}
        public async Task<int> CountAsync<T>(Expression<Func<T, bool>> expression) where T : class
        {
            return await rickDBConext.Set<T>().Where(expression).CountAsync();
        }

        public async Task<int> UpdateAsync<T>(T t) where T : class
        {
            rickDBConext.Set<T>().Attach(t);
            rickDBConext.Set<T>().Update(t);
            return await rickDBConext.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            dbContextTransaction = await rickDBConext.Database.BeginTransactionAsync();
        }

        public async Task CommitAsync()
        {
            await dbContextTransaction.CommitAsync();
        }
    }
}
