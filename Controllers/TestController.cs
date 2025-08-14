using Microsoft.AspNetCore.Mvc;

namespace FileViewer.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { message = "Test controller working", timestamp = DateTime.UtcNow });
        }
    }
}
