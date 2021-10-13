using Microsoft.EntityFrameworkCore;
using Rick.RepositoryExpress.DataBase.Models;
using Rick.RepositoryExpress.IService;
using System;
using System.Collections.Generic;
using System.Text;

namespace Rick.RepositoryExpress.Service
{
    public class RoleService : BaseService, IRoleService
    {
        public RoleService(RickDBConext dbContext) : base(dbContext)
        {

        }
    }
}
