using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using TaskManagerPro.Models;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace TaskManagerPro.Controllers
{
    public class UserController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<UserController> _logger;

        public UserController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, ILogger<UserController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        // GET: User/Index
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userRolesViewModel = new List<UserRoleViewModel>();

            foreach (var user in users)
            {
                var userRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault();  // Single role
                var thisViewModel = new UserRoleViewModel
                {
                    UserId = user.Id,
                    UserName = user.UserName,
                    Role = userRole ?? "No role assigned",  // Handle case where no role is assigned
                };
                userRolesViewModel.Add(thisViewModel);
            }

            return View(userRolesViewModel);
        }

        // GET: User/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: User/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state when creating a new user.");
                return View(model);
            }

            var user = new IdentityUser { UserName = model.Email, Email = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                _logger.LogInformation($"User {user.UserName} created successfully.");
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
                _logger.LogWarning($"Error creating user {user.UserName}: {error.Description}");
            }

            return View(model);
        }

        // GET: User/ManageRoles/{userId}
        public async Task<IActionResult> ManageRoles(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("ManageRoles called with null or empty userId.");
                return BadRequest("User ID cannot be null or empty.");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning($"User with ID '{userId}' not found.");
                return NotFound($"User with ID '{userId}' not found.");
            }

            var currentRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault();  // Single role

            var model = new UserRoleViewModel
            {
                UserId = user.Id,
                UserName = user.UserName,
                Role = currentRole,  // Single role
                AvailableRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync()
            };

            return View(model);
        }

        // POST: User/ManageRoles
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageRoles(UserRoleViewModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Role))
            {
                _logger.LogWarning("ManageRoles POST called with null model or Role.");
                return BadRequest("Invalid role management data.");
            }

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                _logger.LogWarning($"User with ID '{model.UserId}' not found.");
                return NotFound($"User with ID '{model.UserId}' not found.");
            }

            // Remove all current roles
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Count > 0)
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    _logger.LogError($"Failed to remove existing roles for user {user.UserName}.");
                    ModelState.AddModelError("", "Failed to update roles.");
                    return View(model);
                }
            }

            // Add the user to the selected role
            var addResult = await _userManager.AddToRoleAsync(user, model.Role);
            if (!addResult.Succeeded)
            {
                _logger.LogError($"Failed to assign role {model.Role} to user {user.UserName}.");
                ModelState.AddModelError("", "Failed to update roles.");
                return View(model);
            }

            _logger.LogInformation($"Role {model.Role} assigned to user {user.UserName}.");
            return RedirectToAction(nameof(Index));  // Redirect to the user index page
        }
    }
}
