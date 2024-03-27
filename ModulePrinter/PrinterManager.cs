//using MarkingCI.Entities;
using ModulePrinter;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
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
        public void PrintWithData(List<Tuple<string, string>> data, Graphics gr)
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
