# Telegram Navigation

![.NET 9.0](https://img.shields.io/badge/.NET-9.0-8A2BE2)
[![NuGet Version](https://img.shields.io/nuget/v/TelegramNavigation)](https://www.nuget.org/packages/TelegramNavigation)

Telegram Navigation is .NET library based on [.NET Client for Telegram Bot API](https://github.com/TelegramBots/Telegram.Bot) to enchance creating navigation and multi-level telegram menus experience.
# Getting started
### Create inline button
```csharp
var bot = new TelegramBotClient("<YOUR TOKEN>");

var inlineButton = InlineMiddleware.CreateButton("my button",
    async (route, bot, msg, from) =>
    {
        await bot.EditMessageText(msg.Chat.Id, msg.Id,
            $"\"my button\" with myArgument = {{{route.Args?["myArgument"]}}} pressed!");
        // your logic...
    }, new() { ["myArgument"] = "some data" });


await bot.SendMessage(chatId, "Message with inline button", replyMarkup: inlineButton.Button);

await bot.ReceiveAsync(async (bot, update, ct) =>
{
    if (update.Type == UpdateType.CallbackQuery)
        await InlineMiddleware.HandleAsync(bot, update.CallbackQuery!);
}, (bot, ex, ct) => Console.WriteLine(ex));
```
### Create message hook
```csharp
var bot = new TelegramBotClient("<YOUR API TOKEN>");

var messageHandler = new MessageHandler();

messageHandler.RegisterHook(targetChatId, targetUserId,
    async (bot, msg, from) =>
    {
        await bot.SendMessage(msg.Chat.Id, "Message hooked!",
               replyParameters: new ReplyParameters() { MessageId = msg.Id });
        messageHandler.UnregisterHook(msg.Chat.Id, from.Id); // remove hook
        // your logic...
    });

await bot.ReceiveAsync(async (bot, update, ct) =>
{
    if (update.Type == UpdateType.Message)
        await messageHandler.HandleAsync(bot, update.Message!);
}, (bot, ex, ct) => Console.WriteLine(ex));
```
### Create Page
```csharp
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramNavigation;
using TelegramNavigation.Interfaces;
using TelegramNavigation.Routing;

namespace ExampleBot
{
    [InlineComponent(name: "examplePage")] // register component name
    class ExamplePageComponent : IInlineQueryComponent, IInlinePageComponent
    {
        private readonly Dictionary<string, InlineQueryHook> _routes;
        private readonly string[] _source = Enumerable.Range(1,20).Select(i=>$"item {i}").ToArray();
        public ExamplePageComponent()
        {
            _routes = new()
            {
                ["/moveBack"] = MoveBack,
                ["/moveNext"] = MoveNext
            };
        }
        public async Task HandleQueryAsync(Route queryRoute, ITelegramBotClient botClient, Message message, User from)
            => await _routes[queryRoute.Path!].Invoke(queryRoute, botClient, message, from);


        public async Task<Message?> InitializeAsync(Route queryRoute, ITelegramBotClient botClient, long chatId, int? messageThreadId = null)
        {
            var botMessage = await botClient.SendMessage(chatId,
                "Loading..."); // Send placeholder message
            if (_source.Length > 0)
            {
                var page = InlineMiddleware.CreatePage(chatId, botMessage.Id, _source.Length, 5, "myPage");
                await botClient.EditMessageText(chatId, botMessage.Id,
                    GetText(page),
                    replyMarkup: GetMarkup(page));
            }
            else
                await botClient.EditMessageText(chatId, botMessage.Id,
                    "No data found");
            return botMessage;
        }

        public async Task MoveBack(Route queryRoute, ITelegramBotClient botClient, Message message, User user)
        {
            var page = InlineMiddleware.GetPage(message.Chat.Id, message.Id, "myPage");
            page!.MoveBack();
            await botClient.EditMessageText(message.Chat.Id, message.Id,
                GetText(page),
                replyMarkup: GetMarkup(page));

        }

        public async Task MoveNext(Route queryRoute, ITelegramBotClient botClient, Message message, User user)
        {
            var page = InlineMiddleware.GetPage(message.Chat.Id, message.Id, "myPage");
            page!.MoveNext();
            await botClient.EditMessageText(message.Chat.Id, message.Id,
                GetText(page),
                replyMarkup: GetMarkup(page));
        }
        private string GetText(PageController page)
        {
            StringBuilder sb = new();
            var items = _source.Skip(page.Offset).Take(page.ElementsCount);
            foreach (var item in items)
                sb.AppendLine(item);
            return sb.ToString();
        }
        private static InlineKeyboardMarkup GetMarkup(PageController page)
        {
            var markup = new InlineKeyboardMarkup();
            markup.AddButton(new InlineKeyboardButton()
            {
                Text = page.PreviousPage > -1 ? "<<" : " ",
                CallbackData = page.PreviousPage > -1 ? new Route("examplePage", "/moveBack", null).ToString() : "none"
            });
            markup.AddButton($"{page.CurrentPage} / {page.PagesCount}", "none");
            markup.AddButton(new InlineKeyboardButton()
            {
                Text = page.NextPage > -1 ? ">>" : " ",
                CallbackData = page.NextPage > -1 ? new Route("examplePage", "/moveNext", null).ToString() : "none"
            });
            InlineMiddleware.AddCloseButton(markup, "Close");
            return markup;
        }
    }

    internal class Program
    {
        static async Task Main(string[] args)
        {
            var bot = new TelegramBotClient("<YOUR API TOKEN>");

            InlineMiddleware.RegisterComponent(new StandardComponent(null)); // register standard component for close button route
            InlineMiddleware.RegisterComponent(new ExamplePageComponent()); // register our page component

            // Send our component
            await InlineMiddleware.SendComponent(new Route(type: "examplePage", path: string.Empty, args: null), bot, chatId);

            await bot.ReceiveAsync(async (bot, update, ct) =>
            {
                if (update.Type == UpdateType.CallbackQuery) // receive all callback queries with inline middleware
                    await InlineMiddleware.HandleAsync(bot, update.CallbackQuery);

            }, (bot, ex, ct) => Console.WriteLine(ex));

            Console.ReadLine();
        }
    }
}
```

# More examples
You can get more examples of handling commands, hooks, creating some forms and components. See [ExampleBot](/ExampleBot) project.

|![Catalog](/assets/catalog.gif)|![Calendar](/assets/calendar.gif)|
|:---:|:--:|
|Catalog component|Calendar component|
|![Tic-Tac-Toe form](/assets/ticTacToe.gif)|![Full Name form](/assets/fullNameForm.gif)|
|Tic-Tac-Toe form|Full Name form|

|<img alt="State buttons" src="/assets/stateButtons.gif" height="400"/>|
|:---:|
|State buttons form|
