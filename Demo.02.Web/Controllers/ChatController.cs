using Demo._02.Web.Models;
using Demo._02.Web.Services;
using GeminiDotnet.Extensions.AI;
using Google.GenAI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using System.Text;
using static Google.Apis.Requests.BatchRequest;

namespace Demo._02.Web.Controllers;

[Route("api/[controller]")]
public class ChatController(
    IChatClient chatClient,
    ChatService chatService,
    Client geminiClient,
    GeminiChatClient geminiChatClient) : Controller
{
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
        // var response = await geminiClient.Models.GenerateContentAsync(model: "gemini-2.5-flash", prompt);

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

}
