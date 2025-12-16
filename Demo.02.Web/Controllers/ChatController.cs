using Demo._02.Web.Models;
using Demo._02.Web.Services;
using GeminiDotnet.Extensions.AI;
using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using System.Text;

namespace Demo._02.Web.Controllers;

[Route("api/[controller]")]
public class ChatController(
    IChatClient chatClient,
    ChatService chatService,
    Client geminiClient,
    GeminiChatClient geminiChatClient) : Controller
{

    private const string GEMINI_VIDEO_AI_MODEL = "veo-3.1-generate-preview";
    private const string GEMINI_AI_MODEL = "gemini-2.5-flash";
    private const string GEMINI_IMAGE_AI_MODEL = "imagen-4.0-generate-001";

    /// <summary>
    /// Versao simples da resposta
    /// </summary>
    /// <param name="prompt"></param>
    /// <returns></returns>

    [HttpGet]
    [Route("question/{prompt}")]
    public async Task<IActionResult> Question(string prompt)
    {
        /// OPCAO :1 Usando o chatClient generico (Ollama ou Gemini dependendo do ambiente)
        //var response = await chatClient.GetResponseAsync(prompt);

        /// OPCAO 2: Usando o Gemini diretamente -> Client().
        // var response = await geminiClient.Models.GenerateContentAsync(model: GEMINI_AI_MODEL, prompt);

        /// OPCAO 3: Usando o Gemini via GeminiChatClient
        var response = await geminiChatClient.GetResponseAsync(prompt);

        //  return Ok(response.Candidates[0].Content.Parts[0].Text);
        return Ok(response.Text);
    }


    /// <summary>
    /// Versao que usa contexto de conversa
    /// </summary>
    /// <param name="prompt"></param>
    /// <returns></returns>

    [HttpGet]
    [Route("questionv2/{prompt}")]
    public async Task<IActionResult> QuestionV2(string prompt)
    {
        var response = await chatService.ChatAsync(prompt);
        return Ok(response);
    }

    /// <summary>
    /// Versao com streaming da resposta
    /// </summary>
    /// <param name="prompt"></param>
    /// <returns></returns>
    [HttpGet]
    [Route("questionv3/{prompt}")]
    [Produces("text/plain")]
    public async Task Questionv3(string prompt)
    {
        Response.ContentType = "text/plain; charset=utf-8";

        await Response.WriteAsync("🤖 Gerando resposta...\n\n");
        await Response.Body.FlushAsync();

        var responseBuilder = new StringBuilder();

        await foreach (var chunk in chatClient.GetStreamingResponseAsync(prompt))
        {
            if (chunk.Text != null)
            {
                responseBuilder.Append(chunk.Text);
                await Response.WriteAsync(chunk.Text);
                await Response.Body.FlushAsync();

                // Delay opcional para visualização
                await Task.Delay(30);
            }
        }

        await Response.WriteAsync("\n\n✅ Resposta completa!");
        await Response.Body.FlushAsync();
    }

    /// <summary>
    /// Versao que usa contexto por chatId e mantem o contexto da conversa
    /// </summary>
    /// <param name="prompt"></param>
    /// <returns></returns>

    [HttpPost]
    [Route("questionv4")]
    public async Task<IActionResult> QuestionV4([FromBody] QuestionInput questionInput)
    {
        var response = await chatService.ChatUserAsync(questionInput.ChatId.ToString(), questionInput.Prompt);
        return Ok(response);
    }

    /// <summary>
    /// Doc for Google Gen AI : https://googleapis.github.io/dotnet-genai/
    /// </summary>
    /// <param name="prompt"></param>
    /// <returns></returns>
    [HttpPost]
    [Route("video/{prompt}")]
    public async Task<IActionResult> Video(string prompt)
    {
        var source1 = new GenerateVideosSource
        {
            Prompt = prompt //"A short video man with a dog"
        };


        var operation = await geminiClient.Models.GenerateVideosAsync(
            model: GEMINI_VIDEO_AI_MODEL, source: source1, config: new GenerateVideosConfig
            {
                NumberOfVideos = 1,
            });

        while (operation.Done != true)
        {
            try
            {
                await Task.Delay(10000);
                operation = await geminiClient.Operations.GetAsync(operation, null);
            }
            catch (TaskCanceledException)
            {
                System.Console.WriteLine("Task was cancelled while waiting.");
                break;
            }
        }

        // Obter pasta Downloads do usuário atual
        var downloadsPath = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), 
            "Downloads"
        );

        var outputPath = Path.Combine(downloadsPath, $"video_generated_{Guid.NewGuid()}.mp4");

        await geminiClient.Files.DownloadToFileAsync(
            generatedVideo: operation.Response.GeneratedVideos.First(),
            outputPath: outputPath
        );

        return Ok(new { Message = "Video gerado com sucesso!", VideoPath = outputPath });
    }


    /// <summary>
    /// Doc for Google Gen AI : https://googleapis.github.io/dotnet-genai/
    /// </summary>
    /// <param name="prompt"></param>
    /// <returns></returns>
    [HttpPost]
    [Route("image/{prompt}")]
    public async Task<IActionResult> Image(string prompt)
    {
        var generateImagesConfig = new GenerateImagesConfig
        {
            NumberOfImages = 1,
            AspectRatio = "1:1",
            SafetyFilterLevel = SafetyFilterLevel.BLOCK_LOW_AND_ABOVE,
            PersonGeneration = PersonGeneration.DONT_ALLOW,
            IncludeSafetyAttributes = true,
            IncludeRaiReason = true,
            OutputMimeType = "image/jpeg",
        };

        var response = await geminiClient.Models.GenerateImagesAsync(
          model: GEMINI_IMAGE_AI_MODEL,
          prompt: prompt , //"Red skateboard",
          config: generateImagesConfig
        );

        // Obter pasta Downloads do usuário atual
        var downloadsPath = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
            "Downloads"
        );

        var outputPath = Path.Combine(downloadsPath, $"image_generated_{Guid.NewGuid()}.png");

        var image = response.GeneratedImages.First().Image;
        var imageBytes = image.ImageBytes.ToArray();

        // Salvar bytes no arquivo
        await System.IO.File.WriteAllBytesAsync(outputPath, imageBytes);

        return Ok(new { Message = "Imagem gerada com sucesso!", VideoPath = outputPath });
    }
}
