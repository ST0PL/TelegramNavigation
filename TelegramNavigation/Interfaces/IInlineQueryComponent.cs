namespace TelegramNavigation.Interfaces
{
    /// <summary>
    /// A component interface to handle the inline query
    /// </summary>
    public interface IInlineQueryComponent
    {
        /// <summary>
        /// Handle a inline query with specified route
        /// </summary>
        /// <param name="queryRoute">Inline query route</param>
        /// <param name="botClient">Telegram bot client</param>
        /// <param name="message">Inline query message</param>
        /// <param name="from">Inline query sender</param>
        Task HandleQueryAsync(Route queryRoute, ITelegramBotClient botClient, Message message, User from);

        /// <summary>
        /// Initialize a inline query component by sending message to the chat
        /// </summary>
        /// <param name="queryRoute">Inline query route</param>
        /// <param name="botClient">Telegram bot client</param>
        /// <param name="chatId">Chat identifier</param>
        /// <param name="messageThreadId">Inline query sender</param>
        Task<Message?> InitializeAsync(Route queryRoute, ITelegramBotClient botClient, long chatId, int? messageThreadId = null);
    }
}
