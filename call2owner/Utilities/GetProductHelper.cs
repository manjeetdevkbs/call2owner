using Microsoft.AspNetCore.Http;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utilities
{
    public class GetProductHelper
    {
        private readonly RestClient _client;
        public GetProductHelper(RestClient client)
        {
            _client = client;
        }

        //public async Task<> GetAllProductsForInsurer(HttpContext httpContext)
        //{

        //}
    }
}
