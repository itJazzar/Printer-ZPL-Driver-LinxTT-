//using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Data;
//using System.Drawing;
//using System.Drawing.Printing;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Windows.Forms;
//using MaterialSkin;
//using MaterialSkin.Controls;
//using System.Configuration;
//using System.Threading;
//using static System.Windows.Forms.VisualStyles.VisualStyleElement;
//using System.Reflection.Emit;

//namespace LabelDesignerV2
//{
//    public partial class FormLabelEditor : MaterialForm
//    {
//        private static bool keyboardEnabled = true;

//        static MyLabel singleLabel;
//        static MyLabel groupLabel;

//        static MyLabel currentLabel;

//        static bool adding = false;
//        static bool moving = false;
//        static bool choosing = false;
//        static bool programChanges = false;
//        static bool objectInMove = false;

//        static string nominant = null;
//        static Keyboard keyboard = null;
//        static string labelName = null;

//        static string defaultPath = "C:\\Users\\Public\\Labels\\";

//        static int rectStatus = 0; // 0 - выбран для построения, 1 - отмечена 1-я точка, 2 - построен
//        static MyRectangle buildingRect = null;
//        static Point rectOrigin = new Point(0, 0);

//        static string[] specialTextArguments = { "Нет", "Вес", "ДатаПроизв", "ДатаГодн", "ДатаУпак", "Партия" };
//        static string[] barcodeTypes = { "DataMatrix", "EAN13", "EAN128" };

//        static MyLabelObject currentObject = null;
//        static MyLabelObject underCursObject = null;

//        private static Form _Main_Form;

//        public FormLabelEditor(Form form, bool keyboardEnabledFlag)
//        {
//            if (!System.IO.Directory.Exists(defaultPath))
//                System.IO.Directory.CreateDirectory(defaultPath);

//            InitializeComponent();

//            PrinterSettings.StringCollection printers = PrinterSettings.InstalledPrinters;

//            for (int i = 0; i < printers.Count; i++)
//            {
//                ToolStripButton printer = new ToolStripButton();
//                printer.Text = printers[i].ToString();
//                printer.Click += PRINT_MY_ASS;
//                printbut.DropDownItems.Add(printer);
//            }

//            if (keyboardEnabledFlag)
//            {
//                keyboardEnabled = keyboardEnabledFlag;
//                keyboard = new Keyboard(panelKey, 0, 0, panelKey.Width, panelKey.Height);
//            }
//            else
//            {
//                panelKey.Visible = false;
//                tableLayoutPanel1.SetRowSpan(panelLabelPlace, 3);
//            }
//            ((ToolStripProfessionalRenderer)toolStrip1.Renderer).RoundedEdges = false;
//            ((ToolStripProfessionalRenderer)toolStrip2.Renderer).RoundedEdges = false;
//            panelLabelPlace.BackColor = Color.FromArgb(92, 94, 109);
//            //toolStrip1.BackColor = Color.FromArgb(56, 60, 72);
//            _Main_Form = form;

//            panelKey.Enabled = false;
//            Thread.Sleep(100);
//            Invalidate();
//        }

//        private void LabelIsHere()
//        {
//            addBarcodebut.Enabled = true;
//            addPicturebut.Enabled = true;
//            addTextbut.Enabled = true;
//            printbut.Enabled = true;
//            textBoxLabelHeight.Enabled = true;
//            textBoxLabelName.Enabled = true;
//            materialComboBox1.Enabled = true;
//            textBoxLabelWidth.Enabled = true;
//            savebut.Enabled = true;
//            panel1.Visible = true;
//        }

//        private void PictureLabelRelocate()
//        {
//            int xOrigin = panelLabelPlace.Width / 2 - (int)(currentLabel.width * 3.937007874) / 2;
//            int yOrigin = panelLabelPlace.Height / 2 - (int)(currentLabel.height * 3.937007874) / 2 + toolStrip1.Height;
//            pictureBoxLabel.Location = new Point(xOrigin, yOrigin);
//        }

//        private void createFile_Click(object sender, EventArgs e)
//        {
//            unchooseCurrentObject();
//            if (currentLabel != null)
//            {
//                currentLabel.objects.Clear();
//                groupLabel.objects.Clear();
//                singleLabel.objects.Clear();
//            }
//            labelName = "Новая этикетка";
//            splitContainer1.Panel2Collapsed = true;

