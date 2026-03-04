using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.BusinessModels
{
    public class PagedResult<T>
    {
        public List<T> Data { get; set; } = new();

        public int TotalCount { get; set;  }

    }
}
