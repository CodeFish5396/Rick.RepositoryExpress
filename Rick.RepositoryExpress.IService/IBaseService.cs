using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Rick.RepositoryExpress.IService
{
    public interface IBaseService
    {
        public int Add<T>(T t) where T : class;
        public int Delete<T>(long id) where T : class;
        public T Find<T>(long id) where T : class;
        public T Find<T>(Expression<Func<T, bool>> expression) where T : class;
        public IList<T> Query<T>() where T : class;
        public IList<T> Query<T>(Expression<Func<T, bool>> expression) where T : class;
        public int Update<T>(T t) where T : class;
        public void BeginTranstraction();

        public void Commit();
        public Task<int> AddAsync<T>(T t) where T : class;
        public Task<int> DeleteAsync<T>(long id) where T : class;
        public Task<T> FindAsync<T>(long id) where T : class;
        public Task<T> FindAsync<T>(Expression<Func<T, bool>> expression) where T : class;
        public Task<IList<T>> QueryAsync<T>(Expression<Func<T, bool>> expression) where T : class;
        public Task<IList<T>> QueryAsync<T>() where T : class;
        public Task<int> UpdateAsync<T>(T t) where T : class;
        public Task CommitAsync();
        public Task BeginTransactionAsync();

    }
}
