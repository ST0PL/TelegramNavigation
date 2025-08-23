namespace TelegramNavigation.Interfaces
{
    /// <summary>
    /// A inline query page component with routing support
    /// </summary>
    public interface IInlinePageComponent
    {
        /// <summary>
        /// Performs a transition to the previous page
        /// </summary>
        /// <param name="queryRoute">Inline query route</param>
        /// <param name="botClient">Telegram bot client</param>
        /// <param name="message">Inline query message</param>
        /// <param name="user">Inline query sender</param>
        Task MoveBack(Route queryRoute, ITelegramBotClient botClient, Message message, User user);

        /// <summary>
        /// Performs a transition to the next page
        /// </summary>
        /// <param name="queryRoute">Inline query route</param>
        /// <param name="botClient">Telegram bot client</param>
        /// <param name="message">Inline query message</param>
        /// <param name="user">Inline query sender</param>
        Task MoveNext(Route queryRoute, ITelegramBotClient botClient, Message message, User user);
    }
}
