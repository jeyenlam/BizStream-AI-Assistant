using Microsoft.AspNetCore.Mvc;

namespace BizStreamWebsiteClone.Controllers
{
    public class MyController : Controller
    {
        private readonly string _IframeSrc;
        public MyController(IConfiguration config)
        {
            _IframeSrc = config["IframeSrc"];
        }
    }
}