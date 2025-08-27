# Telegram Navigation

![.NET 9.0](https://img.shields.io/badge/.NET-9.0-8A2BE2)
[![NuGet Version](https://img.shields.io/nuget/v/TelegramNavigation)](https://www.nuget.org/packages/TelegramNavigation)

Telegram Navigation is .NET library based on [.NET Client for Telegram Bot API](https://github.com/TelegramBots/Telegram.Bot) to enchance creating navigation and multi-level telegram menus experience.

# About navigation

## Introduction

There are two types of buttons in Telegram: `Reply` and `Inline`.

`Reply` buttons are located under the input area. When pressed, a message with the button text is sent. In groups where the bot does not have access to messages, it can still recognize the text sent by the user after pressing the Reply button.

`Inline` buttons are located under the message sent by the bot. They contain `CallbackData` - text information up to 64 bytes long. When the `Inline` button is pressed, the bot receives an Update of the `CallbackQuery` type, containing a message with a button and the specified `CallbackData`.

If everything is clear with `Reply` buttons (it is enough to process the text and perform the appropriate actions), then `Inline` buttons allow you to create complex menus and multi-level navigation. `CallbackQuery` stores a message with a button, which allows you to change its contents and control the interface. To organize such multi-level logic, you need to pass information identifying the component and its arguments inside CallbackData.

## Routing

To do this, use the `Route` class, which contains:

`string Type` - the component type or its name.

`string Path` - the path inside the component, for example "/" for the root, "/moveNext" or "/moveBack" for navigation.

`Dictionary<string, string> Arg` - the component arguments as a dictionary of strings.

Since `CallbackData` can only contain text, `Route` implements the `ToString()` method, which converts the object to a string of the format `"t=Type;p=/…?arg1=v1&arg2=v2"`, and the static Parse method to convert the string back to a Route object.

To simplify the registration and routing of components, the `InlineMiddleware` class is used.

It contains the `HandleAsync(ITelegramBotClient, CallbackQuery)` method, which converts CallbackData to a Route object and matches its Type with the registered handler.

Components are registered via the `RegisterComponent(IInlineQueryComponent)` method, which stores all components in a `ConcurrentDictionary<string, IInlineQueryComponent>` to protect against race conditions.

The `IInlineQueryComponent` interface defines a contract for components. Each component must implement the `HandleQueryAsync(Route, ITelegramBotClient, Message, User)` method, which is called when a button is pressed and which receives a Route object, a bot client, a message with a button, and user information.

As a result, components allow you to manage the contents of a single message, creating multi-level menus.

To support multi-level navigation, the `InlineMiddleware` uses the `InlineNavigationStack` field of type `ConcurrentDictionary<(long, int), Stack<string>>`. The key is a `tuple (chatId, messageId)`, and the value is a stack of string representations of the Route.

To simplify returning to the previous component, there are `GetBackButton` and `AddBackButton` methods. `GetBackButton` creates a button with CallbackData `../`, and `AddBackButton` adds it to the keyboard. The `removePage` parameter allows you to remove the last page from the stack when returning. When processing `../`, `InlineMiddleware` extracts the last route from the `InlineNavigationStack`.

Not all transitions within a component should be saved in the navigation history. The "Forward" and "Back" buttons within one component should not affect the return to the previous component. For this, the `meta` flag argument is used. If the `Route` contains `meta`, the transition is not saved in the history.

## Forms and Inline components

`Forms` are fully dynamic, each step depends on the previous one, and the interface is built on the fly. In forms, we use methods of the `BaseForm` base class, such as `SendForm` to send the form to the user and `SendData` to process the final data. Inside the form, message hooks are connected via `MessageHandler.RegisterHook`, which allows you to respond to user input and update the interface at each step. An example is the full name input form: the last name, first name and patronymic are asked for in sequence, with the buttons "Back", "Skip", and "Confirm" displayed. In the game "Tic Tac Toe", the board and buttons are formed dynamically, and each player’s move updates both the interface and the internal state. Forms are ideal for interactive, context-sensitive user interactions, when step-by-step logic and storing intermediate data are important.

`Inline components` are built around a fixed structure and routes. They implement the `IInlineQueryComponent` and optionally `IInlinePageComponent` interfaces, where the key methods are `InitializeAsync` for the initial message sending, `HandleQueryAsync` for processing `Inline` queries, and `MoveBack`/`MoveNext` for page-by-page navigation. The content changes mainly when switching between pages, and the structure of the buttons is predetermined. An example is type and media catalogs: the buttons and the order of the elements are fixed, the user can scroll pages, but the menu structure does not change. `Inline components` are convenient for implementing catalogs, lists, and menus, where you need to support routing and display data from the database with minimal interactive logic.

Therefore, `forms` provide dynamic, context-sensitive interaction by managing steps and hooks through `BaseForm`, and `Inline components` - persistent, routed navigation through the `IInlineQueryComponent` and `IInlinePageComponent` interfaces. Together, they allow you to build flexible interfaces where the user can fill out complex forms and navigate through catalogs and menus without losing context.

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
### Create page component
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
