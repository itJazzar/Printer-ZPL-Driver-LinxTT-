using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZXing;
using ZXing.Datamatrix;
using ZXing.Datamatrix.Encoder;
using ZXing.Rendering;

namespace LabelDesignerV2
{
    public class MyLabelObject
    {
        public double px_in_mm = 150 / 25.4;
        public int width;
        public int height;
        public int x;
        public int y;
        public string name;
        bool chosen = false;
        public bool underCursor = false;
        public string specialArgument;
        public string data;

        public MyLabelObject(int width, int height, int x, int y, string name, string specialArgument, string data)
        {
            this.width = width;
            this.height = height;
            this.x = x;
            this.y = y;
            this.name = name;
            this.specialArgument = specialArgument;
            this.data = data;
        }

        public void chooseObject()
        {
            chosen = true;
        }

        public void unchooseobject()
        {
            chosen = false;
        }

        public virtual void draw(Graphics gr)
        {
            if (chosen)
            {
                gr.DrawRectangle(new Pen(Brushes.Red, 1), new Rectangle(x, y, width, height));
            }
            if (underCursor)
            {
                Pen pen = new Pen(Brushes.Red, 1);
                pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
                gr.DrawRectangle(pen, new Rectangle(x, y, width, height));
            }
        }

        public bool ImUnderCursor(int xC, int yC)
        {
            return xC >= x && xC <= x + width && yC >= y && yC <= y + height;
        }

        public virtual string toDFF(int id)
        {
            return "";
        }

        public virtual string toXFF(int id)
        {
            return "";
        }
    }

    class MyText : MyLabelObject
    {
        public int fontSize = 9;
        public FontStyle fontStyle = FontStyle.Regular;

        public MyText(int fontSize, int width, int height, int x, int y, string name, string specialArgument, string data) : base(width, height, x, y, name, specialArgument, data)
        {
            this.fontSize = fontSize;
        }

        public override void draw(Graphics gr)
        {
            gr.DrawString(data, new Font("Arial", fontSize, fontStyle), Brushes.Black, new Rectangle(x, y, width + fontSize, height + fontSize));
            base.draw(gr);
        }

        public void changeFontStyle(bool bold)
        {
            if (bold)
            {
                fontStyle = FontStyle.Bold;
                return;
            }
            fontStyle = FontStyle.Regular;
        }
        public override string toDFF(int id)
        {
            Font usingFont = new Font("Arial", fontSize, fontStyle);
            string heightF = ((int)Math.Round(fontSize * 4.2)).ToString();
            string widthF = ((int)Math.Round(fontSize * 4.2)).ToString();
            return "^FO" + ((int)Math.Round(x * px_in_mm)).ToString() + "," + ((int)Math.Round(y * px_in_mm)).ToString() + "^A0N," + heightF + "," + widthF + "^FN" + id.ToString() + "^FS\n";
        }

        public override string toXFF(int id)
        {
            return "^FN" + id.ToString() + "^FH^FD" + data + "^FS\n";
        }

    }

    class MyRectangle : MyLabelObject
    {
        public int bold = 1; // толщина 1 по умолчанию
        public MyRectangle(int bold, int width, int height, int x, int y, string name, string specialArgument, string data) : base(width, height, x, y, name, specialArgument, data)
        {
            this.bold = bold;
        }
        public override void draw(Graphics gr)
        {
            gr.DrawRectangle(new Pen(Brushes.Black, bold), new Rectangle(x, y, width, height));
            base.draw(gr);
        }
    }

    class MyBarcode : MyLabelObject
    {
        public int fontSize = 9;
        public FontStyle fontStyle = FontStyle.Regular;
        public MyBarcode(int width, int height, int x, int y, string name, string specialArgument, string data) : base(width, height, x, y, name, specialArgument, data)
        {

        }

        public bool isValid()
        {
            byte[] bytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(data);
            String result = Encoding.GetEncoding("ISO-8859-1").GetString(bytes);
            return String.Equals(data, result);
        }

