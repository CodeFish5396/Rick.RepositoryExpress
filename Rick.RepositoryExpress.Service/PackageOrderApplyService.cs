using Microsoft.EntityFrameworkCore;
using Rick.RepositoryExpress.DataBase.Models;
using Rick.RepositoryExpress.IService;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Rick.RepositoryExpress.Service
{
    public class PackageOrderApplyService : BaseService, IPackageOrderApplyService
    {
        public PackageOrderApplyService(RickDBConext dbContext) : base(dbContext)
        {

        }

    }
}
