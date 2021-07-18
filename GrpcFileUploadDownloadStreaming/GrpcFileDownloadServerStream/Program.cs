using Grpc.Net.Client;
using GrpcServer;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GrpcFileDownloadServerStream
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Please press enter key to download.");
            Console.ReadLine();
            var channel = GrpcChannel.ForAddress("https://localhost:5001");
            var client = new FileManager.FileManagerClient(channel);
            string downloadPath = Environment.CurrentDirectory;

            var fileInfo = new GrpcServer.FileInfo
            {
                Extension = ".xls",
                Name = "TaskPoolItems"
            };

            FileStream fileStream = null;

            var request = client.FileDownLoad(fileInfo);
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            int count = 0;
            decimal chunkSize = 0;

            while (await request.ResponseStream.MoveNext(cancellationTokenSource.Token))
            {
                if (count++ == 0)
                {
                    fileStream = new FileStream(@$"{downloadPath}\{request.ResponseStream.Current.Info.Name}{request.ResponseStream.Current.Info.Extension}", FileMode.CreateNew);
                    fileStream.SetLength(request.ResponseStream.Current.FileSize);
                }

                var buffer = request.ResponseStream.Current.Buffer.ToByteArray();
                await fileStream.WriteAsync(buffer, 0, request.ResponseStream.Current.ReadedByte);
                chunkSize += request.ResponseStream.Current.ReadedByte;
                Console.WriteLine($"{Math.Round(chunkSize * 100 / request.ResponseStream.Current.FileSize)}% completed.");
            }
            Console.WriteLine("File Downloaded");

            await fileStream.DisposeAsync();
            fileStream.Close();
        }
    }
}
