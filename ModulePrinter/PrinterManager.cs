//using MarkingCI.Entities;
using ModulePrinter;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace LabelDesignerV2
{
    class PrintManager
    {
        private List<Tuple<string, string>> argumentsGroup = new List<Tuple<string, string>>();
        public MyLabel labelOnWork;
        public PrintManager(string filename, bool isGroup = false)
        {
            labelOnWork = new MyLabel(1, 1);
            if (!isGroup)
                labelOnWork = MyLabel.FromFile(filename);
            else
                labelOnWork = MyLabel.FromFile(filename + "group");
        }
        public MyLabel PrintWithData(List<Tuple<string, string>> data, Graphics gr)
        {
            // изменение данных
            foreach (Tuple<string, string> tuple in data)
            {
                switch (tuple.Item1) // чекаем тип
                {
                    case "DataMatrix":
                    case "EAN13":
                    case "EAN128":
                    case "Вес":
                    case "ВесСумм":
                    case "ДатаПроизв":
                    case "ДатаГодн":
                    case "ДатаУпак":
                    case "Партия":
                        {
                            foreach (MyLabelObject obj in labelOnWork.objects)
                            {
                                if (tuple.Item1 == obj.specialArgument)
                                    obj.data = tuple.Item2;
                            }
                            break;
                        }
                }
            }

            // производим отрисовку
            labelOnWork.draw(gr);

            //// Генерируем изображение печати
            //var printImage = GeneratePrintImage(labelOnWork, gr);

            //// Сохраняем Bitmap в файл формата PNG
            //printImage.Save("output.png", ImageFormat.Png);

            //// Освобождаем ресурсы Bitmap
            //printImage.Dispose();
            return labelOnWork;

        }

        public byte[] GeneratePrintImage(MyLabel labelOnWork, Graphics gr)
        {
            // Создаем Bitmap для сохранения результата печати
            using (Bitmap bitmap = new Bitmap((int)gr.VisibleClipBounds.Width, (int)gr.VisibleClipBounds.Height, PixelFormat.Format24bppRgb) )
            {
                // Создаем объект Graphics для рисования на Bitmap
                using (Graphics bitmapGraphics = Graphics.FromImage(bitmap))
                {
                    // Производим отрисовку на объекте Graphics в памяти
                    labelOnWork.draw(bitmapGraphics);
                }

                //return bitmap;
                
                // Преобразуем Bitmap в массив байтов
                using (MemoryStream stream = new MemoryStream())
                {
                    // Сохраняем Bitmap в поток данных (память)
                    bitmap.Save(stream, ImageFormat.Bmp);

                    // Получаем массив байтов из потока данных
                    return stream.ToArray();
                }
            }
        }
        public string GetZPLIIFormattedString()
        {
            return labelOnWork.toDFF();
        }
        public string PutZPL(List<Tuple<string, string>> data)
        {
            foreach (Tuple<string, string> tuple in data)
            {
                switch (tuple.Item1) // чекаем тип
                {

                    case "DataMatrix":
                    case "EAN13":
                    case "EAN128":
                    case "Вес":
                    case "ВесСумм":
                    case "ДатаПроизв":
                    case "ДатаГодн":
                    case "Партия":
                        {
                            foreach (MyLabelObject obj in labelOnWork.objects)
                            {
                                if (tuple.Item1 == obj.specialArgument)
                                    obj.data = tuple.Item2;
                            }
                            break;
                        }
                }

            }
            return labelOnWork.toXFF();
        }   
    }
}
