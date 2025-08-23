using ExampleBot.Components.Forms;
namespace ExampleBot.Handlers.ReplyButtons
{
    internal class StateButtonHandler : IReplyButtonHandler
    {
        public async Task HandleAsync(ITelegramBotClient botClient, Message message)
            => await new StateButtonsForm().SendForm(botClient, message.Chat.Id, message.From.Id, message.IsTopicMessage ? message.MessageThreadId : null);
    }
}
