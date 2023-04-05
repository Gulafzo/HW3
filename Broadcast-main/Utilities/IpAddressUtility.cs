//код от Сурнова Дмитрия
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utilities
{
    public static class IpAddressUtility
    {
        
        public static string CreateLocalAddress()// Метод для создания локального IP адреса
        {
           
            return Dns.GetHostEntry(Dns.GetHostName()) // Получаем информацию о хосте
                
                .AddressList// Получаем список IP адресов
               
                .First(x => x.AddressFamily == AddressFamily.InterNetwork) // Выбираем первый адрес IPv4
               
                .ToString(); // Преобразуем в строку
        }
        
        public static IPAddress CreateBroadcastAddress()// Метод для создания широковещательного адреса
        {
           
            var localIpAddess = CreateLocalAddress();
            var localIpAddessNumbers = localIpAddess.Split('.');
           
            localIpAddessNumbers[3] = "255"; // Замена послед. число на 255
           
            var remoteIpAddressInString = localIpAddessNumbers // Собираем строку обратно, убирая первую точку
                .Aggregate("", (acc, value) => $"{acc}.{value}")
                .Substring(1);           
            var broadcastAddress = IPAddress.Parse(remoteIpAddressInString);
            return broadcastAddress;// Возвращаем  адрес
        }
    }
}
