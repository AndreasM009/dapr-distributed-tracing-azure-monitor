using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace serviceB.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ServiceBController : ControllerBase
    {

        [HttpGet]
        public async Task<string> Get()
        {
            var random = new Random();
            var sec = random.Next(500, 4000);

            await Task.Delay(sec);

            return "Hello World, ServiceB";
        }
    }
}
