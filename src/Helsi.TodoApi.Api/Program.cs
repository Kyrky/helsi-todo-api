using Helsi.TodoApi.Api.Extensions;
using Helsi.TodoApi.Api.Filters;
using Helsi.TodoApi.Application;
using Helsi.TodoApi.Infrastructure;
using Serilog;

// Catches startup errors that happen before the configured logger exists.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services));

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddControllers()
        .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = InvalidModelStateResponse.Create);
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddProblemDetails();

    var app = builder.Build();

    // Unhandled exceptions return a 500 ProblemDetails; the stack trace stays in the log.
    app.UseExceptionHandler();

    // One line per request, tagged with the caller's user id.
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} by {UserId} responded {StatusCode} in {Elapsed:0} ms";
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            string userId = httpContext.Request.Headers[RequireUserIdAttribute.HeaderName].ToString();
            diagnosticContext.Set("UserId", string.IsNullOrEmpty(userId) ? "anonymous" : userId);
        };
    });

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.MapControllers();
    app.MapHealthChecks("/health");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Lets WebApplicationFactory<Program> in the integration tests see the entry point.
public partial class Program;