//            singleLabel = new MyLabel(56, 40);
//            groupLabel = new MyLabel(120, 60);

//            programChanges = true;
//            currentLabel = singleLabel;
//            textBoxLabelName.Text = "новая";
//            textBoxLabelWidth.Text = "56";
//            textBoxLabelHeight.Text = "40";
//            materialComboBox1.Text = "Одиночная";
//            programChanges = false;

//            updateLabel();
//            PictureLabelRelocate();
//            LabelIsHere();
//            updateListOfElements();
//        }

//        private void openFile_Click(object sender, EventArgs e)
//        {
//            List<string> listLabels = new List<string>();
//            foreach (string filePath in Directory.EnumerateFiles(defaultPath, "*.ci")) // подвыборка сформированных файлов
//            {
//                listLabels.Add(filePath);
//            }

//            if (listLabels.Count == 0)
//            {
//                MessageBox.Show("Не обнаружено ранее добавленных шаблонов. Создайте первый :)");
//                return;
//            }
//            LabelSelector labelSelector = new LabelSelector(listLabels);
//            string fileLabel = labelSelector.GetLabel();

//            if (fileLabel == null)
//                return;

//            try
//            {
//                unchooseCurrentObject();
//                if (singleLabel != null && groupLabel != null)
//                {
//                    singleLabel.objects.Clear();
//                    groupLabel.objects.Clear();
//                }

//                string filename = Path.GetFileName(fileLabel);
//                programChanges = true;

//                textBoxLabelName.Text = filename.Substring(0, filename.Length - 3);

//                programChanges = false;

//                try
//                {
//                    singleLabel = MyLabel.FromFile(defaultPath + filename);
//                    if (singleLabel == null)
//                        throw new Exception("некорректная одиночная этикетка");
//                }
//                catch (Exception exS)
//                {
//                    MessageBox.Show("Ошибка:" + exS.Message);
//                    singleLabel = new MyLabel(50, 50);
//                }

//                try
//                {
//                    groupLabel = MyLabel.FromFile(defaultPath + filename.Substring(0, filename.Length - 3) + ".cigroup");
//                    if (groupLabel == null)
//                        throw new Exception("некорректная групповая этикетка");
//                }
//                catch (Exception exG)
//                {
//                    MessageBox.Show("Ошибка:" + exG.Message);
//                    groupLabel = new MyLabel(120, 60);
//                }
//                materialComboBox1.Text = "Одиночная";
//                currentLabel = singleLabel;


//                updateLabel();
//            }
//            catch (Exception ex)
//            {

//            }
//            finally
//            {
//                programChanges = true;

//                textBoxLabelWidth.Text = currentLabel.width.ToString();
//                textBoxLabelHeight.Text = currentLabel.height.ToString();

//                programChanges = false;

//                updateListOfElements();
//                PictureLabelRelocate();
//                LabelIsHere();
//                splitContainer1.Panel2Collapsed = true;
//            }
//        }

//        private void saveFile_Click(object sender, EventArgs e) // сохранение этикетки
//        {
//            // одиночка
//            string filename = defaultPath + labelName + ".ci";
//            string result = singleLabel.ToFile(filename) + " сохранение в файл " + labelName + ".ci" + '\n';
//            filename = defaultPath + labelName + ".cigroup";
//            result += groupLabel.ToFile(filename) + " сохранение в файл " + labelName + ".cigroup";
//            MessageBox.Show(result);

//            unchooseCurrentObject();
//            Image demo = singleLabel.draw();
//            LinkLabelGTINForm linkLabel = new LinkLabelGTINForm(defaultPath + labelName, demo);
//            linkLabel.ShowDialog();
//        }

//        private void buttonAddBarcode_Click(object sender, EventArgs e) // добавление баркода
//        {
//            nominant = "barcode";
//            adding = true;
//            moving = false;
//            Cursor = System.Windows.Forms.Cursors.Hand;
//            unchooseCurrentObject();
//            updateLabel();
//        }

//        private void buttonAddPicture_Click(object sender, EventArgs e) // добавление картинки
//        {
//            nominant = "picture";
//            adding = true;
//            moving = false;
//            Cursor = System.Windows.Forms.Cursors.Hand;
//            unchooseCurrentObject();
//            updateLabel();
//        }

