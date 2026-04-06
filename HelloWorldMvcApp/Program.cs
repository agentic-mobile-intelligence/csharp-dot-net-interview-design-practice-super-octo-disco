using HelloWorldMvcApp.Services;

var builder = WebApplication.CreateBuilder(args);

// ============ DEPENDENCY INJECTION CONFIGURATION ============
// Add services to the container - follows Dependency Inversion Principle

// Add MVC services
builder.Services.AddControllersWithViews();

// Add Blazor components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ============ ADD CUSTOM SERVICES ============

// Register JSON service for Newtonsoft.Json usage
builder.Services.AddScoped<IJsonService, JsonService>();

// ============ ADD CONFIGURATION ============
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

// API routes (automatically mapped from ApiController)
app.MapControllers();

app.Run();
