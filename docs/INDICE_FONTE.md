# Índice do código selecionado

Os 33 scripts foram copiados sem alterações, mantendo os caminhos relativos em source/Assets/Script. A seleção inclui a adaptação PCG32 com sua licença específica. Não inclui scripts de Editor, verificadores de testes ou código de pacotes de assets.

A pasta source/ não é um projeto Unity compilável. Cenas, prefabs, configurações serializadas, recursos, pacotes e arquivos .meta não acompanham a seleção.

| Arquivo | Responsabilidade |
| --- | --- |
| [Configuration/GameBalanceConfig.cs](../source/Assets/Script/Configuration/GameBalanceConfig.cs) | Definição dos grupos de balanceamento. |
| [Configuration/GameSettings.cs](../source/Assets/Script/Configuration/GameSettings.cs) | Seleção, seed opcional e resultado entre cenas. |
| [Configuration/GradeMathProfile.cs](../source/Assets/Script/Configuration/GradeMathProfile.cs) | Definição dos perfis por série. |
| [Enemies/EnemyRegistry.cs](../source/Assets/Script/Enemies/EnemyRegistry.cs) | Identidade, seleção, capacidade e movimento dos inimigos. |
| [Enemies/EnemyTextPresentation.cs](../source/Assets/Script/Enemies/EnemyTextPresentation.cs) | Textos, materiais e espaço visual na criação. |
| [Gameplay/BatProjectile.cs](../source/Assets/Script/Gameplay/BatProjectile.cs) | Movimento, contato, explosão e resolução do projétil. |
| [Gameplay/DifficultyProgression.cs](../source/Assets/Script/Gameplay/DifficultyProgression.cs) | Integração da curva de progressão arcade. |
| [Gameplay/GameplayInitializer.cs](../source/Assets/Script/Gameplay/GameplayInitializer.cs) | Inicialização da série selecionada. |
| [Gameplay/SpawnDirector.cs](../source/Assets/Script/Gameplay/SpawnDirector.cs) | Admissão e agendamento de ondas e tentativas. |
| [Gameplay/SpecialAbilityState.cs](../source/Assets/Script/Gameplay/SpecialAbilityState.cs) | Estado de Freeze, gravidade, ataque coletivo e Adrenalina. |
| [Gameplay/SpecialEnemyEffects.cs](../source/Assets/Script/Gameplay/SpecialEnemyEffects.cs) | Recompensas e efeitos dos inimigos especiais. |
| [Mathematics/ClassicMathRules.cs](../source/Assets/Script/Mathematics/ClassicMathRules.cs) | Perfis clássicos usados como fallback. |
| [Mathematics/MathAnswerEvaluator.cs](../source/Assets/Script/Mathematics/MathAnswerEvaluator.cs) | Avaliação de respostas e seleção de candidatos. |
| [Mathematics/MathGeneration.cs](../source/Assets/Script/Mathematics/MathGeneration.cs) | Pools, amostragem e progressão numérica. |
| [MathOperationGenerator.cs](../source/Assets/Script/MathOperationGenerator.cs) | Fachada da geração matemática e captura do perfil. |
| [MusicPlayer.cs](../source/Assets/Script/MusicPlayer.cs) | Música compartilhada entre cenas. |
| [OperationEnemySpawner.cs](../source/Assets/Script/OperationEnemySpawner.cs) | Coordenação da partida, entrada, spawn, alvos e poderes. |
| [Persistence/MatchMetadata.cs](../source/Assets/Script/Persistence/MatchMetadata.cs) | Metadados, resultado e assinatura de configuração. |
| [Persistence/PlayerDataSaver.cs](../source/Assets/Script/Persistence/PlayerDataSaver.cs) | Confirmação do nome e salvamento protegido do ranking. |
| [Persistence/SavePlayer.cs](../source/Assets/Script/Persistence/SavePlayer.cs) | Modelos de registro e coleção do ranking. |
| [Player/MageController.cs](../source/Assets/Script/Player/MageController.cs) | Vidas, dano e animações do mago. |
| [Player/OperationReceiver.cs](../source/Assets/Script/Player/OperationReceiver.cs) | Recepção de operações e dano após proteção. |
| [Player/PumpkinReceiver.cs](../source/Assets/Script/Player/PumpkinReceiver.cs) | Colisão na base e perda de vida. |
| [Randomness/MatchRandomSession.cs](../source/Assets/Script/Randomness/MatchRandomSession.cs) | Seed e dois fluxos aleatórios independentes. |
| [Randomness/Pcg32.cs](../source/Assets/Script/Randomness/Pcg32.cs) | Adaptação PCG32 XSH-RR; componente Apache-2.0. |
| [Scoring/PointsManager.cs](../source/Assets/Script/Scoring/PointsManager.cs) | Aceitação de créditos, total e indicador agrupado. |
| [Scoring/ScoreRules.cs](../source/Assets/Script/Scoring/ScoreRules.cs) | Fórmula pura de pontuação e regras de streak. |
| [UI/AdrenalineVignette.cs](../source/Assets/Script/UI/AdrenalineVignette.cs) | Apresentação da vinheta de Adrenalina. |
| [UI/SceneSwitcher.cs](../source/Assets/Script/UI/SceneSwitcher.cs) | Seleção da série e navegação entre cenas. |
| [UI/ScoreboardDisplay.cs](../source/Assets/Script/UI/ScoreboardDisplay.cs) | Leitura, ordenação e destaque do ranking. |
| [UI/ScoreEntryController.cs](../source/Assets/Script/UI/ScoreEntryController.cs) | Preenchimento e destaque de uma linha do ranking. |
| [UI/ScrollItemScroller.cs](../source/Assets/Script/UI/ScrollItemScroller.cs) | Rolagem da interface. |
| [UI/Tutorial.cs](../source/Assets/Script/UI/Tutorial.cs) | Exemplos e fluxo de pausa do tutorial. |

A licença do PCG32 está em [LICENSE-PCG.txt](../source/Assets/Script/Randomness/LICENSE-PCG.txt). Referências internas preservadas não autorizam redistribuir os assets do jogo.
