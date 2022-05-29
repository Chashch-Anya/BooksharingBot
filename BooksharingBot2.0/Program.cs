using System;
using Telegram.Bot;
using Telegram.Bot.Args;
using Google.Apis.Sheets.v4;
using Google.Apis.Auth.OAuth2;
using System.IO;
using System.Threading;
using Google.Apis.Util.Store;
using Google.Apis.Services;
using Google.Apis.Sheets.v4.Data;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Extensions.Polling;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Exceptions;

namespace BooksharingBot2._0
{
    class Program
    {
        //google sheets
        static string ApplicationName = "GoogleBooksharing";
        static string spreadsheetId = "14iKuowIACacNylNU_CSNu2F64iokBj23vaW8Ikq-bc8";

        //books
        public static Book new_book;

        static string chat_status = null;

        
        //telegram
        // private static string token { get; set; } = "5380616636:AAFndJIw1gDacNr1GE7Hu1rQrl9i6oGA3wU";
        private static string token { get; set; } = "5262431118:AAGb9zvcOGNeApz4fcSXvmOCqp5Rz2gFf7w";
        private static TelegramBotClient client;

        static async Task Main(string[] args)
        {
            string menu = null;

            do
            {
            client = new TelegramBotClient(token);
            GoogleSheetsHelper.Start(ApplicationName, spreadsheetId);

            using var cts = new CancellationTokenSource();

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { }
            };

            client.StartReceiving(
                HandleUpdatesAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken: cts.Token);

            var me = await client.GetMeAsync();
            ConsoleMenu(menu);
           //Console.ReadLine();

            cts.Cancel();
            } while (menu != "3");

        }

        public static void ConsoleMenu(string menu) 
        {
            Console.WriteLine("Настройки:\n1-изменить ссылку на документ\n2-изменить токен для бота\n3-остановить программу");

                menu = Console.ReadLine();

                switch (menu)
                {
                    case "1":
                        Console.WriteLine("Изменение ссылки на документ...\nВведите новую ссылку или id документа:");

                        string link = Console.ReadLine().Replace("https://docs.google.com/spreadsheets/d/", "");
                        string str = link.Substring(link.LastIndexOf('/'));
                        spreadsheetId = link.Substring(0, link.LastIndexOf('/'));
                        Console.WriteLine("Ccылка изменена");

                        break;

                    case "2":
                        Console.WriteLine("Изменение токена бота...\nВведите измененный токен:");
                        token = Console.ReadLine();
                        Console.WriteLine(token);
                        break;
                }

        }

        async static Task HandleUpdatesAsync(ITelegramBotClient client, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message && update.Message?.Text != null)
            {              
                Console.WriteLine($"Пользователь {UserName(update.Message)} ввел {update.Message.Text}");

                _ =  HandleMessage(client, update.Message);
                return;
            }

