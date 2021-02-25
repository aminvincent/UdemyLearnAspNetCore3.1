using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpiceWeb.Mvc.Core.Data
{
    public interface IDbInitializer
    {
        void Initialize();
    }
}
