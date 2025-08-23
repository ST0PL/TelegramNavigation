# Telegram Navigation

![.NET 9.0](https://img.shields.io/badge/.NET-9.0-8A2BE2)
[![NuGet Version](https://img.shields.io/nuget/v/TelegramNavigation)](https://www.nuget.org/packages/TelegramNavigation)

Telegram Navigation is .NET library based on [.NET Client for Telegram Bot API](https://github.com/TelegramBots/Telegram.Bot) to enchance creating navigation and multi-level telegram menus experience.
# Getting started
### Create inline button
```csharp
var bot = new TelegramBotClient("<YOUR API TOKEN>");

var inlineButton = InlineMiddleware.CreateButton("my button",
    async (route, bot, msg, from ) =>
    {
        await bot.SendMessage(msg.Chat.Id, "\"my button\" pressed!");
        // your logic...
    });

await bot.SendMessage(chatId, "Message with inline button", replyMarkup: inlineButton.Button);
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
        messageHandler.UnregisterHook(msg.Chat.Id, from.Id);
        // your logic...
    });

await bot.ReceiveAsync(async (bot, update, ct) =>
{
    if (update.Type == UpdateType.Message)
        await messageHandler.HandleAsync(bot, update.Message!);
}, (bot, ex, ct) => Console.WriteLine(ex));
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
