using Microsoft.EntityFrameworkCore;
using Rick.RepositoryExpress.DataBase.Models;
using Rick.RepositoryExpress.IService;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Rick.RepositoryExpress.Service
{
    public class ChannelService : BaseService, IChannelService
    {
        public ChannelService(RickDBConext dbContext) : base(dbContext)
        {

        }

        public void Info()
        {
            var result = from channel in rickDBConext.Channels
                         join channeldetail in rickDBConext.Channeldetails
                         on channel.Id equals channeldetail.Channelid
                         join nation in rickDBConext.Nations
                         on channeldetail.Nationid equals nation.Id
                         join agent in rickDBConext.Agents
                         on channeldetail.Agentid equals agent.Id
                         where channel.Status == 1
                         select channel;
        }
    }
}
