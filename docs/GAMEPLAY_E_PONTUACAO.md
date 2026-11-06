# Gameplay, dificuldade e pontuação

Os parâmetros desta página descrevem a configuração efetiva de Gameplay no projeto completo. Os valores de fallback presentes nos campos de componentes podem ser diferentes.

## Spawn e pressão arcade

| Parâmetro | Configuração atual |
| --- | --- |
| Vidas iniciais | 7 |
| Velocidade inicial de queda | 0,1 unidade/s |
| Intervalo inicial de spawn | 4 s |
| Nível arcade inicial | 0 |
| Progressão base | +2 por período equivalente de 20 s |
| Efeito por ponto arcade | Velocidade +0,03; intervalo −0,07 s |
| Limites de velocidade/intervalo | 10 unidades/s; intervalo mínimo de 0,5 s |
| Aceleração da progressão | A partir de 180 s, +0,25/min na taxa até 2× |
| Capacidade de inimigos ativos | 4 inicialmente, +1 a cada 20 pontos arcade ganhos, até 8 |
| Nova tentativa com tela vazia | Após 0,75 s |
| Margem visual no nascimento | 0,1 unidade |

[DifficultyProgression](../source/Assets/Script/Gameplay/DifficultyProgression.cs) integra a curva de progressão. [SpawnDirector](../source/Assets/Script/Gameplay/SpawnDirector.cs) centraliza admissão e tentativas. A capacidade e o espaço disponível continuam condicionando ondas e inimigos especiais.

A separação visual é verificada na criação; ela não garante ausência de sobreposição ao longo de todas as trajetórias. Alvos já respondidos podem continuar apresentados até a resolução do projétil.

## Habilidades e especiais

| Recurso | Configuração e efeito |
| --- | --- |
| Freeze, tecla X | 7 s de efeito; recarga de 30 s |
| Inversão, tecla C | 5 s de efeito; recarga de 30 s |
| Ataque coletivo, tecla Z | Carga exigida de 10 unidades; sem pontos, streak, cura ou cargas por suas eliminações |
| Adrenalina | Velocidade de queda ×0,8 por 3 s após dano não fatal, com recuperação de 0,5 s |
| Vermelho escuro | Cura o mago e fornece uma unidade de carga |
| Dourado | Multiplicador de pontos 4× e uma unidade de carga |
| Azul | Freeze de 1 s e uma unidade de carga |
| Prateado | 10 unidades de carga |

A chance configurada de tentativa especial é 0,13, sujeita à admissão de spawn; a duração configurada do especial é 10 s. O ataque coletivo bloqueia novos spawns até a resolução das pendências e mais 2 s. Há timeout de 10 s para resolver pendências do ataque.

## Cálculo da pontuação

[ScoreRules](../source/Assets/Script/Scoring/ScoreRules.cs) calcula o crédito usando a streak já atualizada:

~~~text
base = 10 + min(max(streak, 0), 15)
pressão = 1 + clamp((max(nível, 0) − max(nívelInicial, 0)) / 80, 0, 1)
complexidade = min(1,2; 1 + bônusOperador + 0,02 × max(0, D − 3))
crédito = arredondar(base × pressão × complexidade × especial)
~~~

D é o total de dígitos dos dois operandos exibidos e do resultado. Zero conta como um dígito. O bônus de operador é 0,10 para multiplicação/divisão e zero para adição/subtração. O fator especial é 4 para dourado e 1 para os demais.

O código usa double e um único arredondamento final, com empate para o par. Metadados aritméticos inválidos usam complexidade neutra 1. O divisor da pressão e os bônus são configuráveis; os valores acima são os padrões ativos.

## Quando os pontos entram

O acerto congela o valor do crédito. [BatProjectile](../source/Assets/Script/Gameplay/BatProjectile.cs) distingue contato, conclusão da explosão e cancelamento. O primeiro contato válido concede o crédito uma única vez a PointsManager. A conclusão visual da explosão não concede uma segunda recompensa.

Não há pontos passivos, crédito por temporizador nem pontos de fallback por ausência de projétil. A perda de vida zera a streak. Dano não fatal preserva os créditos dos projéteis já lançados; a última vida encerra a pontuação e cancela créditos ainda não impactados.

O indicador de bônus agrupa créditos próximos durante 1 segundo escalado desde o último crédito. O total muda imediatamente; esse agrupamento é apenas apresentação. Pausa e retomada respeitam o estado da partida e dos indicadores.
