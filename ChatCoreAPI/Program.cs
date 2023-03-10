
using ChatCoreAPI.Actors;
using ChatCoreAPI.Hubs;
using ChatCoreAPI.Services;

using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "ToDo API",
        Description = "An ASP.NET Core Web API for managing ToDo items",
        TermsOfService = new Uri("https://example.com/terms"),
        Contact = new OpenApiContact
        {
            Name = "WebSocketTest",
            Url = new Uri("http://localhost:9595")
        },
        License = new OpenApiLicense
        {
            Name = "Example License",
            Url = new Uri("https://example.com/license")
        }
    });

    options.AddSignalRSwaggerGen();
});

// creates instance of IPublicHashingService that can be accessed by ASP.NET
builder.Services.AddSingleton<IActorBridge, AkkaService>();

// starts the IHostedService, which creates the ActorSystem and actors
builder.Services.AddHostedService<AkkaService>(sp => (AkkaService)sp.GetRequiredService<IActorBridge>());


builder.Services.AddRazorPages();
builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        builder =>
        {
            builder.WithOrigins("https://example.com")
                .AllowAnyHeader()
                .WithMethods("GET", "POST")
                .AllowCredentials();
        });
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthorization();

//app.UseCors();
app.MapRazorPages();
app.MapHub<ChatHub>("/chatHub");


app.MapControllers();

app.Run();
