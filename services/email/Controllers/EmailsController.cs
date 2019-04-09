using System.Collections.Generic;
using System.Threading.Tasks;
using Microservices.Services.Core.Entities;
using Microservices.Services.Core.Interface.Services;
using Microsoft.AspNetCore.Mvc;

namespace email.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailsController : ControllerBase
    {
        private readonly IEmailService _emailService;
        
        public EmailsController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        // GET api/values
        [HttpGet]
        public async Task<IEnumerable<Email>> Get()
        {
            return await _emailService.GetAllAsync();
        }
    }
}
