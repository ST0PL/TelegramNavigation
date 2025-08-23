using ExampleBot.Components.Forms;

namespace ExampleBot.Handlers.ReplyButtons
{
    internal class FullNameButtonHandler : IReplyButtonHandler
    {
        public Task HandleAsync(ITelegramBotClient botClient, Message message)
            => new FullNameForm().SendForm(botClient, message.Chat.Id, message.From.Id, message.IsTopicMessage ? message.MessageThreadId : null);
    }
}