//        private void buttonAddTextField_Click(object sender, EventArgs e) // добавление текстового поля
//        {
//            nominant = "text";
//            adding = true;
//            moving = false;
//            Cursor = System.Windows.Forms.Cursors.Hand;
//            unchooseCurrentObject();
//            updateLabel();
//        }

//        private void buttonAddRect_Click(object sender, EventArgs e) // добавление прямоугольника
//        {
//            nominant = "rectangle";
//            adding = true;
//            moving = false;
//            Cursor = System.Windows.Forms.Cursors.IBeam;
//            rectStatus = 0;
//            unchooseCurrentObject();
//            updateLabel();
//            updateListOfElements();
//            UpdateProperties();
//        }

//        private void unchooseCurrentObject()
//        {
//            if (currentObject != null)
//            {
//                keyboard.unsetTB();
//                currentObject.unchooseobject();
//                currentObject = null;
//                panelKey.Enabled = false;
//            }
//        }
//        private void pictureBoxLabel_MouseDown(object sender, MouseEventArgs e) // добавление айтема на лейбл
//        {

//            if (choosing)
//            {
//                currentObject = currentLabel.WhoIsUnderCursor(e.X, e.Y);
//                if (currentObject != null)
//                {
//                    chooseElement.Text = "Выбрать";

//                    choosing = false;

//                    splitContainer1.Panel2Collapsed = false;
//                    UpdateProperties();
//                    return;
//                }
//            }


//            if (adding)
//            {
//                moving = false;
//                switch (nominant)
//                {
//                    case "text":
//                        {
//                            currentLabel.counterTextBoxes++;
//                            string text = "Текст";
//                            Size size = TextRenderer.MeasureText(text, new Font("Arial", 9, FontStyle.Regular));
//                            MyText newText = new MyText(9, size.Width, size.Height, e.X, e.Y, "textfield" + currentLabel.counterTextBoxes.ToString(), "Нет", text);
//                            currentLabel.addNewItem(newText);

//                            Cursor = System.Windows.Forms.Cursors.Default;
//                            adding = false;
//                            currentObject = newText;
//                            splitContainer1.Panel2Collapsed = false;
//                            break;
//                        }
//                    case "rectangle":
//                        {
//                            if (rectStatus == 0)
//                            {
//                                currentLabel.counterRects++;
//                                MyRectangle newRect = new MyRectangle(1, 5, 5, e.X, e.Y, "rectangle" + currentLabel.counterRects.ToString(), "Нет", "Нет");
//                                rectOrigin = new Point(e.X, e.Y);
//                                currentLabel.addNewItem(newRect);

//                                buildingRect = newRect;
//                                rectStatus = 1;
//                            }
//                            break;
//                        }
//                    case "barcode":
//                        {
//                            currentLabel.counterBarcodes++;
//                            MyBarcode newBarcode = new MyBarcode(50, 50, e.X, e.Y, "barcode" + currentLabel.counterBarcodes.ToString(), "DataMatrix", "010461005141025121!+\"YQQswdBBgq93LTrw");

//                            currentLabel.addNewItem(newBarcode);
//                            adding = false;
//                            Cursor = System.Windows.Forms.Cursors.Default;
//                            currentObject = newBarcode;
//                            splitContainer1.Panel2Collapsed = false;
//                            break;
//                        }
//                    case "picture":
//                        {
//                            OpenFileDialog chooseFile = new OpenFileDialog();
//                            chooseFile.Filter = "Image files (*.BMP, *.JPG, *.GIF, *.TIF, *.PNG, *.ICO, *.EMF, *.WMF)|*.bmp;*.jpg;*.gif; *.tif; *.png; *.ico; *.emf; *.wmf";
//                            if (chooseFile.ShowDialog() == DialogResult.Cancel)
//                            {
//                                adding = false;
//                                Cursor = System.Windows.Forms.Cursors.Default;
//                                return;
//                            }
//                            currentLabel.counterPictures++;
//                            string filename = chooseFile.FileName;
//                            MyPicture newPicture = new MyPicture(100, 100, e.X, e.Y, "picture" + currentLabel.counterPictures.ToString(), "Нет", filename);
//                            currentLabel.addNewItem(newPicture);
//                            adding = false;
//                            Cursor = System.Windows.Forms.Cursors.Default;
//                            currentObject = newPicture;
//                            splitContainer1.Panel2Collapsed = false;
//                            break;
//                        }
//                }


