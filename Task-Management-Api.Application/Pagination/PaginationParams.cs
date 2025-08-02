using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Task_Management_Api.Application.Pagination
{
    public class PaginationParams
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 3;

        public const int MaxPageSize = 50;

        public int ValidatedPageSize => (PageSize > MaxPageSize) ? MaxPageSize : PageSize;
    }
}
