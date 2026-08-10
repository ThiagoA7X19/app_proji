
const express = require('express');
const path = require('path');

const app = express();

app.use(express.json());

app.use(express.static(path.join(__dirname, 'frontend')));

const dados = [];

app.post('/api/temperatura', async (req, res) => {
    try {
        const temperatura = req.body.temperatura;

        const resposta = await fetch('http://127.0.0.1:8000/prever', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                temperatura: temperatura
            })
        });

        const resultado = await resposta.json();

        dados.push({
            temperatura: resultado.temperatura,
            classificacao: resultado.classificacao
        });

        res.json(resultado);

    } catch (erro) {
        console.error(erro);

        res.status(500).json({
            erro: 'Erro ao comunicar com a IA'
        });
    }
});

app.get('/api/dados', (req, res) => {
    res.json(dados);
});

app.listen(3000, () => {
    console.log('Servidor rodando na porta 3000');
});