//                updateListOfElements();
//                UpdateProperties();
//            }
//            else
//                moving = true;
//            if (moving)
//            {
//                objectInMove = true;
//            }

//            updateLabel();

//        }

//        private void pictureBoxLabel_MouseMove(object sender, MouseEventArgs e)
//        {
//            if (adding && rectStatus == 1)
//            {
//                buildingRect.width = Math.Abs(e.X - buildingRect.x);
//                buildingRect.height = Math.Abs(e.Y - buildingRect.y);
//                if (e.X < rectOrigin.X)
//                {
//                    buildingRect.x = e.X;
//                    buildingRect.width = Math.Abs(e.X - rectOrigin.X);
//                }
//                if (e.Y < rectOrigin.Y)
//                {
//                    buildingRect.y = e.Y;
//                    buildingRect.height = Math.Abs(e.Y - rectOrigin.Y);
//                }
//            }
//            if (objectInMove && currentObject != null)
//            {
//                currentObject.x = e.X;
//                currentObject.y = e.Y;
//                if (splitContainer1.Panel2Collapsed == false)
//                    UpdateLocationProperties();
//            }

//            updateLabel();

//        }

//        private void pictureBoxLabel_MouseUp(object sender, MouseEventArgs e)
//        {
//            if (adding && rectStatus == 1)
//            {
//                buildingRect.width = Math.Abs(e.X - buildingRect.x);
//                buildingRect.height = Math.Abs(e.Y - buildingRect.y);
//                if (e.X < rectOrigin.X)
//                {
//                    buildingRect.x = e.X;
//                    buildingRect.width = Math.Abs(e.X - rectOrigin.X);
//                }
//                if (e.Y < rectOrigin.Y)
//                {
//                    buildingRect.y = e.Y;
//                    buildingRect.height = Math.Abs(e.Y - rectOrigin.Y);
//                }
//                rectStatus = 2;
//                adding = false;
//                Cursor = System.Windows.Forms.Cursors.Default;
//                currentObject = buildingRect;
//                splitContainer1.Panel2Collapsed = false;
//            }
//            if (objectInMove && currentObject != null)
//            {
//                currentObject.x = e.X;
//                currentObject.y = e.Y;
//                objectInMove = false;
//                moving = false;
//                if (splitContainer1.Panel2Collapsed == false)
//                    UpdateLocationProperties();
//            }

//            updateLabel();
//        }

//        // ПАРАМЕТРЫ ЭТИКЕТКИ

//        private void textBoxLabelWidth_TextChanged(object sender, EventArgs e)
//        {
//            if (!programChanges)
//            {
//                string width = textBoxLabelWidth.Text.ToString();
//                if (currentLabel != null)
//                {
//                    if (width.All(Char.IsDigit) && width.Length > 0 && width.Length < 5)
//                    {
//                        if (Int32.Parse(width) > 0)
//                        {
//                            currentLabel.width = Int32.Parse(width);
//                            updateLabel();
//                        }
//                    }

//                    PictureLabelRelocate();
//                }
//            }
//        }

//        private void textBoxLabelHeight_TextChanged(object sender, EventArgs e)
//        {
//            if (!programChanges)
//            {

//                string height = textBoxLabelHeight.Text.ToString();
//                if (currentLabel != null)
//                {
//                    if (height.All(Char.IsDigit) && height.Length > 0 && height.Length < 5)
//                    {
//                        if (Int32.Parse(height) > 0)
//                        {
//                            currentLabel.height = Int32.Parse(height);
//                            updateLabel();
//                        }
//                    }

//                    PictureLabelRelocate();
//                }
//            }
//        }

//        private void updateLabel()
//        {
//            if (currentLabel != null)
//            {
//                if (currentObject != null)
//                {
//                    currentObject.chooseObject();
//                    buttonDeleteCur.Enabled = true;
//                    buttonCancel.Enabled = true;
//                }
//                else
//                {
//                    buttonCancel.Enabled = false;
//                    buttonDeleteCur.Enabled = false;
//                }

//                programChanges = true;
//                programChanges = false;

//                pictureBoxLabel.Image = currentLabel.draw();
//                pictureBoxLabel.Invalidate();

//                chooseElement.Enabled = currentLabel.objects.Count > 0;
//            }
//        }

//        // Элементы

