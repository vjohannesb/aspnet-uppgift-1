﻿using aspnet_uppgift_1.Data;
using aspnet_uppgift_1.Models;
using aspnet_uppgift_1.Services.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace aspnet_uppgift_1.Controllers
{
    [Authorize(Policy = "Admins")]
    public class UsersController : Controller
    {
        private readonly IIdentityService _identityService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly Random rnd = new Random();

        public UsersController(
            IIdentityService identityService,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager, 
            ApplicationDbContext context)
        {
            _identityService = identityService;
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        public async Task<UserViewModel> GetUserViewModelAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                return null;

            var roles = await _userManager.GetRolesAsync(user);

            var userViewModel = (UserViewModel)user;
            userViewModel.Role = roles.FirstOrDefault();
            return userViewModel;
        }

        public ApplicationUser CreateApplicationUser(UserViewModel userViewModel)
            => new ApplicationUser()
            {
                FirstName = userViewModel.FirstName,
                LastName = userViewModel.LastName,
                Email = userViewModel.Email,
                ImageURI = rnd.Next(1, 7) + ".jpg",
                UserName = userViewModel.Email,
                Role = userViewModel.Role
            };

        // GET: UsersController
        public async Task<ActionResult> Index(string filter)
        {
            var userViewModels = await _identityService.GetAllUserViewModelsAsync();

            switch (filter?.ToLower() ?? string.Empty)
            {
                case "teachers":
                    userViewModels = userViewModels.Where(user => user.Role == "Teacher");
                    break;
                case "students":
                    userViewModels = userViewModels.Where(user => user.Role == "Student");
                    break;
                default:
                    break;
            }

            return View(userViewModels);
        }
        // GET: UsersController/Details/5
        public async Task<ActionResult> Details(string id)
        {
            var userViewModel = await GetUserViewModelAsync(id);

            if (userViewModel == null)
                return RedirectToAction("Index");

            if (userViewModel.Role == "Student")
            {
                var classId = _context.SchoolClassStudents
                    .FirstOrDefault(scs => scs.StudentId == id)?
                    .SchoolClassId;

                userViewModel.SchoolClasses = new List<SchoolClass> { await _context.SchoolClasses.FindAsync(classId) };
            }
            else if (userViewModel.Role == "Teacher")
                userViewModel.SchoolClasses = _context.SchoolClasses.Where(sc => sc.TeacherId == id);
            
            return View(userViewModel);
        }

        // GET: UsersController/Create
        public ActionResult Create()
        {
            ViewBag.Roles = _roleManager.Roles as IEnumerable<IdentityRole>;
            return View();
        }

        // POST: UsersController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(UserViewModel userViewModel)
        {
            if (ModelState.IsValid)
            {
                if (_identityService.GetAllUsers().Any(u => u.Email == userViewModel.Email))
                    ModelState.AddModelError("Email", "This email is already registered.");
                else
                {
                    var user = CreateApplicationUser(userViewModel);
                    var role = userViewModel.Role;

                    await _identityService.CreateNewUserAsync(user, "BytMig123!");
                    await _identityService.AddUserToRoleAsync(user, role);

                    return RedirectToAction("Index");
                }
            }

            ViewBag.Roles = _identityService.GetAllRoles() as IEnumerable<IdentityRole>;
            return View();
        }

        // GET: UsersController/Edit/5
        public async Task<ActionResult> Edit(string id)
        {
            var userViewModel = await GetUserViewModelAsync(id);
            if (userViewModel == null)
                return RedirectToAction("Index");

            ViewBag.Roles = _roleManager.Roles as IEnumerable<IdentityRole>;
            return View(userViewModel);
        }

        // POST: UsersController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(UserViewModel userViewModel)
        {
            if (ModelState.IsValid)
            {
                if (_userManager.Users.Any(u => u.Email == userViewModel.Email && u.Id != userViewModel.Id))
                    ModelState.AddModelError("Email", "This email is already registered.");

                else
                {
                    var user = await _userManager.FindByIdAsync(userViewModel.Id);
                    var oldRoles = await _userManager.GetRolesAsync(user);
                    var oldRole = oldRoles.FirstOrDefault();

                    user.FirstName = userViewModel.FirstName;
                    user.LastName = userViewModel.LastName;
                    user.Email = userViewModel.Email;
                    user.UserName = userViewModel.Email;
                    //user.ImageURI = userViewModel.ImageURI;
                    var newRole = userViewModel.Role;

                    if (newRole != oldRole)
                    {
                        await _userManager.RemoveFromRoleAsync(user, oldRole);
                        await _userManager.AddToRoleAsync(user, newRole);
                    }

                    await _userManager.UpdateAsync(user);

                    return RedirectToAction("Index");
                }
            }

            ViewBag.Roles = _roleManager.Roles as IEnumerable<IdentityRole>;
            return View(userViewModel);
        }

        // GET: UsersController/Delete/5
        public async Task<ActionResult> Delete(string id)
        {
            var userViewModel = await GetUserViewModelAsync(id);
            if (userViewModel == null)
                return RedirectToAction("Index");

            return View(userViewModel);
        }

        // POST: UsersController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(string id, IFormCollection collection)
        {
            var result = await _identityService.DeleteUserAsync(id);
            if (result == null || !result.Succeeded)
                throw new InvalidOperationException($"Unexpected error occurred deleting user with ID '{id}'.");

            var schoolClassStudent = _context.SchoolClassStudents.FirstOrDefault(scs => scs.StudentId == id);
            if (schoolClassStudent != null)
            {
                _context.SchoolClassStudents.Remove(schoolClassStudent);
                await _context.SaveChangesAsync();
            }


            return RedirectToAction("Index");
        }
    }
}
