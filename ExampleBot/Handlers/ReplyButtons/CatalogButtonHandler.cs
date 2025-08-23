namespace ExampleBot.Handlers.ReplyButtons
{
    internal class CatalogButtonHandler : IReplyButtonHandler
    {
        public Task HandleAsync(ITelegramBotClient botClient, Message message)
            => InlineMiddleware.SendComponent(new Route("types", "/", null), botClient, message.Chat.Id, message.IsTopicMessage ? message.MessageThreadId : null);
    }
}