//        private void listViewElements_SelectedIndexChanged(object sender, EventArgs e)
//        {
//            if (materialListView1.SelectedItems.Count > 0)
//            {
//                string selected = materialListView1.SelectedItems[0].Text;
//                foreach (MyLabelObject obj in currentLabel.objects)
//                {
//                    if (selected == obj.name)
//                    {
//                        if (currentObject != null)
//                        {
//                            unchooseCurrentObject();
//                        }
//                        currentObject = obj;
//                        currentObject.chooseObject();
//                        splitContainer1.Panel2Collapsed = false;
//                        UpdateProperties();
//                    }
//                }
//            }
//            updateLabel();

//        }

//        private void updateListOfElements()
//        {
//            materialListView1.Clear();
//            foreach (MyLabelObject obj in currentLabel.objects)
//            {
//                ListViewItem itemTemp = new ListViewItem();
//                itemTemp.Text = obj.name;
//                if (obj.name.Contains("barcode"))
//                {

//                    itemTemp.Group = materialListView1.Groups[0];

//                }
//                if (obj.name.Contains("text"))
//                {

//                    itemTemp.Group = materialListView1.Groups[1];

//                }
//                if (obj.name.Contains("picture"))
//                {

//                    itemTemp.Group = materialListView1.Groups[2];

//                }
//                if (obj.name.Contains("rectangle"))
//                {

//                    itemTemp.Group = materialListView1.Groups[3];

//                }
//                materialListView1.Items.Add(itemTemp);
//            }
//        }

//        // СВОЙСТВА

//        private void textBoxName_TextChanged(object sender, EventArgs e)
//        {
//            string text = textBoxName.Text;
//            if (text.Length > 0)
//            {
//                currentObject.name = text;
//            }
//            updateLabel();
//        }

//        private void textBoxX_TextChanged(object sender, EventArgs e) // x
//        {
//            if (!programChanges)
//                if (!moving)
//                {
//                    string text = textBoxX.Text;
//                    if (text.Length > 0 && text.Length < 5 && Int32.TryParse(text, out currentObject.x))
//                    {
//                        updateLabel();
//                    }

//                }
//        }

//        private void textBoxY_TextChanged(object sender, EventArgs e) // y
//        {
//            if (!programChanges)
//                if (!moving)
//                {
//                    string text = textBoxY.Text;
//                    if (text.Length > 0 && text.Length < 5 && Int32.TryParse(text, out currentObject.y))
//                    {
//                        updateLabel();
//                    }

//                }
//        }

//        private void textBoxWidth_TextChanged(object sender, EventArgs e) // ширина
//        {
//            if (!programChanges)
//            {
//                string text = textBoxWidth.Text;
//                if (text.Length >= 1 && text.Length < 5 && text.All(Char.IsDigit))
//                {
//                    currentObject.width = Int32.Parse(text);
//                    updateLabel();

//                }
//            }
//        }

//        private void textBoxHeight_TextChanged(object sender, EventArgs e) // высота
//        {
//            if (!programChanges)
//            {
//                string text = textBoxHeight.Text;

//                if (text.Length >= 1 && text.Length < 5 && text.All(Char.IsDigit))
//                {

//                    currentObject.height = Int32.Parse(text);
//                    updateLabel();

//                }
//            }

//        }

//        private void textBoxFontSize_TextChanged(object sender, EventArgs e) // размер шрифта
//        {
//            if (!programChanges)
//            {
//                string text = textBoxFontSize.Text;
//                if (text.Length > 0 && text.Length < 5 && text.All(Char.IsDigit) && Int32.Parse(text) > 0)
//                {
//                    if (currentObject is MyText)
//                    {
//                        ((MyText)currentObject).fontSize = Int32.Parse(text);
//                        Size size = TextRenderer.MeasureText(((MyText)currentObject).data, new Font("Arial", ((MyText)currentObject).fontSize, ((MyText)currentObject).fontStyle));
//                        ((MyText)currentObject).width = size.Width;
//                        ((MyText)currentObject).height = size.Height;
//                        updateLabel();
//                        UpdateProperties();
//                    }
//                    if (currentObject is MyRectangle && Int32.Parse(text) > 0)
//                    {
//                        ((MyRectangle)currentObject).bold = Int32.Parse(text);
//                        updateLabel();
//                        UpdateProperties();
//                    }
//                    if (currentObject is MyBarcode)
//                    {
//                        ((MyBarcode)currentObject).fontSize = Int32.Parse(text);
//                        updateLabel();
//                    }
//                }

