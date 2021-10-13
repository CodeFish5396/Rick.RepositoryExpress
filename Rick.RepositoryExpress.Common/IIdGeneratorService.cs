using System;
using System.Collections.Generic;
using System.Text;

namespace Rick.RepositoryExpress.Common
{
    public interface IIdGeneratorService
    {
        public long NextId();

    }
}
