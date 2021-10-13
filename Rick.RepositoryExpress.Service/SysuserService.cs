using Microsoft.EntityFrameworkCore;
using Rick.RepositoryExpress.DataBase.Models;
using Rick.RepositoryExpress.IService;
using System;
using System.Collections.Generic;
using System.Text;

namespace Rick.RepositoryExpress.Service
{
    public class SysuserService : BaseService, ISysuserService
    {
        public SysuserService(RickDBConext dbContext) : base(dbContext)
        {

        }
    }
}
