using Microsoft.EntityFrameworkCore;
using Rick.RepositoryExpress.DataBase.Models;
using Rick.RepositoryExpress.IService;
using System;
using System.Collections.Generic;
using System.Text;

namespace Rick.RepositoryExpress.Service
{
    public class SyssettingService : BaseService, ISyssettingService
    {
        public SyssettingService(RickDBConext dbContext) : base(dbContext)
        {

        }
    }
}