//                if (text.Length == 0 && currentObject is MyBarcode)
//                {
//                    ((MyBarcode)currentObject).fontSize = 1;
//                    updateLabel();
//                }

//                if (text.Length == 0 && currentObject is MyRectangle)
//                {
//                    ((MyRectangle)currentObject).bold = 1;
//                    updateLabel();
//                }
//                if (text.Length == 0 && currentObject is MyText)
//                {
//                    ((MyText)currentObject).fontSize = 1;
//                    updateLabel();
//                }
//            }
//        }

//        private void checkBoxBold_CheckedChanged(object sender, EventArgs e) // жир
//        {
//            if (!programChanges)
//            {
//                if (currentObject is MyText)
//                {
//                    ((MyText)currentObject).changeFontStyle(checkBoxBold.Checked);
//                    Size size = TextRenderer.MeasureText(((MyText)currentObject).data, new Font("Arial", ((MyText)currentObject).fontSize, ((MyText)currentObject).fontStyle));
//                    ((MyText)currentObject).width = size.Width;
//                    ((MyText)currentObject).height = size.Height;
//                }
//                updateLabel();
//                UpdateProperties();
//            }
//        }

//        private void textBoxInside_TextChanged(object sender, EventArgs e) // начинка
//        {
//            if (!programChanges)
//            {
//                string text = textBoxInside.Text;
//                if (currentObject is MyText)
//                {
//                    ((MyText)currentObject).data = text;
//                    Size size = TextRenderer.MeasureText(text, new Font("Arial", ((MyText)currentObject).fontSize, ((MyText)currentObject).fontStyle));
//                    ((MyText)currentObject).width = size.Width;
//                    ((MyText)currentObject).height = size.Height;

//                }
//                currentObject.data = text;
//                updateLabel();
//                UpdateProperties();

//            }

//        }

//        private void comboBoxAdditional_SelectedIndexChanged(object sender, EventArgs e)
//        {
//            if (!programChanges)
//            {
//                if (currentObject is MyText)
//                {
//                    ((MyText)currentObject).specialArgument = comboBoxAdditional.Text;
//                    switch (comboBoxAdditional.Text)
//                    {

//                        case "Вес":
//                        case "ВесСумм":
//                            {
//                                textBoxInside.Text = "0,000 кг";
//                                UpdateProperties();
//                                break;
//                            }
//                        case "ДатаПроизв":
//                        case "ДатаГодн":
//                        case "ДатаУпак":
//                            {
//                                textBoxInside.Text = "12.12.2002";
//                                UpdateProperties();
//                                break;
//                            }
//                        case "Цена":
//                        case "Стоимость":
//                            {
//                                textBoxInside.Text = "1000.00";
//                                UpdateProperties();
//                                break;
//                            }
//                        case "ДатаСумм":
//                            {
//                                textBoxInside.Text = "12.12.2002 12:00:00 / 12.12.2002 12:30:00";
//                                UpdateProperties();
//                                break;
//                            }
//                        case "Количество":
//                        case "Партия":
//                            {
//                                textBoxInside.Text = "12345678";
//                                UpdateProperties();
//                                break;
//                            }
//                    }


//                    updateLabel();
//                }
//                if (currentObject is MyBarcode)
//                {
//                    ((MyBarcode)currentObject).specialArgument = comboBoxAdditional.Text;
//                    switch (((MyBarcode)currentObject).specialArgument)
//                    {
//                        case "DataMatrix":
//                            {
//                                ((MyBarcode)currentObject).data = "0104607112814912215ze6<J93aoAx";
//                                currentObject.width = currentObject.height = 50;
//                                break;
//                            }
//                        case "EAN13":
//                            {
//                                ((MyBarcode)currentObject).data = "2200008125684";
//                                currentObject.width = 110;
//                                currentObject.height = 40;
//                                break;
//                            }
//                        case "EAN128":
//                            {
//                                ((MyBarcode)currentObject).data = "0104610051410244310301256611210605102106050";
//                                currentObject.width = 360;
//                                currentObject.height = 40;
//                                break;
//                            }
//                    }
//                    UpdateProperties();
//                    updateLabel();
//                }
//            }

//        }

//        private void textBox_Click_To_Change(object sender, EventArgs e)
//        {
//            if (!programChanges)
//            {
//                panelKey.Enabled = true;
//                if (sender.GetType().Equals(typeof(MaterialTextBox.BaseTextBox)) && keyboardEnabled)
//                    keyboard.setTB(sender as MaterialTextBox.BaseTextBox);

