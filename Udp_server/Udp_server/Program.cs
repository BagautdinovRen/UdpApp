using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml.Serialization;
using System.Diagnostics;
using System.Threading;

public class UdpFileServer
{
    // Информация о файле (требуется для получателя)
    [Serializable]
    public class FileDetails
    {
        public string FILETYPE = "";
        public long FILESIZE = 0;
    }

    private static FileDetails fileDet = new FileDetails();

    // Поля, связанные с UdpClient
    private static IPAddress remoteIPAddress;
    private const int remotePort = 5002;
    private static UdpClient sender = new UdpClient();
    private static IPEndPoint endPoint;

    // Filestream object
    private static FileStream fs;

    [STAThread]
    static void Main(string[] args)
    {
        try
        {
            // Получаем удаленный IP-адрес и создаем IPEndPoint
            Console.WriteLine("Введите удаленный IP-адрес");
            remoteIPAddress = IPAddress.Parse(Console.ReadLine().ToString()); //"127.0.0.1, 192.168......");
            endPoint = new IPEndPoint(remoteIPAddress, remotePort);

            // Получаем путь файла и его размер
            Console.WriteLine("Введите путь к файлу и его имя");
            fs = new FileStream(@Console.ReadLine().ToString(), FileMode.Open, FileAccess.Read);

            // Отправляем информацию о файле
            SendFileInfo();

            // Получаем подтверждение от клиента
            ReceiveConfirmation();

            Console.ReadLine();

        }
        catch (Exception eR)
        {
            Console.WriteLine(eR.ToString());
        }
    }

    public static void SendFileInfo()
    {

        // Получаем тип и расширение файла
        fileDet.FILETYPE = fs.Name.Substring(fs.Name.Length - 3, 3);

        // Получаем длину файла
        fileDet.FILESIZE = fs.Length;

        XmlSerializer fileSerializer = new XmlSerializer(typeof(FileDetails));
        MemoryStream stream = new MemoryStream();

        // Сериализуем объект
        fileSerializer.Serialize(stream, fileDet);

        // Считываем поток в байты
        stream.Position = 0;
        byte[] bytes = new byte[stream.Length];
        stream.Read(bytes, 0, Convert.ToInt32(stream.Length));

        Console.WriteLine("Отправка деталей файла...");

        // Отправляем информацию о файле
        sender.Send(bytes, bytes.Length, endPoint);
        stream.Close();
    }
    
    private static void ReceiveConfirmation()
    {
        byte[] bytes = sender.Receive(ref endPoint);

        string confirmation = Encoding.Unicode.GetString(bytes);

        if (confirmation.ToLower() == "да")
        {
            SendFile();
        }

        Console.WriteLine("Клиент отменил скачивание файла!");

        Console.ReadLine();
    }

    private static void SendFile()
    {
        // Создаем файловый поток и переводим его в байты
        Console.WriteLine("Отправка файла размером " + fs.Length + " байт");
        
        // Считываем с потока по 8000 байт (8кБ - макс размер для датаграммы udp) 
        byte[] bytes = new byte[8000];
        try
        {
            while (fs.Position < fs.Length - 8000)
            {
                Console.WriteLine("{0}", fs.Position);
                fs.Read(bytes, 0, bytes.Length);
                Send(bytes);
            }

            // Передаем последнюю порцию байт файла
            fs.Read(bytes, 0, (int)(fs.Length - fs.Position - 1));
            Send(bytes);

            Send(Encoding.Unicode.GetBytes("theendfile")); // конец файла, последний пакет (threendfile - последняя датаграмма)
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
        finally
        {
            // Закрываем соединение и очищаем поток
            fs.Close();
            sender.Close();
        }
      
        Console.WriteLine("Файл успешно отправлен.");
        Console.Read();
    }

    private static void Send(byte[] bytes)
    {
        sender.Send(bytes, bytes.Length, endPoint);
    }
}

