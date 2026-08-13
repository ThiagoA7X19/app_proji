let graficoTemperatura = null;

async function atualizarDados() {

    try {

        const resposta = await fetch('/api/dados');
        const dados = await resposta.json();


        if (dados.length === 0) {
            return;
        }
        const temperaturas = dados.map(
            leitura => Number(leitura.temperatura)
        );
        const media = temperaturas.reduce(
    (soma, temperatura) => soma + temperatura,
    0
) / temperaturas.length;

const ultimas = temperaturas.slice(-5);

if (ultimas.length >= 2) {

    const primeira = ultimas[0];
    const ultima = ultimas[ultimas.length - 1];

    if (ultima > primeira) {
        document.getElementById('tendencia').textContent =
            '📈 Temperatura subindo';
    }
    else if (ultima < primeira) {
        document.getElementById('tendencia').textContent =
            '📉 Temperatura diminuindo';
    }
    else {
        document.getElementById('tendencia').textContent =
            '➡️ Temperatura estável';
    }

}
        const maxima = Math.max(...temperaturas);
        const minima = Math.min(...temperaturas);

        document.getElementById('media').textContent = media.toFixed(1) + ' °C';
        document.getElementById('maxima').textContent = maxima + ' °C';
        document.getElementById('minima').textContent = minima + ' °C';

        const ultima = dados[dados.length - 1];

        document.getElementById('temperatura').textContent =
            ultima.temperatura + ' °C';

        document.getElementById('classificacao').textContent =
            ultima.classificacao.toUpperCase();

        const estado = document.getElementById('estado');
const estadoTexto = document.getElementById('estado-texto');

if (ultima.classificacao.toLowerCase() === 'alta') {

    estadoTexto.textContent = '🚨 TEMPERATURA CRÍTICA!';
    estado.classList.add('critico');

} else {

    estadoTexto.textContent = '✅ Sistema funcionando';
    estado.classList.remove('critico');

}


        const tabela = document.getElementById('historico-lista');

        tabela.innerHTML = '';

        dados.slice(-10).reverse().forEach(leitura => {

            const linha = document.createElement('tr');

            linha.innerHTML = `
                <td>${leitura.temperatura} °C</td>
                <td>${leitura.classificacao.toUpperCase()}</td>
            `;

            tabela.appendChild(linha);

        });


        atualizarGrafico(dados);

    }

    catch (erro) {

        console.error('Erro ao buscar dados:', erro);

        document.getElementById('estado-texto').textContent =
            'Erro de conexão';

    }

}

function atualizarGrafico(dados) {

    const canvas =
        document.getElementById('graficoTemperatura');

    const temperaturas = dados.map(leitura =>
        Number(leitura.temperatura)
    );


    const labels = dados.map((_, index) =>
        `Leitura ${index + 1}`
    );



    if (graficoTemperatura === null) {

        graficoTemperatura = new Chart(canvas, {

            type: 'line',

            data: {

                labels: labels,

                datasets: [
                    {
                        label: 'Temperatura da CPU (°C)',

                        data: temperaturas,

                        tension: 0.3
                    }
                ]

            },

            options: {

                responsive: true,

                maintainAspectRatio: true,

                scales: {

                    y: {
                        title: {
                            display: true,
                            text: 'Temperatura (°C)'
                        }
                    },

                    x: {
                        title: {
                            display: true,
                            text: 'Leituras'
                        }
                    }

                }

            }

        });

    }

    else {

        graficoTemperatura.data.labels = labels;

        graficoTemperatura.data.datasets[0].data =
            temperaturas;

        graficoTemperatura.update();

    }

}

atualizarDados();


setInterval(atualizarDados, 2000);