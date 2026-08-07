from fastapi import FastAPI
from pydantic import BaseModel
from sklearn.tree import DecisionTreeClassifier

app = FastAPI()



baixa = [5, 8, 10, 12, 15, 17, 13, 1, 2, 14]
media = [18, 20, 22, 24, 25, 27, 23, 19, 21, 26]
alta = [28, 30, 32, 35, 38, 40, 31, 37, 29, 33]

temperaturas = baixa + media + alta

classes = (
    ["baixa"] * len(baixa) +
    ["media"] * len(media) +
    ["alta"] * len(alta)
)

X = [[temperatura] for temperatura in temperaturas]
y = classes



classificador = DecisionTreeClassifier()
classificador.fit(X, y)



class Temperatura(BaseModel):
    temperatura: float



@app.post("/prever")
def prever(dados: Temperatura):

    resultado = classificador.predict(
        [[dados.temperatura]]
    )

    return {
        "temperatura": dados.temperatura,
        "classificacao": resultado[0]
    }