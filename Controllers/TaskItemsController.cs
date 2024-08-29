using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TaskManagerPro.Data;
using TaskManagerPro.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace TaskManagerPro.Controllers
{
    public class TaskItemsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<TaskItemsController> _logger;

        public TaskItemsController(ApplicationDbContext context, UserManager<IdentityUser> userManager, ILogger<TaskItemsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: TaskItems
        public async Task<IActionResult> Index(string sortOrder)
        {
            try
            {
                // Populate users for the sidebar
                ViewBag.Users = await _userManager.Users.ToListAsync();

                // Setup sorting parameters
                ViewData["TitleSortParm"] = String.IsNullOrEmpty(sortOrder) ? "title_desc" : "";
                ViewData["DeadlineSortParm"] = sortOrder == "Deadline" ? "deadline_desc" : "Deadline";
                ViewData["ProgressSortParm"] = sortOrder == "Progress" ? "progress_desc" : "Progress";

                // Fetch tasks from the database
                var tasks = from t in _context.TaskItems select t;

                // Apply sorting based on the sortOrder parameter
                tasks = sortOrder switch
                {
                    "title_desc" => tasks.OrderByDescending(t => t.Title),
                    "Deadline" => tasks.OrderBy(t => t.Deadline),
                    "deadline_desc" => tasks.OrderByDescending(t => t.Deadline),
                    "Progress" => tasks.OrderBy(t => t.Progress),
                    "progress_desc" => tasks.OrderByDescending(t => t.Progress),
                    _ => tasks.OrderBy(t => t.Title),
                };

                // Return the sorted and tracked task items
                return View(await tasks.AsNoTracking().ToListAsync());
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occurred in Index: {ex.Message}");
                return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }

        // GET: TaskItems/UserTasks/{userId}
        public async Task<IActionResult> UserTasks(string userId, string sortOrder)
        {
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("UserTasks called with null or empty userId.");
                return NotFound("User ID is required");
            }

            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning($"User with ID '{userId}' not found.");
                    return NotFound("User not found");
                }

                // Populate users for the sidebar
                ViewBag.Users = await _userManager.Users.ToListAsync();
                ViewBag.SelectedUser = user.UserName;

                // Setup sorting parameters
                ViewData["TitleSortParm"] = String.IsNullOrEmpty(sortOrder) ? "title_desc" : "";
                ViewData["DeadlineSortParm"] = sortOrder == "Deadline" ? "deadline_desc" : "Deadline";
                ViewData["ProgressSortParm"] = sortOrder == "Progress" ? "progress_desc" : "Progress";

                var tasks = _context.TaskItems.Where(t => t.AssignedTo == user.UserName);

                // Apply sorting based on the sortOrder parameter
                tasks = sortOrder switch
                {
                    "title_desc" => tasks.OrderByDescending(t => t.Title),
                    "Deadline" => tasks.OrderBy(t => t.Deadline),
                    "deadline_desc" => tasks.OrderByDescending(t => t.Deadline),
                    "Progress" => tasks.OrderBy(t => t.Progress),
                    "progress_desc" => tasks.OrderByDescending(t => t.Progress),
                    _ => tasks.OrderBy(t => t.Title),
                };

                return View("Index", await tasks.AsNoTracking().ToListAsync());
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occurred in UserTasks: {ex.Message}");
                return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }

        // GET: TaskItems/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var taskItem = await _context.TaskItems.FirstOrDefaultAsync(m => m.Id == id);
                if (taskItem == null)
                {
                    return NotFound();
                }

                return View(taskItem);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occurred in Details: {ex.Message}");
                return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }

        // GET: TaskItems/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                // Populate the AssignedTo dropdown with available users
                ViewData["AssignedTo"] = new SelectList(await _userManager.Users.ToListAsync(), "UserName", "UserName");
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occurred in Create (GET): {ex.Message}");
                return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }

        // POST: TaskItems/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Description,Deadline,Progress,AssignedTo")] TaskItem taskItem)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(taskItem);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error occurred in Create (POST): {ex.Message}");
                    ModelState.AddModelError("", "Unable to save changes. Try again later.");
                }
            }

            // Repopulate AssignedTo dropdown if model state is invalid
            ViewData["AssignedTo"] = new SelectList(await _userManager.Users.ToListAsync(), "UserName", "UserName", taskItem.AssignedTo);
            return View(taskItem);
        }

        // GET: TaskItems/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var taskItem = await _context.TaskItems.FindAsync(id);
                if (taskItem == null)
                {
                    return NotFound();
                }

                ViewData["AssignedTo"] = new SelectList(await _userManager.Users.ToListAsync(), "UserName", "UserName", taskItem.AssignedTo);
                return View(taskItem);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occurred in Edit (GET): {ex.Message}");
                return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }

        // POST: TaskItems/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Deadline,Progress,AssignedTo")] TaskItem taskItem)
        {
            if (id != taskItem.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(taskItem);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TaskItemExists(taskItem.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        _logger.LogError($"Concurrency error while updating TaskItem with ID {taskItem.Id}.");
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error occurred in Edit (POST): {ex.Message}");
                    ModelState.AddModelError("", "Unable to save changes. Try again later.");
                }
            }

            ViewData["AssignedTo"] = new SelectList(await _userManager.Users.ToListAsync(), "UserName", "UserName", taskItem.AssignedTo);
            return View(taskItem);
        }

        // GET: TaskItems/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var taskItem = await _context.TaskItems.FirstOrDefaultAsync(m => m.Id == id);
                if (taskItem == null)
                {
                    return NotFound();
                }

                return View(taskItem);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occurred in Delete (GET): {ex.Message}");
                return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }

        // POST: TaskItems/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var taskItem = await _context.TaskItems.FindAsync(id);
                if (taskItem == null)
                {
                    return NotFound();
                }

                _context.TaskItems.Remove(taskItem);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occurred in DeleteConfirmed: {ex.Message}");
                return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }

        private bool TaskItemExists(int id)
        {
            return _context.TaskItems.Any(e => e.Id == id);
        }
    }
}
