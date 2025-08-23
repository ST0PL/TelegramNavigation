using ExampleBot.Handlers;

namespace ExampleBot
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var bot = new TelegramBotClient("<YOUR API TOKEN>");
            bot.StartReceiving(new UpdateHandler());
            Console.ReadLine();
        }
    }
}
