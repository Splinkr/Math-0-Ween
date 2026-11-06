# Math-0-Ween

**Matemática, estratégia e sobrevivência em um arcade com temática de Halloween.**

Math-0-Ween é um jogo educativo desenvolvido em **Unity 6000.0.41f1 e C#**, voltado a estudantes do **2º ao 5º ano do Ensino Fundamental**. O jogador controla as respostas de um mago: resolve as operações apresentadas pelos inimigos que descem pela tela e lança morcegos para interceptá-los.

O objetivo educativo é oferecer prática de cálculo mental com operações adequadas à série escolhida, enquanto o jogador administra a pressão da partida e suas habilidades. Essa proposta não representa uma avaliação comprovada de eficácia pedagógica.

> Este repositório reúne documentação técnica e uma seleção de código. A distribuição Windows e as imagens de divulgação aguardam a conclusão das verificações de licenciamento e execução.

## Como jogar

Escolha a série no menu, digite o resultado de uma operação e confirme a resposta. Quando vários inimigos têm o mesmo resultado, o sistema prioriza o alvo elegível mais baixo na tela. A confirmação lança um projétil; o impacto válido concede os pontos. Operações que alcançam a base podem causar perda de vida. Ao final da partida, o jogador pode registrar o resultado no ranking local.

| Série | Operações do perfil inicial |
| --- | --- |
| 2º ano | Adição |
| 3º ano | Adição e subtração |
| 4º ano | Adição, subtração e multiplicação |
| 5º ano | Adição, subtração, multiplicação e divisão inteira exata |

Os perfis atuais não geram resultados negativos nem fracionários. Os limites numéricos aumentam ao longo da partida. Veja os [perfis matemáticos e o RNG](docs/MATEMATICA_E_RNG.md).

## Controles

| Entrada | Ação |
| --- | --- |
| Dígitos do teclado | Compor a resposta |
| Enter ou Espaço | Confirmar |
| Backspace | Apagar o último dígito |
| Esc ou P | Pausar ou iniciar a retomada |
| X | Ativar Freeze, quando disponível |
| C | Inverter a gravidade, quando disponível |
| Z | Ativar o ataque coletivo com a barra carregada |
| Mouse | Interagir com os botões e campos da interface |

As teclas de gameplay estão implementadas em [OperationEnemySpawner.cs](source/Assets/Script/OperationEnemySpawner.cs). A navegação utiliza a interface do jogo.

## Habilidades e inimigos especiais

Freeze interrompe temporariamente o movimento dos inimigos; a inversão de gravidade os faz subir. Ambas têm recarga. O ataque coletivo usa a barra de morcegos, elimina alvos elegíveis e não concede pontos ou recompensas individuais. A Adrenalina aplica uma redução temporária da velocidade de queda após uma perda de vida não fatal.

| Inimigo especial | Efeito do acerto |
| --- | --- |
| Vermelho escuro | Cura do mago |
| Dourado | Multiplicador de pontuação de 4× |
| Azul | Freeze breve |
| Prateado | Carga de 10 unidades para o ataque coletivo |

Na configuração atual, a partida começa com sete vidas. A dificuldade aumenta com o tempo: os inimigos ficam mais rápidos, os intervalos diminuem e a capacidade ativa cresce de quatro a oito. A pontuação combina sequência de acertos, pressão arcade, complexidade da expressão e o bônus dourado. Não há pontuação passiva. Veja as [regras de gameplay e pontuação](docs/GAMEPLAY_E_PONTUACAO.md).

## Destaques técnicos

- **Arquitetura modular:** coordenação em componentes Unity e regras delegadas a módulos de matemática, spawn, habilidades e pontuação.
- **Matemática por série:** perfis configuráveis, pesos de operações, limites e progressão numérica.
- **RNG determinístico:** PCG32 com fluxos independentes para matemática e gameplay.
- **Controle de spawn:** capacidade progressiva, agendamento e verificação de espaço visual no nascimento.
- **Crédito por impacto:** valores calculados no acerto e confirmados uma única vez no contato válido.
- **Persistência local:** gravação do ranking com proteção contra confirmação repetida e metadados opcionais da partida.

A [visão de arquitetura](docs/ARQUITETURA.md) apresenta o fluxo entre esses módulos.

## Código e documentação

| Conteúdo | Referência |
| --- | --- |
| Organização dos módulos | [Arquitetura](docs/ARQUITETURA.md) |
| Perfis e aleatoriedade | [Matemática e RNG](docs/MATEMATICA_E_RNG.md) |
| Ritmo, poderes e recompensas | [Gameplay e pontuação](docs/GAMEPLAY_E_PONTUACAO.md) |
| Compilação Windows e execução | [Execução Windows](docs/EXECUCAO_WINDOWS.md) |
| Os 33 scripts selecionados | [Índice do código](docs/INDICE_FONTE.md) |
| Evidências e pendências | [Validação e distribuição](docs/VALIDACAO_E_DISTRIBUICAO.md) |
| Capturas a disponibilizar | [Screenshots](screenshots/README.md) |

A pasta source/ preserva os arquivos selecionados e seus identificadores internos. **Ela não constitui um projeto Unity compilável.** Cenas, prefabs, configurações serializadas, pacotes e recursos artísticos não acompanham os scripts.

## Versão Windows

Ainda não há download público aprovado. Quando a distribuição estiver disponível, será necessário extrair o pacote completo e manter o executável junto de todas as suas dependências. As [instruções de execução](docs/EXECUCAO_WINDOWS.md) descrevem o procedimento.

As screenshots permanecem pendentes; nenhuma imagem ilustrativa foi usada como captura do jogo.

## Autoria e recursos de terceiros

Desenvolvimento: **Splinkr**. O jogo utiliza recursos de terceiros; a adaptação do PCG32 preserva a atribuição a Melissa O'Neill. Consulte os [créditos e avisos](THIRD_PARTY_NOTICES.md).

O projeto Unity completo permanece privado por questões de redistribuição de assets. Não há uma licença open-source geral para este repositório; as permissões específicas dos componentes de terceiros continuam limitadas aos respectivos arquivos.
