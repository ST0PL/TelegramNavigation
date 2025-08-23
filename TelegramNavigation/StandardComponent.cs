namespace TelegramNavigation
{
    /// <summary>
    /// A global inline query component
    /// </summary>
    public class StandardComponent : IInlineQueryComponent
    {
        private readonly IMessageHandler _messageHandler;
        private readonly Dictionary<string, InlineQueryHook> _routes;

        /// <summary>
        /// Create a new <see cref="StandardComponent"/> instance
        /// </summary>
        /// <param name="messageHandler">instance of class implementing <see cref="IMessageHandler"/> interface</param>
        public StandardComponent(IMessageHandler messageHandler)
        {
            _messageHandler = messageHandler;
            _routes = new()
            {
                ["/close"] = CloseComponentAsync,
            };
        }

        /// <inheritdoc/>
        public async Task HandleQueryAsync(Route queryRoute, ITelegramBotClient botClient, Message message, User from)
            => await _routes[queryRoute.Path].Invoke(queryRoute, botClient, message,from);

        /// <inheritdoc/>
        public Task<Message?> InitializeAsync(Route queryRoute, ITelegramBotClient botClient, long chatId, int? messageThreadId = null)
            => Task.FromResult<Message?>(null);

        private async Task CloseComponentAsync(Route route, ITelegramBotClient botClient, Message message, User from)
        {
            await botClient.DeleteMessage(message.Chat.Id, message.Id);
            InlineMiddleware.RemovePagesStack(message.Chat.Id, message.Id);
            InlineMiddleware.RemoveInlineStack(message.Chat.Id, message.Id);
            _messageHandler.UnregisterHook(message.Chat.Id, from.Id);
        }
    }
}