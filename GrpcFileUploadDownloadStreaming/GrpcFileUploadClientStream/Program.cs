using Google.Protobuf;
using Grpc.Net.Client;
using GrpcServer;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GrpcFileUploadClientStream
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Please press enter key to upload.");
            Console.ReadLine();
            var channel = GrpcChannel.ForAddress("https://localhost:5001");
            var client = new FileManager.FileManagerClient(channel);

            string file = @$"{Environment.CurrentDirectory}\TaskPoolItems.xls";
            using FileStream fileStream = new FileStream(file, FileMode.Open);

            var content = new BytesContent
            {
                FileSize = fileStream.Length,
                ReadedByte = 0,
                Info = new GrpcServer.FileInfo { Name = Guid.NewGuid().ToString(), Extension = Path.GetExtension(file) }
            };

            byte[] buffer = new byte[8192];
            var upload = client.FileUpLoad();

            while ((content.ReadedByte = fileStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                content.Buffer = ByteString.CopyFrom(buffer);
                await upload.RequestStream.WriteAsync(content);
            }
            await upload.RequestStream.CompleteAsync();
            fileStream.Close();
        }
    }
}
