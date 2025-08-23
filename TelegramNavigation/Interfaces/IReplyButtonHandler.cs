namespace TelegramNavigation.Interfaces
{
    /// <summary>
    /// A reply button handler interface
    /// </summary>
    public interface IReplyButtonHandler
    {
        /// <summary>
        /// Handle a reply button
        /// </summary>
        /// <param name="botClient">Telegram bot client instance</param>
        /// <param name="message">Telegram message</param>
        Task HandleAsync(ITelegramBotClient botClient, Message message);
    }
}
