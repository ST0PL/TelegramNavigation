namespace TelegramNavigation
{
    /// <summary>
    /// A base abstract telegram form
    /// </summary>
    public abstract class BaseForm
    {
        /// <summary>
        /// Sends a from to the telegram chat
        /// </summary>
        /// <param name="botClient">Telergram bot client</param>
        /// <param name="chatId">Chat idenitifer</param>
        /// <param name="fromId">Sender identifier</param>
        /// <param name="messageThreadId">Message thread identifier</param>
        public abstract Task SendForm(ITelegramBotClient botClient, long chatId, long fromId, int? messageThreadId);

        /// <summary>
        /// Sends a form data by pressing the inline button.
        /// </summary>
        /// <param name="route">Inline query route</param>
        /// <param name="botClient">Telegram bot client</param>
        /// <param name="message">Form message</param>
        /// <param name="from">Inline query sender</param>
        protected abstract Task SendData(Route route, ITelegramBotClient botClient, Message message, User from);
    }
}