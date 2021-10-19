using Rick.RepositoryExpress.DataBase.Models;
using Rick.RepositoryExpress.DataBase.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Rick.RepositoryExpress.IService
{
    public interface IPackageService : IBaseService
    {
        Task<IList<PackageInPreDetailView>> GetAppusers(string expressNumber);
        Task<(IList<PackageInView>, int)> GetList(string expressNumber, DateTime startTime, DateTime endTime, int index, int pageSize);

    }
}