        public override void draw(Graphics gr)
        {
            switch (specialArgument)
            {
                case "DataMatrix":
                    {
                        width = this.height;
                        var writer = new BarcodeWriter
                        {
                            Format = BarcodeFormat.DATA_MATRIX,

                            Options = new DatamatrixEncodingOptions
                            {
                                Height = 150,
                                Width = 150,
                                PureBarcode = true,
                                SymbolShape = SymbolShapeHint.FORCE_SQUARE,
                                GS1Format = true,
                                Margin = 0,
                                NoPadding = true,
                            }
                        };
                        if (data.Length > 0 && isValid())
                        {
                            try
                            {
                                Image im = writer.Write(data);
                                gr.DrawImage(im, x, y, width, height);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
                            }
                        }

                        break;
                    }
                case "EAN13":
                    {
                        var writer = new BarcodeWriter()
                        {
                            //Renderer = new AlternateBitmapRenderer() { TextFont = new Font("Times New Roman",9, FontStyle.Regular) },
                            Format = BarcodeFormat.EAN_13,
                            Options = new ZXing.Common.EncodingOptions
                            {
                                Height = height,
                                Width = width,
                                Margin = 0,
                                PureBarcode = true,
                                GS1Format = true
                            }
                        };
                        if (data.Length > 0 && isValid())
                        {
                            Image im = writer.Write(data);

                            gr.DrawImage(im, x, y, width, height);
                            gr.DrawString(data, new Font("Arial", fontSize, fontStyle), Brushes.Black, new Rectangle(x, y + height, width, TextRenderer.MeasureText(data, new Font("Arial", fontSize, fontStyle)).Height), new StringFormat() { Alignment = StringAlignment.Center });
                        }
                        break;
                    }
                case "EAN128":
                    {
                        var writer = new BarcodeWriter()
                        {

                            Format = BarcodeFormat.CODE_128,
                            Options = new ZXing.Common.EncodingOptions
                            {
                                Height = 420,
                                Width = 60,
                                Margin = 0,
                                PureBarcode = true,
                                GS1Format = true
                            }
                        };
                        if (data.Length > 0 && isValid())
                        {
                            Image im = writer.Write(data);
                            gr.DrawImage(im, x, y, width, height);
                            gr.DrawString(data, new Font("Arial", fontSize, fontStyle), Brushes.Black, new Rectangle(x, y + height, width, TextRenderer.MeasureText(data, new Font("Arial", fontSize, fontStyle)).Height), new StringFormat() { Alignment = StringAlignment.Center });

                        }
                        break;
                    }

            }
            base.draw(gr);
        }

        public override string toDFF(int id)
        {
            switch (specialArgument)
            {
                case "DataMatrix":
                    {
                        return "^FO" + ((int)Math.Round(x * px_in_mm)).ToString() + "," + ((int)Math.Round(y * px_in_mm)).ToString() + "^BX," + (height / 10 + 1).ToString() + ",200,,,,$,^FN" + id.ToString() + "^FS\n";
                    }
                case "EAN13":
                    {
                        return "^FO" + ((int)Math.Round(x * px_in_mm)).ToString() + "," + ((int)Math.Round(y * px_in_mm)).ToString() + "^BY3^BEN," + ((int)(height * 2.5)).ToString() + ",Y,N^FN" + id.ToString() + "^FS\n";
                    }
                case "EAN128":
                    {
                        return "";
                    }
                default:
                    {
                        return "";
                    }
            }
        }

        public override string toXFF(int id)
        {
            switch (specialArgument)
            {
                case "DataMatrix":
                    {
                        return "^FN" + id.ToString() + "^FD$1" + PrepareEscapeSymbol(data) + "^FS\n";
                    }
                case "EAN13":
                    {
                        return "^FN" + id.ToString() + "^FD" + data + "^FS\n";
                    }
                case "EAN128":
                    {
                        return "";
                    }
                default:
                    {
                        return "";
                    }
            }

        }
        private string PrepareEscapeSymbol(string code)
        {
            return code.Replace(((char)29).ToString(), "$d029");
        }
    }

    class MyPicture : MyLabelObject
    {
        public MyPicture(int width, int height, int x, int y, string name, string specialArgument, string data) : base(width, height, x, y, name, specialArgument, data)
        {
        }
        public override void draw(Graphics gr)
        {
            Bitmap bmp = (Bitmap)Image.FromFile(data);
            var dependence = (double)bmp.Width / (double)bmp.Height;
            var dependenceNow = (double)width / (double)height;
            if (dependence != dependenceNow)
            {
                width = (int)Math.Ceiling((double)height * dependence);
            }
            gr.DrawImage(Image.FromFile(data), x, y, width, height);
            base.draw(gr);
        }
    }
}
