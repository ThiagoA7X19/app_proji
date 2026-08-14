using System.Diagnostics;
using System.IO.Ports;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Management;

const string classificador_path = "http://localhost:3000/api/temperatura";
const string vid = "VID_0483";
const string pid = "PID_5740";
HttpClient client = new HttpClient();
async Task EnviarLeitura(StringContent conteudo)
{
    
    

    HttpResponseMessage  resposta =  await client.PostAsync(
        classificador_path,
        conteudo
    );

    string jsonResposta = await resposta.Content.ReadAsStringAsync();
    if (!resposta.IsSuccessStatusCode)
    {
        Console.WriteLine($"Erro HTTP:  {resposta.StatusCode}");
    }
    else Console.WriteLine(jsonResposta);
}

async Task ProcessarLeitura(string dado)
{
    float temperatura;
    if(!float.TryParse(dado, NumberStyles.Float ,CultureInfo.InvariantCulture, out temperatura))
    {
        Console.WriteLine($"Erro na conversão da temperatura. Provavelmente o dado foi mal formatado");
        return;
    }
    Console.WriteLine($"Recebido do STM: {temperatura} graus");
    Sensor sensor = new Sensor{Temperatura=temperatura};

    string json = JsonSerializer.Serialize(sensor);

    var conteudo = new StringContent(
        json,
        Encoding.UTF8,
        "application/json"

    );

    await EnviarLeitura(conteudo);
    
}

async void Porta_DataReceived(object sender, SerialDataReceivedEventArgs e)
{
    SerialPort porta = (SerialPort)sender;
    string linha = porta.ReadLine();
    string[] dados_recebidos = linha.Split(';');
    
    foreach(string dado in dados_recebidos)
    {
        if (dado.Contains("TEMP"))
        {
            string[] par_nome_valor = dado.Split(':');
            if(par_nome_valor.Length != 2)
            {
                Console.WriteLine("Dado recebido em formato inválido.");
                continue;
            }
            Console.WriteLine($"Temperatura recebida do STM: {par_nome_valor[1]}");
            await ProcessarLeitura(par_nome_valor[1]);
        }
    }

    
    

}

string? encontrarPorta()
{
    var searcher = new ManagementObjectSearcher(
    "SELECT * FROM Win32_PnPEntity"
);

foreach (ManagementObject device in searcher.Get())
{
    string? deviceID = device["DeviceID"]?.ToString();
    if(deviceID != null && deviceID.Contains(vid) && deviceID.Contains(pid))
    {
        string? name = device["Name"]?.ToString();
        if(name != null){
        string? porta = Regex.Match(name, @"COM\d+").Value;
        return porta;
        }
    }
    

}
    return null;
}

using ManagementEventWatcher watcher =
    new ManagementEventWatcher(
        "SELECT * FROM Win32_DeviceChangeEvent");

watcher.Start();

string? nome_porta = encontrarPorta();

while (true)
{
    while (nome_porta == null)
    {
        Console.WriteLine("Procurando STM...");
        ManagementBaseObject evento =
            watcher.WaitForNextEvent();

        ushort eventType =
            (ushort)evento["EventType"];

        if (eventType == 2)
        {
            
            nome_porta = encontrarPorta();

            if (nome_porta != null)
            {
                Console.WriteLine("STM conectado.");
            }
        }
    }

    try
    {
        using SerialPort porta =
            new SerialPort(nome_porta);

        porta.DataReceived += Porta_DataReceived;
        porta.Open();

        ManagementBaseObject eventoRemocao =
            watcher.WaitForNextEvent();

        ushort tipo =
            (ushort)eventoRemocao["EventType"];

        if (tipo == 3)
        {
            string? porta_atual = encontrarPorta();

            if (porta_atual == null)
            {
                Console.WriteLine("O STM foi removido.");
                nome_porta = null;
            }
        }
    }
    catch (IOException)
    {
        Console.WriteLine(
            "A porta deixou de estar disponível."
        );

        nome_porta = null;
    }
    
}

public class Sensor
{
    [JsonPropertyName("temperatura")]    
    public float Temperatura {get; set;}
}



