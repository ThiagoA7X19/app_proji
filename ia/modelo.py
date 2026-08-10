from fastapi import FastAPI
from pydantic import BaseModel
from sklearn.tree import DecisionTreeClassifier

app = FastAPI()



baixa = [30, 32, 34, 35, 36, 37, 38, 39, 40, 41]

media = [42, 44, 46, 48, 50, 52, 54, 55, 56, 58]

alta = [60, 62, 65, 68, 70, 72, 75, 78, 80, 85]

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