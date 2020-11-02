using Microsoft.Management.Infrastructure;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RDS_Abmelder
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public GUIState WindowBindings = GUIStateManager.GUIStateInstance;

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = WindowBindings;
            datagrid_sessions.ItemsSource = WindowBindings.Sessions;
            // Initially populate the domain search field
            GUIStateManager.GUIStateInstance.searchUserDomain = Environment.UserDomainName;
            GUIStateManager.GUIStateInstance.connectionBroker = functions.TryGetConnectionBroker();
        }

        private void btn_findSessions_Click(object sender, RoutedEventArgs e)
        {
            WindowBindings.Sessions.Clear();
            var Sessions = functions.GetSessions(GUIStateManager.GUIStateInstance.connectionBroker, GUIStateManager.GUIStateInstance.searchUserDomain, GUIStateManager.GUIStateInstance.searchUserName);
            if (Sessions != null && Sessions.Any())
            {
                GUIStateManager.GUIStateInstance.bNoSessions = false;
                foreach (var Session in Sessions)
                {
                    RemoteSession obj = new RemoteSession(Session.CimInstanceProperties["ServerName"].Value.ToString(), Session.CimInstanceProperties["SessionID"].Value.ToString());
                    WindowBindings.Sessions.Add(obj);
                }
            }
            else
            {
                GUIStateManager.GUIStateInstance.bNoSessions = true;
            }
        }

        private void username_keyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                btn_findSessions_Click(this, new RoutedEventArgs());
            }
        }

        private void DataGrid_ScrollParent(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
            var parent = ((Control)sender).Parent as UIElement;
            parent?.RaiseEvent(new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
            {
                RoutedEvent = MouseWheelEvent,
                Source = sender
            });
        }

        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            for (var vis = sender as Visual; vis != null; vis = VisualTreeHelper.GetParent(vis) as Visual)
                if (vis is DataGridRow)
                {
                    var row = (DataGridRow)vis;
                    row.DetailsVisibility = row.DetailsVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
                    break;
                }
        }

        private void Expander_Collapsed(object sender, RoutedEventArgs e)
        {
            for (var vis = sender as Visual; vis != null; vis = VisualTreeHelper.GetParent(vis) as Visual)
                if (vis is DataGridRow)
                {
                    var row = (DataGridRow)vis;
                    row.DetailsVisibility = row.DetailsVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
                    break;
                }
        }

        private void btn_OpenSettingsWindow(object sender, RoutedEventArgs e)
        {
            var x = new SettingsWindow();
            x.Show();
        }
    }

    public class GUIStateManager
    {
        private static GUIState _State = new GUIState();

        public static GUIState GUIStateInstance
        {
            get { return _State; }
            set { _State = value; }
        }
    }

    public class GUIState : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private bool _bNoSessions;
        public ObservableCollection<RemoteSession> Sessions = new ObservableCollection<RemoteSession>();
        private ButtonCommands.LogoffCommand m_LogoffCommand = new ButtonCommands.LogoffCommand();

        public bool bNoSessions
        {
            get { return _bNoSessions; }
            set
            {
                _bNoSessions = value;
                OnPropertyChanged();
            }
        }

        public ButtonCommands.LogoffCommand CmdLogoff
        {
            get { return m_LogoffCommand; }
        }

        public string searchUserDomain { get; set; }
        public string searchUserName { get; set; }
        public string connectionBroker { get; set; }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public class ButtonCommands
    {
        public class LogoffCommand : ICommand
        {
            public event EventHandler CanExecuteChanged;
            public LogoffCommand()
            { }

            // Wird aufgerufen um zu prüfen ob das Command ausgeführt werden kann (Button wird ausgegraut, wenn false zurückgegeben wird
            public bool CanExecute(object parameter)
            {
                return true;
            }

            // Wird vom Command Binding aufgerufen. parameter enthält das mit "CommandParameter" übergebene Objekt
            public void Execute(object parameter)
            {
                RemoteSession TypedParameter = parameter as RemoteSession;
                logoffReturn results = functions.LogoffRDPSession(TypedParameter.ServerName, TypedParameter.SessionID);
                if (results.ExitCode != 0)
                {
                    LogoffError ErrorWindow = new LogoffError(results);
                    // Make sure the ErrorWindow pops up on the same monitor,
                    // without this it's always the primary (I think)
                    ErrorWindow.Top = Application.Current.MainWindow.Top + 20;
                    ErrorWindow.Left = Application.Current.MainWindow.Left + 20;
                    ErrorWindow.Show();
                }
            }
        }

        public class KillProcessCommand : ICommand
        {
            public event EventHandler CanExecuteChanged;
            public KillProcessCommand()
            { }

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public void Execute(object parameter)
            {
                CimInstance TypedParameter = parameter as CimInstance;
                CimSession cimSession = CimSession.Create(TypedParameter.CimInstanceProperties["CSName"].Value.ToString());

                uint result = 0;
                try
                {
                    result = (uint)cimSession.InvokeMethod(TypedParameter, @"Terminate", null).ReturnValue.Value;
                }
                catch (CimException e)
                {
                    MessageBox.Show($"{e.Message} ({e.HResult})" + Environment.NewLine + "Das beenden des Prozesses schlug fehl.", "Fehler aufgetreten");
                }

                if (result != 0)
                {
                    MessageBox.Show($"Das beenden des Prozesses schlug mit Fehlercode {result} fehl.", "Fehler aufgetreten");
                }
            }
        }
    }

    public class ToLowerValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var str = value as string;
            return string.IsNullOrEmpty(str) ? string.Empty : str.ToLower();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
