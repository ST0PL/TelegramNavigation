using System.Collections.Concurrent;
using System.Reflection;

namespace TelegramNavigation.Routing
{
    /// <summary>
    /// Inline query router class
    /// </summary>
    public static class InlineMiddleware
    {
        /// <summary>
        /// Represents an inline button with handler associated with hook ID
        /// </summary>
        /// <param name="button"></param>
        /// <param name="hookId"></param>
        public class InlineButton(InlineKeyboardButton button, string hookId)
        {
            /// <summary>
            /// <see cref="InlineKeyboardButton"/> instance
            /// </summary>
            public InlineKeyboardButton Button => button;
            /// <summary>
            /// Inline hook id
            /// </summary>
            public string HookId => hookId;
        }

        class InlineHook(DateTime creationDate, InlineQueryHook handler)
        {
            public DateTime CreationDate => creationDate;
            public InlineQueryHook Handler => handler;
        }

        /// <summary>
        /// Registered components
        /// </summary>
        private static readonly Dictionary<string, IInlineQueryComponent> Components = new();

        /// <summary>
        /// Registered inline hooks
        /// </summary>
        private static readonly ConcurrentDictionary<string, InlineHook> InlineHooks = new();

        /// <summary>
        /// Callback stacks for menus with navigation logic associated with chat and message ids
        /// </summary>
        private static readonly ConcurrentDictionary<(long, int), Stack<string>> InlineNavigationStack = new();
        /// <summary>
        /// Page controllers for menus with pagination logic associated with chat and message ids
        /// </summary>
        private static readonly ConcurrentDictionary<(long, int), Stack<PageController>> PageStates = new ConcurrentDictionary<(long, int), Stack<PageController>>();


        /// <summary>
        /// Handle callback queries
        /// </summary>
        /// <param name="botClient">Telegram bot client</param>
        /// <param name="query"><see cref="CallbackQuery"/> instance</param>
        /// <returns></returns>
        public static async Task HandleAsync(ITelegramBotClient botClient, CallbackQuery query)
        {
            try
            {
                // Always answer the query

                await botClient.AnswerCallbackQuery(query.Id);

                // Check if we must skip the route processing

                if (query.Data == "none")
                    return;

                #region Route handling
                // parse route from callback data
                Route? queryRoute = Route.Parse(query.Data);

                // Add route to the navigation stack


                if (queryRoute.Type.Equals("hook"))
                    await InlineHooks[queryRoute.Args["id"]].Handler.Invoke(queryRoute, botClient, query.Message, query.From);
                else
                {
                    // Check if route path is "return path" if it, then taking back route from call stacks

                    queryRoute = queryRoute.Path.Equals("../") ? GetBackRoute(query.Message.Chat.Id, query.Message.Id, bool.Parse(queryRoute.Args["removePage"])) : queryRoute;
                    
                    // Save route if no "meta" argument

                    if (!queryRoute?.Args?.ContainsKey("meta") ?? true)
                        SaveRoute(query.Message.Chat.Id, query.Message.Id, queryRoute);

                    // Call the "HandleQueryAsync" method of IInlineQueryComponent interface according to "type" route argument

                    if (Components.TryGetValue(queryRoute.Type, out var handler))
                    {
                        await handler.HandleQueryAsync(
                            queryRoute,
                            botClient,
                            query.Message,
                            query.From);
                    }
                }
                #endregion

            }
            catch (Exception ex) when
                (ex is KeyNotFoundException or NullReferenceException or InvalidOperationException)
            {
                await botClient.DeleteMessage(query.Message.Chat.Id, query.Message.Id);
            }
        }

