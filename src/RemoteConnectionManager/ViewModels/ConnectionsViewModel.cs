using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using RemoteConnectionManager.Core;
using RemoteConnectionManager.Properties;
using RemoteConnectionManager.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace RemoteConnectionManager.ViewModels
{
    public class ConnectionsViewModel : ViewModelBase
    {
        private readonly IConnectionFactory[] _connectionFactories;
        private readonly IDialogService _dialogService;

        public ConnectionsViewModel(
            IConnectionFactory[] connectionFactories,
            IDialogService dialogService)
        {
            _connectionFactories = connectionFactories;
            _dialogService = dialogService;

            Protocols = _connectionFactories
                .SelectMany(x => x.Protocols)
                .Distinct()
                .ToArray();

            Connections = new ObservableCollection<IConnection>();

            ConnectCommand = new RelayCommand<ConnectionSettings>(ExecuteConnectCommand);
            DisconnectCommand = new RelayCommand<IConnection>(ExecuteDisconnectCommand);
        }

        public Protocol[] Protocols { get; }

        public bool OnClosing()
        {
            if (Connections.Count > 0)
            {
                if (!_dialogService.ShowConfirmationDialog(Resources.ConfirmClose))
                {
                    return false;
                }
            }

            foreach (var connection in Connections)
            {
                Disconnect(connection, DisconnectReason.ApplicationExit);
            }

            return true;
        }
        
        public ObservableCollection<IConnection> Connections { get; }
        
        public RelayCommand<ConnectionSettings> ConnectCommand { get; }
        public void ExecuteConnectCommand(ConnectionSettings connectionSettings)
        {
            var connection = Connections.FirstOrDefault(x => x.ConnectionSettings == connectionSettings);
            if (connection == null)
            {
                connection = _connectionFactories
                    .First(x => x.Protocols.Contains(connectionSettings.Protocol))
                    .CreateConnection(connectionSettings);
                Connections.Add(connection);
            }
            if (!connection.IsConnected)
            {
                connection.Disconnected += ConnectionDisconnected;
                connection.Connect();
            }
        }

        public RelayCommand<IConnection> DisconnectCommand { get; }
        public void ExecuteDisconnectCommand(IConnection connection)
        {
            Disconnect(connection, DisconnectReason.ConnectionEnded);
        }

        private void ConnectionDisconnected(object sender, DisconnectReason e)
        {
            Application.Current.Dispatcher.Invoke(() => Disconnect((IConnection)sender, e));
        }

        private void Disconnect(IConnection connection, DisconnectReason reason)
        {
            connection.Disconnected -= ConnectionDisconnected;
            connection.Disconnect();

            // The user initiated the disconnect so we
            // can remove the connection.
            if (reason == DisconnectReason.ApplicationExit ||
                reason == DisconnectReason.ConnectionEnded)
            {
                connection.Destroy();
            }
            if (reason == DisconnectReason.ConnectionEnded)
            {
                Connections.Remove(connection);
            }
        }
    }
}