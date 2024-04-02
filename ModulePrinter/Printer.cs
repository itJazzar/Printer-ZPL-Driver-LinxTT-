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
using BinaryKits.Zpl.Label;
using BinaryKits.Zpl.Label.Elements;
using System.Data.SqlTypes;


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
        public string PackingDate { get; set; } = DateTime.Now.ToShortDateString();
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
        public TemplateLabel TemplateLabel = new TemplateLabel();
        private List<Tuple<string, string>> _arguments = new List<Tuple<string, string>>();
        Printer(PrinterTypes printerType = PrinterTypes.Driver, int port = 9100)
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
        public string PrintZPLWithData(List<Tuple<string, string>> data, MyLabel label) //now i need to Draw pictures. My help https://github.com/BinaryKits/BinaryKits.Zpl https://zplprinter.azurewebsites.net/
        {           

            var font = new ZplFont(fontWidth: 14, fontHeight: 14);
            var elements = new List<ZplElementBase>();
            ZplElementBase elementToAdd = null;

            foreach (var tuple in data)
            {
                var key = tuple.Item1;
                var value = tuple.Item2;

                var labelObjects = label.objects.Where(obj => obj.specialArgument == key).ToList();
                foreach (var labelObject in labelObjects)
                {
                    labelObject.data = value;

                    switch (key)
                    {
                        case "DataMatrix":
                            elementToAdd = new ZplDataMatrix(value, labelObject.x, labelObject.y, 3, 200, FieldOrientation.Normal);
                            break;
                        case "EAN13":
                            elementToAdd = new ZplBarcodeEan13(value, labelObject.x, labelObject.y, labelObject.height, 1);
                            break;
                        case "EAN128":
                            elementToAdd = new ZplBarcode128(value, labelObject.x, labelObject.y, labelObject.height, 1);
                            break;
                        default:
                            elementToAdd = new ZplTextField(value, labelObject.x, labelObject.y, font);
                            break;
                    }

                    if (elementToAdd != null)
                    {
                        elements.Add(elementToAdd);
                    }
                }
            }

            var renderEngine = new ZplEngine(elements);
            string output = renderEngine.ToZplString(new ZplRenderOptions { AddEmptyLineBeforeElementStart = false, SourcePrintDpi = 203, TargetPrintDpi = 300 });

            return output;
        }
        public bool SendZplString(string zplString)
        {
            if (_tcpClient == null || !_tcpClient.Connected || _networkStream == null)
            {
                Console.WriteLine("Принтер не подключен.");
                return false;
            }

            try
            {
                byte[] data = Encoding.UTF8.GetBytes(zplString);
                _networkStream.Write(data, 0, data.Length);

                Console.WriteLine("Строка ZPL успешно отправлена на принтер.");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при отправке строки ZPL на принтер: {ex.Message}");
                return false;
            }
        }
        private void PrintWithDriver(Printer printer, List<Tuple<string, string>> arguments, PrintManager printManager)
        {
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

            paperSizes.Add(new PaperSize("15x15", 15, 15));
            paperSizes.Add(new PaperSize("20x20", 20, 20));

            //foreach (PaperSize paperSize in paperSizes)
            //{
            //    Console.WriteLine($"Name: {paperSize.PaperName}, Width: {paperSize.Width}, Height: {paperSize.Height}, Type: {paperSize.Kind}");
            //}

            PaperSize selectedPaperSize = null;
            foreach (PaperSize size in paperSizes)
            {
                if (size.PaperName == printer.PrinterSizeName)
                {
                    selectedPaperSize = size;
                    break;
                }
            }

            //if (selectedPaperSize != null)
            //{
            //    printerSettings.DefaultPageSettings.PaperSize = selectedPaperSize;
            //    Console.WriteLine($"Выбранный размер печати: {selectedPaperSize}");
            //}
            //else
            //    Console.WriteLine($"Заданный размер бумаги {printer.PrinterSizeName} у принтера {printer.PrinterName} не найден.");

            // Привязка обработчика события печати
            printCode.PrintPage += (s, e) => PrintPageHandler(s, e, arguments, printer, selectedPaperSize, printCode);

            printCode.Print();
            Console.WriteLine("Печать успешно завершена");
        }
        private void PrintPageHandler(object sender, PrintPageEventArgs e, List<Tuple<string, string>> arguments, Printer printer, PaperSize selectedPaperSize, PrintDocument printCode)
        {
            printer._arguments = arguments;
            printCode.DefaultPageSettings.PaperSize = selectedPaperSize;
            printer._printManager.PrintWithData(arguments, e.Graphics);
        }
        public void PrintDataToLinxTT()
        {
            //Code
        }
        public void Dispose()
        {
            if (_tcpClient != null && _networkStream != null)
            {
                _tcpClient.Dispose();
                _networkStream.Dispose();
            }
        }


        async static Task Main(string[] args)
        {
            Printer printer = new Printer(); //параметры по умолчанию
            printer.PrinterType = PrinterTypes.ZPL;
            //printer.PrinterIP = "192.168.10.136";
            //printer.Counter = 10;
            printer.PrinterName = "Microsoft Print to PDF";
            //printer.PrinterName = "HP Office1";
            //printer.PrinterName = "TSC TE210";
            printer.PrinterSizeName = "15x15";
            //printer.PrinterPort = 9100;
            printer.ConnectionTimeoutMSec = 1000;
            printer.FolderPath = @"C:\Users\Public\Labels\3 DM + 1 EAN13.ci";

            printer._printManager = new PrintManager(printer.FolderPath);
            MyLabel label = MyLabel.FromFile(printer.FolderPath);

            Console.WriteLine($"Выбран принтер: {printer.PrinterName}");


 
            ///public static double px_in_mm = Application.OpenForms[0].DeviceDpi / 25.4; ЗДЕСЬ БЫЛ ЗАТЫК



            printer._arguments = new List<Tuple<string, string>>
                {
                        new Tuple<string, string>("DataMatrix", printer.TemplateLabel.DataMatrix),
                        new Tuple<string, string>("EAN128", printer.TemplateLabel.EAN128),
                        new Tuple<string, string>("EAN13", printer.TemplateLabel.EAN13),
                        new Tuple<string, string>("ДатаПроизв", printer.TemplateLabel.ProductionDate),
                        new Tuple<string, string>("ДатаГодн", printer.TemplateLabel.ExpirationDate),
                        new Tuple<string, string>("ДатаУпак", printer.TemplateLabel.PackingDate),
                        new Tuple<string, string>("Вес", printer.TemplateLabel.Weight),
                        new Tuple<string, string>("Партия", printer.TemplateLabel.LotNumber),
                        new Tuple<string, string>("Нет", "фальшивка")
                };


            if (printer.PrinterType == PrinterTypes.Driver)
            {
                printer.PrintWithDriver(printer, printer._arguments, printer._printManager);
            }

            //Console.WriteLine("Press SPACEBAR for printing to Driver or any other key for exit\n");
            //while (true)
            //{
            //    ConsoleKeyInfo key = Console.ReadKey();
            //    if (key.Key == ConsoleKey.Spacebar)
            //    {
            //        printer.PrintWithDriver(printer, printer._arguments, printer._printManager);
            //    }
            //    else
            //    {
            //        break;
            //    }
            //}

            if (printer.PrinterType == PrinterTypes.ZPL)
            {
                string zplText = printer.PrintZPLWithData(printer._arguments, label);
                Console.WriteLine(zplText);
            }



            //bool isConnected = await printer.Start();
            //if (isConnected && (printer.PrinterType == PrinterTypes.ZPL || printer.PrinterType == PrinterTypes.LinxTT))
            //{
            //    Console.WriteLine("Press SPACEBAR for printing to ZPL or any other key for exit\n");
            //    while (true)
            //    {
            //        ConsoleKeyInfo key = Console.ReadKey();
            //        if (key.Key == ConsoleKey.Spacebar)
            //        {
            //            printer.SendZplString(zplText);
            //        }
            //        else
            //        {
            //            break;
            //        }
            //    }

            //}

        }
    }
}