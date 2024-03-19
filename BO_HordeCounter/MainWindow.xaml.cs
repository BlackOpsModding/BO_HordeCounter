using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace BO_HordeCounter
{
    public partial class MainWindow : Window
    {
        public static DependencyProperty ApplicationItemsProperty = DependencyProperty.Register("ApplicationItems", typeof(ObservableCollection<KeyValuePair<string, int>>), typeof(MainWindow), new PropertyMetadata(new ObservableCollection<KeyValuePair<string, int>>()));
        public static DependencyProperty GameConnectedStringProperty = DependencyProperty.Register("GameConnectedString", typeof(string), typeof(MainWindow), new PropertyMetadata("Game not connected"));

        public static DependencyProperty TotalHeightProperty = DependencyProperty.Register("TotalHeight", typeof(int), typeof(MainWindow), new PropertyMetadata(100, SaveApplicationConfig));
        public static DependencyProperty TotalWidthProperty = DependencyProperty.Register("TotalWidth", typeof(int), typeof(MainWindow), new PropertyMetadata(275, SaveApplicationConfig));

        public static DependencyProperty ApplicationItemsFontFamilyProperty = DependencyProperty.Register("ApplicationItemsFontFamily", typeof(System.Windows.Media.FontFamily), typeof(MainWindow), new PropertyMetadata(new System.Windows.Media.FontFamily(), SaveApplicationConfig));
        public static DependencyProperty ApplicationItemsForegroundProperty = DependencyProperty.Register("ApplicationItemsForeground", typeof(System.Windows.Media.Brush), typeof(MainWindow), new PropertyMetadata(System.Windows.Media.Brushes.White, SaveApplicationConfig));
        public static DependencyProperty ApplicationItemsSizeProperty = DependencyProperty.Register("ApplicationItemsSize", typeof(float), typeof(MainWindow), new PropertyMetadata(20f, SaveApplicationConfig));
        public static DependencyProperty ApplicationItemsFontWeightProperty = DependencyProperty.Register("ApplicationItemsFontWeight", typeof(FontWeight), typeof(MainWindow), new PropertyMetadata(FontWeights.Normal, SaveApplicationConfig));
        public static DependencyProperty ApplicationItemsFontStyleProperty = DependencyProperty.Register("ApplicationItemsFontStyle", typeof(FontStyle), typeof(MainWindow), new PropertyMetadata(FontStyles.Normal, SaveApplicationConfig));

        public ObservableCollection<KeyValuePair<string, int>> ApplicationItems
        {
            get => Dispatcher.Invoke(() => (ObservableCollection<KeyValuePair<string, int>>)GetValue(ApplicationItemsProperty));
            set => Dispatcher.Invoke(() => SetValue(ApplicationItemsProperty, value));
        }

        public System.Windows.Media.FontFamily ApplicationItemsFontFamily
        {
            get => (System.Windows.Media.FontFamily)GetValue(ApplicationItemsFontFamilyProperty);
            set => SetValue(ApplicationItemsFontFamilyProperty, value);
        }

        public System.Windows.Media.Brush ApplicationItemsForeground
        {
            get => (System.Windows.Media.Brush)GetValue(ApplicationItemsForegroundProperty);
            set => SetValue(ApplicationItemsForegroundProperty, value);
        }

        public float ApplicationItemsSize
        {
            get => (float)GetValue(ApplicationItemsSizeProperty);
            set => SetValue(ApplicationItemsSizeProperty, value);
        }

        public FontWeight ApplicationItemsFontWeight
        {
            get => (FontWeight)GetValue(ApplicationItemsFontWeightProperty);
            set => SetValue(ApplicationItemsFontWeightProperty, value);
        }

        public FontStyle ApplicationItemsFontStyle
        {
            get => (FontStyle)GetValue(ApplicationItemsFontStyleProperty);
            set => SetValue(ApplicationItemsFontStyleProperty, value);
        }

        public int TotalHeight
        {
            get => (int)GetValue(TotalHeightProperty);
            set => SetValue(TotalHeightProperty, value);
        }

        public int TotalWidth
        {
            get => (int)GetValue(TotalWidthProperty);
            set => SetValue(TotalWidthProperty, value);
        }

        public string GameConnectedString
        {
            get => Dispatcher.Invoke(() => (string)GetValue(GameConnectedStringProperty));
            set => Dispatcher.Invoke(() => SetValue(GameConnectedStringProperty, value));
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            Loaded += MainWindow_Loaded;
            Closed += MainWindow_Closed;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            if (PresentationSource.FromVisual(this) is HwndSource hwndSource)
                hwndSource.CompositionTarget.RenderMode = RenderMode.SoftwareOnly;

            base.OnSourceInitialized(e);
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            BlackOpsLibrary.m_ProcessHandle = null;
            BlackOpsLibrary.m_BlackOpsProcess = null;
            GameConnectedString = "Game not connected";

            if (Application.Current != null)
            {
                Application.Current.Shutdown();
            }
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ListMaps.ContextMenu = ContextMenu = (ContextMenu)Resources["MainMenu"];
            LoadApplicationConfig();

            await UpdateApplication();
        }

        private async Task UpdateApplication()
        {
            await Task.Run(() =>
            {
                while (true)
                {
                    if (!BlackOpsLibrary.IsGameClosed())
                    {
                        while (BlackOpsLibrary.m_BlackOpsProcess != null && !BlackOpsLibrary.m_BlackOpsProcess.HasExited)
                        {
                            try
                            {
                                GameConnectedString = string.Empty;

                                int maxZombies = 24; // not going to literally give the zombie count
                                int totalZombiesOutsideBarriers = BlackOpsLibrary.ReadInt(Constants.C_ZOMBIES_OUTSIDE_BARRIER_ADDRESS);

                                if (totalZombiesOutsideBarriers < 0)
                                {
                                    totalZombiesOutsideBarriers = 0;
                                }

                                if (maxZombies < 0)
                                {
                                    maxZombies = 0;
                                }

                                int zombiesBehindABarrier = maxZombies - totalZombiesOutsideBarriers;

                                var items = new ObservableCollection<KeyValuePair<string, int>>();
                                items.Add(new KeyValuePair<string, int>("Presumably Behind Barriers: ", zombiesBehindABarrier));
                                items.Add(new KeyValuePair<string, int>("Zombies Outside Barriers: ", totalZombiesOutsideBarriers));
                                ApplicationItems = new ObservableCollection<KeyValuePair<string, int>>(items);

                                Thread.Sleep(50);
                            }
                            catch (Exception)
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        BlackOpsLibrary.m_ProcessHandle = null;
                        BlackOpsLibrary.m_BlackOpsProcess = null;
                        GameConnectedString = "Game not connected";
                        ApplicationItems = new ObservableCollection<KeyValuePair<string, int>>();
                        Thread.Sleep(1000);
                    }
                }
            });
        }

        private static void SaveApplicationConfig(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MainWindow mainWindow = (MainWindow)d;
            mainWindow.SaveApplicationConfig();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void RightClick_Exit(object sender, RoutedEventArgs e)
        {
            BO_HordeCounterWindow.Close();
        }

        private void AboutClick(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.MessageBox.Show("Version 1.0", "About", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
        }

        private void SetApplicationItemsFont(object sender, RoutedEventArgs e)
        {
            var color = (ApplicationItemsForeground as System.Windows.Media.SolidColorBrush).Color;
            System.Drawing.FontStyle fontStyle = System.Drawing.FontStyle.Regular;

            if (ApplicationItemsFontStyle == FontStyles.Italic)
            {
                fontStyle = System.Drawing.FontStyle.Italic;
            }

            if (ApplicationItemsFontWeight == FontWeights.Bold)
            {
                fontStyle = System.Drawing.FontStyle.Bold;
            }
            System.Windows.Forms.FontDialog fontDialog = new System.Windows.Forms.FontDialog()
            {
                AllowVerticalFonts = false,
                MinSize = 0,
                MaxSize = 0,
                Font = new System.Drawing.Font(ApplicationItemsFontFamily.Source, ApplicationItemsSize, fontStyle),
                Color = System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B),
                ShowColor = true,
            };

            if (fontDialog.ShowDialog() != System.Windows.Forms.DialogResult.Cancel)
            {
                ApplicationItemsFontFamily = new System.Windows.Media.FontFamily(fontDialog.Font.FontFamily.Name);
                ApplicationItemsForeground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(fontDialog.Color.R, fontDialog.Color.G, fontDialog.Color.B));
                ApplicationItemsSize = fontDialog.Font.Size;
                ApplicationItemsFontStyle = fontDialog.Font.Style.HasFlag(System.Drawing.FontStyle.Italic) ? FontStyles.Italic : FontStyles.Normal;
                ApplicationItemsFontWeight = fontDialog.Font.Style.HasFlag(System.Drawing.FontStyle.Bold) ? FontWeights.Bold : FontWeights.Regular;
            }
        }

        private void LoadApplicationConfig()
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "BO_HordeCounter_Config.txt");
            if (File.Exists(filePath))
            {
                string[] fileText = File.ReadAllLines(filePath);

                // items
                string itemsFont = fileText.FirstOrDefault(x => x.StartsWith("ApplicationItemsFont=")).Substring("ApplicationItemsFont=".Length);
                string itemsColor = fileText.FirstOrDefault(x => x.StartsWith("ApplicationItemsColor=")).Substring("ApplicationItemsColor=".Length);
                string itemsSize = fileText.FirstOrDefault(x => x.StartsWith("ApplicationItemsSize=")).Substring("ApplicationItemsSize=".Length);
                string itemsStyle = fileText.FirstOrDefault(x => x.StartsWith("ApplicationItemsStyle=")).Substring("ApplicationItemsStyle=".Length);
                string itemsWeight = fileText.FirstOrDefault(x => x.StartsWith("ApplicationItemsWeight=")).Substring("ApplicationItemsWeight=".Length);

                // etc.
                string totalHeight = fileText.FirstOrDefault(x => x.StartsWith("TotalHeight=")).Substring("TotalHeight=".Length);
                string totalWidth = fileText.FirstOrDefault(x => x.StartsWith("TotalWidth=")).Substring("TotalWidth=".Length);

                ApplicationItemsFontFamily = new System.Windows.Media.FontFamily(itemsFont);
                ApplicationItemsForeground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(itemsColor));
                ApplicationItemsSize = float.Parse(itemsSize);
                ApplicationItemsFontStyle = (FontStyle)(new FontStyleConverter()).ConvertFromString(itemsStyle);
                ApplicationItemsFontWeight = (FontWeight)(new FontWeightConverter()).ConvertFromString(itemsWeight);
                TotalHeight = int.Parse(totalHeight);
                TotalWidth = int.Parse(totalWidth);
            }
        }

        private void SaveApplicationConfig()
        {
            string createdFilepath = Path.Combine(Directory.GetCurrentDirectory(), "BO_HordeCounter_Config.txt");
            string toWrite = string.Empty;

            toWrite += "ApplicationItemsFont=";
            toWrite += ApplicationItemsFontFamily.Source;
            toWrite += "\n";

            toWrite += "ApplicationItemsColor=";
            toWrite += (ApplicationItemsForeground as System.Windows.Media.SolidColorBrush).Color.ToString();
            toWrite += "\n";

            toWrite += "ApplicationItemsSize=";
            toWrite += ApplicationItemsSize.ToString();
            toWrite += "\n";

            toWrite += "ApplicationItemsStyle=";
            toWrite += ApplicationItemsFontStyle.ToString();
            toWrite += "\n";

            toWrite += "ApplicationItemsWeight=";
            toWrite += ApplicationItemsFontWeight.ToString();
            toWrite += "\n";

            toWrite += "TotalHeight=";
            toWrite += TotalHeight.ToString();
            toWrite += "\n";

            toWrite += "TotalWidth=";
            toWrite += TotalWidth.ToString();
            toWrite += "\n";

            File.WriteAllText(createdFilepath, toWrite);
        }
    }
}
