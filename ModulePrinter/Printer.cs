using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Net.Http;
using NLog;
using System.Printing;
using System.Drawing.Printing;
using System.Reflection.Emit;
using System.Drawing;



namespace ModulePrinter
{
    public class Printer
    {
        //private List<Tuple<string, string>> argumentsGroup = new List<Tuple<string, string>>();
        public PrinterTypes PrinterType { get; set; }
        public string PrinterIP { get; set; }
        public string PrinterName { get; set; }
        public int PrinterPort { get; set; }
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
        private Logger logger = LogManager.GetCurrentClassLogger();
        //static MyLabel currentLabel;


        Printer(PrinterTypes printerType = PrinterTypes.LinxTT, int port = 9100, string printerIP = "192.168.1.1")
        {
            PrinterType = printerType;
            PrinterPort = port;
            printerIP = PrinterIP;
        }
        public enum PrinterTypes
        {
            LinxTT,
            Driver,
            ZPL
        }
        public string PrintZPLData()
        {
            return "^XA^FO50,50^ADN,36,20^FD!Complex Integration^FS^XZ"; //ZPL Language
        }
   
        public void GetAvailablePaperSizes()
        {
            PrintDocument printDoc = new PrintDocument();

            printDoc.PrinterSettings.PrinterName = PrinterName;

            // Получаем доступные размеры бумаги для принтера
            foreach (PaperSize paperSize in printDoc.PrinterSettings.PaperSizes)
            {
                Console.WriteLine($"Размер бумаги: {paperSize.PaperName}, Ширина: {paperSize.Width}, Высота: {paperSize.Height}, Тип: {paperSize.Kind}");
            }
        }
        public bool SetWindowsPrinter()
        {
            try
            {
                PrinterSettings settings = new PrinterSettings();
                settings.PrinterName = PrinterName;

                if (!settings.IsValid)
                {
                    Console.WriteLine($"Принтер {PrinterName} не найден.");
                    return false;
                }

                // Установка выбранного принтера в системе по умолчанию
                PrinterSettings.InstalledPrinters.Cast<string>()
                    .Any(printer => printer.Equals(PrinterName, StringComparison.OrdinalIgnoreCase));

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при установке принтера: {ex.Message}");
                return false;
            }
        }
        public async Task<bool> Start()
        {
            if (_tcpClient != null && _tcpClient.Connected)
            {
                Console.WriteLine("Принтер уже подключен");
                return true;
            }

            if (PrinterPort < 0 || PrinterPort > 65536 || !IPAddress.TryParse(PrinterIP, out IPAddress _))
            {
                Console.WriteLine("Некорректный IP адрес или порт.");
                return false;
            }

            try
            {
                _tcpClient = new TcpClient();

                var timeoutToConnect = Task.Delay(ConnectionTimeoutMSec);
                var connectTask = _tcpClient.ConnectAsync(PrinterIP, PrinterPort);

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

        //public void PrintAggFromDriver(TemplateLabel templateLabel, string printerAgg)
        //{
        //    PrintDocument printCode = new PrintDocument
        //    {
        //        PrintController = new StandardPrintController(),
        //        PrinterSettings = new PrinterSettings
        //        {
        //            PrinterName = printerAgg
        //        }
        //    };

        //    printCode.PrintPage += (sender, e) =>
        //    {
        //        argumentsGroup.Clear();
        //        argumentsGroup.Add(new Tuple<string, string>("DataMatrix", templateLabel.DataMatrix));
        //        argumentsGroup.Add(new Tuple<string, string>("EAN128", templateLabel.EAN128));
        //        argumentsGroup.Add(new Tuple<string, string>("ДатаПроизв", templateLabel.ProductionDate));
        //        argumentsGroup.Add(new Tuple<string, string>("ДатаГодн", templateLabel.ExpirationDate));
        //        argumentsGroup.Add(new Tuple<string, string>("Партия", templateLabel.LotNumber));

        //        PrintWithData(argumentsGroup, e.Graphics);
        //    };
        //    printCode.DefaultPageSettings.PaperSize = new PaperSize(
        //        "work",
        //        (int)Math.Ceiling((labelOnWork.width + 3) * 0.03937007874 * 100),
        //        (int)Math.Ceiling(labelOnWork.height * 0.03937007874 * 100));
        //    printCode.Print();
        //}





        //private void PRINT_MY_ASS(object sender, EventArgs e)
        //{
        //    PrintDocument printCode = new PrintDocument
        //    {
        //        PrintController = new StandardPrintController(),
        //        PrinterSettings = new PrinterSettings
        //        {
        //            PrinterName = PrinterName
        //        }
        //    };
        //    printCode.PrintPage += new PrintPageEventHandler(PrintCode_PrintPage);
        //    printCode.DefaultPageSettings.PaperSize = new PaperSize("work", (int)Math.Ceiling((currentLabel.width + 3) * 0.03937007874 * 100), (int)Math.Ceiling((currentLabel.height) * 0.03937007874 * 100));

        //    printCode.Print();
        //}




        private void PrintTextPage(object sender, PrintPageEventArgs e)
        {
            // Определяем, что нужно напечатать на странице
            string textToPrint = "Complex Integration";

            // Определяем шрифт и кисть для текста
            Font font = new Font("Times New Romans", 14);
            Brush brush = Brushes.Black;

            // Определяем координаты печати
            float x = e.MarginBounds.Left;
            float y = e.MarginBounds.Top;

            // Рисуем текст на странице
            e.Graphics.DrawString(textToPrint, font, brush, x, y);
        }

        private void PrintTextToPdf()
        {
            // Создаем новый объект PrintDocument
            PrintDocument printDocument = new PrintDocument();

            // Устанавливаем обработчик события PrintPage
            printDocument.PrintPage += new PrintPageEventHandler(PrintTextPage);

            // Устанавливаем имя принтера
            printDocument.PrinterSettings.PrinterName = PrinterName;

            // Вызываем метод для отправки на печать
            printDocument.Print();
        }

        async static Task Main(string[] args)
        {

            ///Печать через виндовый драйвер, я должнен предоставлять список доступных принтеру размеров
            ///15х15, 20х20 - подгруппа кастомные, А4 - не кастомные
            

            Printer printer = new Printer();
            printer.PrinterType = PrinterTypes.Driver;
            //printer.PrinterIP = "192.168.10.110";
            //printer.Counter = 10;
            printer.PrinterName = "Microsoft Print to PDF";
            //printer.Port = 9100;
            printer.ConnectionTimeoutMSec = 1000;


            //bool isConnected = await printer.Start();
            //if (isConnected && (printer.PrinterType == PrinterTypes.ZPL || printer.PrinterType == PrinterTypes.LinxTT))
            //{
            //    Console.WriteLine("Соединение с принтером установлено.");

            //    string data = printer.PrintZPLData();
            //    Console.WriteLine($"Полученная информация: {data}");

            //}
            //else
            //{
            //    Console.WriteLine("Не удалось установить соединение с принтером.\n");
            //}

            if (printer.PrinterType == PrinterTypes.Driver)
            {
                for (int i = 0; i < PrinterSettings.InstalledPrinters.Count; i++)
                {
                    Console.WriteLine(PrinterSettings.InstalledPrinters[i]);
                }

               //bool printerSet = printer.SetWindowsPrinter();

                //if (printerSet)
                //{
                //    Console.WriteLine("printer");
                //}

                // printer.GetAvailablePaperSizes();

                //printer.PrintTextToPdf();






            }







            Console.ReadKey();
        }
    }

    public class TemplateLabel
    {

        public string DataMatrix { get; set; } = "0104607112814912215ze6<J\u001d93aoAx";
        public string EAN13 { get; set; } = "2200008125684";
        public string EAN128 { get; set; } = "0104610051410244310301256611210605102106050";
        public string Weight { get; set; } = "0,000 кг";
        public string WeightSum { get; set; } = "0,000 кг";
        public string ProductionDate { get; set; } = DateTime.Now.ToShortDateString();
        public string ExpirationDate { get; set; } = DateTime.Now.ToShortDateString();
        public string LotNumber { get; set; } = "12345678";

    }
}
