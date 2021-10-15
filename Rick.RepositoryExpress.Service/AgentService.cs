using Microsoft.EntityFrameworkCore;
using Rick.RepositoryExpress.DataBase.Models;
using Rick.RepositoryExpress.IService;
using System;
using System.Collections.Generic;
using System.Text;

namespace Rick.RepositoryExpress.Service
{
    public class AgentService : BaseService, IAgentService
    {
        public AgentService(RickDBConext dbContext) : base(dbContext)
        {

        }
    }
}
