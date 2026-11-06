# Arquitetura do Math-0-Ween

Esta seleção apresenta o código de gameplay do projeto Unity 6000.0.41f1. Os scripts conservam seu conteúdo e organização relativos. As configurações efetivas descritas aqui foram conferidas nos assets privados; esses assets não fazem parte do repositório.

## Organização dos módulos

| Módulo | Responsabilidade |
| --- | --- |
| Fachadas | MathOperationGenerator produz operações; OperationEnemySpawner coordena a partida |
| Configuration | Série selecionada, configurações e definição dos perfis matemáticos |
| Mathematics | Preparação de perfis, amostragem de expressões e avaliação de respostas |
| Randomness | Sessão por partida e fluxos PCG32 independentes |
| Gameplay | Dificuldade, admissão de spawn, poderes, efeitos e projéteis |
| Enemies | Registro, seleção de alvos, movimento e apresentação dos textos |
| Player | Vidas, animações e recepção de operações na base |
| Scoring | Fórmula pura, crédito aceito e apresentação da pontuação |
| Persistence | Modelos de resultado, metadados e salvamento local |
| UI | Navegação, tutorial, ranking, rolagem e vinheta |

Consulte o [índice dos arquivos](INDICE_FONTE.md) para abrir cada implementação.

## Fluxo da partida

~~~mermaid
flowchart TD
    Menu[Seleção da série] --> Settings[GameSettings]
    Settings --> Generator[MathOperationGenerator]
    Generator --> Math[MathGeneration]
    Session[MatchRandomSession] --> Math
    Session --> Spawn[SpawnDirector]
    Generator --> Spawner[OperationEnemySpawner]
    Spawn --> Spawner
    Spawner --> Registry[EnemyRegistry]
    Spawner --> Powers[SpecialAbilityState e SpecialEnemyEffects]
    Spawner --> Bat[BatProjectile]
    Rules[ScoreRules] --> Spawner
    Bat -->|primeiro contato válido| Points[PointsManager]
    Points --> Result[Resultado final]
    Result --> Saver[PlayerDataSaver]
    Saver --> Board[ScoreboardDisplay]
~~~

As setas indicam fluxo de dados ou responsabilidade; algumas ligações passam pelos callbacks coordenados pela fachada.

1. O menu define a série em GameSettings. GameplayInitializer valida a seleção.
2. A fachada matemática captura o perfil e compartilha sua sessão aleatória com o spawner.
3. SpawnDirector agenda tentativas e verifica capacidade; a apresentação mede o espaço dos textos antes de admitir a criação.
4. O spawner interpreta a resposta e solicita ao registro o alvo elegível mais baixo com aquele resultado.
5. O acerto confirma o alvo, calcula e congela o valor do crédito, aplica efeitos e lança o morcego.
6. O primeiro contato válido do projétil encaminha o crédito a PointsManager. Resoluções duplicadas não repetem a recompensa.
7. A última perda de vida encerra a pontuação; a cena de resultado permite gravar o nome e abrir o ranking.

## Configuração e estado

GameBalanceConfig e GradeMathProfile são definições de ScriptableObject. No jogo completo, os componentes capturam snapshots das configurações nos pontos de inicialização existentes. Editar o asset não atualiza uma partida já capturada. Sem configuração válida, cada módulo conserva seus fallbacks.

GameSettings transporta a seleção e o resultado entre cenas. MatchRandomSession pertence à partida. Os auxiliares de matemática e pontuação concentram regras que não dependem diretamente da interface.

A geração matemática, a dificuldade arcade e a pontuação têm responsabilidades distintas. Aumentar o nível arcade altera pressão e spawn; a progressão dos limites matemáticos depende do perfil e do tempo.

## Persistência

O ranking usa scoreboard.json em Application.persistentDataPath. O salvamento utiliza arquivo temporário, substituição e backup; dados inválidos não são sobrescritos silenciosamente. A confirmação repetida é bloqueada, e o ranking destaca o registro pelo identificador salvo.

Metadados opcionais incluem série, seed, versões das regras, tempo ativo e assinatura da configuração efetiva. São dados de rastreabilidade, sem garantia de replay completo.

## Limites desta seleção

Não estão incluídos cenas, prefabs, assets de configuração, GUIDs/.meta, pacotes, recursos gráficos ou áudios. Referências serializadas e dependências Unity/TextMesh Pro precisam do projeto completo para funcionar. Nenhuma API ou referência foi renomeada para compor o portfólio.
