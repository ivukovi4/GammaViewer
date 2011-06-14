using System;
using System.Linq;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;

namespace GammaViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private Dictionary<string, Shape> notes = new Dictionary<string, Shape>();
        public static string KeyFormat { get { return "line: {0};tab: {1};"; } }

        public double DelayLine { get { return 30; } }
        public double DelayTab { get { return 30; } }

        private void mouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var posXCursor = e.GetPosition(grif).X;
            var posYCursor = e.GetPosition(grif).Y;

            var line = grif.Children.OfType<Line>().Where(delegate(Line l)
            {
                if (!Regex.IsMatch(l.Name, "_line.*")) return false;

                var posLine = (l.Y1 + l.Y2) / 2;

                return posYCursor > posLine - 15 && posYCursor <= posLine + 15;
            }).FirstOrDefault();

            var tab = grif.Children.OfType<Line>().Where(delegate(Line l)
            {
                if (!Regex.IsMatch(l.Name, "_tab.*")) return false;

                var posLine = (l.X1 + l.X2) / 2;

                return posXCursor > posLine - 100 && posXCursor <= posLine;
            }).FirstOrDefault();

            if (line == null || tab == null) return;

            RefreshNote(line, tab);

            if (!Changed) Changed = true;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!(sender is MenuItem)) return;

                switch ((sender as MenuItem).Name)
                {
                    case "miQuit":
                        Close();
                        break;
                    case "miClear":
                        Clear();
                        break;
                    case "miSave":
                        Save();
                        break;
                    case "miSaveAs":
                        SaveAs();
                        break;
                    case "miOpen":
                        Open();
                        break;
                    case "miNew":
                        New();
                        break;
                    case "miAbout":
                        About();
                        break;
                    default: break;
                }
            }
            catch (FileNotFoundException fileNotFoundException)
            {
                MessageBox.Show(fileNotFoundException.Message, fileNotFoundException.Source, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void About()
        {
            MessageBox.Show(this, string.Format("© 2011 ivukovi4.{0}All rights reserved. ^_^", Environment.NewLine), "Gamma viewer", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void New()
        {
            if (Changed && !SaveQuestion()) return;

            _file = string.Empty;

            Clear();

            Changed = false;
        }

        private void Clear()
        {
            foreach (var note in notes.Values)
            {
                grif.Children.Remove(note);
            }

            notes.Clear();
            Changed = true;
        }

        private string _file = string.Empty;

        private bool _changed = false;
        public bool Changed
        {
            get { return _changed; }
            private set
            {
                _changed = value;

                if (_changed)
                {
                    Title = string.Format("Gamma viewer [{0}*]", _file);
                }
                else
                {
                    if (string.IsNullOrEmpty(_file))
                    {
                        Title = "Gamma viewer";
                    }
                    else
                    {
                        Title = string.Format("Gamma viewer [{0}]", _file);
                    }
                }
            }
        }

        private void Save()
        {
            if (string.IsNullOrEmpty(_file))
            {
                SaveAs();
            }
            else
            {
                SaveTxt(_file);
            }
        }

        private void SaveAs()
        {
            var saveDialog = new Microsoft.Win32.SaveFileDialog { Filter = "Text file|*.txt|Bitmap|*.bmp" };/*|JPG|*.jpg|PNG|*.png*/

            var dialogResult = saveDialog.ShowDialog(this);
            if (dialogResult != null && (bool)dialogResult)
            {
                if (saveDialog.FileName.EndsWith(".txt"))
                {
                    SaveTxt(saveDialog.FileName);
                    Changed = false;
                }
                else if (saveDialog.FileName.EndsWith(".bmp"))
                {
                    SaveBMP(saveDialog.FileName);
                }
            }
        }

        private void SaveTxt(string file)
        {
            var fs = File.OpenWrite(file);
            var sw = new StreamWriter(fs);
            sw.Write(string.Join(Environment.NewLine, notes.Select(n => n.Key).ToArray()));
            sw.Close();
            fs.Close();

            _file = fs.Name;
        }

        private void SaveBMP(string file)
        {
            var fs = File.Create(file);
            var bmp = new RenderTargetBitmap((int)grif.ActualWidth, (int)grif.ActualHeight + 20, 1 / 32, 1 / 32, PixelFormats.Pbgra32);
            bmp.Render(grif);
            var encoder = new TiffBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bmp));
            encoder.Save(fs);
            fs.Close();
        }

        /// <summary>
        /// Вопрос о сохранении, возвращает false если ткнули отмену, и true если да или нет
        /// </summary>
        /// <returns></returns>
        private bool SaveQuestion()
        {
            switch (MessageBox.Show(this, "Save changes to current file?", "Gamma viewer", MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Yes))
            {
                case MessageBoxResult.Yes:
                    Save();
                    return true;
                case MessageBoxResult.No:
                    return true;
                default: return false;
            }
        }

        private void Open()
        {
            if (Changed && !SaveQuestion()) return;

            var openDialog = new Microsoft.Win32.OpenFileDialog { Filter = "Text filte|*.txt" };

            var dialogResult = openDialog.ShowDialog(this);
            if (dialogResult != null && (bool)dialogResult)
            {
                if (!File.Exists(openDialog.FileName))
                    throw new FileNotFoundException();

                Clear();

                var fs = File.Open(openDialog.FileName, FileMode.Open);
                var sr = new StreamReader(fs);
                foreach (Match match in Regex.Matches(sr.ReadToEnd(), string.Format(KeyFormat, ".*", ".*")))
                {
                    var line = Regex.Match(match.Value, "_line..").Value.Trim(new[] { ';' });
                    var tab = Regex.Match(match.Value, "_tab..").Value.Trim(new[] { ';' });

                    RefreshNote(grif.Children.OfType<Line>().Where(l => l.Name == line).FirstOrDefault(),
                        grif.Children.OfType<Line>().Where(l => l.Name == tab).FirstOrDefault());

                }
                sr.Close();
                fs.Close();

                _file = fs.Name;
                Title = string.Format("Gamma viewer [{0}]", fs.Name);
                Changed = false;
            }
        }

        private void RefreshNote(Line line, Line tab)
        {
            if (notes.ContainsKey(string.Format(KeyFormat, line.Name, tab.Name)))
            {
                var note = notes[string.Format(KeyFormat, line.Name, tab.Name)];

                grif.Children.Remove(note);
                notes.Remove(string.Format(KeyFormat, line.Name, tab.Name));
            }
            else
            {
                var note = new Ellipse
                {
                    Stroke = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                    Fill = new SolidColorBrush(Color.FromRgb(192, 192, 192)),
                    StrokeThickness = 1,
                    Width = 15,
                    Height = 15
                };
                Canvas.SetLeft(note, (tab.X1 + tab.X2) / 2 - 57.5);
                Canvas.SetTop(note, (line.Y1 + line.Y2) / 2 - 7.5);

                var i = grif.Children.Add(note);
                notes.Add(string.Format(KeyFormat, line.Name, tab.Name), note);
            }
        }

        public FileStream fs { get; set; }

        private void main_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Changed)
            {
                e.Cancel = !SaveQuestion();
            }
        }
    }
}
