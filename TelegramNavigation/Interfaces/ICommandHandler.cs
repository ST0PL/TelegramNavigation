namespace TelegramNavigation.Interfaces
{
    /// <summary>
    /// A command handler interface
    /// </summary>
    public interface ICommandHandler
    {
        /// <summary>
        /// Handle a text command
        /// </summary>
        /// <param name="botClient">Telegram bot client instance</param>
        /// <param name="command">Command instance</param>
        Task HandleAsync(ITelegramBotClient botClient, Command command);
    }
}
