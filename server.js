const express = require('express');
const app = express();

app.use(express.json());

const dados = [];

app.post('/api/temperatura', async (req, res) => {
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
});
app.get('/api/dados', (req, res) => {
    res.json(dados);
});
app.listen(3000, () => {
    console.log('Servidor rodando na porta 3000');
});