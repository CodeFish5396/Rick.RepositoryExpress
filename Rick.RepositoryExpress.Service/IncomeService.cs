using Microsoft.EntityFrameworkCore;
using Rick.RepositoryExpress.DataBase.Models;
using Rick.RepositoryExpress.IService;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Rick.RepositoryExpress.Service
{
    public class IncomeService : BaseService, IIncomeService
    {
        public IncomeService(RickDBConext dbContext) : base(dbContext)
        {

        }

    }
}
