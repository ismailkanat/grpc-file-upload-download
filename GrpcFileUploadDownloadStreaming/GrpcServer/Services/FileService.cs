using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GrpcServer
{
    public class FileService : FileManager.FileManagerBase
    {
        readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<FileService> _logger;
        public FileService(ILogger<FileService> logger, IWebHostEnvironment webHostEnvironment)
        {
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
        }

        public override async Task<Empty> FileUpLoad(IAsyncStreamReader<BytesContent> requestStream, ServerCallContext context)
        {
            string path = Path.Combine(_webHostEnvironment.WebRootPath, "files");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            FileStream fileStream = null;
            try
            {
                int count = 0;
                decimal chunkSize = 0;
                while (await requestStream.MoveNext())
                {
                    if (count++ == 0)
                    {
                        fileStream = new FileStream($"{path}/{requestStream.Current.Info.Name}{requestStream.Current.Info.Extension}", FileMode.CreateNew);
                        fileStream.SetLength(requestStream.Current.FileSize);
                    }
                    var buffer = requestStream.Current.Buffer.ToByteArray();

                    await fileStream.WriteAsync(buffer, 0, requestStream.Current.ReadedByte);
                    chunkSize += requestStream.Current.ReadedByte;
                    Console.WriteLine($"{Math.Round(chunkSize * 100 / requestStream.Current.FileSize)}% completed.");
                }
                Console.WriteLine("File Uploaded.");
            }
            catch (Exception ex)
            {
            }
            await fileStream.DisposeAsync();
            fileStream.Close();
            return new Empty();
        }

        public override async Task FileDownLoad(FileInfo request, IServerStreamWriter<BytesContent> responseStream, ServerCallContext context)
        {
            string path = Path.Combine(_webHostEnvironment.WebRootPath, "files", request.Name + request.Extension);
            var file = new System.IO.FileInfo(path);

            using FileStream fileStream = new FileStream($"{path}", FileMode.Open, FileAccess.Read);
            byte[] buffer = new byte[8192];

            BytesContent content = new BytesContent
            {
                FileSize = fileStream.Length,
                Info = new FileInfo { Name = Guid.NewGuid().ToString(), Extension = file.Extension },
                ReadedByte = 0
            };

            while ((content.ReadedByte = fileStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                content.Buffer = ByteString.CopyFrom(buffer);
                await responseStream.WriteAsync(content);
            }

            fileStream.Close();
        }
    }
}
