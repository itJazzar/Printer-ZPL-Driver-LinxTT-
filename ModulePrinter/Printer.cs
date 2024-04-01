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
        public void PrintZPLWithData(List<Tuple<string, string>> data, MyLabel label)
        {
            var font = new ZplFont(fontWidth: 20, fontHeight: 20);
            var elements = new List<ZplElementBase>();
            int verticalOffset = 0;

            foreach (Tuple<string, string> tuple in data)
            {

                switch (tuple.Item1)
                {
                    case "DataMatrix":
                        {
                            bool dataMatrixAdded = false;
                            foreach (MyLabelObject obj in label.objects)
                            {
                                if (tuple.Item1 == obj.specialArgument)
                                {
                                    obj.data = tuple.Item2;

                                    if (!dataMatrixAdded)
                                    {
                                        elements.Add(new ZplDataMatrix($"{obj.data}", 350, verticalOffset, 5, 200, FieldOrientation.Normal));
                                        dataMatrixAdded = true; // Устанавливаем флаг, чтобы избежать дублирования
                                        verticalOffset += 150;
                                    }
                                }
                            }

                            break;
                        }
                    case "EAN13":
                        {
                            bool ean13Added = false; // Флаг для отслеживания добавления EAN13
                            foreach (MyLabelObject obj in label.objects)
                            {
                                if (tuple.Item1 == obj.specialArgument)
                                {
                                    obj.data = tuple.Item2;

                                    if (!ean13Added)
                                    {
                                        elements.Add(new ZplBarcodeEan13($"{obj.data}", 350, verticalOffset));
                                        ean13Added = true;
                                        verticalOffset += 150;
                                    }
                                }
                            }

                            break;
                        }
                    case "EAN128":
                        {
                            bool ean128Added = false;
                            foreach (MyLabelObject obj in label.objects)
                            {
                                if (tuple.Item1 == obj.specialArgument)
                                {
                                    obj.data = tuple.Item2;

                                    if (!ean128Added)
                                    {
                                        elements.Add(new ZplBarcode128($"{obj.data}", 350, verticalOffset));
                                        ean128Added = true;
                                        verticalOffset += 150;

                                    }
                                }
                            }
                            break;
                        }
                    case "Вес":
                        {
                            bool weightAdded = false;
                            foreach (MyLabelObject obj in label.objects)
                            {
                                if (tuple.Item1 == obj.specialArgument)
                                {
                                    obj.data = tuple.Item2;

                                    if (!weightAdded)
                                    {
                                        elements.Add(new ZplTextField($"{obj.data}", 350, verticalOffset, font));
                                        weightAdded = true;
                                        verticalOffset += 150;
                                    }

                                }

                            }
                            break;
                        }
                    case "ВесСумм":
                        {
                            bool weightSumAdded = false;
                            foreach (MyLabelObject obj in label.objects)
                            {
                                if (tuple.Item1 == obj.specialArgument)
                                {
                                    obj.data = tuple.Item2;

                                    if (!weightSumAdded)
                                    {
                                        elements.Add(new ZplTextField($"{obj.data}", 350, verticalOffset, font));
                                        weightSumAdded = true;
                                        verticalOffset += 150;
                                    }

                                }

                            }
                            break;
                        }
                    case "ДатаПроизв":
                        {
                            bool dateProdAdded = false;
                            foreach (MyLabelObject obj in label.objects)
                            {
                                if (tuple.Item1 == obj.specialArgument)
                                {
                                    obj.data = tuple.Item2;

                                    if (!dateProdAdded)
                                    {
                                        elements.Add(new ZplTextField($"{obj.data}", 350, verticalOffset, font));
                                        dateProdAdded = true;
                                        verticalOffset += 150;
                                    }

                                }

                            }
                            break;
                        }
                    case "ДатаГодн":
                        {
                            bool dateEpirAdded = false;
                            foreach (MyLabelObject obj in label.objects)
                            {
                                if (tuple.Item1 == obj.specialArgument)
                                {
                                    obj.data = tuple.Item2;

                                    if (!dateEpirAdded)
                                    {
                                        elements.Add(new ZplTextField($"{obj.data}", 350, verticalOffset, font));
                                        dateEpirAdded = true;
                                        verticalOffset += 150;
                                    }

                                }

                            }
                            break;
                        }
                    case "ДатаУпак":
                        {
                            bool datePackingAdded = false;
                            foreach (MyLabelObject obj in label.objects)
                            {
                                if (tuple.Item1 == obj.specialArgument)
                                {
                                    obj.data = tuple.Item2;

                                    if (!datePackingAdded)
                                    {
                                        elements.Add(new ZplTextField($"{obj.data}", 350, verticalOffset, font));
                                        datePackingAdded = true;
                                        verticalOffset += 150;
                                    }

                                }

                            }
                            break;
                        }
                    case "Партия":
                        {
                            bool partyAdded = false;
                            foreach (MyLabelObject obj in label.objects)
                            {
                                if (tuple.Item1 == obj.specialArgument)
                                {
                                    obj.data = tuple.Item2;

                                    if (!partyAdded)
                                    {
                                        elements.Add(new ZplTextField($"{obj.data}", 350, verticalOffset, font));
                                        partyAdded = true;
                                        verticalOffset += 150;
                                    }

                                }

                            }
                            break;
                        }
                }

            }

            var renderEngine = new ZplEngine(elements);
            var output = renderEngine.ToZplString(new ZplRenderOptions { AddEmptyLineBeforeElementStart = false, });
            Console.WriteLine(output);

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
            printer.PrinterIP = "192.168.10.136";
            //printer.Counter = 10;
            //printer.PrinterName = "Microsoft Print to PDF";
            //printer.PrinterName = "HP Office1";
            printer.PrinterName = "TSC TE210";
            //printer.PrinterSizeName = "15x15";
            //printer.PrinterPort = 9100;
            printer.ConnectionTimeoutMSec = 1000;
            printer.FolderPath = @"C:\Users\Public\Labels\max.ci";

            //string[] templatesLabels = printer.GetFilesInFolder();


            printer._printManager = new PrintManager(printer.FolderPath);
            MyLabel label = MyLabel.FromFile(printer.FolderPath);

            //var list = label.ToTupleList();
            //foreach (var item in list)
            //{
            //    Console.WriteLine(item);
            //}


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

            //printCode.PrintPage += (s, e) =>
            //{
            printer._arguments = new List<Tuple<string, string>>
                {
                        new Tuple<string, string>("DataMatrix", printer.TemplateLabel.DataMatrix),
                        new Tuple<string, string>("EAN128", printer.TemplateLabel.EAN128),
                        new Tuple<string, string>("EAN13", printer.TemplateLabel.EAN13),
                        new Tuple<string, string>("ДатаПроизв", printer.TemplateLabel.ProductionDate),
                        new Tuple<string, string>("ДатаГодн", printer.TemplateLabel.ExpirationDate),
                        new Tuple<string, string>("Вес", printer.TemplateLabel.Weight),
                        new Tuple<string, string>("Партия", printer.TemplateLabel.LotNumber)
                };

            //    printCode.DefaultPageSettings.PaperSize = selectedPaperSize;

            //    printer._printManager.PrintWithData(printer._arguments, e.Graphics);
            //};
            //Console.WriteLine("Press SPACEBAR for printing or any other key for exit\n");
            //while (true)
            //{
            //    ConsoleKeyInfo key = Console.ReadKey();
            //    if (key.Key == ConsoleKey.SPACEBAR)
            //    {
            //        printCode.Print();
            //    }
            //    else
            //    {
            //        break;
            //    }
            //}


            printer.PrintZPLWithData(printer._arguments, label);



            //elements.Add(new ZplBarcode128("CIMarking", 0, 1100));

            //elements.Add(new ZplTextField("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", 0, 0, font));


            //var dm = new ZplDataMatrix($"0104607032146742215&FF/w{(char)29}93nLyu", 350, 0, 5, 200, FieldOrientation.Normal); //350 - офсет по Х для корректной печати

            //dm.ToZplString();
            //elements.Add(dm);

            //var renderEngine = new ZplEngine(elements);
            //var output = renderEngine.ToZplString(new ZplRenderOptions { AddEmptyLineBeforeElementStart = false, });
            //Console.WriteLine(output);


            //await printer.Start();

            //Console.WriteLine("Press SPACEBAR for printing or any other key for exit\n");
            //while (true)
            //{
            //    ConsoleKeyInfo key = Console.ReadKey();
            //    if (key.Key == ConsoleKey.Spacebar)
            //    {
            //        printer.SendZplString(output);
            //    }
            //    else
            //    {
            //        break;
            //    }
            //}
            //printer.Dispose();






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