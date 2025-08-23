namespace TelegramNavigation.Interfaces
{
    /// <summary>
    /// A message handler interface
    /// </summary>
    public interface IMessageHandler
    {
        /// <summary>
        /// Get enumerable of reply buttons
        /// </summary>
        /// <returns>Enumerable of reply buttons</returns>
        IEnumerable<string> GetReplyButtons();
        /// <summary>
        /// Handle a bot message
        /// </summary>
        /// <param name="botClient">Telegram bot client instance</param>
        /// <param name="message">Message to process</param>
        Task HandleAsync(ITelegramBotClient botClient, Message message);
        /// <summary>
        /// Register a command with specified name and handler <see cref="ICommandHandler">ICommandHandler</see>
        /// </summary>
        /// <param name="commandName">The command name following the "/", such as "/commandName"</param>
        /// <param name="handler"><see cref="ICommandHandler">ICommandHandler</see> instance</param>
        void RegisterCommand(string commandName, ICommandHandler handler);
        /// <summary>
        /// Register a message hook associated with chat id and user id
        /// </summary>
        /// <param name="chatId">Telegram chat identifier</param>
        /// <param name="userId">Telegram user identifier</param>
        /// <param name="hook">The method that will be invoked when user sends a message</param>
        void RegisterHook(long chatId, long userId, MessageHook hook);
        /// <summary>
        /// Registers a reply button with the specified text and associates it with a handler.
        /// </summary>
        /// <param name="text">The text to display on the reply button. Cannot be null or empty.</param>
        /// <param name="handler">The handler that will be invoked when the reply button is pressed. Cannot be null.</param>
        void RegisterReplyButton(string text, IReplyButtonHandler handler);
        /// <summary>
        /// Unregister a command with specified name
        /// </summary>
        /// <param name="commandName">The command name following the "/", such as "/commandName</param>
        void UnregisterCommand(string commandName);
        /// <summary>
        /// Removes a message hook associated with chat id and user id
        /// </summary>
        /// <param name="chatId">Telegram chat identifier</param>
        /// <param name="userId">Telegram user identifier</param>
        void UnregisterHook(long chatId, long userId);
        /// <summary>
        /// Unregisters a reply button associated with the specified text.
        /// </summary>
        /// <param name="text">The text of the reply button to unregister. Cannot be null or empty.</param>
        void UnregisterReplyButton(string text);
    }
}
