using ExampleBot.Components.Forms.TicTacToe;

namespace ExampleBot.Handlers.ReplyButtons
{
    internal class TicTacToeButtonHandler : IReplyButtonHandler
    {
        public async Task HandleAsync(ITelegramBotClient botClient, Message message)
            => await new StartForm().SendForm(botClient, message.Chat.Id, message.From.Id, message.IsTopicMessage ? message.MessageThreadId : null);
    }
}