//                if (sender.GetType().Equals(typeof(TextBox)) && keyboardEnabled)
//                    keyboard.setTB(sender as TextBox);

//                if (currentObject is MyPicture && sender.Equals(textBoxInside))
//                {
//                    OpenFileDialog chooseFile = new OpenFileDialog();
//                    chooseFile.Filter = "Image files (*.BMP, *.JPG, *.GIF, *.TIF, *.PNG, *.ICO, *.EMF, *.WMF)|*.bmp;*.jpg;*.gif; *.tif; *.png; *.ico; *.emf; *.wmf";
//                    if (chooseFile.ShowDialog() == DialogResult.Cancel)
//                        return;
//                    ((MyPicture)currentObject).data = chooseFile.FileName;
//                }
//            }
//        }

//        private void UpdateLocationProperties()
//        {
//            textBoxX.Text = currentObject.x.ToString();
//            textBoxY.Text = currentObject.y.ToString();
//        }

//        private void UpdateProperties()
//        {
//            programChanges = true;
//            textBoxName.Text = currentObject.name;
//            textBoxX.Text = currentObject.x.ToString();
//            textBoxY.Text = currentObject.y.ToString();
//            textBoxWidth.Text = currentObject.width.ToString();
//            textBoxHeight.Text = currentObject.height.ToString();
//            if (currentObject is MyText)
//            {
//                label7.Text = "Размер шрифта";
//                label8.Visible = true;
//                textBoxInside.Visible = true;
//                checkBoxBold.Visible = true;
//                label9.Visible = true;
//                label6.Text = "Размер";
//                textBoxWidth.Visible = true;
//                comboBoxAdditional.Visible = true;
//                if (((MyText)currentObject).fontStyle == FontStyle.Regular)
//                    checkBoxBold.Checked = false;
//                else
//                    checkBoxBold.Checked = true;
//                label7.Visible = true;
//                textBoxFontSize.Visible = true;
//                label8.Text = "Содержимое";
//                textBoxFontSize.Text = ((MyText)currentObject).fontSize.ToString();
//                textBoxInside.Text = ((MyText)currentObject).data;
//                comboBoxAdditional.Items.Clear();
//                comboBoxAdditional.Items.AddRange(specialTextArguments);
//                comboBoxAdditional.Text = ((MyText)currentObject).specialArgument;
//            }
//            if (currentObject is MyRectangle)
//            {
//                label7.Visible = true;
//                textBoxFontSize.Visible = true;
//                label7.Text = "Толщина линии";
//                textBoxFontSize.Text = ((MyRectangle)currentObject).bold.ToString();
//                label6.Text = "Размер";
//                textBoxWidth.Visible = true;
//                label8.Visible = false;
//                textBoxInside.Visible = false;
//                checkBoxBold.Visible = false;
//                label9.Visible = false;
//                comboBoxAdditional.Visible = false;
//            }
//            if (currentObject is MyBarcode)
//            {
//                label7.Visible = true;
//                label7.Text = "Размер шрифта";
//                textBoxFontSize.Visible = false;
//                //label8.Visible = false;
//                //textBoxInside.Visible = false;
//                textBoxInside.Text = currentObject.data;
//                checkBoxBold.Visible = false;
//                label6.Text = "Размер";

//                textBoxFontSize.Visible = true;
//                textBoxFontSize.Text = ((MyBarcode)currentObject).fontSize.ToString();
//                textBoxWidth.Visible = true;
//                label9.Visible = true;
//                comboBoxAdditional.Visible = true;
//                comboBoxAdditional.Items.Clear();
//                comboBoxAdditional.Items.AddRange(barcodeTypes);
//                comboBoxAdditional.Text = currentObject.specialArgument;
//                if (currentObject.specialArgument == "DataMatrix")
//                {
//                    label7.Visible = false;
//                    textBoxWidth.Visible = false;
//                    textBoxFontSize.Visible = false;
//                }

//            }
//            if (currentObject is MyPicture)
//            {
//                label6.Text = "Размер";
//                textBoxWidth.Visible = false;
//                checkBoxBold.Visible = false;
//                label9.Visible = false;
//                comboBoxAdditional.Visible = false;
//                label7.Visible = false;
//                textBoxFontSize.Visible = false;

