﻿using Trackings.Domain.Pagination;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Trackings.Services.Interfaces
{
    public interface IPaginationService<TSource> where TSource : class
    {
        Task<List<TSource>> ApplyPaginationAsync(IQueryable<TSource> data, PageRequest request);

        int TotalRecords { get; }
    }
}