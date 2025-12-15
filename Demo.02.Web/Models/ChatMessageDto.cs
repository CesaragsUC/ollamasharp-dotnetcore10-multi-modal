namespace Demo._02.Web.Models;

// DTO para serialização
public class ChatMessageDto
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}