//                label8.Visible = true;
//                textBoxInside.Visible = true;
//                label8.Text = "Источник";
//                textBoxInside.Text = ((MyPicture)currentObject).data;
//            }
//            programChanges = false;
//        }

//        private void buttonDeleteCur_Click(object sender, EventArgs e)
//        {
//            if (currentObject != null)
//            {
//                currentLabel.objects.Remove(currentObject);
//                panelKey.Enabled = false;
//                currentObject = null;
//                updateLabel();
//                updateListOfElements();
//                splitContainer1.Panel2Collapsed = true;
//            }
//        }

//        private void buttonCancel_Click(object sender, EventArgs e)
//        {
//            if (currentObject != null)
//            {
//                unchooseCurrentObject();
//                moving = false;
//                updateLabel();
//                splitContainer1.Panel2Collapsed = true;
//                materialListView1.SelectedItems.Clear();


//            }
//        }


//        private void PrintCode_PrintPage(object sender, PrintPageEventArgs e) //формирование страницы печати
//        {
//            currentLabel.draw(e.Graphics);
//        }

//        private void textBoxLabelName_TextChanged(object sender, EventArgs e)
//        {
//            if (textBoxLabelName.Text.Length > 0)
//            {
//                labelName = textBoxLabelName.Text;
//            }
//        }

//        private void toolStripLabelToMain_Click(object sender, EventArgs e)
//        {
//            if (_Main_Form == null)
//                this.Close();
//            else
//            {
//                unchooseCurrentObject();
//                this.Visible = false;
//                _Main_Form.Visible = true;
//            }

//        }

//        private void PRINT_MY_ASS(object sender, EventArgs e)
//        {
//            PrintDocument printCode = new PrintDocument
//            {
//                PrintController = new StandardPrintController(),
//                PrinterSettings = new PrinterSettings
//                {
//                    PrinterName = ((ToolStripButton)sender).Text
//                }
//            };
//            printCode.PrintPage += new PrintPageEventHandler(PrintCode_PrintPage);
//            printCode.DefaultPageSettings.PaperSize = new PaperSize("work", (int)Math.Ceiling((currentLabel.width + 3) * 0.03937007874 * 100), (int)Math.Ceiling((currentLabel.height) * 0.03937007874 * 100));

//            printCode.Print();
//        }

//        private void FormLabelEditor_Resize(object sender, EventArgs e)
//        {
//            if (keyboardEnabled)
//            {
//                panelKey.Controls.Clear();
//                keyboard = new Keyboard(panelKey, 0, 0, panelKey.Width, panelKey.Height);
//            }
//        }

//        private void materialComboBox1_SelectedIndexChanged(object sender, EventArgs e)
//        {
//            splitContainer1.Panel2Collapsed = true;
//            switch (materialComboBox1.Text)
//            {
//                case "Одиночная":
//                    {
//                        currentLabel = singleLabel;
//                        textBoxLabelWidth.Text = currentLabel.width.ToString();
//                        textBoxLabelHeight.Text = currentLabel.height.ToString();
//                        break;
//                    }
//                case "Групповая":
//                    {
//                        currentLabel = groupLabel;
//                        textBoxLabelWidth.Text = currentLabel.width.ToString();
//                        textBoxLabelHeight.Text = currentLabel.height.ToString();
//                        break;
//                    }
//            }
//            if (currentObject != null)
//                unchooseCurrentObject();
//            updateLabel();
//            updateListOfElements();
//            splitContainer1.Panel2Collapsed = true;
//        }

//        private void toolStrip2_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
//        {

//        }

//        private void toolStripButton2_Click(object sender, EventArgs e)
//        {
//            if (choosing)
//            {
//                chooseElement.Text = "Выбрать";
//                choosing = false;
//            }
//            else
//            {
//                chooseElement.Text = "Отменить";
//                splitContainer1.Panel2Collapsed = true;
//                choosing = true;
//                if (currentObject != null)
//                    unchooseCurrentObject();
//                updateLabel();
//            }
//        }

//        private void addPicturebut_Click(object sender, EventArgs e)
//        {
//            nominant = "picture";
//            adding = true;
//            moving = false;
//            Cursor = System.Windows.Forms.Cursors.Hand;
//            unchooseCurrentObject();
//            updateLabel();
//        }

//        private void printbut_Click(object sender, EventArgs e)
//        {

//        }
//    }
//}
