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
using LabelDesignerV2;
using System.IO;
using System.Diagnostics;
using static System.Drawing.Printing.PrinterSettings;
using System.Net.PeerToPeer;
//using BinaryKits.Zpl.Label;
//using BinaryKits.Zpl.Label.Elements;


namespace ModulePrinter
{
    public class TemplateLabel
    {
        public string DataMatrix { get; set; } = "0104607112814912215ze6<J\u001d93aoAx";
        public string EAN13 { get; set; } = "2200008125684";
        public string EAN128 { get; set; } = "0104610051410244310301256611210605102106050";
        public string Weight { get; set; } = "7,500 кг";
        public string WeightSum { get; set; } = "0,000 кг";
        public string ProductionDate { get; set; } = DateTime.Now.ToShortDateString();
        public string ExpirationDate { get; set; } = DateTime.Now.ToShortDateString();
        public string LotNumber { get; set; } = "12345678";
    }

public class Printer
    {
        public PrinterTypes PrinterType { get; set; }
        public string PrinterIP { get; set; }
        public string PrinterName { get; set; }
        public int PrinterPort { get; set; }
        public string PrinterSizeName { get; set; }
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
        public string FolderPath { get; set; }

        private TcpClient _tcpClient;
        private NetworkStream _networkStream;
        private Logger _logger = LogManager.GetCurrentClassLogger();

        private PrintManager _printManager;

        private List<Tuple<string, string>> _arguments = new List<Tuple<string, string>>();
        public TemplateLabel TemplateLabel = new TemplateLabel();


        Printer(PrinterTypes printerType = PrinterTypes.LinxTT, int port = 9100)
        {
            PrinterType = printerType;
            PrinterPort = port;
        }
        public enum PrinterTypes
        {
            LinxTT,
            Driver,
            ZPL
        }
        public string[] GetFilesInFolder()
        {
            if (string.IsNullOrEmpty(FolderPath) || !Directory.Exists(FolderPath))
            {
                return new string[0];
            }

            try
            {
                return Directory.GetFiles(FolderPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка при получении списка файлов: " + ex.Message);
                return new string[0];
            }
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

        public async Task<bool> Start()
        {
            if (_tcpClient != null && _tcpClient.Connected)
            {
                Console.WriteLine($"Принтер {PrinterType} уже подключен");
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

        public string PrintZPLWithData(List<Tuple<string, string>> data)
        {
            return "";
        }


        async static Task Main(string[] args)
        {

            Printer printer = new Printer(); //параметры по умолчанию
            printer.PrinterType = PrinterTypes.ZPL;
            //printer.PrinterIP = "192.168.10.110";
            //printer.Counter = 10;
            //printer.PrinterName = "Microsoft Print to PDF";
            //printer.PrinterName = "HP Office1";
            printer.PrinterName = "TSC TE210";
            //printer.PrinterSizeName = "15x15";
            //printer.Port = 9100;
            printer.ConnectionTimeoutMSec = 1000;
            printer.FolderPath = @"C:\Users\Public\Labels\zpl.ci";

            //string[] templatesLabels = printer.GetFilesInFolder();


            printer._printManager = new PrintManager(printer.FolderPath);

            ///public static double px_in_mm = Application.OpenForms[0].DeviceDpi / 25.4; ЗДЕСЬ БЫЛ ЗАТЫК

            PrintDocument printCode = new PrintDocument
            {
                PrintController = new StandardPrintController(),
                PrinterSettings = new PrinterSettings
                {
                    PrinterName = printer.PrinterName,
                }
            };

            PrinterSettings printerSettings = printCode.PrinterSettings;
            PaperSizeCollection paperSizes = printerSettings.PaperSizes;

            // Добавление пользовательских размеров 15x15 мм и 20x20 мм
            paperSizes.Add(new PaperSize("15x15", 15, 15));
            paperSizes.Add(new PaperSize("20x20", 20, 20));

            Console.WriteLine($"Выбран принтер: {printer.PrinterName}");

            foreach (PaperSize paperSize in paperSizes)
            {
                Console.WriteLine($"Name: {paperSize.PaperName}, Width: {paperSize.Width}, Height: {paperSize.Height}, Type: {paperSize.Kind}");
            }


            PaperSize selectedPaperSize = null;
            foreach (PaperSize size in paperSizes)
            {
                if (size.PaperName == printer.PrinterSizeName)
                {
                    selectedPaperSize = size;
                    break;
                }
            }

            if (selectedPaperSize != null)
            {
                printerSettings.DefaultPageSettings.PaperSize = selectedPaperSize;
                Console.WriteLine($"Выбранный размер печати: {selectedPaperSize}");
            }
            else
            {
                Console.WriteLine($"Заданный размер бумаги {printer.PrinterSizeName} у принтера {printer.PrinterName} не найден.");
            }

            printCode.PrintPage += (s, e) =>
            {
                printer._arguments = new List<Tuple<string, string>>
                {
                    new Tuple<string, string>("DataMatrix", "Zdarova, zaebal"),
                    new Tuple<string, string>("EAN128", printer.TemplateLabel.EAN128),
                    new Tuple<string, string>("ДатаПроизв", printer.TemplateLabel.ProductionDate),
                    new Tuple<string, string>("ДатаГодн", printer.TemplateLabel.ExpirationDate),
                    new Tuple<string, string>("Вес", printer.TemplateLabel.Weight),
                    new Tuple<string, string>("Партия", printer.TemplateLabel.LotNumber)
                };

                printCode.DefaultPageSettings.PaperSize = selectedPaperSize;

                printer._printManager.PrintWithData(printer._arguments, e.Graphics);
            };

            //printCode.Print();

            //var elements = new List<ZplElementBase>();
            ////elements.Add(new ZplBarcode128("CIMarking", 10, 150));

            //var dm = new ZplDataMatrix($"0104607032146742215&FF/w{(char)29}93nLyu", 10, 10, 5, 200, FieldOrientation.Normal);

            //dm.ToZplString();
            //elements.Add(dm);

            //var renderEngine = new ZplEngine(elements);
            //var output = renderEngine.ToZplString(new ZplRenderOptions { AddEmptyLineBeforeElementStart = true, });

            //Console.WriteLine(output);




            //Console.WriteLine();
            //var output = new ZplGraphicBox(100, 100, 100, 100).ToZplString();
            //Console.WriteLine(output);

            //var output1 = new ZplBarcode128("123ABC", 10, 50).ToZplString();
            //Console.WriteLine(output1);

            //string result = printer._printManager.GetZPLIIFormattedString();
            //string res = printer._printManager.PutZPL(printer._arguments);

            //Console.WriteLine(res);

            //Console.WriteLine(result);




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


            //if (printer.PrinterType == PrinterTypes.Driver)


            Console.ReadKey();
        }
    }
}