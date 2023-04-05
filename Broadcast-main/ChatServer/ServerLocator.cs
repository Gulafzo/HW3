using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Utilities;

namespace ChatServer
{
    internal class ServerLocator : IDisposable
    {
        public static int Port = 0;
        private bool _isStarted;
        private readonly Thread _serverLocatorSenderThread;
        private readonly Thread _serverLocatorResieverThread;
        private readonly Socket _udpBroadcastSocketReciever;
        private readonly List<string> _listRequests;
        private static int _portReciever;
        private static object _lockListRequests;

     
      
        public ServerLocator()  // Конструктор 
        {
            // Инициализация переменных
            _isStarted = false;
            _portReciever = 11111; 
            _serverLocatorSenderThread = new Thread(ServerLocatorSender); // Создание потока для отправки сообщений
            _serverLocatorResieverThread = new Thread(ServerLocatorReciever); // Создание потока для приема сообщений

            _udpBroadcastSocketReciever = new Socket(SocketType.Dgram, ProtocolType.Udp); // Создание сокета для приема сообщений
            _udpBroadcastSocketReciever.EnableBroadcast = true; // Включение режима широковещательной рассылки

            _listRequests = new List<String>(); // Создание списка запросов
            _lockListRequests = new object(); // Создание объекта для блокировки списка запросов
        }

        public void Start()
        {
            _isStarted = true;

            _serverLocatorSenderThread.Start();
            _serverLocatorResieverThread.Start();
        }

        public void Stop()
        {
            _isStarted = false;

            Task.Delay(100).Wait();

            _serverLocatorResieverThread.Abort();
            _serverLocatorSenderThread.Abort();
        }

       
        private void ServerLocatorSender() // метод для отпрвки сообщений о доступности сервера локатора
        {
            while (_isStarted)
            {
                if (Port == 0) // Если порт 0 то пропускаем 
                    continue;

                string IP_Adress_Port = "";
                lock (_lockListRequests) // Блокировка списка запросов
                {
                    if (_listRequests.Count > 0) // Если в списке есть запросы, то берем первый запрос из списка
                    {
                        IP_Adress_Port = _listRequests[0];
                    }
                }

                if (IP_Adress_Port == "") // Если запроса нету то ждем 100 мс и продолжаем цикл
                {
                    Task.Delay(100).Wait();
                    continue;
                }

                var mass = IP_Adress_Port.Split(':'); // Разбиваем запрос на IP-адрес и порт

                if (mass.Length == 0) // Если запрос некорректен  удаляем его из списка 
                {
                    lock (_lockListRequests)
                    {
                        IP_Adress_Port.Remove(0);
                    }
                    continue;
                }

                Socket udpBroadcastSocketSender = new Socket(SocketType.Dgram, ProtocolType.Udp); //  сокет для отправки сообщения
                IPAddress broadcastAddress = IpAddressUtility.CreateBroadcastAddress(); //  адрес для широковещательной рассылки
                int port = int.Parse(mass[1]); // Получаем порт из запроса
                var broadcastIpEndPoint = new IPEndPoint(IPAddress.Parse(mass[0]), port); // Создаем точку подключения для отправки сообщения
                udpBroadcastSocketSender.Connect(broadcastIpEndPoint); // Подключаемся к точке отправки
                string Message = IpAddressUtility.GetLocalAddress() + ":" + Port; // Формируем сообщение о доступности сервера локатора
                Console.WriteLine("ServerLocatorSender"); // Выводим в консоль сообщение о начале отправки сообщения
                Console.WriteLine(Message); 
                SocketUtility.SendString(udpBroadcastSocketSender, Message, () => { }); // Отправляем сообщение о доступности сервера локатора
                lock (_lockListRequests) // Блокируем список запросов
                {
                    _listRequests.RemoveAt(0); // Удаляем первый запрос из списка
                }
                Task.Delay(100).Wait(); // Ждем 100 мс перед отправкой
                udpBroadcastSocketSender.Close(); // закрываем скет отправки сообщений
            }
        }


        private void ServerLocatorReciever()//метод является методом экземпляра класса `ServerLocator` запускается в потоке `_serverLocatorResieverThread
        {
            //метод служит для принятия ответов от серверов на широковещательный запрос и записи
            try
            {
                _udpBroadcastSocketReciever.Bind(new IPEndPoint(IPAddress.Any, _portReciever));
                Console.WriteLine("Reciever port - " + _portReciever);
            }
            catch
            {
                _portReciever++;
                ServerLocatorReciever();
                return;
            }

            while (_isStarted)
            {
                string stroka = SocketUtility.ReceiveString(_udpBroadcastSocketReciever);
                Console.WriteLine("ServerLocatorReciever");
                Console.WriteLine(stroka);
                lock (_lockListRequests)
                {
                    _listRequests.Add(stroka);
                }
            }
        }

        public void Dispose()
        {
            Stop();
            _udpBroadcastSocketReciever.Dispose();
        }
    }
}
