using Microsoft.AspNetCore.Mvc;
using TicTacToe;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<GameStore>();
builder.Services.AddSingleton<GameService>();
builder.Services.AddSingleton<ScoreboardService>();
builder.Services.AddSingleton<ComputerMoveStrategy>();
builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
    options.InvalidModelStateResponseFactory = _ => new BadRequestObjectResult(new { code = "INVALID_REQUEST", message = "Provide a valid JSON request with the required fields." }));
var app = builder.Build();
app.Use(async (context, next) =>
{
    try { await next(); }
    catch (GameException error)
    {
        context.Response.StatusCode = error.Status;
        await context.Response.WriteAsJsonAsync(new { code = error.Code, message = error.Message });
    }
});
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();
app.MapFallback("/api/{**path}", () => Results.NotFound(new { code = "INVALID_REQUEST", message = "API endpoint not found or invalid game ID." }));
app.MapFallbackToFile("index.html");
app.Run();
