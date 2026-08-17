
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Management;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

const string classificador_path = "http://localhost:3000/api/temperatura";

const string vid = "VID_0483";
const string pid = "PID_5740";

// Estrutura do protocolo:
// SOF | TIPO | TEMPERATURA_LSB | TEMPERATURA_MSB | CRC | EOF
const byte SOF = 0xAA;
const byte TIPO_TEMPERATURA = 0x01;
const byte EOF_BYTE = 0x55;
const int TAMANHO_PACOTE = 6;

HttpClient client = new HttpClient();

// Buffer para lidar com pacotes recebidos de forma fragmentada.
List<byte> bufferRecepcao = new List<byte>();

async Task EnviarLeitura(StringContent conteudo)
{
    try
    {
        HttpResponseMessage resposta =
            await client.PostAsync(
                classificador_path,
                conteudo
            );

        string jsonResposta =
            await resposta.Content.ReadAsStringAsync();

        if (!resposta.IsSuccessStatusCode)
        {
            Console.WriteLine(
                $"Erro HTTP: {resposta.StatusCode}"
            );
        }
        else
        {
            Console.WriteLine(jsonResposta);
        }
    }
    catch (HttpRequestException erro)
    {
        Console.WriteLine(
            $"Erro ao comunicar com o classificador: {erro.Message}"
        );
    }
}

// CRC-8/ATM: polinômio 0x07 e valor inicial 0x00.
byte CRC8(byte[] dados, int inicio, int tamanho)
{
    byte crc = 0x00;

    for (int i = inicio; i < inicio + tamanho; i++)
    {
        crc ^= dados[i];

        for (int bit = 0; bit < 8; bit++)
        {
            if ((crc & 0x80) != 0)
            {
                crc = (byte)((crc << 1) ^ 0x07);
            }
            else
            {
                crc = (byte)(crc << 1);
            }
        }
    }

    return crc;
}

async Task ProcessarPacote(byte[] pacote)
{
    if (pacote.Length != TAMANHO_PACOTE)
    {
        Console.WriteLine(
            "Erro: pacote com tamanho inválido."
        );
        return;
    }

    // Validação da estrutura do pacote.
    if (pacote[0] != SOF)
    {
        Console.WriteLine(
            $"Erro: SOF inválido. Recebido: 0x{pacote[0]:X2}"
        );
        return;
    }

    if (pacote[1] != TIPO_TEMPERATURA)
    {
        Console.WriteLine(
            $"Erro: tipo de pacote desconhecido. " +
            $"Recebido: 0x{pacote[1]:X2}"
        );
        return;
    }

    if (pacote[5] != EOF_BYTE)
    {
        Console.WriteLine(
            $"Erro: EOF inválido. Recebido: 0x{pacote[5]:X2}"
        );
        return;
    }

    // O CRC é calculado sobre TIPO + LSB + MSB.
    byte crcCalculado = CRC8(pacote, 1, 3);
    byte crcRecebido = pacote[4];

    if (crcCalculado != crcRecebido)
    {
        Console.WriteLine(
            "ERRO DE INTEGRIDADE: CRC inválido."
        );

        Console.WriteLine(
            $"CRC recebido: 0x{crcRecebido:X2}"
        );

        Console.WriteLine(
            $"CRC calculado: 0x{crcCalculado:X2}"
        );

        Console.WriteLine(
            "Pacote descartado."
        );

        return;
    }

    // Reconstrói o valor de 16 bits a partir do LSB e MSB.
    short temperaturaInteira =
        (short)(
            pacote[2] |
            (pacote[3] << 8)
        );

    // O STM envia a temperatura multiplicada por 100.
    float temperatura =
        temperaturaInteira / 100.0f;

    Console.WriteLine(
        $"Recebido do STM: {temperatura:F2} graus"
    );

    Sensor sensor = new Sensor
    {
        Temperatura = temperatura
    };

    string json =
        JsonSerializer.Serialize(sensor);

    var conteudo =
        new StringContent(
            json,
            Encoding.UTF8,
            "application/json"
        );

    await EnviarLeitura(conteudo);
}

