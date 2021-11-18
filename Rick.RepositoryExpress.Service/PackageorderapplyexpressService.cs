using Microsoft.EntityFrameworkCore;
using Rick.RepositoryExpress.DataBase.Models;
using Rick.RepositoryExpress.IService;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Rick.RepositoryExpress.Service
{
    public class PackageorderapplyexpressService : BaseService, IPackageorderapplyexpressService
    {
        public PackageorderapplyexpressService(RickDBConext dbContext) : base(dbContext)
        {

        }

    }
}
