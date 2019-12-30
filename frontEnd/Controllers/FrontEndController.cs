using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace frontEnd.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FrontEndController : ControllerBase
    {
        [HttpGet("serviceA")]
        public async Task<string> GetServiceA()
        {
            var url = $"http://localhost:3500/v1.0/invoke/serviceaapp/method/servicea";
            var client = new HttpClient();
            return await client.GetStringAsync(url);
        }

        [HttpGet("serviceB")]
        public async Task<string> GetServiceB()
        {
            var url = $"http://localhost:3500/v1.0/invoke/servicebapp/method/serviceb";
            var client = new HttpClient();
            return await client.GetStringAsync(url);
        }

        [HttpGet("serviceC")]
        public async Task<string> GetServiceC()
        {
            var url = $"http://localhost:3500/v1.0/invoke/servicecapp/method/servicec";
            var client = new HttpClient();
            return await client.GetStringAsync(url);
        }
    }
}
