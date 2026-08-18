# Classificador de Temperatura de CPU

Sistema que lê a temperatura ambiente/da placa através do ADC de um microcontrolador STM32, envia a leitura via USB para uma aplicação desktop em C#, repassa o valor a um backend em Node.js, classifica cada medição ("baixa", "média" ou "alta") com um modelo de Machine Learning em Python/scikit-learn exposto via FastAPI, e exibe os resultados em um dashboard web com histórico e gráfico de evolução.

Este repositório contém o firmware do STM32, a ponte serial em C#, o backend Node.js, o serviço de IA em Python e o frontend estático — veja o diagrama de arquitetura abaixo.

## Arquitetura

Resumo do fluxo:

1. O STM32 (linha F103, "Blue Pill") amostra a temperatura pelo seu ADC, calcula a média de um conjunto de amostras e monta um pacote binário com cabeçalho de início (SOF), tipo, valor da temperatura, checksum CRC-8 e marcador de fim (EOF).
2. O pacote é transmitido por USB, emulando uma porta serial (CDC), para o computador.
3. Uma aplicação desktop em C# monitora a chegada e remoção do dispositivo, identifica automaticamente a porta COM do STM32 pelo VID/PID do USB, lê o pacote, valida a estrutura e o CRC, remonta o valor de temperatura e envia esse valor como JSON para o backend via HTTP.
4. O backend em Node.js recebe a temperatura, repassa-a a um microsserviço Python (FastAPI) que a classifica com um modelo de árvore de decisão treinado em faixas de temperatura, e guarda o par temperatura/classificação em memória.
5. O frontend consulta periodicamente o backend e atualiza o dashboard: temperatura atual, classificação, histórico em tabela, gráfico de evolução (Chart.js), além de média, máxima, mínima e tendência das últimas leituras.

## Linguagens & Tecnologias

| Camada | Tecnologia |
|---|---|
| Firmware do sensor | C (STM32 HAL, ADC, USB Device / CDC) |
| Ponte serial (desktop) | C# / .NET (System.IO.Ports, WMI para detecção de dispositivo, HttpClient) |
| Backend | Node.js, Express |
| Classificação (IA) | Python 3, FastAPI, scikit-learn (árvore de decisão) |
| Frontend | HTML, CSS, JavaScript, Chart.js |
| Armazenamento | Em memória, no processo do backend |

## Estrutura do projeto

- `TEMPERATURA_CPU/` — projeto de firmware do STM32 (STM32CubeIDE), incluindo drivers HAL, biblioteca USB Device (classe CDC) e o código da aplicação (leitura do ADC, montagem do pacote e transmissão via USB).
- `Comunicacao/` — aplicação de console em C# (.NET) responsável por localizar a porta serial do STM32, ler e validar os pacotes recebidos e encaminhar a temperatura ao backend.
- `server.js` — servidor Express: recebe a temperatura, consulta o serviço de classificação, guarda o histórico e serve o frontend.
- `ia/modelo.py` — serviço FastAPI que treina um classificador de árvore de decisão sobre faixas de temperatura predefinidas e expõe um endpoint de previsão.
- `frontend/` — dashboard estático (formulário/visualização, estilos e script de atualização periódica dos dados).
- `package.json` — dependências e script de inicialização do backend Node.js.

## Pré-requisitos

- STM32CubeIDE (ou toolchain ARM GCC equivalente) para compilar e gravar o firmware na placa STM32F103.
- .NET SDK compatível com o projeto em `Comunicacao/`, para compilar e executar a ponte serial (Windows, devido ao uso de WMI/`System.Management` para detecção automática da porta).
- Node.js e npm, para o backend.
- Python 3 com FastAPI, um servidor ASGI (como Uvicorn) e scikit-learn, para o serviço de classificação.

## Como executar

O sistema é composto por três processos que precisam estar em execução simultaneamente, além do STM32 conectado por USB:

1. Grave o firmware em `TEMPERATURA_CPU/` na placa STM32 usando o STM32CubeIDE.
2. Inicie o serviço de classificação em Python (`ia/modelo.py`) com um servidor ASGI, deixando-o disponível na porta 8000.
3. Instale as dependências do backend com o gerenciador de pacotes do Node e inicie o servidor (`server.js`), que sobe na porta 3000 e também serve o frontend.
4. Compile e execute a aplicação em `Comunicacao/`. Ela aguarda a conexão do STM32, identifica a porta COM automaticamente e começa a repassar as leituras ao backend assim que o pacote USB começar a chegar.
5. Abra o endereço do backend no navegador para acompanhar o dashboard em tempo real.

## Endpoints da API (backend)

| Método | Rota | Descrição |
|---|---|---|
| POST | `/api/temperatura` | Recebe uma temperatura, encaminha ao serviço de IA para classificação e armazena o resultado |
| GET | `/api/dados` | Lista todas as leituras já classificadas, usadas pelo dashboard |

## Protocolo de comunicação (STM32 → C#)

As leituras são transmitidas em pacotes binários de tamanho fixo, com a seguinte estrutura: byte de início de quadro, byte de tipo de pacote, valor da temperatura em dois bytes (little-endian, multiplicado por 100 para preservar casas decimais), um byte de verificação CRC-8 calculado sobre o tipo e o valor, e um byte de fim de quadro. O lado C# remonta pacotes fragmentados a partir de um buffer de recepção, descarta qualquer pacote cujo CRC não confira e só então envia a leitura validada ao backend.

## Notas sobre o classificador

O serviço em `ia/modelo.py` treina, a cada inicialização, uma árvore de decisão com conjuntos de temperaturas de exemplo associados às classes "baixa", "média" e "alta", e usa esse modelo para classificar cada nova leitura recebida. Para adaptar o sistema a um cenário real, essas faixas de exemplo podem ser substituídas por dados de temperatura reais coletados do ambiente monitorado.


## Vídeo explicativo sobre o projeto:

https://youtu.be/owmaUb8qVmM
