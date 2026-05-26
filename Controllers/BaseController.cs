using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BuildWise.Controllers
{
    public abstract class BaseController : Controller
    {
        protected int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue("UserId");
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        protected int GetUserId()
        {
            return GetCurrentUserId();
        }

        protected int? GetSelectedProjectId()
        {
            return HttpContext.Session.GetInt32("SelectedProjectId");
        }

        protected void RemoveModelStateEntries(params string[] keys)
        {
            foreach (var key in keys)
            {
                ModelState.Remove(key);
            }
        }
    }
}
