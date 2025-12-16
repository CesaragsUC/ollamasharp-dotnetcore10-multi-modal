using Microsoft.AspNetCore.StaticFiles;

namespace Demo._02.Web.Utils;

public static class FileDownloadHelper
{
    public static async Task<IResult> DownloadFileAsync(
        string filename,
        string? sourceDirectory = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            sourceDirectory ??= Path.Combine(Directory.GetCurrentDirectory(), "Files");

            // Criar pasta se não existir
            Directory.CreateDirectory(sourceDirectory);

            var safeFilename = Path.GetFileName(filename);
            var filePath = Path.Combine(sourceDirectory, safeFilename);

            if (!File.Exists(filePath))
                return Results.NotFound(new { error = $"Arquivo '{safeFilename}' não encontrado" });

            var contentType = GetContentType(safeFilename);

            // Ler arquivo de forma assíncrona
            var fileBytes = await File.ReadAllBytesAsync(filePath, cancellationToken);

            // Deletar todos os arquivos na pasta após o download
            await DeleteAllFiles(sourceDirectory);

            // Results.File envia o arquivo para o NAVEGADOR
            // O navegador salva na pasta Downloads do usuário
            return Results.File(
                fileBytes,
                contentType: contentType,
                fileDownloadName: safeFilename,
                enableRangeProcessing: true
            );
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message);
        }
    }

    private static string GetContentType(string filename)
    {
        var provider = new FileExtensionContentTypeProvider();
        provider.TryGetContentType(filename, out var contentType);
        return contentType ?? "application/octet-stream";
    }

    public static async Task<int> DeleteAllFiles(string directory, string searchPattern = "*.*")
    {
        try
        {
            if (!Directory.Exists(directory))
                return 0;

            var files = Directory.GetFiles(directory, searchPattern);
            var deletedCount = 0;

            foreach (var file in files)
            {
                try
                {
                    File.Delete(file);
                    deletedCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao deletar {file}: {ex.Message}");
                }
            }

            return deletedCount;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro: {ex.Message}");
            return 0;
        }
    }
}