var builder = WebApplication.CreateBuilder(args);

// ============ DEPENDENCY INJECTION CONFIGURATION ============
// Add services to the container - follows Dependency Inversion Principle

// Add MVC services
builder.Services.AddControllersWithViews();

// Add Blazor components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add configuration
var configuration = builder.Configuration;

// ============ BUILD APPLICATION ============
var app = builder.Build();

// ============ HTTP REQUEST PIPELINE CONFIGURATION ============
// Middleware order matters - process requests in this order

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// ============ ROUTING CONFIGURATION ============

// MVC Routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Blazor component routes
app.MapRazorComponents<HelloWorldMvcApp.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
