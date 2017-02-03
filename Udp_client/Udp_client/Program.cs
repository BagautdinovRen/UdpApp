using System;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Xml.Serialization;

public class UdpFileClient
{
    // Детали файла
    [Serializable]
    public class FileDetails
    {
        public string FILETYPE = "";
        public long FILESIZE = 0;
    }

    private static FileDetails fileDet;

    // Поля, связанные с UdpClient
    private static int localPort = 5002;
    private static UdpClient receivingUdpClient = new UdpClient(localPort);
    private static IPEndPoint RemoteIpEndPoint = null;

    private static FileStream fs;
    private static byte[] receiveBytes = new byte[0];

    [STAThread]
    static void Main(string[] args)
    {
        // Получаем информацию о файле
        GetFileDetails();

        // Подтверждаем получение
        SendResponse();
    }

    private static void GetFileDetails()
    {
        try
        {
            Console.WriteLine("-----------*******Ожидание информации о файле от сервера*******-----------");

            // Получаем информацию о файле
            receiveBytes = receivingUdpClient.Receive(ref RemoteIpEndPoint);
            Console.WriteLine("----Информация о файле получена!");

            XmlSerializer fileSerializer = new XmlSerializer(typeof(FileDetails));
            MemoryStream stream1 = new MemoryStream();

            // Считываем информацию о файле
            stream1.Write(receiveBytes, 0, receiveBytes.Length);
            stream1.Position = 0;

            // Вызываем метод Deserialize
            fileDet = (FileDetails)fileSerializer.Deserialize(stream1);
            Console.WriteLine("Получен файл типа ." + fileDet.FILETYPE +
                " имеющий размер " + fileDet.FILESIZE.ToString() + " байт");
        }
        catch (Exception eR)
        {
            Console.WriteLine(eR.ToString());
        }
    }

    private static void SendResponse()
    {
        Console.WriteLine("Скачать файл?");

        string response = Console.ReadLine();

        byte[] bytes = Encoding.Unicode.GetBytes(response);

        try
        {  
            // Отправляем ответ серверу
            receivingUdpClient.Send(bytes, bytes.Length, RemoteIpEndPoint);
        }
        catch (Exception e)
        {
            Console.WriteLine("Возникла ошибка при отправке подтвержения: " + e.Message);

            return;
        }

        if (response.ToLower() == "да")
        {
            // Получаем файл
            ReceiveFile();

            return;
        }

        Console.WriteLine("Скачивание файла отменено");

        Console.ReadLine();
    }

    public static void ReceiveFile()
    {
        try
        {
            Console.WriteLine("-----------*******Ожидайте получение файла*******-----------");

            fs = new FileStream("temp." + fileDet.FILETYPE, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);

            Console.WriteLine("Скачивание файла запущено!");

            while (true)
            {
                receiveBytes = receivingUdpClient.Receive(ref RemoteIpEndPoint);
                
                // theendfile - последняя датаграмма
                if (Encoding.Unicode.GetString(receiveBytes) == "theendfile")
                {
                    break;
                }
                
                // Записываем полученную датаграмму в файл
                fs.Write(receiveBytes, 0, receiveBytes.Length);
            }

            Console.WriteLine("----Файл сохранен...");

            Console.WriteLine("-------Открытие файла------");

            // Открываем файл связанный с ним программой
            Process.Start(fs.Name);
        }
        catch (Exception eR)
        {
            Console.WriteLine(eR.ToString());
        }
        finally
        {
            fs.Close();
            receivingUdpClient.Close();
            Console.Read();
        }
    }
}