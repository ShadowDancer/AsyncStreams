using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace AsyncStreams
{
    public class AsyncByteStream : IDisposable
    {
        private readonly BufferBlock<byte[]> _dataQueue = new BufferBlock<byte[]>();
        private int _inputBufferOffset = 0;
        private readonly SemaphoreSlim _readSemaphore = new SemaphoreSlim(1, 1);
        private byte[]? _inputBuffer = null;

        public void Write(byte[] buffer)
        {
            _dataQueue.Post(buffer);
        }

        public async Task Read(Memory<byte> buffer)
        {
            if (buffer.Length == 0)
            {
                return;
            }

            await _readSemaphore.WaitAsync();
            int bufferOffset = 0;
            try
            {
                while (bufferOffset < buffer.Length)
                {
                    if (_inputBuffer == null)
                    {
                        _inputBuffer = await _dataQueue.ReceiveAsync();
                        _inputBufferOffset = 0;
                    }

                    int bytesLeftInBuffer = buffer.Length - bufferOffset;
                    int bytesLeftInInput = _inputBuffer.Length - _inputBufferOffset;
                    int bytesToCopy = Math.Min(bytesLeftInBuffer, bytesLeftInInput);

                    _inputBuffer.AsSpan(_inputBufferOffset, bytesToCopy).CopyTo(buffer.Slice(bufferOffset, bytesToCopy).Span);

                    _inputBufferOffset += bytesToCopy;
                    bufferOffset += bytesToCopy;

                    if (_inputBufferOffset == _inputBuffer.Length)
                    {
                        _inputBuffer = null;
                    }
                }
            }
            finally
            {
                _readSemaphore.Release();
            }
        }

        public void Dispose()
        {
            _dataQueue.Complete();
            _readSemaphore.Dispose();
        }
    }
}