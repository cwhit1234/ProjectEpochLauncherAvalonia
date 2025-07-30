using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectEpochLauncherAvalonia.Services
{
    public class ServerStatusService : IDisposable
    {
        private const string SERVER_HOST = "game.project-epoch.net";
        private const int LOGIN_SERVER_PORT = 3724;
        private const int WORLD_SERVER_PORT = 8085;
        private const int CONNECTION_TIMEOUT_MS = 5000;
        private const int STATUS_UPDATE_INTERVAL_MS = 30000; // 30 seconds
        private const int OFFLINE_UPDATE_INTERVAL_MS = 60000; // 60 seconds when both servers are offline

        private readonly Timer _statusTimer;
        private readonly object _lockObject = new object();
        private bool _disposed = false;
        private bool _isChecking = false;
        private int _currentInterval = STATUS_UPDATE_INTERVAL_MS;

        public event EventHandler<ServerStatusEventArgs>? StatusUpdated;

        public ServerStatus LoginServerStatus { get; private set; } = new() { IsOnline = false, ResponseTime = 0 };
        public ServerStatus WorldServerStatus { get; private set; } = new() { IsOnline = false, ResponseTime = 0 };

        public bool IsInitialized { get; private set; } = false;

        public ServerStatusService()
        {
            LogDebug("ServerStatusService initialized");

            _statusTimer = new Timer(async _ => await CheckServerStatusAsync(), null,
                TimeSpan.Zero, TimeSpan.FromMilliseconds(_currentInterval));
        }

        private async Task CheckServerStatusAsync()
        {
            lock (_lockObject)
            {
                if (_isChecking || _disposed)
                    return;
                _isChecking = true;
            }

            try
            {
                LogDebug("Starting server status check");

                var loginStatus = await CheckServerConnectionAsync(SERVER_HOST, LOGIN_SERVER_PORT);
                var worldStatus = await CheckServerConnectionAsync(SERVER_HOST, WORLD_SERVER_PORT);

                lock (_lockObject)
                {
                    LoginServerStatus = loginStatus;
                    WorldServerStatus = worldStatus;
                    IsInitialized = true;
                }

                UpdateTimerInterval(loginStatus.IsOnline || worldStatus.IsOnline);

                StatusUpdated?.Invoke(this, new ServerStatusEventArgs
                {
                    LoginServerStatus = loginStatus,
                    WorldServerStatus = worldStatus
                });

                LogDebug($"Server status check completed - Login: {loginStatus.IsOnline}, World: {worldStatus.IsOnline}");
            }
            catch (Exception ex)
            {
                LogError($"Error during server status check: {ex.Message}");

                var errorStatus = new ServerStatus { IsOnline = false, ResponseTime = 0 };

                lock (_lockObject)
                {
                    LoginServerStatus = errorStatus;
                    WorldServerStatus = errorStatus;
                    IsInitialized = true;
                }

                StatusUpdated?.Invoke(this, new ServerStatusEventArgs
                {
                    LoginServerStatus = errorStatus,
                    WorldServerStatus = errorStatus
                });
            }
            finally
            {
                lock (_lockObject)
                {
                    _isChecking = false;
                }
            }
        }

        private void UpdateTimerInterval(bool anyServerOnline)
        {
            var newInterval = anyServerOnline ? STATUS_UPDATE_INTERVAL_MS : OFFLINE_UPDATE_INTERVAL_MS;

            if (newInterval != _currentInterval)
            {
                _currentInterval = newInterval;
                _statusTimer.Change(TimeSpan.FromMilliseconds(newInterval), TimeSpan.FromMilliseconds(newInterval));
                LogDebug($"Timer interval updated to {newInterval}ms (servers online: {anyServerOnline})");
            }
        }

        private async Task<ServerStatus> CheckServerConnectionAsync(string host, int port)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                using var tcpClient = new TcpClient();

                var connectTask = Task.Run(async () =>
                {
                    try
                    {
                        await tcpClient.ConnectAsync(host, port);
                        return tcpClient.Connected;
                    }
                    catch (SocketException ex)
                    {
                        LogDebug($"Server {host}:{port} socket error: {ex.SocketErrorCode}");
                        return false;
                    }
                    catch (Exception ex)
                    {
                        LogDebug($"Server {host}:{port} connection error: {ex.GetType().Name}");
                        return false;
                    }
                });

                // Wait for connection with timeout
                var timeoutTask = Task.Delay(CONNECTION_TIMEOUT_MS);
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                stopwatch.Stop();

                if (completedTask == connectTask)
                {
                    var connected = await connectTask;
                    if (connected)
                    {
                        LogDebug($"Server {host}:{port} connection successful in {stopwatch.ElapsedMilliseconds}ms");
                        return new ServerStatus
                        {
                            IsOnline = true,
                            ResponseTime = stopwatch.ElapsedMilliseconds
                        };
                    }
                    else
                    {
                        LogDebug($"Server {host}:{port} connection failed");
                        return new ServerStatus
                        {
                            IsOnline = false,
                            ResponseTime = 0
                        };
                    }
                }
                else
                {
                    LogDebug($"Server {host}:{port} connection timed out after {CONNECTION_TIMEOUT_MS}ms");
                    return new ServerStatus
                    {
                        IsOnline = false,
                        ResponseTime = 0
                    };
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LogError($"Unexpected error connecting to {host}:{port} - {ex.Message}");
                return new ServerStatus
                {
                    IsOnline = false,
                    ResponseTime = 0
                };
            }
        }

        public string FormatServerStatus(string serverName, ServerStatus status)
        {
            if (status.IsOnline)
            {
                return $"{serverName}: Online ({status.ResponseTime}ms)";
            }
            else
            {
                return $"{serverName}: Offline";
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            lock (_lockObject)
            {
                _disposed = true;
            }

            _statusTimer?.Dispose();
            LogDebug("ServerStatusService disposed");
        }

        private static void LogDebug(string message)
        {
            Debug.WriteLine($"[SERVER-STATUS-DEBUG] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        }

        private static void LogError(string message)
        {
            Debug.WriteLine($"[SERVER-STATUS-ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        }
    }

    public class ServerStatus
    {
        public bool IsOnline { get; set; }
        public long ResponseTime { get; set; }
    }

    public class ServerStatusEventArgs : EventArgs
    {
        public ServerStatus LoginServerStatus { get; set; } = new();
        public ServerStatus WorldServerStatus { get; set; } = new();
    }
}