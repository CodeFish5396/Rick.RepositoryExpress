using Microsoft.EntityFrameworkCore;
using Rick.RepositoryExpress.DataBase.Models;
using Rick.RepositoryExpress.DataBase.ViewModels;
using Rick.RepositoryExpress.IService;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Linq;
using System.Threading.Tasks;

namespace Rick.RepositoryExpress.Service
{
    public class PackageService : BaseService, IPackageService
    {
        public PackageService(RickDBConext dbContext) : base(dbContext)
        {

        }
        public async Task<IList<Expressclaim>> GetExpressclaims(string expressNumber)
        {
            var result = from ec in rickDBConext.Expressclaims
                         join ei in rickDBConext.Expressinfos
                         on ec.Expressinfoid equals ei.Id
                         where ei.Expressnumber == expressNumber
                         && ei.Status == 1 && ec.Status == 1
                         select ec;
            return await result.ToListAsync();
        }

        public async Task<IList<PackageInPreDetailView>> GetAppusers(string expressNumber)
        {
            var result = from user in rickDBConext.Appusers
                         join ec in rickDBConext.Expressclaims
                         on user.Id equals ec.Appuser
                         join ei in rickDBConext.Expressinfos
                         on ec.Expressinfoid equals ei.Id
                         where ei.Expressnumber == expressNumber
                         && ei.Status == 1 && ec.Status == 1
                         select new PackageInPreDetailView()
                         {
                             UserId = user.Id,
                             UserCode = user.Usercode,
                             UserName = user.Name,
                             ExpressclaimId = ec.Id
                         };
            return await result.ToListAsync();
        }
        //public async Task<(IList<PackageInView>, int)> GetList(string expressNumber, DateTime startTime, DateTime endTime, int index = 0, int pageSize = 0)
        //{
        //    index = index <= 1 ? 1 : index;
        //    pageSize = pageSize <= 10 ? 10 : pageSize;
        //    var result = from package in rickDBConext.Packages.Where(t => t.Status == 1)
        //                 join exclaim in rickDBConext.Expressclaims.Where(t => t.Status == 2 || t.Status == 3)
        //                 on package.Id equals exclaim.Packageid
        //                 into exclaimtemp
        //                 from exclaimt in exclaimtemp.DefaultIfEmpty()
        //                 join user in rickDBConext.Appusers
        //                 on exclaimt.Appuser equals user.Id
        //                 into usertemp
        //                 from usert in usertemp.DefaultIfEmpty()
        //                 where (string.IsNullOrEmpty(expressNumber) || package.Expressnumber == expressNumber)
        //                  && (startTime == DateTime.MinValue || package.Addtime >= startTime)
        //                  && (endTime == DateTime.MinValue || package.Addtime <= endTime)
        //                 select new PackageInView()
        //                 {
        //                     Userid = usert == null ? 0 : usert.Id,
        //                     Username = usert == null ? string.Empty : usert.Name,
        //                     Packageid = package.Id,
        //                     Count = package.Count,
        //                     Addtime = package.Addtime,
        //                     Courierid = package.Courierid,
        //                     Expressnumber = package.Expressnumber,
        //                     Lastuser = package.Lastuser,
        //                     Lasttime = package.Lasttime
        //                 };
        //    int count = await result.CountAsync();
        //    var list = await result.OrderBy(t => t.Addtime).Skip(pageSize * (index - 1)).Take(pageSize).ToListAsync();

        //    return (list, count);
        //}


    }
}
