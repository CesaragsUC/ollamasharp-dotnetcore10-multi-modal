using Demo._02.Web.Services;
using GeminiDotnet;
using GeminiDotnet.Extensions.AI;
using Google.GenAI;
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
var envTypeName = builder.Environment.EnvironmentName.ToLower();

#region Gemini Setup
// Doc usar gemini client diretamente: https://ai.google.dev/gemini-api/docs/quickstart?hl=pt-br#c_1
// Doc implemetacao alternativa com GeminiChatClient: https://github.com/rabuckley/GeminiDotnet

var aiModel = Environment.GetEnvironmentVariable("AI_MODEL");
var geminiApiKey = builder.Configuration["GOOGLE_API_KEY"]; // esse valor vem das variaveis de ambiente do sistema

var geminiClient = new GeminiChatClient(new GeminiClientOptions
{
    ApiKey = geminiApiKey!,
    ModelId = "gemini-2.5-flash"
});

// The client gets the API key from the environment variable `GEMINI_API_KEY`.
var client = new Client();
builder.Services.AddSingleton(client);
builder.Services.AddSingleton(geminiClient);


#endregion

// Just to show alternative way to create the ChatClient for OpenAI ChatGPT.
var chatGptApiKey = builder.Configuration["OPENAI_API_KEY"];
var chatGptClient = new OpenAI.Chat.ChatClient(
                             model:"gpt-4o-mini",
                             apiKey:chatGptApiKey)
                            .AsIChatClient();


builder.Services.AddChatClient(services => 
         builder.Environment.IsDevelopment()
         ? new OllamaApiClient(uri, "phi3:3.8b")
         : chatGptClient)
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
