using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace LSG.Infrastructure
{
    /// <summary>
    /// Provides asynchronous client connections for TCP network services.
    /// https://github.com/ygoe/AsyncTcpClient/blob/master/AsyncTcpClient/AsyncTcpClient.cs
    /// </summary>
    /// <remarks>
    /// This class can be used directly when setting the relevant callback methods
    /// <see cref="ConnectedCallback"/>, <see cref="ClosedCallback"/> or
    /// <see cref="ReceivedCallback"/>. Alternatively, a class inheriting from
    /// <see cref="AsyncTcpClient"/> can implement the client logic by overriding the protected
    /// methods.
    /// </remarks>
    public class AsyncTcpClient : IDisposable
    {
        #region Private data

        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private TaskCompletionSource<bool> _closedTcs = new TaskCompletionSource<bool>();

        #endregion Private data

        #region Constructors

        /// <summary>
        /// Initialises a new instance of the <see cref="AsyncTcpClient"/> class.
        /// </summary>
        public AsyncTcpClient()
        {
            _closedTcs.SetResult(true);
        }

        #endregion Constructors

        #region Events

        /// <summary>
        /// Occurs when a trace message is available.
        /// </summary>
        public event EventHandler<AsyncTcpEventArgs> Message;

        #endregion Events

        #region Properties

        /// <summary>
        /// Gets or sets the <see cref="TcpClient"/> to use. Only for client connections that were
        /// accepted by an <see cref="AsyncTcpListener"/>.
        /// </summary>
        public TcpClient ServerTcpClient { get; set; }

        /// <summary>
        /// Gets or sets the amount of time an <see cref="AsyncTcpClient"/> will wait to connect
        /// once a connection operation is initiated.
        /// </summary>
        public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Gets or sets the maximum amount of time an <see cref="AsyncTcpClient"/> will wait to
        /// connect once a repeated connection operation is initiated. The actual connection
        /// timeout is increased with every try and reset when a connection is established.
        /// </summary>
        public TimeSpan MaxConnectTimeout { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Gets or sets a value indicating whether the client should try to reconnect after the
        /// connection was closed.
        /// </summary>
        public bool AutoReconnect { get; set; }

        /// <summary>
        /// Gets or sets the name of the host to connect to.
        /// </summary>
        public string HostName { get; set; }

        /// <summary>
        /// Gets or sets the IP address of the host to connect to.
        /// Only regarded if <see cref="HostName"/> is null or empty.
        /// </summary>
        public IPAddress IPAddress { get; set; }

        /// <summary>
        /// Gets or sets the port number of the remote host.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Gets the buffer of data that was received from the remote host.
        /// </summary>
        public ByteBuffer ByteBuffer { get; private set; } = new ByteBuffer();

        /// <summary>
        /// A <see cref="Task"/> that can be awaited to close the connection. This task will
        /// complete when the connection was closed remotely.
        /// </summary>
        public Task ClosedTask => _closedTcs.Task;

        /// <summary>
        /// Gets a value indicating whether the <see cref="ClosedTask"/> has completed.
        /// </summary>
        public bool IsClosing => ClosedTask.IsCompleted;

        /// <summary>
        /// Called when the client has connected to the remote host. This method can implement the
        /// communication logic to execute when the connection was established. The connection will
        /// not be closed before this method completes.
        /// </summary>
        /// <remarks>
        /// This callback method may not be called when the <see cref="OnConnectedAsync"/> method
        /// is overridden by a derived class.
        /// </remarks>
        public Func<AsyncTcpClient, bool, Task> ConnectedCallback { get; set; }

        /// <summary>
        /// Called when the connection was closed. The parameter specifies whether the connection
        /// was closed by the remote host.
        /// </summary>
        /// <remarks>
        /// This callback method may not be called when the <see cref="OnClosed"/> method is
        /// overridden by a derived class.
        /// </remarks>
        public Action<AsyncTcpClient, bool> ClosedCallback { get; set; }

        public bool IsConnected => _tcpClient?.Connected ?? false;

        /// <summary>
        /// Called when data was received from the remote host. The parameter specifies the number
        /// of bytes that were received. This method can implement the communication logic to
        /// execute every time data was received. New data will not be received before this method
        /// completes.
        /// </summary>
        /// <remarks>
        /// This callback method may not be called when the <see cref="OnReceivedAsync"/> method
        /// is overridden by a derived class.
        /// </remarks>
        public Func<AsyncTcpClient, int, Task> ReceivedCallback { get; set; }

        #endregion Properties

        #region Public methods

        /// <summary>
        /// Runs the client connection asynchronously.
        /// </summary>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public async Task ConnectAsync()
        {
            var isReconnected = false;
            var reconnectTry = -1;
            do
            {
                reconnectTry++;
                ByteBuffer = new ByteBuffer();
                if (ServerTcpClient != null)
                {
                    // Take accepted connection from listener
                    _tcpClient = ServerTcpClient;
                }
                else
                {
                    // Try to connect to remote host
                    var connectTimeout = TimeSpan.FromTicks(ConnectTimeout.Ticks +
                                                            (MaxConnectTimeout.Ticks - ConnectTimeout.Ticks) / 20 *
                                                            Math.Min(reconnectTry, 20));
                    _tcpClient = new TcpClient(AddressFamily.InterNetworkV6);
                    _tcpClient.Client.DualMode = true;
                    Message?.Invoke(this, new AsyncTcpEventArgs("Connecting to server"));
                    Task connectTask;
                    if (!string.IsNullOrWhiteSpace(HostName))
                    {
                        connectTask = _tcpClient.ConnectAsync(HostName, Port);
                    }
                    else
                    {
                        connectTask = _tcpClient.ConnectAsync(IPAddress, Port);
                    }

                    var timeoutTask = Task.Delay(connectTimeout);
                    if (await Task.WhenAny(connectTask, timeoutTask) == timeoutTask)
                    {
                        Message?.Invoke(this, new AsyncTcpEventArgs("Connection timeout"));
                        continue;
                    }

                    try
                    {
                        await connectTask;
                    }
                    catch (Exception ex)
                    {
                        Message?.Invoke(this, new AsyncTcpEventArgs("Error connecting to remote host", ex));
                        await timeoutTask;
                        continue;
                    }
                }

                reconnectTry = -1;

                // Read until the connection is closed.
                // A closed connection can only be detected while reading, so we need to read
                // permanently, not only when we might use received data.


                _closedTcs = new TaskCompletionSource<bool>();
                await OnConnectedAsync(isReconnected);


                isReconnected = true;
            } while (AutoReconnect && ServerTcpClient == null);
        }

        public async Task ReceiveAsync()
        {
            // 10 KiB should be enough for every Ethernet packet
            _stream = _tcpClient.GetStream();

            var buffer = new byte[10240];
            while (true)
            {
                int readLength;
                try
                {
                    readLength = await _stream.ReadAsync(buffer, 0, buffer.Length);
                }
                catch (IOException ex) when ((ex.InnerException as SocketException)?.ErrorCode ==
                                             (int) SocketError.OperationAborted)
                {
                    // Warning: This error code number (995) may change.
                    // See https://docs.microsoft.com/en-us/windows/desktop/winsock/windows-sockets-error-codes-2
                    Message?.Invoke(this, new AsyncTcpEventArgs("Connection closed locally", ex));
                    readLength = -1;
                }
                catch (IOException ex) when ((ex.InnerException as SocketException)?.ErrorCode ==
                                             (int) SocketError.ConnectionAborted)
                {
                    Message?.Invoke(this, new AsyncTcpEventArgs("Connection aborted", ex));
                    readLength = -1;
                }
                catch (IOException ex) when ((ex.InnerException as SocketException)?.ErrorCode ==
                                             (int) SocketError.ConnectionReset)
                {
                    Message?.Invoke(this, new AsyncTcpEventArgs("Connection reset remotely", ex));
                    readLength = -2;
                }
                catch (Exception ex)
                {
                    Message?.Invoke(this, new AsyncTcpEventArgs("Connection closed", ex));
                    readLength = -2;
                }


                if (readLength <= 0)
                {
                    if (readLength == 0)
                    {
                        Message?.Invoke(this, new AsyncTcpEventArgs("Connection closed remotely"));
                    }

                    _closedTcs.TrySetResult(true);
                    OnClosed(readLength != -1);
                    return;
                }

                var segment = new ArraySegment<byte>(buffer, 0, readLength);
                ByteBuffer.Enqueue(segment);
                await OnReceivedAsync(readLength);
            }
        }

        /// <summary>
        /// Closes the socket connection normally. This does not release the resources used by the
        /// <see cref="AsyncTcpClient"/>.
        /// </summary>
        public void Disconnect(bool reuseSocket = false)
        {
            _tcpClient.Client.Disconnect(reuseSocket);
        }


        /// <summary>
        /// Releases the managed and unmanaged resources used by the <see cref="AsyncTcpClient"/>.
        /// Closes the connection to the remote host and disabled automatic reconnecting.
        /// </summary>
        public void Dispose()
        {
            AutoReconnect = false;
            _tcpClient?.Dispose();
            _stream = null;
        }

        /// <summary>
        /// Waits asynchronously until received data is available in the buffer.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token used to propagate notification that this operation should be canceled.</param>
        /// <returns>true, if data is available; false, if the connection is closing.</returns>
        /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was canceled.</exception>
        public async Task<bool> WaitAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return await Task.WhenAny(ByteBuffer.WaitAsync(cancellationToken), _closedTcs.Task) != _closedTcs.Task;
        }

        /// <summary>
        /// Sends data to the remote host.
        /// </summary>
        /// <param name="bytes">The data to send.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public async Task SendAsync(byte[] bytes)
        {
            if (_tcpClient.Client.Connected)
            {
                await _stream.WriteAsync(bytes, 0, bytes.Length);
            }
        }

        #endregion Public methods

        #region Protected virtual methods

        /// <summary>
        /// Called when the client has connected to the remote host. This method can implement the
        /// communication logic to execute when the connection was established. The connection will
        /// not be closed before this method completes.
        /// </summary>
        /// <param name="isReconnected">true, if the connection was closed and automatically reopened;
        ///   false, if this is the first established connection for this client instance.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        protected virtual Task OnConnectedAsync(bool isReconnected)
        {
            if (ConnectedCallback != null)
            {
                return ConnectedCallback(this, isReconnected);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Called when the connection was closed.
        /// </summary>
        /// <param name="remote">true, if the connection was closed by the remote host; false, if
        ///   the connection was closed locally.</param>
        protected virtual void OnClosed(bool remote)
        {
            ClosedCallback?.Invoke(this, remote);
        }

        /// <summary>
        /// Called when data was received from the remote host. This method can implement the
        /// communication logic to execute every time data was received. New data will not be
        /// received before this method completes.
        /// </summary>
        /// <param name="count">The number of bytes that were received. The actual data is available
        ///   through the <see cref="ByteBuffer"/>.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        protected virtual Task OnReceivedAsync(int count)
        {
            if (ReceivedCallback != null)
            {
                return ReceivedCallback(this, count);
            }

            return Task.CompletedTask;
        }

        #endregion Protected virtual methods
    }

    /// <summary>
    /// Provides data for the <see cref="AsyncTcpClient.Message"/> event.
    /// </summary>
    public class AsyncTcpEventArgs : EventArgs
    {
        /// <summary>
        /// Initialises a new instance of the <see cref="AsyncTcpEventArgs"/> class.
        /// </summary>
        /// <param name="message">The trace message.</param>
        /// <param name="exception">The exception that was thrown, if any.</param>
        public AsyncTcpEventArgs(string message, Exception exception = null)
        {
            Message = message;
            Exception = exception;
        }

        /// <summary>
        /// Gets the trace message.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the exception that was thrown, if any.
        /// </summary>
        public Exception Exception { get; }
    }

    public class ByteBuffer
    {
        #region Private data

        private readonly object syncObj = new object();

        /// <summary>
        /// The internal buffer.
        /// </summary>
        private byte[] buffer = new byte[1024];

        /// <summary>
        /// The buffer index of the first byte to dequeue.
        /// </summary>
        private int head;

        /// <summary>
        /// The buffer index of the last byte to dequeue.
        /// </summary>
        private int tail = -1;

        /// <summary>
        /// Indicates whether the buffer is empty. The empty state cannot be distinguished from the
        /// full state with <see cref="head"/> and <see cref="tail"/> alone.
        /// </summary>
        private bool isEmpty = true;

        /// <summary>
        /// Used to signal the waiting <see cref="DequeueAsync"/> method.
        /// Set when new data becomes available. Only reset there.
        /// </summary>
        private TaskCompletionSource<bool> dequeueManualTcs =
            new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>
        /// Used to signal the waiting <see cref="WaitAsync"/> method.
        /// Set when new data becomes availalble. Reset when the queue is empty.
        /// </summary>
        private TaskCompletionSource<bool> availableTcs =
            new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        #endregion Private data

        #region Constructors

        /// <summary>
        /// Initialises a new instance of the <see cref="ByteBuffer"/> class that is empty and has
        /// the default initial capacity.
        /// </summary>
        public ByteBuffer()
        {
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="ByteBuffer"/> class that contains bytes
        /// copied from the specified collection and has sufficient capacity to accommodate the
        /// number of bytes copied.
        /// </summary>
        /// <param name="bytes">The collection whose bytes are copied to the new <see cref="ByteBuffer"/>.</param>
        public ByteBuffer(byte[] bytes)
        {
            Enqueue(bytes);
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="ByteBuffer"/> class that is empty and has
        /// the specified initial capacity.
        /// </summary>
        /// <param name="capacity">The initial number of bytes that the <see cref="ByteBuffer"/> can contain.</param>
        public ByteBuffer(int capacity)
        {
            SetCapacity(capacity);
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Gets the number of bytes contained in the buffer.
        /// </summary>
        public int Count
        {
            get
            {
                lock (syncObj)
                {
                    if (isEmpty)
                    {
                        return 0;
                    }

                    if (tail >= head)
                    {
                        return tail - head + 1;
                    }

                    return Capacity - head + tail + 1;
                }
            }
        }

        /// <summary>
        /// Gets the current buffer contents.
        /// </summary>
        public byte[] Buffer
        {
            get
            {
                lock (syncObj)
                {
                    byte[] bytes = new byte[Count];
                    if (!isEmpty)
                    {
                        if (tail >= head)
                        {
                            Array.Copy(buffer, head, bytes, 0, tail - head + 1);
                        }
                        else
                        {
                            Array.Copy(buffer, head, bytes, 0, Capacity - head);
                            Array.Copy(buffer, 0, bytes, Capacity - head, tail + 1);
                        }
                    }

                    return bytes;
                }
            }
        }

        /// <summary>
        /// Gets the capacity of the buffer.
        /// </summary>
        public int Capacity => buffer.Length;

        #endregion Properties

        #region Public methods

        /// <summary>
        /// Removes all bytes from the buffer.
        /// </summary>
        public void Clear()
        {
            lock (syncObj)
            {
                head = 0;
                tail = -1;
                isEmpty = true;
                Reset(ref availableTcs);
            }
        }

        /// <summary>
        /// Sets the buffer capacity. Existing bytes are kept in the buffer.
        /// </summary>
        /// <param name="capacity">The new buffer capacity.</param>
        public void SetCapacity(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "The capacity must not be negative.");
            if (capacity < Count)
                throw new ArgumentOutOfRangeException(nameof(capacity),
                    "The capacity is too small to hold the current buffer content.");

            lock (syncObj)
            {
                if (capacity != buffer.Length)
                {
                    byte[] newBuffer = new byte[capacity];
                    Array.Copy(Buffer, newBuffer, Count);
                    buffer = newBuffer;
                }
            }
        }

        /// <summary>
        /// Sets the capacity to the actual number of bytes in the buffer, if that number is less
        /// than 90 percent of current capacity.
        /// </summary>
        public void TrimExcess()
        {
            lock (syncObj)
            {
                if (Count < Capacity * 0.9)
                {
                    SetCapacity(Count);
                }
            }
        }

        /// <summary>
        /// Adds bytes to the end of the buffer.
        /// </summary>
        /// <param name="bytes">The bytes to add to the buffer.</param>
        public void Enqueue(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            Enqueue(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Adds bytes to the end of the buffer.
        /// </summary>
        /// <param name="segment">The bytes to add to the buffer.</param>
        public void Enqueue(ArraySegment<byte> segment)
        {
            Enqueue(segment.Array, segment.Offset, segment.Count);
        }

        /// <summary>
        /// Adds bytes to the end of the buffer.
        /// </summary>
        /// <param name="bytes">The bytes to add to the buffer.</param>
        /// <param name="offset">The index in <paramref name="bytes"/> of the first byte to add.</param>
        /// <param name="count">The number of bytes to add.</param>
        public void Enqueue(byte[] bytes, int offset, int count)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (offset + count > bytes.Length)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (count == 0)
                return; // Nothing to do

            lock (syncObj)
            {
                if (Count + count > Capacity)
                {
                    SetCapacity(Math.Max(Capacity * 2, Count + count));
                }

                int tailCount;
                int wrapCount;
                if (tail >= head || isEmpty)
                {
                    tailCount = Math.Min(Capacity - 1 - tail, count);
                    wrapCount = count - tailCount;
                }
                else
                {
                    tailCount = Math.Min(head - 1 - tail, count);
                    wrapCount = 0;
                }

                if (tailCount > 0)
                {
                    Array.Copy(bytes, offset, buffer, tail + 1, tailCount);
                }

                if (wrapCount > 0)
                {
                    Array.Copy(bytes, offset + tailCount, buffer, 0, wrapCount);
                }

                tail = (tail + count) % Capacity;
                isEmpty = false;
                Set(dequeueManualTcs);
                Set(availableTcs);
            }
        }

        /// <summary>
        /// Removes and returns bytes at the beginning of the buffer.
        /// </summary>
        /// <param name="maxCount">The maximum number of bytes to dequeue.</param>
        /// <returns>The dequeued bytes. This can be fewer than requested if no more bytes are available.</returns>
        public byte[] Dequeue(int maxCount)
        {
            return DequeueInternal(maxCount, peek: false);
        }

        /// <summary>
        /// Returns bytes at the beginning of the buffer without removing them.
        /// </summary>
        /// <param name="maxCount">The maximum number of bytes to peek.</param>
        /// <returns>The bytes at the beginning of the buffer. This can be fewer than requested if
        ///   no more bytes are available.</returns>
        public byte[] Peek(int maxCount)
        {
            return DequeueInternal(maxCount, peek: true);
        }

        /// <summary>
        /// Removes and returns bytes at the beginning of the buffer. Waits asynchronously until
        /// <paramref name="count"/> bytes are available.
        /// </summary>
        /// <param name="count">The number of bytes to dequeue.</param>
        /// <param name="cancellationToken">A cancellation token used to propagate notification that
        ///	  this operation should be canceled.</param>
        /// <returns>The bytes at the beginning of the buffer.</returns>
        public async Task<byte[]> DequeueAsync(int count,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "The count must not be negative.");

            while (true)
            {
                TaskCompletionSource<bool> myDequeueManualTcs;
                lock (syncObj)
                {
                    if (count <= Count)
                    {
                        return Dequeue(count);
                    }

                    myDequeueManualTcs = Reset(ref dequeueManualTcs);
                }

                await AwaitAsync(myDequeueManualTcs, cancellationToken);
            }
        }

        /// <summary>
        /// Waits asynchronously until bytes are available.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token used to propagate notification that
        ///   this operation should be canceled.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public async Task WaitAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            TaskCompletionSource<bool> myAvailableTcs;
            lock (syncObj)
            {
                if (Count > 0)
                {
                    return;
                }

                myAvailableTcs = Reset(ref availableTcs);
            }

            await AwaitAsync(myAvailableTcs, cancellationToken);
        }

        #endregion Public methods

        #region Private methods

        private byte[] DequeueInternal(int count, bool peek)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "The count must not be negative.");
            if (count == 0)
                return new byte[0]; // Easy

            lock (syncObj)
            {
                if (count > Count)
                    count = Count;

                byte[] bytes = new byte[count];
                if (tail >= head)
                {
                    Array.Copy(buffer, head, bytes, 0, count);
                }
                else
                {
                    if (count <= Capacity - head)
                    {
                        Array.Copy(buffer, head, bytes, 0, count);
                    }
                    else
                    {
                        int headCount = Capacity - head;
                        Array.Copy(buffer, head, bytes, 0, headCount);
                        int wrapCount = count - headCount;
                        Array.Copy(buffer, 0, bytes, headCount, wrapCount);
                    }
                }

                if (!peek)
                {
                    if (count == Count)
                    {
                        isEmpty = true;
                        head = 0;
                        tail = -1;
                        Reset(ref availableTcs);
                    }
                    else
                    {
                        head = (head + count) % Capacity;
                    }
                }

                return bytes;
            }
        }

        // Must be called within the lock
        private TaskCompletionSource<bool> Reset(ref TaskCompletionSource<bool> tcs)
        {
            if (tcs.Task.IsCompleted)
            {
                tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            return tcs;
        }

        // Must be called within the lock
        private void Set(TaskCompletionSource<bool> tcs)
        {
            tcs.TrySetResult(true);
        }

        // Must NOT be called within the lock
        private async Task AwaitAsync(TaskCompletionSource<bool> tcs, CancellationToken cancellationToken)
        {
            if (await Task.WhenAny(tcs.Task, Task.Delay(-1, cancellationToken)) == tcs.Task)
            {
                await tcs.Task; // Already completed
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        #endregion Private methods
    }
}