async void Porta_DataReceived(
    object sender,
    SerialDataReceivedEventArgs e)
{
    SerialPort porta = (SerialPort)sender;

    try
    {
        int quantidade = porta.BytesToRead;

        if (quantidade <= 0)
        {
            return;
        }

        byte[] dados = new byte[quantidade];

        int bytesLidos =
            porta.Read(
                dados,
                0,
                quantidade
            );

        bufferRecepcao.AddRange(
            dados[..bytesLidos]
        );

        // Pode haver mais de um pacote no buffer.
        while (bufferRecepcao.Count >= TAMANHO_PACOTE)
        {
            // Procura o início de um pacote válido.
            int indiceSOF =
                bufferRecepcao.IndexOf(SOF);

            if (indiceSOF == -1)
            {
                bufferRecepcao.Clear();
                return;
            }

            // Descarta bytes anteriores ao SOF.
            if (indiceSOF > 0)
            {
                bufferRecepcao.RemoveRange(
                    0,
                    indiceSOF
                );
            }

            // O SOF foi encontrado, mas o pacote pode estar incompleto.
            if (bufferRecepcao.Count < TAMANHO_PACOTE)
            {
                return;
            }

            byte[] pacote =
                bufferRecepcao
                    .GetRange(
                        0,
                        TAMANHO_PACOTE
                    )
                    .ToArray();

            bufferRecepcao.RemoveRange(
                0,
                TAMANHO_PACOTE
            );

            await ProcessarPacote(pacote);
        }
    }
    catch (IOException)
    {
        Console.WriteLine(
            "A porta serial deixou de estar disponível."
        );
    }
    catch (InvalidOperationException)
    {
        Console.WriteLine(
            "A porta serial foi fechada."
        );
    }
}

string? encontrarPorta()
{
    var searcher =
        new ManagementObjectSearcher(
            "SELECT * FROM Win32_PnPEntity"
        );

    foreach (
        ManagementObject device
        in searcher.Get()
    )
    {
        string? deviceID =
            device["DeviceID"]?.ToString();

        if (
            deviceID != null &&
            deviceID.Contains(vid) &&
            deviceID.Contains(pid)
        )
        {
            string? name =
                device["Name"]?.ToString();

            if (name != null)
            {
                string porta =
                    Regex.Match(
                        name,
                        @"COM\d+"
                    ).Value;

                if (!string.IsNullOrEmpty(porta))
                {
                    return porta;
                }
            }
        }
    }

    return null;
}

// Monitora conexão e remoção de dispositivos.
using ManagementEventWatcher watcher =
    new ManagementEventWatcher(
        "SELECT * FROM Win32_DeviceChangeEvent"
    );

watcher.Start();

string? nome_porta =
    encontrarPorta();

while (true)
{
    while (nome_porta == null)
    {
        Console.WriteLine(
            "Procurando STM..."
        );

        ManagementBaseObject evento =
            watcher.WaitForNextEvent();

        ushort eventType =
            (ushort)evento["EventType"];

        // EventType 2 indica conexão de dispositivo.
        if (eventType == 2)
        {
            nome_porta =
                encontrarPorta();

            if (nome_porta != null)
            {
                Console.WriteLine(
                    $"STM conectado na porta {nome_porta}."
                );
            }
        }
    }

    try
    {
        using SerialPort porta =
            new SerialPort(nome_porta);

        porta.BaudRate = 115200;
        porta.DataBits = 8;
        porta.Parity = Parity.None;
        porta.StopBits = StopBits.One;

        porta.DataReceived +=
            Porta_DataReceived;

        porta.Open();

        Console.WriteLine(
            $"Porta {nome_porta} aberta."
        );

        ManagementBaseObject eventoRemocao =
            watcher.WaitForNextEvent();

        ushort tipo =
            (ushort)eventoRemocao["EventType"];

        // EventType 3 indica remoção de dispositivo.
        if (tipo == 3)
        {
            string? porta_atual =
                encontrarPorta();

            if (porta_atual == null)
            {
                Console.WriteLine(
                    "O STM foi removido."
                );

                nome_porta = null;
                bufferRecepcao.Clear();
            }
        }
    }
    catch (IOException)
    {
        Console.WriteLine(
            "A porta deixou de estar disponível."
        );

        nome_porta = null;
        bufferRecepcao.Clear();
    }
    catch (UnauthorizedAccessException)
    {
        Console.WriteLine(
            "Não foi possível acessar a porta serial."
        );

        nome_porta = null;
    }
}

public class Sensor
{
    [JsonPropertyName("temperatura")]
    public float Temperatura { get; set; }
}