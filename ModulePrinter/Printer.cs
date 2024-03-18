using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Net.Http;
using System.Drawing;
using static ModulePrinter.Printer;



namespace ModulePrinter
{
    public class Printer
    {
        public PrinterTypes PrinterType { get; set; }
        public string PrinterIP { get; set; }
        public int Port { get; set; }
        public int ConnectionTimeoutMSec { get; set; }
        private int _counter;
        public int Counter
        {
            get { return _counter; }
            set
            {
                if (value >= MinLimitCounter && value <= MaxLimitCounter)
                    _counter = value;
                else
                    Console.WriteLine("Ошибка. Выход за пределы лимитов");
            }
        }
        public int MinLimitCounter { get; set; }
        public int MaxLimitCounter { get; set; }

        private TcpClient _tcpClient;
        private NetworkStream _networkStream;

        Printer(PrinterTypes printerType = PrinterTypes.LinxTT, int port = 9100, string printerIP = "192.168.1.1")
        {
            PrinterType = printerType;
            Port = port;
            printerIP = PrinterIP;
        }
        public enum PrinterTypes
        {
            LinxTT,
            Driver,
            ZPL
        }
        public string PrintData()
        {
            return "^XA^FO50,50^ADN,36,20^FD!Complex Integration^FS^XZ"; //ZPL Language
        }
        public async Task<bool> Start()
        {
            if (_tcpClient != null && _tcpClient.Connected)
            {
                Console.WriteLine("Принтер уже подключен");
                return true;
            }

            if (Port < 0 || Port > 65536 || !IPAddress.TryParse(PrinterIP, out IPAddress _))
            {
                Console.WriteLine("Некорректный IP адрес или порт.");
                return false;
            }

            try
            {
                _tcpClient = new TcpClient();

                var timeoutToConnect = Task.Delay(ConnectionTimeoutMSec);
                var connectTask = _tcpClient.ConnectAsync(PrinterIP, Port);

                if (await Task.WhenAny(connectTask, timeoutToConnect) != connectTask)
                {
                    // Таймаут подключения
                    Console.WriteLine($"Таймаут при подключении к принтеру: {ConnectionTimeoutMSec / 1000} сек.");
                    return false;
                }

                await connectTask;
                //Console.WriteLine("Клиент запущен");
                _networkStream = _tcpClient.GetStream();

                if (_tcpClient.Connected)
                {
                    Console.WriteLine($"Подключение с {_tcpClient.Client.RemoteEndPoint} установлено.\n");
                }
                return true;
            }

            catch (SocketException ex)
            {
                Console.WriteLine(ex);
                return false;
            }
        }








        async static Task Main(string[] args)
        {

            Printer printer = new Printer();
            printer.PrinterType = PrinterTypes.ZPL;
            printer.PrinterIP = "10.10.10.10";
            //printer.Port = 9100;
            printer.ConnectionTimeoutMSec = 1000;

            bool isConnected = await printer.Start();
            if (isConnected)
            {
                Console.WriteLine("Соединение с принтером установлено.");

                string data = printer.PrintData();
                Console.WriteLine($"Полученная информация: {data}");
            }
            else
            {
                Console.WriteLine("Не удалось установить соединение с принтером.");
            }


        }
    }
}
