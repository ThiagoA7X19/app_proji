async function atualizarDados() {
    try {
        const resposta = await fetch('/api/dados');
        const dados = await resposta.json();

        console.log(dados);

        if (dados.length === 0) {
            return;
        }

        const ultima = dados[dados.length - 1];

        document.getElementById('temperatura').textContent =
            ultima.temperatura + ' °C';

        document.getElementById('classificacao').textContent =
            ultima.classificacao.toUpperCase();

        document.getElementById('estado-texto').textContent =
            'Sistema funcionando';

        const tabela = document.getElementById('historico-lista');

        tabela.innerHTML = '';

        dados.slice().reverse().forEach(leitura => {

            const linha = document.createElement('tr');

            linha.innerHTML = `
                <td>${leitura.temperatura} °C</td>
                <td>${leitura.classificacao.toUpperCase()}</td>
            `;

            tabela.appendChild(linha);
        });

    } catch (erro) {

        console.error('Erro ao buscar dados:', erro);

        document.getElementById('estado-texto').textContent =
            'Erro de conexão';
    }
}


atualizarDados();

setInterval(atualizarDados, 2000);