using System.Diagnostics;
using System.IO.Ports;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using System.Globalization;

const string classificador_path = "http://localhost:3000/api/temperatura";

static async Task EnviarLeitura(StringContent conteudo)
{
    
    HttpClient client = new HttpClient();

    HttpResponseMessage  resposta =  await client.PostAsync(
        classificador_path,
        conteudo
    );

    string jsonResposta = await resposta.Content.ReadAsStringAsync();
    Console.WriteLine(jsonResposta);
}

static async Task ProcessarLeitura(string dado)
{

    float temperatura = float.Parse(
    dado,
    CultureInfo.InvariantCulture
);
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

 static async void Porta_DataReceived(object sender, SerialDataReceivedEventArgs e)
{
    SerialPort porta = (SerialPort)sender;
    string linha = porta.ReadLine();
    
    await ProcessarLeitura(linha);

}


SerialPort porta = new SerialPort("COM6", 115200);

porta.DataReceived += Porta_DataReceived;

porta.Open();

Console.ReadLine();


public class Sensor
{
    [JsonPropertyName("temperatura")]    
    public float Temperatura {get; set;}
}



