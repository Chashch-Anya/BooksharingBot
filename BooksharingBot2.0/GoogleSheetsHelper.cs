using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace BooksharingBot2._0
{
    class GoogleSheetsHelper
    {
        public static UserCredential credential;
        static string[] Scopes = { SheetsService.Scope.Spreadsheets };
        static string spreadsheetId = null;
        static string ApplicationName = null;

        public static void Start(string name, string Id)
        {
            spreadsheetId = Id;
            ApplicationName = name;
        }

        //1 - авторизация
        public static SheetsService AuthorizeGoogleApp()
        {
            using (var stream =
                new FileStream("client_secret.json", FileMode.Open, FileAccess.Read))
            {

                string credPath = System.Environment.GetFolderPath(
                    System.Environment.SpecialFolder.Personal);
                credPath = Path.Combine(credPath, ".credentials/sheets.googleapis.com-dotnet-quickstart.json");

                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    clientSecrets: GoogleClientSecrets.Load(stream).Secrets,
                    scopes: Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }

            var service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            return service;
        }

        //2 - Получение списка книг с информацией о владельце и месте хранения, а также о доступности на текущий момент
        //ТЕСТОВАЯ ФУНКЦИЯ
        public static Book[] ReadSheets()
        {
            var service = AuthorizeGoogleApp();
            String range = "Книги!A2:G";
            SpreadsheetsResource.ValuesResource.GetRequest request =
                    service.Spreadsheets.Values.Get(spreadsheetId, range);

            ValueRange response = request.Execute();
            IList<IList<Object>> values = response.Values;
            Book[] book = new Book[values.Count];

            if (values != null && values.Count > 0)
            {
                int i = 0;
                foreach (var row in values)
                {
                    Book b = new Book();
                    b.category = row[0].ToString();
                    b.name = row[2].ToString();
                    b.author = row[1].ToString();
                    b.link = row[3].ToString();
                    b.owner = row[4].ToString();
                    b.place = row[5].ToString();
                    if (row.Count < 7)
                    {
                     b.reader = "";
                    }
                   else
                   b.reader = row[6].ToString();
                    book[i]= b;
                    i++;
                }
            }
            else
            {
            }
            return book;
        }

        //2 - Получение списка книг с информацией о владельце и месте хранения, а также о доступности на текущий момент
        public static string[,] BookArray()
        {
            var service = AuthorizeGoogleApp();
            String range = "Книги!A2:G";
            SpreadsheetsResource.ValuesResource.GetRequest request =
                    service.Spreadsheets.Values.Get(spreadsheetId, range);

            ValueRange response = request.Execute();
            IList<IList<Object>> values = response.Values;

            string[,] category_list = new string[values.Count,2];

            if (values != null && values.Count > 0)
            {
                int i = 0;
                foreach (var row in values)
                {
                    category_list[i,0] = $"{row[2].ToString()}\n" +
                        $"Автор: {row[1].ToString()}\n" +
                        $"Категория: {row[0].ToString()}\n" +
                        $"Владелец: {row[4].ToString()}\n" +
                        $"Место хранения: {row[5].ToString()}";
                    if (row.Count < 7)
                    {
                        category_list[i,0] += "\nКнига доступна на текущий момент";
                    }
                    else
                        category_list[i,0] += "\nКнига на руках у " + row[6].ToString();
                    category_list[i, 1] = row[3].ToString();
                    i++;
                }
            }
            else
            {
                category_list = new string[1,0];
                category_list[0,0] = "Список пуст";
            }

            return category_list;
        }

        //3 - Список всех категорий
        public static string ReadCategory()
        {
            var service = AuthorizeGoogleApp();
            String range = "Категории!A2:A";
            SpreadsheetsResource.ValuesResource.GetRequest request =
                    service.Spreadsheets.Values.Get(spreadsheetId, range);

            ValueRange response = request.Execute();
            IList<IList<Object>> values = response.Values;
            string category = null;
            if (values != null && values.Count > 0)
            {
                int i = 1;
                foreach (var row in values)
                {
                    category += i+") ";
                    category += row[0].ToString() + "\n";
                    i++;
                }
            }
            else
            {
                category = "Список пуст";
            }
            return category;
        }

        //3 - Список всех категорий(массив)
        public static string[] ArrayCategory()
        {
            var service = AuthorizeGoogleApp();
            String range = "Категории!A2:A";
            SpreadsheetsResource.ValuesResource.GetRequest request =
                    service.Spreadsheets.Values.Get(spreadsheetId, range);

            ValueRange response = request.Execute();
            IList<IList<Object>> values = response.Values;

            string[] category_list = new string[values.Count];

            if (values != null && values.Count > 0)
            {
                int i = 0;
                foreach (var row in values)
                {
                    category_list[i] += row[0].ToString();
                    i++;
                }
            }
            else
            {
                category_list = new string[1];
                category_list[0] = "Список пуст";
            }

            return category_list;
        }



        //4 - Добавление книги в таблицу
        public static void AddBook(Book book)
        {
            var service = AuthorizeGoogleApp();

            var range = $"Книги!A:F";
            var valueRange = new ValueRange();

            var oblist = new List<object>() { book.category, book.author,book.name,book.link,book.owner,book.place };
            valueRange.Values = new List<IList<object>> { oblist };

            var appendRequest = service.Spreadsheets.Values.Append(valueRange, spreadsheetId, range);
            appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
            var appendReponse = appendRequest.Execute();
        }

        

        //5 - добавление категории
        public static void AddCategory( string s)
        {
            var service = AuthorizeGoogleApp();
            String range = "Категории!A2:A";
            SpreadsheetsResource.ValuesResource.GetRequest request =
                    service.Spreadsheets.Values.Get(spreadsheetId, range);

            ValueRange response = request.Execute();
            IList<IList<Object>> values = response.Values;

            var category = new List<object>();

            if (values != null && values.Count > 0)
            {
                foreach (var row in values)
                {
                    category.Add(row[0]);
                }
            }
            else
            {

            }
            category.Add(s);
            category.Sort();

            //обновление - посмотреть другие способы обновить столбец данных
            int i = 2;
            var valueRange = new ValueRange();

            for (int n = 0; n < category.Count; n++)
            {
                range = $"Категории!A{i}";
                var oblist2 = new List<object>() { category[n] };
                valueRange.Values = new List<IList<object>> { oblist2 };

                var updateRequest = service.Spreadsheets.Values.Update(valueRange, spreadsheetId, range);
                updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                var appendReponse = updateRequest.Execute();
                i++;
            }
        }

        //6 - Взятие свободной книги с записью текущего пользователя в колонку Читатель и текущей даты в колонку Дата взятия
        public static void GetBook(int i, string user)
        {
            var service = AuthorizeGoogleApp();

            //добавить проверку что книга не занята

            var range = $"Книги!G{i}:H{i}";
            var valueRange = new ValueRange();

            DateTime thisDay = DateTime.Today;

            var oblist = new List<object>() { user, thisDay.ToShortDateString() };
            valueRange.Values = new List<IList<object>> { oblist };

            var updateRequest = service.Spreadsheets.Values.Update(valueRange, spreadsheetId, range);
            updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            var appendReponse = updateRequest.Execute();
        }

        public static bool CheckBook(int num)
        {

            num += 2;
            var service = AuthorizeGoogleApp();
            String range = $"Книги!G{num}";

            SpreadsheetsResource.ValuesResource.GetRequest request =
                    service.Spreadsheets.Values.Get(spreadsheetId, range);

            ValueRange response = request.Execute();

            IList<IList<Object>> values = response.Values;

            if (values != null && values.Count > 0)
            {
                return true;
            }
            else return false;
        }


        //Вовзрат книги(testing)
        public static void ReturnBook(int i)
        {
            var service = AuthorizeGoogleApp();

            var range = $"Книги!G{i}:H{i}";
            var valueRange = new ValueRange();

            DateTime thisDay = DateTime.Today;

            var oblist = new List<object>() { "", "" };
            valueRange.Values = new List<IList<object>> { oblist };

            var updateRequest = service.Spreadsheets.Values.Update(valueRange, spreadsheetId, range);


            updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            var appendReponse = updateRequest.Execute();
        }


    }
}
