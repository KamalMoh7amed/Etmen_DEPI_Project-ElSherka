namespace Etmen_BLL.Repositories.IServices
{
    public interface IChatbotService
    {
        Task<string> AskAsync(string message, CancellationToken cancellationToken = default);
    }
}

