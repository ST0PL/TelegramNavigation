namespace ExampleBot.Handlers.Commands
{
    internal class StartCommandHandler : ICommandHandler
    {
        private ReplyKeyboardMarkup? _replyMarkup;
        public ReplyKeyboardMarkup ReplyMarkup => _replyMarkup ??= GetMarkup();

        public async Task HandleAsync(ITelegramBotClient botClient, Command command)
        {
            await botClient.SendMessage(command.Message.Chat.Id,
                "_To see menu examples, use the buttons below\\._",
                messageThreadId: command.Message.IsTopicMessage ? command.Message.MessageThreadId : null,
                parseMode: ParseMode.MarkdownV2,
                replyMarkup: ReplyMarkup);
        }
        private static ReplyKeyboardMarkup GetMarkup()
        {
            var markup = new ReplyKeyboardMarkup();
            foreach (var item in UpdateHandler.MessageHandler.GetReplyButtons())
                markup.AddNewRow(item);
            return markup;
        }
    }
}
