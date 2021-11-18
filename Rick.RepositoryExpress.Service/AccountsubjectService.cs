using Microsoft.EntityFrameworkCore;
using Rick.RepositoryExpress.DataBase.Models;
using Rick.RepositoryExpress.IService;
using System;
using System.Collections.Generic;
using System.Text;

namespace Rick.RepositoryExpress.Service
{
    public class AccountsubjectService : BaseService, IAccountsubjectService
    {
        public AccountsubjectService(RickDBConext dbContext) : base(dbContext)
        {

        }
    }
}
