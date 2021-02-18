﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace aspnet_uppgift_1.Areas.Identity.Pages.Account.Manage
{
    public class Disable2faModel : PageModel
    {
        public IActionResult OnGet()
            => Redirect("/Identity/Account/Manage");
    }
}