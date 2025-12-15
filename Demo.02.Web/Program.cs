using Demo._02.Web.Services;
using Microsoft.Extensions.AI;
using Microsoft.OpenApi;
using OllamaSharp;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();

builder.Services.AddOpenApi();
builder.Services.AddScoped<ChatService>();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Trace));

builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new OpenApiInfo { Title = "Fundamentos AI Demo 02", Version = "v1" }));

//Doc: https://www.nuget.org/packages/Microsoft.Extensions.AI.OpenAI

var uri = new Uri("http://localhost:11434");

var envType = builder.Environment.IsDevelopment();

builder.Services.AddChatClient(services => 
         builder.Environment.IsDevelopment()
         ? new OllamaApiClient(uri, "phi3:3.8b")
         : new OpenAI.Chat.ChatClient("gpt-4o-mini", Environment.GetEnvironmentVariable("OPENAI_API_KEY")).AsIChatClient())
            .UseDistributedCache()
            .UseLogging();


if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDistributedMemoryCache();
}
else
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = builder.Configuration.GetConnectionString("Redis");
        options.InstanceName = "ChatHistory:";
    });
}

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
