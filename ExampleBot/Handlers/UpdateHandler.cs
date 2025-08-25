using ExampleBot.Components.Inline.Calendar;
using ExampleBot.Components.Inline.Catalog;
using ExampleBot.Handlers.Commands;
using ExampleBot.Handlers.ReplyButtons;
using Telegram.Bot.Polling;

namespace ExampleBot.Handlers
{
    internal class UpdateHandler : IUpdateHandler
    {
        public static IMessageHandler MessageHandler { get; }

        static UpdateHandler()
            => MessageHandler = new MessageHandler();

        public UpdateHandler()
        {
            InlineMiddleware.RegisterComponent(new StandardComponent(MessageHandler));
            InlineMiddleware.RegisterComponent(new YearsComponent());
            InlineMiddleware.RegisterComponent(new MonthsComponent());
            InlineMiddleware.RegisterComponent(new DaysComponent());
            InlineMiddleware.RegisterComponent(new TypesComponent());
            InlineMiddleware.RegisterComponent(new MediaComponent());
            MessageHandler.RegisterCommand("start", new StartCommandHandler());
            MessageHandler.RegisterReplyButton("Calendar", new CalendarButtonHandler());
            MessageHandler.RegisterReplyButton("Tic-Tac-Toe", new TicTacToeButtonHandler());
            MessageHandler.RegisterReplyButton("Full name form", new FullNameButtonHandler());
            MessageHandler.RegisterReplyButton("Catalog", new CatalogButtonHandler());
            MessageHandler.RegisterReplyButton("State buttons", new StateButtonHandler());
        }

        public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
        {
            Console.WriteLine(exception);
            return Task.CompletedTask;
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            switch (update.Type)
            {
                case UpdateType.Message:
                    await MessageHandler.HandleAsync(botClient, update.Message!);
                    break;
                case UpdateType.CallbackQuery:
                    await InlineMiddleware.HandleAsync(botClient, update.CallbackQuery!);
                    break;
            }
        }
    }
}
