
namespace ExampleBot.Handlers.ReplyButtons
{
    internal class CalendarButtonHandler : IReplyButtonHandler
    {
        private readonly Route _yearsRoute = new Route("years", "/", 
            new() {["year"] = DateTime.Now.Year.ToString(), ["rows"] = "2", ["columns"] = "3" });
        
        public Task HandleAsync(ITelegramBotClient botClient, Message message)
            => InlineMiddleware.SendComponent(_yearsRoute, botClient, message.Chat.Id, message.IsTopicMessage ? message.MessageThreadId : null);
    }
}