        #region navigation
        /// <summary>
        /// Initialize and sends <see cref="IInlineQueryComponent"/> message
        /// </summary>
        /// <param name="route"></param>
        /// <param name="botClient"></param>
        /// <param name="chatId"></param>
        /// <param name="messageThreadId"></param>
        /// <returns><c>true</c> if component message was successfully initialized, otherwise <c>false</c></returns>
        public static async Task<bool> SendComponent(Route route, ITelegramBotClient botClient, long chatId, int? messageThreadId = null)
        {
            ArgumentNullException.ThrowIfNull(route);

            if (Components.TryGetValue(route.Type, out var component))
            {
                var msg = await component.InitializeAsync(route, botClient, chatId, messageThreadId);
                if (msg != null)
                {
                    if (!(route.Args?.ContainsKey("meta") ?? false))
                        SaveRoute(msg.Chat.Id, msg.Id, route);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Navigates to a component specified in a <see cref="Route"/> instance
        /// </summary>
        /// <param name="route"></param>
        /// <param name="botClient"></param>
        /// <param name="componentMessage"></param>
        /// <param name="from"></param>
        /// <returns><c>true</c>if navigation successfull, otherwise <c>false</c></returns>
        public static async Task<bool> NavigateTo(
            Route route,
            ITelegramBotClient botClient,
            Message componentMessage,
            User from)
        {
            ArgumentNullException.ThrowIfNull(route);

            if (Components.TryGetValue(route.Type, out var component))
            {
                if (!route.Args?.ContainsKey("meta") ?? false)
                    SaveRoute(componentMessage.Chat.Id, componentMessage.Id, route);
                await component.HandleQueryAsync(route, botClient, componentMessage, from);
                return true;
            }
            return false;
        }

        private static void SaveRoute(long chatId, int messageId, Route route)
        {
            if(!InlineNavigationStack.TryGetValue((chatId, messageId), out var stack))
            {
                stack = new();
                InlineNavigationStack[(chatId, messageId)] = stack;
            }
            stack.Push(route.ToString());
        }
        /// <summary>
        /// Removes navigation stack associated with chat and message ids
        /// </summary>
        /// <param name="chatId"></param>
        /// <param name="messageId"></param>
        public static void RemoveInlineStack(long chatId, int messageId)
            => InlineNavigationStack.TryRemove((chatId, messageId), out _);


        /// <summary>
        /// Returns the last route in the navigation stack
        /// </summary>
        /// <param name="chatId"></param>
        /// <param name="messageId"></param>
        /// <param name="removePage">Pop top page of the pages stack</param>
        /// <returns>The last route in the navigation stack</returns>
        public static Route? GetBackRoute(long chatId, int messageId, bool removePage = false)
        {
            Route? backRoute = null;
            if (InlineNavigationStack.TryGetValue((chatId, messageId), out var navigationStack))
            {
                if (removePage && PageStates.TryGetValue((chatId, messageId), out var pagesStack) && pagesStack.Count > 0)
                    pagesStack.Pop();
                _ = navigationStack.Pop();
                string prevRoute = navigationStack.Pop();
                backRoute = Route.Parse(prevRoute);
            }
            return backRoute;
        }
        #endregion

        #region buttons
        /// <summary>
        /// Returns a button that opens the previous page
        /// </summary>
        /// <param name="title"></param>
        /// <param name="chatId"></param>
        /// <param name="messageId"></param>
        /// <param name="removePage"></param>
        /// <returns>Button that opens the previous page</returns>
        public static InlineKeyboardButton? GetBackButton(string title, long chatId, int messageId, bool removePage = false)
        {
            InlineKeyboardButton? backButton = null;
            if (InlineNavigationStack.TryGetValue((chatId, messageId), out var stack) && stack is { Count: > 1 })
                backButton = new InlineKeyboardButton(title, new Route(string.Empty, "../", new() { ["removePage"] = removePage.ToString()}).ToString());
            return backButton;
        }
        /// <summary>
        /// Returns a button that close current component using the standard component
        /// </summary>
        /// <param name="title"></param>
        /// <returns>Button that close current component</returns>
        public static InlineKeyboardButton GetCloseButton(string title)
            => new InlineKeyboardButton(title, new Route("standard", "/close", new() { ["meta"] = string.Empty }).ToString());

        /// <summary>
        /// The method creates a two-dimensional list with Back and Close buttons, which placed in separate rows.
        /// If the call stack does not contain any previous routes, the Back button will not be included.
        /// </summary>
        /// <param name="backButtonTitle"></param>
        /// <param name="closeButtonTitle"></param>
        /// <param name="chatId"></param>
        /// <param name="messageId"></param>
        /// <param name="removePage">if true, the last page in stack will be removed</param>
        /// <returns>Two-dimensional list with back and close buttons</returns>

        public static List<List<InlineKeyboardButton>> GetBackAndCloseButtons(
            string backButtonTitle,
            string closeButtonTitle,
            long chatId,
            int messageId,
            bool removePage = false)
        {
            List<List<InlineKeyboardButton>> buttons = new();
            if (GetBackButton(backButtonTitle, chatId, messageId, removePage) is { } backButton)
                buttons.Add([backButton]);
            buttons.Add([GetCloseButton(closeButtonTitle)]);
            return buttons;
        }

        /// <summary>
        /// Adds a close button to a new <see cref="InlineKeyboardMarkup"/> row
        /// </summary>
        /// <param name="markup"></param>
        /// <param name="title"></param>
        /// <returns><see cref="InlineKeyboardMarkup"/></returns>
        public static InlineKeyboardMarkup AddCloseButton(InlineKeyboardMarkup markup, string title)
            => markup.AddNewRow(GetCloseButton(title));

        /// <summary>
        /// If navigation stack has back route adds a back button to a new <see cref="InlineKeyboardMarkup"/> row, otherwise does nothing
        /// </summary>
        /// <param name="markup"></param>
        /// <param name="title"></param>
        /// <param name="chatId"></param>
        /// <param name="messageId"></param>
        /// <returns><see cref="InlineKeyboardMarkup"/></returns>
        public static InlineKeyboardMarkup AddBackButton(InlineKeyboardMarkup markup, string title, long chatId, int messageId)
        {
            if (GetBackButton(title, chatId, messageId) is { } backButton)
                markup.AddNewRow(backButton);
            return markup;
        }

        /// <summary>
        /// Creates a <see cref="InlineButton"/> instance
        /// </summary>
        /// <param name="title">Button title</param>
        /// <param name="handler">Button handler</param>
        /// <param name="args">Route arguments</param>
        /// <returns></returns>
        public static InlineButton CreateButton(
            string title,
            InlineQueryHook handler,
            Dictionary<string, string>? args = null)
        {
            var hookId = RegisterHook(handler);

            if (args is null)
                args = new();
            args.Add("id", hookId);
            args.Add("meta", string.Empty);

            return new InlineButton(
                new InlineKeyboardButton(title, new Route("hook", string.Empty, args).ToString()),
                hookId);
        }

        #endregion

        #region hooks

        /// <summary>
        /// Register a new inline hook
        /// </summary>
        /// <param name="handler"></param>
        /// <returns>Hook identitifer</returns>
        public static string RegisterHook(InlineQueryHook handler)
        {
            var hookId = GenerateId(8);
            InlineHooks[hookId] = new InlineHook(DateTime.UtcNow, handler);
            return hookId;
            
        }
        /// <summary>
        /// Remove an inline hook by id
        /// </summary>
        /// <param name="id"></param>
        public static void UnregisterHook(string id)
            => InlineHooks.TryRemove(id, out _);

        #endregion

        #region pages
        /// <summary>
        /// Creates PageController instance and save it to the pages stack associated with chat id and message id
        /// </summary>
        /// <param name="chatId"></param>
        /// <param name="messageId"></param>
        /// <param name="sourceLength"></param>
        /// <param name="elementsPerPage"></param>
        /// <param name="title"></param>
        /// <returns>PageController instance</returns>
        public static PageController CreatePage(long chatId, int messageId, int sourceLength, int elementsPerPage, string? title = null)
        {
            PageController pageController = new PageController(sourceLength, elementsPerPage, title);
            if(!PageStates.TryGetValue((chatId, messageId), out var pagesStack))
            {
                pagesStack = new Stack<PageController>();
                PageStates.TryAdd((chatId, messageId), pagesStack);
            }
            pagesStack.Push(pageController);
            return pageController;
        }

        /// <summary>
        /// Returns last page in stack associated with the chat id and message id
        /// </summary>
        /// <param name="chatId"></param>
        /// <param name="messageId"></param>
        /// <returns>Last page in stack associated with the chat id and message id</returns>
        public static PageController? GetPage(long chatId, int messageId)
        {
            PageStates.TryGetValue((chatId, messageId), out var pagesStack);
            return pagesStack?.Peek();
        }

        /// <summary>
        /// Returns last page in stack with specified name associated with the chat and message ids
        /// </summary>
        /// <param name="chatId"></param>
        /// <param name="messageId"></param>
        /// <param name="title"></param>
        /// <returns>Last page in stack with specified name associated with the chat and message ids</returns>
        public static PageController? GetPage(long chatId, int messageId, string title)
        {
            PageStates.TryGetValue((chatId, messageId), out var pagesStack);
            return pagesStack?.Where(p => p.Title == title)?.FirstOrDefault();
        }

        /// <summary>
        /// Removes the pages stack associated with chat and message ids
        /// </summary>
        /// <param name="chatId"></param>
        /// <param name="messageId"></param>
        public static void RemovePagesStack(long chatId, int messageId)
            => PageStates.Remove((chatId, messageId), out _);
        #endregion

        #region components

        /// <summary>
        /// Register <see cref="IInlineQueryComponent"/> component with specified <see cref="Route"/> type
        /// </summary>
        /// <param name="inlineQueryComponent"></param>
        public static void RegisterComponent(IInlineQueryComponent inlineQueryComponent)
        {
            var attribute = inlineQueryComponent.GetType().GetCustomAttribute<InlineComponentAttribute>() ??
                throw new NullReferenceException("InlineComponentAttribute not found");

            Components.Add(attribute.Name, inlineQueryComponent);
        }

        /// <summary>
        /// Unregister <see cref="IInlineQueryComponent"/> component by specified <see cref="Route"/> type
        /// </summary>
        /// <param name="type"></param>
        public static void UnregisterComponent(string type)
            => Components.Remove(type);

        #endregion

        /// <summary>
        /// Generates inline hook id
        /// </summary>
        /// <param name="length"></param>
        /// <returns>String identifier</returns>
        private static string GenerateId(int length)
        {
            byte[] buffer = new byte[length];
            Random.Shared.NextBytes(buffer);
            return string.Join("", buffer.Select(b => b.ToString("x")));
        }
    }
}