            if (update.Type == UpdateType.CallbackQuery)
            {
               // Console.WriteLine($"Пользователь {UserName(update.CallbackQuery.Message)} ввел {update.Message.Text}");
                 HandleCallbackQuery(client, update.CallbackQuery);
                return;
            }
        }

        async static Task HandleMessage(ITelegramBotClient client, Message message)
        {
            if (message.Text == "/start")
            {
                ZeroingChat();

                await client.SendTextMessageAsync(message.Chat.Id,
                   $"Привет! Я {client.GetMeAsync().Result.FirstName}\n" +
                   $"/booklist - получить список книг\n" +
                   $"/addbook - добавить книгу\n" +
                   $"/getbook - вернуть книгу\n" +
                   $"/categorylist - список категорий\n" +
                   $"/addcategory - добавить категорию\n" +
                   $"/setting - настройки\n" +
                   $"/keyboard - меню"
                   );
            }
            else

            if (message.Text == "/keyboard")
            {
                ZeroingChat();

                ReplyKeyboardMarkup replyKeyboard = new ReplyKeyboardMarkup(new[]
                  {
                    new KeyboardButton[] { "Список книг", "Добавить книгу"},
                    new KeyboardButton[] { "Вернуть книгу" },
                    new KeyboardButton[] { "Список категорий","Добавить категорию книги" },
                    new KeyboardButton[] { "Настройки" }
                })
                {
                    ResizeKeyboard = true
                };
                await client.SendTextMessageAsync(message.Chat.Id, "Меню", replyMarkup: replyKeyboard);

            }
            else

            if (message.Text == "/booklist" || message.Text == "Список книг")
            {
                ZeroingChat();
                _ = client.SendTextMessageAsync(message.Chat.Id, $"Список книг...");

                Book[] book = GoogleSheetsHelper.ReadSheets();
                for (int i = 0; i < book.Length; i++)
                {
                    string str = $"{ book[i].name}\n" +
                        $"Автор: {book[i].author}\n" +
                        $"Категория: {book[i].category}\n" +
                        $"Владелец: {book[i].owner}\n" +
                        $"Место хранения: {book[i].place}";

                    if (book[i].reader == "")
                    {
                        var keyboardMarkup = new InlineKeyboardMarkup(new InlineKeyboardButton[]
                        {
                          InlineKeyboardButton.WithCallbackData($"Взять", $"0-{i.ToString()}")
                            // ,InlineKeyboardButton.WithUrl($"Ссылка", $"{test[i].link}")
                        });
                        await client.SendTextMessageAsync(message.Chat.Id, $"{str}\nКнига доступна на текущий момент", replyMarkup: keyboardMarkup);
                    }
                    else if (book[i].reader == UserName(message))
                    {
                        var keyboardMarkup = new InlineKeyboardMarkup(new InlineKeyboardButton[] { InlineKeyboardButton.WithCallbackData($"Вернуть книгу", $"1-{i.ToString()}") });
                        await client.SendTextMessageAsync(message.Chat.Id, $"{str}\nКнига находится у вас", replyMarkup: keyboardMarkup);
                    }
                    else
                    {
                        await client.SendTextMessageAsync(message.Chat.Id, $"{str}\nКнига на руках у {book[i].reader}");
                    }
                }
            }
            else

            if (message.Text == "/addbook" || message.Text == "Добавить книгу")
            {
                ZeroingChat();
                chat_status = "addbook_category";
                var test = GoogleSheetsHelper.ArrayCategory();
                var keyboardMarkup = new InlineKeyboardMarkup(GetInlineKeyboard(test));
                await client.SendTextMessageAsync(message.Chat.Id, "Выберите категорию книги", replyMarkup: keyboardMarkup);
            }
            else

            if (message.Text == "/getbook" || message.Text == "Вернуть книгу")
            {
                ZeroingChat();
                Book[] book = GoogleSheetsHelper.ReadSheets();
                for (int i = 0; i < book.Length; i++)
                {
                    string str = $"{ book[i].name}\n" +
                        $"Автор: {book[i].author}\n" +
                        $"Категория: {book[i].category}\n" +
                        $"Владелец: {book[i].owner}\n" +
                        $"Место хранения: {book[i].place}";

                    if (book[i].reader == UserName(message))
                    {
                        var keyboardMarkup = new InlineKeyboardMarkup(new InlineKeyboardButton[] { InlineKeyboardButton.WithCallbackData($"Вернуть книгу", $"1-{i.ToString()}") });
                        await client.SendTextMessageAsync(message.Chat.Id, $"{str}\nКнига находится у вас", replyMarkup: keyboardMarkup);
                    }
                }
            }
            else

             if (message.Text == "/categorylist" || message.Text == "Список категорий")
            {
                ZeroingChat();
                _ = client.SendTextMessageAsync(message.Chat.Id, $"Список категорий книг:\n{GoogleSheetsHelper.ReadCategory()}");
            }
            else

            if (message.Text == "/addcategory" || message.Text == "Добавить категорию книги")
            {
                ZeroingChat();

                chat_status = "addcategory";
                _ = client.SendTextMessageAsync(message.Chat.Id, "Введите название категории книги");
            }
            else

            if (message.Text == "/setting" || message.Text == "Настройки")
            {
                ZeroingChat();
            }
            else DataChange(message);

        }

        private static void ZeroingChat()
        {
            chat_status = null;
        }

        private static string UserName(Message message)
        {
            string user;
            if (message.Chat.Username == null) { user = message.Chat.FirstName; }
            else { user = "@"+message.Chat.Username; }
            return user;
        }

        private static void DataChange(Message message)
        {
            switch (chat_status)
            {
                case "addcategory":
                    _ = client.SendTextMessageAsync(message.Chat.Id, "Добавление категории......");
                    GoogleSheetsHelper.AddCategory(message.Text);
                    _ = client.SendTextMessageAsync(message.Chat.Id, $"Категория '{message.Text}' успешно добавлена");
                    ZeroingChat();
                    break;

                case "addbook_name":
                    new_book.name = message.Text;
                    chat_status = "addbook_author";
                    _ = client.SendTextMessageAsync(message.Chat.Id, "Введите автора книги...");

                    break;

                case "addbook_author":
                    new_book.author = message.Text;
                    chat_status = "addbook_place";
                    _ = client.SendTextMessageAsync(message.Chat.Id, "Введите место хранения книги...");
                    break;

                case "addbook_place":
                    new_book.place = message.Text;
                    chat_status = "addbook_link";
                    _ = client.SendTextMessageAsync(message.Chat.Id, "Введите ссылку на книгу...");
                    break;

                case "addbook_link":
                    new_book.link = message.Text;
                    new_book.owner = UserName(message);

                    GoogleSheetsHelper.AddBook(new_book);
                    _ = client.SendTextMessageAsync(message.Chat.Id, $"Книга '{new_book.name}' успешно добавлена");
                    chat_status = null;
                    break;
            }
        }

        private static InlineKeyboardButton[][] GetInlineKeyboard(string[] stringArray)
        {
            var keyboardInline = new InlineKeyboardButton[stringArray.Length][];
            for (var i = 0; i < stringArray.Length; i++)
            {
                keyboardInline[i] = new InlineKeyboardButton[] {
                    InlineKeyboardButton.WithCallbackData(stringArray[i], stringArray[i]), };
            }
            return keyboardInline;
        }

        static void HandleCallbackQuery(ITelegramBotClient client, CallbackQuery callbackQuery)
        {
            Console.WriteLine($"Пользователь {UserName(callbackQuery.Message)} нажал {callbackQuery.Data.ToString()}");

            if (chat_status == "addbook_category")
            {
                new_book = new Book();
                new_book.category = callbackQuery.Data.ToString();
                chat_status = "addbook_name";
                _ = client.SendTextMessageAsync(callbackQuery.Message.Chat.Id, "Введите название книги...");
            }
            else
            {
                string[] str = callbackQuery.Data.ToString().Split("-");
                int num;
                Console.WriteLine(callbackQuery.Data.ToString());
                //тест
                if (int.TryParse(str[1], out num) && str[0] == "1")
                {
                    if (GoogleSheetsHelper.CheckBook(num))
                    {
                        num += 2;
                        GoogleSheetsHelper.ReturnBook(num);
                        _ = client.SendTextMessageAsync(callbackQuery.Message.Chat.Id, $"Книга возвращена\nУбедитесь, что книга возвращена на место");
                    }

                }
                else

                if (int.TryParse(str[1], out num) && str[0] == "0")
                {
                    if (!GoogleSheetsHelper.CheckBook(num))
                    {
                        num += 2;
                        GoogleSheetsHelper.GetBook(num, UserName(callbackQuery.Message));
                        _ = client.SendTextMessageAsync(callbackQuery.Message.Chat.Id, $"Выбранная книга успешно закреплена за вами.\nПросим с уважением относиться к коллегам и стараться возвращать книги в разумные сроки");
                    }
                    else
                    {
                        _ = client.SendTextMessageAsync(callbackQuery.Message.Chat.Id, $"Выбранная книга занята");
                    }
                };

            }
        }

        static Task HandleErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                => $"Ошибка Telegram API:\n{apiRequestException.ErrorCode}\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
             return Task.CompletedTask;
        }
    }
}
