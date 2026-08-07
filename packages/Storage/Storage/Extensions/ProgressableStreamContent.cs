using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Supabase.Storage.Interfaces;

namespace Supabase.Storage.Extensions
{
	internal class ProgressableStreamContent : HttpContent, IProgressableStreamContent
	{
		private const int defaultBufferSize = 4096;

		private Stream content;
		private int bufferSize;

		public EventHandler<UploadState>? StateChanged;
		public IProgress<float>? Progress { get; private set; }

		public enum UploadState
		{
			PendingUpload,
			InProgress,
			PendingResponse,
			Complete
		}

		public ProgressableStreamContent(Stream content) : this(content, defaultBufferSize) { }

		public ProgressableStreamContent(Stream content, int bufferSize, Progress<float>? progress = null)
		{
			if (content == null)
			{
				throw new ArgumentNullException("content");
			}

			if (bufferSize <= 0)
			{
				throw new ArgumentOutOfRangeException("bufferSize");
			}

			if (progress == null)
			{
				progress = new Progress<float>();
			}

			this.content = content;
			this.bufferSize = bufferSize;

			Progress = progress;
		}

		protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
		{
			Contract.Assert(stream != null);

			return Task.Run(() =>
			{
				var buffer = new byte[bufferSize];
				decimal size = content.Length;
				decimal uploaded = 0;

				StateChanged?.Invoke(this, UploadState.PendingUpload);

				using (content)
				{
					while (true)
					{
						var length = content.Read(buffer, 0, buffer.Length);
						if (length <= 0) break;

						uploaded += length;
						float currentProgress = (float)((uploaded / size) * 100);
						Progress?.Report(currentProgress);

						stream!.Write(buffer, 0, length);

						StateChanged?.Invoke(this, UploadState.InProgress);
					}
				}

				StateChanged?.Invoke(this, UploadState.PendingResponse);
			});
		}

		protected override bool TryComputeLength(out long length)
		{
			length = content.Length;
			return length > 0;
		}
	}
}
