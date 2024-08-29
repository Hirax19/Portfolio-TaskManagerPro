using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskManagerPro.Data;
using TaskManagerPro.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()  // Registers RoleManager and role-related services
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Apply migrations and seed users, roles, and tasks
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var context = services.GetRequiredService<ApplicationDbContext>();

    await context.Database.MigrateAsync(); // Ensure the database is up to date with migrations
    await SeedUsersAndRoles(userManager, roleManager, context);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=TaskItems}/{action=Index}/{id?}");  // Start at TaskItems/Index
app.MapRazorPages();

app.Run();

// Seed method
async Task SeedUsersAndRoles(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext context)
{
    // Check if the roles exist, and create them if not
    string[] roleNames = { "Admin", "Manager", "User" };
    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    var adminUser = new IdentityUser
    {
        UserName = "admin@taskmanagerpro.com",
        Email = "admin@taskmanagerpro.com",
        EmailConfirmed = true
    };

    var userExist = await userManager.FindByEmailAsync(adminUser.Email);
    if (userExist == null)
    {
        var createUserResult = await userManager.CreateAsync(adminUser, "Admin@123");
        if (createUserResult.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
    else
    {
        adminUser = userExist;
    }

    var managerUser = new IdentityUser
    {
        UserName = "manager@taskmanagerpro.com",
        Email = "manager@taskmanagerpro.com",
        EmailConfirmed = true
    };

    userExist = await userManager.FindByEmailAsync(managerUser.Email);
    if (userExist == null)
    {
        var createUserResult = await userManager.CreateAsync(managerUser, "Manager@123");
        if (createUserResult.Succeeded)
        {
            await userManager.AddToRoleAsync(managerUser, "Manager");
        }
    }
    else
    {
        managerUser = userExist;
    }

    var regularUser = new IdentityUser
    {
        UserName = "user@taskmanagerpro.com",
        Email = "user@taskmanagerpro.com",
        EmailConfirmed = true
    };

    userExist = await userManager.FindByEmailAsync(regularUser.Email);
    if (userExist == null)
    {
        var createUserResult = await userManager.CreateAsync(regularUser, "User@123");
        if (createUserResult.Succeeded)
        {
            await userManager.AddToRoleAsync(regularUser, "User");
        }
    }
    else
    {
        regularUser = userExist;
    }

    // Seed tasks for each user
    if (!context.TaskItems.Any())
    {
        context.TaskItems.AddRange(
            new TaskItem
            {
                Title = "Server Onderhoud",
                Description = "Zorg ervoor dat alle servers up-to-date zijn en veilig draaien.",
                Deadline = DateTime.Now.AddDays(7),
                Progress = 50,
                AssignedTo = adminUser.UserName
            },
            new TaskItem
            {
                Title = "Nieuwe Beveiligingsbeleid",
                Description = "Ontwikkel en implementeer een nieuw beveiligingsbeleid voor de organisatie.",
                Deadline = DateTime.Now.AddDays(10),
                Progress = 75,
                AssignedTo = adminUser.UserName
            },
            new TaskItem
            {
                Title = "Incident Responsplan",
                Description = "Creëer een responsplan voor mogelijke beveiligingsincidenten.",
                Deadline = DateTime.Now.AddDays(12),
                Progress = 40,
                AssignedTo = adminUser.UserName
            },
            new TaskItem
            {
                Title = "Teamvergadering plannen",
                Description = "Plan een vergadering om de voortgang van het project te bespreken.",
                Deadline = DateTime.Now.AddDays(5),
                Progress = 75,
                AssignedTo = managerUser.UserName
            },
            new TaskItem
            {
                Title = "Projectrapportage",
                Description = "Maak een rapport over de voortgang van het huidige project.",
                Deadline = DateTime.Now.AddDays(10),
                Progress = 00,
                AssignedTo = managerUser.UserName
            },
            new TaskItem
            {
                Title = "Budget Evaluatie",
                Description = "Evalueer het budget voor het volgende kwartaal.",
                Deadline = DateTime.Now.AddDays(8),
                Progress = 70,
                AssignedTo = managerUser.UserName
            },
            new TaskItem
            {
                Title = "Gebruikershandleiding bijwerken",
                Description = "Werk de gebruikershandleiding bij voor de nieuwste softwareversie.",
                Deadline = DateTime.Now.AddDays(3),
                Progress = 100,
                AssignedTo = regularUser.UserName
            },
            new TaskItem
            {
                Title = "Klantenondersteuning",
                Description = "Beantwoord vragen van klanten en los technische problemen op.",
                Deadline = DateTime.Now.AddDays(4),
                Progress = 60,
                AssignedTo = regularUser.UserName
            },
            new TaskItem
            {
                Title = "Nieuwe software testen",
                Description = "Test de nieuwe software update voordat deze wordt uitgerold.",
                Deadline = DateTime.Now.AddDays(6),
                Progress = 30,
                AssignedTo = regularUser.UserName
            }
        );

        await context.SaveChangesAsync();
    }
}
