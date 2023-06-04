using System.Threading;
using System;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using HotelReservation;
using System.Collections.Generic;
using System.Linq;
using static HotelReservation.Program;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Exceptions;

namespace HotelReservation
{
    class Program
    {
        public abstract class HotelRoom
        {
            public int RoomNumber { get; set; }
            public string RoomType { get; set; }
            public double RoomPrice { get; set; }
            public bool IsAvailable { get; set; }

            public virtual string GetRoomDetails()
            {
                return $"Номер {RoomNumber} ({RoomType}): {RoomPrice}$ ";
            }
        }

        // Классы для каждого типа номера:
        public class StandardRoom : HotelRoom
        {
            public StandardRoom(int number)
            {
                RoomNumber = number;
                RoomType = "Стандарт";
                RoomPrice = 49.90;
                IsAvailable = true;
            }
        }
        public class LuxuryRoom : HotelRoom
        {
            public LuxuryRoom(int number)
            {
                RoomNumber = number;
                RoomType = "Люкс";
                RoomPrice = 119.99;
                IsAvailable = true;
            }
        }
        public class ApartmentRoom : HotelRoom
        {
            public ApartmentRoom(int number)
            {
                RoomNumber = number;
                RoomType = "Апартаменты";
                RoomPrice = 239.99;
                IsAvailable = true;
            }
        }
        // Класс RoomFactory для создания номеров:
        public class RoomFactory
        {
            public static HotelRoom CreateRoom(string roomType, int roomNumber)
            {
                switch (roomType.ToLower())
                {
                    case "standard":
                        return new StandardRoom(roomNumber);
                    case "luxury":
                        return new LuxuryRoom(roomNumber);
                    case "apartment":
                        return new ApartmentRoom(roomNumber);
                    default:
                        throw new ArgumentException("Не существует такого номера");
                }
            }
        }
        // Класс Hotel для управления номерами и бронирования:
        public class Hotel
        {
            private List<HotelRoom> rooms;

