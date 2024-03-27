using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LabelDesignerV2
{


    public class MyLabel
    {
        //public static double px_in_mm = 150 / 25.4;
        //public static double px_in_mm = PrinterSettings.DefaultPageSettings.PrinterResolution.X / 25.4; // X или Y, в зависимости от направления печати
        public static double px_in_mm = Screen.PrimaryScreen.Bounds.Width / (Screen.PrimaryScreen.Bounds.Width * 0.0393701); //Height

        public int width;
        public int height;

        public List<MyLabelObject> objects = new List<MyLabelObject>();

        public int counterTextBoxes = 0;
        public int counterBarcodes = 0;
        public int counterRects = 0;
        public int counterPictures = 0;


        public MyLabelObject WhoIsUnderCursor(int xC, int yC)
        {
            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i].ImUnderCursor(xC, yC))
                {
                    return objects[i];
                }
            }
            return null;
        }

        public MyLabel(int width, int height)
        {
            this.width = width;
            this.height = height;
        }

        public static MyLabel FromFile(string filename)
        {
            MyLabel myLabel = null;
            try
            {
                using (var sr = new StreamReader(filename))
                {
                    while (sr.Peek() >= 0)
                    {
                        string curr_string = sr.ReadLine();

                        if (curr_string.Substring(0, curr_string.IndexOf(':')) == "~LABEL")
                        {
                            // SIZE
                            curr_string = sr.ReadLine();
                            int index_st = curr_string.IndexOf(' ');
                            int index_end = curr_string.LastIndexOf(' ');
                            int width = Int32.Parse(curr_string.Substring(index_st + 1, index_end - index_st));
                            int height = Int32.Parse(curr_string.Substring(index_end + 1));


                            myLabel = new MyLabel(width, height);
                        }
                        else

                        if (curr_string.Substring(0, curr_string.IndexOf(':')) == "~TEXT_FIELD")
                        {
                            // LOCATION
                            curr_string = sr.ReadLine();
                            int index_st = curr_string.IndexOf(' ');
                            int index_end = curr_string.LastIndexOf(' ');
                            int X = Int32.Parse(curr_string.Substring(index_st + 1, index_end - index_st));
                            int Y = Int32.Parse(curr_string.Substring(index_end + 1));
                            // SIZE
                            curr_string = sr.ReadLine();
                            index_st = curr_string.IndexOf(' ');
                            index_end = curr_string.LastIndexOf(' ');
                            int width = Int32.Parse(curr_string.Substring(index_st + 1, index_end - index_st));
                            int height = Int32.Parse(curr_string.Substring(index_end + 1));
                            //TYPE
                            curr_string = sr.ReadLine();
                            index_st = curr_string.IndexOf(' ');
                            string type = curr_string.Substring(index_st + 1);
                            // CONTENT
                            curr_string = sr.ReadLine();
                            index_st = curr_string.IndexOf(' ') + 1;
                            string text = curr_string.Substring(index_st).Replace("%%%", "\r\n");
                            // FONT SIZE
                            curr_string = sr.ReadLine();
                            index_st = curr_string.IndexOf(' ') + 1;
                            int font_size = Int32.Parse(curr_string.Substring(index_st));
                            // FONT STYLE
                            curr_string = sr.ReadLine();
                            index_st = curr_string.IndexOf(' ') + 1;
                            string font_style = curr_string.Substring(index_st);

                            myLabel.counterTextBoxes++;
                            MyText newText = new MyText(font_size, width, height, X, Y, "textfield" + myLabel.counterTextBoxes.ToString(), type, text);
                            if (font_style == "Regular")
                                newText.fontStyle = FontStyle.Regular;
                            else
                                newText.fontStyle = FontStyle.Bold;

                            myLabel.addNewItem(newText);
                        }
                        else
                        if (curr_string.Substring(0, curr_string.IndexOf(':')) == "~BARCODE")
                        {
                            // LOCATION
                            curr_string = sr.ReadLine();
                            int index_st = curr_string.IndexOf(' ');
                            int index_end = curr_string.LastIndexOf(' ');
                            int X = Int32.Parse(curr_string.Substring(index_st + 1, index_end - index_st));
                            int Y = Int32.Parse(curr_string.Substring(index_end + 1));
                            // SIZE
                            curr_string = sr.ReadLine();
                            index_st = curr_string.IndexOf(' ');
                            index_end = curr_string.LastIndexOf(' ');
                            int width = Int32.Parse(curr_string.Substring(index_st + 1, index_end - index_st));
                            int height = Int32.Parse(curr_string.Substring(index_end + 1));
                            // TYPE
                            curr_string = sr.ReadLine();
                            index_st = curr_string.IndexOf(' ') + 1;
                            string type = curr_string.Substring(index_st);
                            // FONT
                            curr_string = sr.ReadLine();
                            index_st = curr_string.IndexOf(' ') + 1;
                            string fontSize = curr_string.Substring(index_st);

                            string dataBar = "";
                            myLabel.counterBarcodes++;
                            switch (type)
                            {
                                case "DataMatrix":
                                    {
                                        dataBar = "0104607112814912215ze6<J93aoAx";
                                        break;
                                    }
                                case "EAN13":
                                    {
                                        dataBar = "2200008125684";
                                        break;
                                    }
                                case "EAN128":
                                    {
                                        dataBar = "0104610051410244310301256611210605102106050";
                                        break;
                                    }
                            }
                            MyBarcode newBarcode = new MyBarcode(width, height, X, Y, "barcode" + myLabel.counterBarcodes.ToString(), type, dataBar);
                            newBarcode.fontSize = Int32.Parse(fontSize);
                            myLabel.addNewItem(newBarcode);
                        }
                        else
                        if (curr_string.Substring(0, curr_string.IndexOf(':')) == "~PICTURE")
                        {
                            // LOCATION
                            curr_string = sr.ReadLine();
                            int index_st = curr_string.IndexOf(' ');
                            int index_end = curr_string.LastIndexOf(' ');
                            int X = Int32.Parse(curr_string.Substring(index_st + 1, index_end - index_st));
                            int Y = Int32.Parse(curr_string.Substring(index_end + 1));
                            // SIZE
                            curr_string = sr.ReadLine();
                            index_st = curr_string.IndexOf(' ');
                            index_end = curr_string.LastIndexOf(' ');
                            int width = Int32.Parse(curr_string.Substring(index_st + 1, index_end - index_st));
                            int height = Int32.Parse(curr_string.Substring(index_end + 1));
                            // SOURCE
                            curr_string = sr.ReadLine();
                            index_st = curr_string.IndexOf(' ') + 1;
                            string source = curr_string.Substring(index_st);

                            myLabel.counterPictures++;
                            MyPicture newPicture = new MyPicture(width, height, X, Y, "picture" + myLabel.counterPictures.ToString(), "НЕТ", source);

                            myLabel.addNewItem(newPicture);
                        }
                        else
                        if (curr_string.Substring(0, curr_string.IndexOf(':')) == "~RECTANGLE")
                        {
                            // LOCATION
                            curr_string = sr.ReadLine();
                            int index_st = curr_string.IndexOf(' ');
                            int index_end = curr_string.LastIndexOf(' ');
                            int X = Int32.Parse(curr_string.Substring(index_st + 1, index_end - index_st));
                            int Y = Int32.Parse(curr_string.Substring(index_end + 1));
                            // SIZE
                            curr_string = sr.ReadLine();
                            index_st = curr_string.IndexOf(' ');
                            index_end = curr_string.LastIndexOf(' ');
                            int width = Int32.Parse(curr_string.Substring(index_st + 1, index_end - index_st));
                            int height = Int32.Parse(curr_string.Substring(index_end + 1));
                            // BOLD
                            curr_string = sr.ReadLine();
                            index_st = curr_string.IndexOf(' ') + 1;
                            int bold = Int32.Parse(curr_string.Substring(index_st));

                            myLabel.counterRects++;
                            MyRectangle newRectangle = new MyRectangle(bold, width, height, X, Y, "rectangle" + myLabel.counterRects.ToString(), "Нет", "Нет");
                            myLabel.addNewItem(newRectangle);
                        }
                        else
                        {
                            throw new Exception("Некорректная этикетка");
                        }
                    }
                }
                return myLabel;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка:" + ex.Message);
                return null;
            }


        }

        public string ToFile(string filename)
        {
            try
            {
                using (var sw = new StreamWriter(filename))
                {

                    sw.WriteLine("~LABEL:");
                    sw.WriteLine("SIZE: " + this.width + ' ' + this.height);

                    foreach (MyLabelObject cont in this.objects)
                    {
                        if (cont.GetType() == typeof(MyText)) // текстовые поля
                        {
                            sw.WriteLine("~TEXT_FIELD:");
                            sw.WriteLine("LOCATION: " + cont.x.ToString() + ' ' + cont.y.ToString());
                            sw.WriteLine("SIZE: " + cont.width.ToString() + ' ' + cont.height.ToString());
                            sw.WriteLine("TYPE: " + ((MyText)cont).specialArgument.ToString());
                            sw.WriteLine("CONTENT: " + ((MyText)cont).data.ToString().Replace("\r\n", "%%%"));
                            sw.WriteLine("FONT_SIZE: " + ((MyText)cont).fontSize.ToString());
                            sw.WriteLine("FONT_STYLE: " + ((MyText)cont).fontStyle.ToString());
                        }
                        if (cont.GetType() == typeof(MyBarcode)) // штрихкоды
                        {

                            sw.WriteLine("~BARCODE:");
                            sw.WriteLine("LOCATION: " + cont.x.ToString() + ' ' + cont.y.ToString());
                            sw.WriteLine("SIZE: " + cont.width.ToString() + ' ' + cont.height.ToString());
                            sw.WriteLine("TYPE: " + ((MyBarcode)cont).specialArgument.ToString());
                            sw.WriteLine("FONT_SIZE: " + ((MyBarcode)cont).fontSize.ToString());
                        }
                        if (cont.GetType() == typeof(MyPicture)) // картинки
                        {
                            sw.WriteLine("~PICTURE:");
                            sw.WriteLine("LOCATION: " + cont.x.ToString() + ' ' + cont.y.ToString());
                            sw.WriteLine("SIZE: " + cont.width.ToString() + ' ' + cont.height.ToString());
                            sw.WriteLine("SOURCE: " + ((MyPicture)cont).data.ToString());
                        }
                        if (cont.GetType() == typeof(MyRectangle)) // прямоугольники
                        {
                            sw.WriteLine("~RECTANGLE:");
                            sw.WriteLine("LOCATION: " + cont.x.ToString() + ' ' + cont.y.ToString());
                            sw.WriteLine("SIZE: " + cont.width.ToString() + ' ' + cont.height.ToString());
                            sw.WriteLine("BOLD: " + ((MyRectangle)cont).bold.ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return "Не удалось выполнить";
            }
            return "Выполнено";

        }

        public void changeParametrs(int width, int height)
        {
            this.width = width;
            this.height = height;
        }

        public void addNewItem(MyLabelObject myLabelObject)
        {
            objects.Add(myLabelObject);
        }

        public Image drawEmpty()
        {
            Bitmap bitmap = new Bitmap(((int)Math.Round(width * px_in_mm)), ((int)Math.Round(height * px_in_mm)));
            using (Graphics gr = Graphics.FromImage(bitmap))
            {
                Brush brush = Brushes.White;
                gr.FillRectangle(brush, new Rectangle(0, 0, ((int)Math.Round(width * px_in_mm)), ((int)Math.Round(height * px_in_mm))));

            }
            return bitmap;
        }

        public Image draw()
        {
            Bitmap bitmap = new Bitmap(((int)Math.Round(width * px_in_mm)), ((int)Math.Round(height * px_in_mm)));
            using (Graphics gr = Graphics.FromImage(bitmap))
            {
                Brush brush = Brushes.White;
                gr.FillRectangle(brush, new Rectangle(0, 0, ((int)Math.Round(width * px_in_mm)), ((int)Math.Round(height * px_in_mm))));
                foreach (MyLabelObject obj in objects)
                {
                    obj.draw(gr);
                }

            }
            return bitmap;
        }

        public void draw(Graphics gr)
        {
            foreach (MyLabelObject obj in objects)
            {
                obj.draw(gr);
            }
        }
        public string toDFF() // дадим шаблон
        {
            string result = "^XA\n";
            result += "^DFFORMAT^FS\n^CI28\n";
            for (int i = 0; i < objects.Count; i++)
            {
                result += objects[i].toDFF(i + 1);
            }
            result += "^XZ";
            return result;
        }

        public string toXFF() // заполним
        {
            string result = "^XA\n";
            result += "^XFFORMAT^FS\n^CI28\n";
            for (int i = 0; i < objects.Count; i++)
            {
                result += objects[i].toXFF(i + 1);
            }
            result += "^XZ";
            return result;
        }
    }
}
