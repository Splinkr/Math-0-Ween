# Matemática e RNG

O Math-0-Ween separa a geração de operações das decisões de spawn. As implementações centrais são [MathOperationGenerator](../source/Assets/Script/MathOperationGenerator.cs), [MathGeneration](../source/Assets/Script/Mathematics/MathGeneration.cs) e [MatchRandomSession](../source/Assets/Script/Randomness/MatchRandomSession.cs).

## Perfis por série

Os valores abaixo descrevem os assets efetivos do jogo completo, que não são redistribuídos.

| Série | Regras iniciais |
| --- | --- |
| 2º | Adição com operandos de 1 a 9 e resultado até 10 |
| 3º | Adição com operandos de 1 a 9 e resultado até 20; subtração com operandos de 1 a 9 e resultado não negativo |
| 4º | Adição/subtração com operandos de 1 a 20; multiplicação com operandos de 1 a 10 |
| 5º | Adição/subtração com operandos de 1 a 30; multiplicação com operandos de 1 a 10; divisão com divisor e quociente de 1 a 9 |

Os operadores permitidos começam com peso 1. Pesos de operadores não implicam distribuição uniforme de resultados. Divisões são construídas com resultado inteiro exato; os perfis atuais não habilitam negativos ou frações.

MathGeneration prepara os candidatos válidos e mantém um histórico das três últimas expressões para reduzir repetições. A fachada entrega texto, resposta e operandos usados na pontuação. MathAnswerEvaluator valida respostas e auxilia na seleção entre resultados iguais.

## Progressão numérica

Após 180 segundos escalados, os limites superiores configurados crescem 20% do valor inicial por minuto:

~~~text
limite(t) = floor(limiteInicial × (1 + 0,20 × max(0, t − 180) / 60))
~~~

Um limite inicial 10 permanece 10 aos 180 s, passa a 11 aos 210 s e a 12 aos 240 s. Os mínimos permanecem constantes. O envelope técnico limita operandos a 99 e resultados/dividendos a 9999.

Perfil ausente, inválido ou duplicado ativa integralmente as regras clássicas da série, sem a progressão numérica do perfil. Isso é diferente de misturar parcialmente parâmetros válidos e inválidos.

## Sessão aleatória

MatchRandomSession conserva uma seed de 64 bits e a versão pcg32-v1. Cada sessão cria dois fluxos:

| Fluxo | Uso |
| --- | --- |
| Math | Operadores e operandos |
| Gameplay | Decisões aleatórias de spawn |

Cada fluxo usa seu próprio estado e identificador. Consumir sorteios de gameplay não desloca o fluxo matemático. A implementação PCG32 XSH-RR usa estado de 64 bits e saída de 32 bits; Range aplica rejeição para limitar o intervalo.

GameSettings.FixedSeed permite configurar uma seed antes da criação da sessão; null seleciona uma seed automática. A configuração é aplicada uma vez por nova sessão. Ler metadados não consome sorteios.

## O que o determinismo garante

A mesma seed, configuração e sequência de chamadas reproduz as sequências aleatórias correspondentes. Tempo, entrada, estado de cena e condições de execução também afetam uma partida; registrar apenas seed e assinatura não garante um replay.

A adaptação do [PCG32](../source/Assets/Script/Randomness/Pcg32.cs) conserva seus avisos e a [licença Apache-2.0](../source/Assets/Script/Randomness/LICENSE-PCG.txt). Veja os [avisos de terceiros](../THIRD_PARTY_NOTICES.md).