            public Hotel(int numberOfRooms)
            {
                rooms = new List<HotelRoom>();

                for (int index = 1; index <= numberOfRooms; ++index)
                {
                    rooms.Add(RoomFactory.CreateRoom("standard", index));
                }
            }
            public void AddRooms(string roomType, int numberOfRooms)
            {
                for (int index = 1; index <= numberOfRooms; ++index)
                {
                    rooms.Add(RoomFactory.CreateRoom(roomType, rooms.Count + 1));
                }
            }
            public List<HotelRoom> GetAvailableRooms()
            {
                return rooms.Where(room => room.IsAvailable).ToList();
            }
            public List<HotelRoom> GetBookedRooms()
            {
                return rooms.Where(room => !room.IsAvailable).ToList();
            }
            public bool BookRoom(int roomNumber)
            {
                HotelRoom room = rooms.FirstOrDefault(rooms => rooms.RoomNumber == roomNumber && rooms.IsAvailable);

                if (room != null)
                {
                    room.IsAvailable = false;
                    return true;
                }

                return false;
            }
            public bool CancelBooking(int roomNumber)
            {
                HotelRoom room = rooms.FirstOrDefault(rooms => rooms.RoomNumber == roomNumber && !rooms.IsAvailable);

                if (room != null)
                {
                    room.IsAvailable = true;
                    return true;
                }

                return false;
            }
        }
        class TelegramBot
        {
            public void Bot()
            {
                var client = new TelegramBotClient("5833810875:AAGI6M3127ROdjsHdDdDcRkMWoLHKSHV4to");
                client.StartReceiving(Update, Error);
            }
            async static Task Update(ITelegramBotClient botClient, Update update, CancellationToken token)
            {
                var message = update.Message;
                var replyMarkup = new ForceReplyMarkup();
                var updates = await botClient.GetUpdatesAsync();
                Hotel hotel = new Hotel(10); // Создание отеля с 10 стандартными номерами
                hotel.AddRooms("luxury", 3);
                hotel.AddRooms("apartment", 2); // Добавление 3 номеров класса Люкс и 2 номера класса апартаменты
                List<HotelRoom> bookedRooms = hotel.GetBookedRooms();
                List<HotelRoom> availableRooms = hotel.GetAvailableRooms();
                if (message.Text != null)
                {
                    if (message.Text.ToLower().Contains("/start"))
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Здравствуйте! Я - бот отеля \"Голубая Лагуна\", который поможет вам с выбором подходящего номера в отеле. " +
                            "Вот список доступных комманд:\n1./list - Показать все номера отеля.\n2./reserve - Забронировать номер." +
                            "\n3./alreadyBusy - Показать список уже забронированых номеров отеля.\n4./description - Описание номера." +
                            "\n5./cancel - Отменить бронирование номера\n6./help - Показать все команды");
                        return;
                    }
                    else if (message.Text.ToLower().Contains("list"))
                    {
                        availableRooms = hotel.GetAvailableRooms(); // Получение списка номеров
                        foreach (HotelRoom room in availableRooms)
                        {
                            await botClient.SendTextMessageAsync(message.Chat.Id, room.GetRoomDetails()); // Вывод информации о каждом номере
                        }
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Отель предоставляет 15 различных номеров для наших гостей: 10 класса стандарт, " +
                            "3 класса Люкс и 2 класса Апартаменты.\n/help");
                    }
                    else if (message.Text == "/alreadyBusy")
                    {
                        for (int x = 1; x < 16; ++x)
                        {
                            hotel.BookRoom(3);
                            hotel.BookRoom(11);
                            hotel.BookRoom(15);
                            if (hotel.CancelBooking(x) == false)
                            {
                                await botClient.SendTextMessageAsync(message.Chat.Id, $"Номер " + x +  " не забронирован");
                            }
                            else
                            {
                                await botClient.SendTextMessageAsync(message.Chat.Id, $"Номер " + x + " уже занят");
                            }
                            await botClient.SendTextMessageAsync(message.Chat.Id, "/help - Показать все команды");
                        }
                        foreach (HotelRoom room in bookedRooms)
                        {
                            await botClient.SendTextMessageAsync(message.Chat.Id, room.GetRoomDetails()); // Вывод информации о каждом забронированном номере
                        }
                    }
                    else if (message.Text == "/reserve")
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Напишите #число - номер, который хотите забронировать (Например: #5)");
                    }
                    else if (message.Text == "#1")
                    {
                        hotel.BookRoom(1);
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Вы забронировали 2 номер отеля. Помните - бронь действует ограниченное время, поэтому вам нужно " +
                            "поскорее оплатить ваш номер. Хорошего отдыха!");
                    }
                    else if (message.Text == "#2")
                    {
                        hotel.BookRoom(2);
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Вы забронировали 2 номер отеля. Помните - бронь действует ограниченное время, поэтому вам нужно " +
                            "поскорее оплатить ваш номер. Хорошего отдыха!");
                    }
                    else if (message.Text == "#3")
                    {
                        hotel.BookRoom(3);
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Вы забронировали 3 номер отеля. Помните - бронь действует ограниченное время, поэтому вам нужно " +
                            "поскорее оплатить ваш номер. Хорошего отдыха!");
                    }
                    else if (message.Text == "#4")
                    {
                        hotel.BookRoom(4);
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Вы забронировали 4 номер отеля. Помните - бронь действует ограниченное время, поэтому вам нужно " +
                            "поскорее оплатить ваш номер. Хорошего отдыха!");
                    }
                    else if (message.Text == "#5")
                    {
                        hotel.BookRoom(5);
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Вы забронировали 5 номер отеля. Помните - бронь действует ограниченное время, поэтому вам нужно " +
                            "поскорее оплатить ваш номер. Хорошего отдыха!");
                    }
                    else if (message.Text == "#6")
                    {
                        hotel.BookRoom(6);
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Вы забронировали 6 номер отеля. Помните - бронь действует ограниченное время, поэтому вам нужно " +
                            "поскорее оплатить ваш номер. Хорошего отдыха!");
                    }
                    else if (message.Text == "#7")
                    {
                        hotel.BookRoom(7);
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Вы забронировали 7 номер отеля. Помните - бронь действует ограниченное время, поэтому вам нужно " +
                            "поскорее оплатить ваш номер. Хорошего отдыха!");
                    }
                    else if (message.Text == "#8")
                    {
                        hotel.BookRoom(8);
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Вы забронировали 8 номер отеля. Помните - бронь действует ограниченное время, поэтому вам нужно " +
                            "поскорее оплатить ваш номер. Хорошего отдыха!");
                    }
                    else if (message.Text == "#9")
                    {
                        hotel.BookRoom(9);
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Вы забронировали 9 номер отеля. Помните - бронь действует ограниченное время, поэтому вам нужно " +
                            "поскорее оплатить ваш номер. Хорошего отдыха!");
                    }
                    else if (message.Text == "#10")
                    {
                        hotel.BookRoom(10);
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Вы забронировали 10 номер отеля. Помните - бронь действует ограниченное время, поэтому вам нужно " +
                            "поскорее оплатить ваш номер. Хорошего отдыха!");
                    }
                    else if (message.Text == "#11")
                    {
                        hotel.BookRoom(11);
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Вы забронировали 11 номер отеля классом люкс. Помните - бронь действует ограниченное время, поэтому вам нужно " +
                            "поскорее оплатить ваш номер. В холодильнике вашего номера будут добавлены бесплатные напитки и разные виды шоколада. Прекрасного отдыха!");
                    }
                    else if (message.Text == "#12")
                    {
                        hotel.BookRoom(12);
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Вы забронировали 12 номер отеля классом люкс. Помните - бронь действует ограниченное время, поэтому вам нужно " +
                            "поскорее оплатить ваш номер. В холодильнике вашего номера будут добавлены бесплатные напитки и разные виды шоколада. Прекрасного отдыха!");
                    }
                    else if (message.Text == "#13")
                    {
                        hotel.BookRoom(13);
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Вы забронировали 13 номер отеля классом люкс. Помните - бронь действует ограниченное время, поэтому вам нужно " +
                            "поскорее оплатить ваш номер. В холодильнике вашего номера будут добавлены бесплатные напитки и разные виды шоколада. Прекрасного отдыха!");
                    }
                    else if (message.Text == "#14")
                    {
                        hotel.BookRoom(14);
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Вы забронировали 14 номер отеля класса апартаменты. Все дополнительные опции входят " +
                            "в стоимость. Мы сделаем все, чтобы вы ни о чем не беспокоились и ни в чем себе не отказывали!");
                    }
                    else if (message.Text == "#15")
                    {
                        hotel.BookRoom(15);
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Вы забронировали 15 номер отеля класса апартаменты. Все дополнительные опции входят " +
                            "в стоимость. Мы сделаем все, чтобы вы ни о чем не беспокоились и ни в чем себе не отказывали!");
                    }
                    else if (message.Text.ToLower().Contains("description"))
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Напишите класс номера, о котором хотите узнать подробнее:\n/standard - Стандарт\n/luxury - Люкс\n/apartment - Апартаменты");
                    }
                    else if (message.Text == "/standard")
                    {
                        await botClient.SendPhotoAsync(message.Chat.Id, photo: InputFile.FromUri("https://i.pinimg.com/564x/0c/a7/ad/0ca7ad124f982ec44dee5ffe6eebb368.jpg"),
                                caption: "Отель \"Голубая Лагуна\" располагается в центре города Кемерово. Гости, путешествующие на личном автомобиле, могут воспользоваться уличной парковкой." +
                                " Для удобства гостей стойка регистрации работает в круглосуточном режиме." +
                                "На всей территории доступен бесплатный скоростной интернет.\r\nК размещению подготовлены просторные номера, интерьер подобран в классическом стиле. " +
                                "В каждом номере имеется эргономичная мебель и современный телевизор.\r\nВ шаговой доступности есть несколько кафе и ресторанов быстрого питания, " +
                                "где туристы смогут перекусить или полноценно покушать.\n/help - Показать все команды");
                    }
                    else if (message.Text == "/luxury")
                    {
                        await botClient.SendPhotoAsync(message.Chat.Id, photo: InputFile.FromUri("https://i.pinimg.com/564x/d9/8c/8a/d98c8a713edad7f8d1fa8f870bb330ff.jpg"),
                                caption: "Отель \"Голубая Лагуна\" располагается в центре города Кемерово. Своим гостям отель рад предложить размещение в комфортных номерах с индивидуальной ванной комнатой" +
                                " с феном, спутниковым телевидением, кондиционером. На всей территории работает бесплатное покрытие Wi-Fi. Уборка в номерах производится каждый день. " +
                                "Для личного автотранспорта — парковка. Регистрация постояльцев осуществляется круглосуточно. Каждому гостю по запросу предоставляется зубной набор и халат.\r\n " +
                                "В отеле имеется собственный ресторан, который работает по меню или в формате «Шведский стол».\n/help - Показать все команды");
                    }
                    else if (message.Text == "/apartment")
                    {
                        await botClient.SendPhotoAsync(message.Chat.Id, photo: InputFile.FromUri("https://i.pinimg.com/564x/53/2d/32/532d32d1ddb9bbc204e6192ebf913726.jpg"),
                                caption: "Отель \"Голубая Лагуна\" располагается в центре города Кемерово. Каждый номер оснащён эргономичной мебелью, местом для хранения вещей, " +
                                "телефоном для связи с персоналом, телевизором и кондиционером. В собственной ванной комнате есть халат, тапочки, фен и гигиенические принадлежности. " +
                                "На территории работает лаундж - бар, кафе, бар - ресторан с открытой террасой, откуда открывается красивый вид но город. " +
                                "Оборудованы 2 зала для проведения торжеств, мероприятий и конференций.\n/help - Показать все команды");
                    }
                    else if (message.Text.ToLower().Contains("cancel"))
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Для отмены брони напишите число. - номер, который вы забронировали (Например: 5.)");
                    }
                    else if (message.Text == "1.")
                    {
                        hotel.CancelBooking(1);
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Вы отменили бронь. До скорой встречи!");
                    }
                    else if (message.Text == "2.")
                    {
                        hotel.CancelBooking(2);
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Вы отменили бронь. До скорой встречи!");
                    }
                    else if (message.Text == "3.")
                    {
                        hotel.CancelBooking(3);
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Вы отменили бронь. До скорой встречи!");
                    }
                    else if (message.Text == "4.")
                    {
                        hotel.CancelBooking(4);
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Вы отменили бронь. До скорой встречи!");
                    }
                    else if (message.Text == "5.")
                    {
                        hotel.CancelBooking(5);
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Вы отменили бронь. До скорой встречи!");
                    }
                    else if (message.Text == "6.")
                    {
                        hotel.CancelBooking(6);
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Вы отменили бронь. До скорой встречи!");
                    }
                    else if (message.Text == "7.")
                    {
                        hotel.CancelBooking(7);
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Вы отменили бронь. До скорой встречи!");
                    }
                    else if (message.Text == "8.")
                    {
                        hotel.CancelBooking(8);
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Вы отменили бронь. До скорой встречи!");
                    }
                    else if (message.Text == "9.")
                    {
                        hotel.CancelBooking(9);
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Вы отменили бронь. До скорой встречи!");
                    }
                    else if (message.Text == "10.")
                    {
                        hotel.CancelBooking(10);
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Вы отменили бронь. До скорой встречи!");
                    }
                    else if (message.Text == "11.")
                    {
                        hotel.CancelBooking(11);
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Вы отменили бронь. До скорой встречи!");
                    }
                    else if (message.Text == "12.")
                    {
                        hotel.CancelBooking(12);
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Вы отменили бронь. До скорой встречи!");
                    }
                    else if (message.Text == "13.")
                    {
                        hotel.CancelBooking(13);
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Вы отменили бронь. До скорой встречи!");
                    }
                    else if (message.Text == "14.")
                    {
                        hotel.CancelBooking(14);
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Вы отменили бронь. До скорой встречи!");
                    }
                    else if (message.Text == "15.")
                    {
                        hotel.CancelBooking(15);
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Вы отменили бронь. До скорой встречи!");
                    }
                    else if (message.Text.ToLower().Contains("help"))
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Вот список возможных команд:\n1./list - Показать все номера отеля." +
                            "\n2./reserve - Забронировать номер.\n3./alreadyBusy - Показать список уже забронированых номеров отеля." +
                            "\n4./description - Описание номера.\n5./cancel - Отменить бронирование номера\n6./help - Показать все команды");
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Я не знаю что вам на это ответить. Если вам нужен список команд - напишите /help");
                    }
                }
            }
            //нужен для обработки ошибок в случае обновлений
            Task Error(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
            {
                var ErrorMesssage = exception switch
                {
                    ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                    _ => exception.ToString()
                };
                Console.WriteLine(ErrorMesssage);
                return Task.CompletedTask;
            }
        }
        static void Main()
        {
            TelegramBot tgBot = new TelegramBot();
            tgBot.Bot();
            Console.ReadLine();
        }
    }
}
